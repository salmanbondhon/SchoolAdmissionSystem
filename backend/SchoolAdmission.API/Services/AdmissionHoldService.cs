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

    var hold = await _context.AdmissionHolds
        .FirstOrDefaultAsync(h =>
            h.Id == holdId &&
            h.StudentId == studentId);

    if (hold == null)
    {
        return false;
    }

    // Lock the admission class row.
    var admissionClass = await _context.AdmissionClasses
        .FromSqlInterpolated($"""
            SELECT * FROM "AdmissionClasses"
            WHERE "Id" = {hold.AdmissionClassId}
            FOR UPDATE
            """)
        .FirstOrDefaultAsync();

    if (admissionClass == null)
    {
        throw new KeyNotFoundException(
            "Admission class not found.");
    }

    // Reload the hold after acquiring the lock.
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




    public async Task<ConfirmHoldResponse> ConfirmHoldAsync(
    Guid holdId,
    Guid studentId)
{
    await using var transaction =
        await _context.Database.BeginTransactionAsync();

    // Find the student's hold.
    var hold = await _context.AdmissionHolds
        .FirstOrDefaultAsync(h =>
            h.Id == holdId &&
            h.StudentId == studentId);

    if (hold == null)
    {
        throw new KeyNotFoundException(
            "Admission hold not found.");
    }

    // Lock the associated admission class.
    await _context.AdmissionClasses
        .FromSqlInterpolated($"""
            SELECT * FROM "AdmissionClasses"
            WHERE "Id" = {hold.AdmissionClassId}
            FOR UPDATE
            """)
        .FirstOrDefaultAsync();

    // Reload the hold after acquiring the class lock.
    await _context.Entry(hold).ReloadAsync();

    // Check whether the hold is already confirmed.
    if (hold.Status == HoldStatus.Confirmed)
    {
        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p =>
                p.AdmissionHoldId == hold.Id);

        if (existingPayment == null)
        {
            throw new InvalidOperationException(
                "Payment record not found for this confirmation.");
        }

        await transaction.CommitAsync();

        // Return the existing confirmation.
        return new ConfirmHoldResponse
        {
            HoldId = hold.Id,
            Status = hold.Status.ToString(),
            TransactionReference =
                existingPayment.TransactionReference,
            Amount = existingPayment.Amount,
            ConfirmedAt = hold.ConfirmedAt!.Value,
            Message = "Admission application was already confirmed."
        };
    }

    // Only active holds can be confirmed.
    if (hold.Status != HoldStatus.Active)
    {
        throw new InvalidOperationException(
            "This reservation cannot be confirmed.");
    }

    // Check expiration.
    var now = DateTime.UtcNow;

    if (hold.ExpiresAt <= now)
    {
        hold.Status = HoldStatus.Expired;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        throw new InvalidOperationException(
            "Your reservation has expired. Please create a new reservation.");
    }

    // Read simulated payment amount.
    // Default amount: 1000.
    var amount = 1000m;

    var configuredAmount = _configuration["PAYMENT_AMOUNT"];

    if (!string.IsNullOrWhiteSpace(configuredAmount) &&
        decimal.TryParse(
            configuredAmount,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedAmount) &&
        parsedAmount >= 0)
    {
        amount = parsedAmount;
    }

    // Generate a simulated transaction reference.
    var transactionReference =
        $"TXN-{Guid.NewGuid():N}".ToUpperInvariant();

    // Create payment record.
    var payment = new Payment
    {
        AdmissionHoldId = hold.Id,
        Amount = amount,
        TransactionReference = transactionReference,
        IsSuccessful = true,
        CreatedAt = now,
        PaidAt = now
    };

    _context.Payments.Add(payment);

    // Confirm the admission hold.
    hold.Status = HoldStatus.Confirmed;
    hold.ConfirmedAt = now;

    await _context.SaveChangesAsync();

    await transaction.CommitAsync();

    return new ConfirmHoldResponse
    {
        HoldId = hold.Id,
        Status = hold.Status.ToString(),
        TransactionReference = transactionReference,
        Amount = amount,
        ConfirmedAt = now,
        Message = "Admission application confirmed successfully."
    };
}



public async Task<List<HoldResponse>> GetMyHoldsAsync(
    Guid studentId)
{
    var now = DateTime.UtcNow;

    var holds = await _context.AdmissionHolds
        .Include(h => h.AdmissionClass)
        .AsNoTracking()
        .Where(h => h.StudentId == studentId)
        .OrderByDescending(h => h.CreatedAt)
        .ToListAsync();

    return holds.Select(hold =>
    {
        var remainingSeconds =
            hold.Status == HoldStatus.Active
                ? Math.Max(0, (int)Math.Ceiling(
                    (hold.ExpiresAt - now).TotalSeconds))
                : 0;

        var status =
            hold.Status == HoldStatus.Active &&
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
    }).ToList();
}
}