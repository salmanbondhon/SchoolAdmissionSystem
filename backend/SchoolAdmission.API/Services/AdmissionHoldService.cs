using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Data;
using SchoolAdmission.API.DTOs.Holds;
using SchoolAdmission.API.Enums;
using SchoolAdmission.API.Models;

namespace SchoolAdmission.API.Services;

public class AdmissionHoldService
{
    private readonly ApplicationDbContext _context;

    private readonly IConfiguration _configuration;

    public AdmissionHoldService(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<HoldResponse> CreateHoldAsync(
    Guid admissionClassId,
    Guid studentId)
    {
        // Start a database transaction.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;

        // Lock the selected admission class row.
        // Other requests for this class must wait.
        var admissionClass = await _context.AdmissionClasses
            .FromSqlInterpolated($"""
            SELECT * FROM "AdmissionClasses"
            WHERE "Id" = {admissionClassId}
            FOR UPDATE
            """)
            .FirstOrDefaultAsync();

        if (admissionClass == null)
        {
            throw new KeyNotFoundException(
                "Admission class not found.");
        }

        // Find expired active holds for this class.
        var expiredHolds = await _context.AdmissionHolds
            .Where(h =>
                h.AdmissionClassId == admissionClassId &&
                h.Status == HoldStatus.Active &&
                h.ExpiresAt <= now)
            .ToListAsync();

        // Update their status.
        foreach (var hold in expiredHolds)
        {
            hold.Status = HoldStatus.Expired;
        }

        // Check whether the student already has an
        // active or confirmed application in this class.
        var existingApplication = await _context.AdmissionHolds
            .AnyAsync(h =>
                h.AdmissionClassId == admissionClassId &&
                h.StudentId == studentId &&
                (h.Status == HoldStatus.Confirmed ||
                 (h.Status == HoldStatus.Active &&
                  h.ExpiresAt > now)));

        if (existingApplication)
        {
            throw new InvalidOperationException(
                "You already have an active or confirmed application for this class.");
        }

        // Count confirmed seats.
        var confirmedSeats = await _context.AdmissionHolds
            .CountAsync(h =>
                h.AdmissionClassId == admissionClassId &&
                h.Status == HoldStatus.Confirmed);

        // Count active, unexpired holds.
        var heldSeats = await _context.AdmissionHolds
            .CountAsync(h =>
                h.AdmissionClassId == admissionClassId &&
                h.Status == HoldStatus.Active &&
                h.ExpiresAt > now);

        // Calculate available seats.
        var availableSeats =
            admissionClass.TotalSeats - confirmedSeats - heldSeats;

        if (availableSeats <= 0)
        {
            throw new InvalidOperationException(
                "Sorry, no seats are currently available in this class.");
        }

        // Read configurable hold duration.
        // Default duration is 600 seconds (10 minutes).
        var holdDuration = 600;

        var configuredDuration =
            _configuration["HOLD_DURATION_SECONDS"];

        if (!string.IsNullOrWhiteSpace(configuredDuration) &&
            int.TryParse(configuredDuration, out var parsedDuration) &&
            parsedDuration > 0)
        {
            holdDuration = parsedDuration;
        }

        // Create a new admission hold.
        var newHold = new AdmissionHold
        {
            AdmissionClassId = admissionClassId,
            StudentId = studentId,
            Status = HoldStatus.Active,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(holdDuration)
        };

        _context.AdmissionHolds.Add(newHold);

        await _context.SaveChangesAsync();

        // Commit the transaction.
        await transaction.CommitAsync();

        // Return the response.
        return new HoldResponse
        {
            Id = newHold.Id,
            AdmissionClassId = admissionClass.Id,
            ClassName = admissionClass.Name,
            Status = newHold.Status.ToString(),
            CreatedAt = newHold.CreatedAt,
            ExpiresAt = newHold.ExpiresAt,
            ConfirmedAt = newHold.ConfirmedAt,
            RemainingSeconds = holdDuration
        };
    }

    public async Task<HoldResponse?> GetHoldAsync(
        Guid holdId,
        Guid studentId)
    {
        var now = DateTime.UtcNow;

        var hold = await _context.AdmissionHolds
            .Include(h => h.AdmissionClass)
            .AsNoTracking()
            .FirstOrDefaultAsync(h =>
                h.Id == holdId &&
                h.StudentId == studentId);

        if (hold == null)
        {
            return null;
        }

        // Calculate remaining time.
        var remainingSeconds = hold.Status == HoldStatus.Active
            ? Math.Max(0, (int)Math.Ceiling(
                (hold.ExpiresAt - now).TotalSeconds))
            : 0;

        // Display expired status even if the background
        // expiration worker has not updated the database yet.
        var status = hold.Status == HoldStatus.Active &&
                     hold.ExpiresAt <= now
            ? HoldStatus.Expired.ToString()
            : hold.Status.ToString();

        return new HoldResponse
        {
            Id = hold.Id,
            AdmissionClassId = hold.AdmissionClassId,
            ClassName = hold.AdmissionClass.Name,
            Status = status,
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt,
            ConfirmedAt = hold.ConfirmedAt,
            RemainingSeconds = remainingSeconds
        };
    }

    public async Task<bool> CancelHoldAsync(
    Guid holdId,
    Guid studentId)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // Find the hold and lock its associated class.
        var hold = await _context.AdmissionHolds
            .FirstOrDefaultAsync(h =>
                h.Id == holdId &&
                h.StudentId == studentId);

        if (hold == null)
        {
            return false;
        }

        // Lock the admission class to synchronize
        // cancellation with reservation and confirmation.
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
        SELECT "Id" FROM "AdmissionClasses"
        WHERE "Id" = {hold.AdmissionClassId}
        FOR UPDATE
        """);

        // Reload the hold after acquiring the class lock.
        await _context.Entry(hold).ReloadAsync();

        if (hold.Status != HoldStatus.Active)
        {
            throw new InvalidOperationException(
                "Only active reservations can be cancelled.");
        }

        hold.Status = HoldStatus.Cancelled;
        hold.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return true;
    }
}