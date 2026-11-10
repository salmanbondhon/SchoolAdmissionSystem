
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Enums;
using SchoolAdmission.API.Models;
using SchoolAdmission.API.Services;

namespace SchoolAdmission.Tests;

public class AdmissionHoldTests
{
    [Fact]
    public async Task CreateHold_SameStudentAndClass_RejectsDuplicate()
    {
        // Arrange: create a fresh test database.
        await using var fixture = new PostgresTestFixture();
        await fixture.InitializeAsync();

        await using var context = fixture.CreateDbContext();

        var admissionClass = new AdmissionClass
        {
            Name = "Test Class",
            TotalSeats = 5,
            CreatedAt = DateTime.UtcNow
        };

        var student = new Student
        {
            RegistrationNumber = "12345678901234567"
        };

        context.AdmissionClasses.Add(admissionClass);
        context.Students.Add(student);

        await context.SaveChangesAsync();

        var service = new AdmissionHoldService(
            context,
            fixture.Configuration);

        // Act: create the first hold.
        var firstHold = await service.CreateHoldAsync(
            admissionClass.Id,
            student.Id);

        // Assert: the first hold should be active.
        Assert.Equal("Active", firstHold.Status);

        // Act and Assert: the second hold must be rejected.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateHoldAsync(
                admissionClass.Id,
                student.Id));

        Assert.Contains(
            "already have an active or confirmed application",
            exception.Message);

        // Verify only one hold exists in the database.
        var holdCount = await context.AdmissionHolds.CountAsync();

        Assert.Equal(1, holdCount);
    }



[Fact]
public async Task CreateHold_ExpiredHold_ReleasesSeat()
{
    // Arrange: create a fresh test database.
    await using var fixture = new PostgresTestFixture();
    await fixture.InitializeAsync();

    await using var context = fixture.CreateDbContext();

    var admissionClass = new AdmissionClass
    {
        Name = "Expiration Test Class",
        TotalSeats = 1,
        CreatedAt = DateTime.UtcNow
    };

    var firstStudent = new Student
    {
        RegistrationNumber = "12345678901234568"
    };

    var secondStudent = new Student
    {
        RegistrationNumber = "12345678901234569"
    };

    context.AdmissionClasses.Add(admissionClass);
    context.Students.AddRange(firstStudent, secondStudent);

    await context.SaveChangesAsync();

    // Create an already-expired hold.
    var expiredHold = new AdmissionHold
    {
        AdmissionClassId = admissionClass.Id,
        StudentId = firstStudent.Id,
        Status = HoldStatus.Active,
        CreatedAt = DateTime.UtcNow.AddMinutes(-20),
        ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
    };

    context.AdmissionHolds.Add(expiredHold);
    await context.SaveChangesAsync();

    var service = new AdmissionHoldService(
        context,
        fixture.Configuration);

    // Act: the second student tries to reserve the seat.
    var newHold = await service.CreateHoldAsync(
        admissionClass.Id,
        secondStudent.Id);

    // Assert: the second student gets an active hold.
    Assert.Equal("Active", newHold.Status);

    // Verify that the previous hold was marked expired.
    var previousHold = await context.AdmissionHolds
        .AsNoTracking()
        .FirstAsync(h => h.Id == expiredHold.Id);

    Assert.Equal(HoldStatus.Expired, previousHold.Status);

    // Verify only one active, unexpired hold remains.
    var activeHolds = await context.AdmissionHolds
        .CountAsync(h =>
            h.AdmissionClassId == admissionClass.Id &&
            h.Status == HoldStatus.Active &&
            h.ExpiresAt > DateTime.UtcNow);

    Assert.Equal(1, activeHolds);
}




[Fact]
public async Task CreateHold_ConcurrentRequests_NeverExceedsClassCapacity()
{
    await using var fixture = new PostgresTestFixture();
    await fixture.InitializeAsync();

    var classId = Guid.NewGuid();

    var students = Enumerable.Range(1, 10)
        .Select(i => new Student
        {
            RegistrationNumber = (20000000000000000L + i).ToString()
        })
        .ToList();

    // Create a class with only 3 seats.
    await using (var seedContext = fixture.CreateDbContext())
    {
        seedContext.AdmissionClasses.Add(new AdmissionClass
        {
            Id = classId,
            Name = "Concurrency Test Class",
            TotalSeats = 3,
            CreatedAt = DateTime.UtcNow
        });

        seedContext.Students.AddRange(students);

        await seedContext.SaveChangesAsync();
    }

    // Each request gets its own database context.
    var tasks = students.Select(async student =>
    {
        await using var requestContext = fixture.CreateDbContext();

        var service = new AdmissionHoldService(
            requestContext,
            fixture.Configuration);

        try
        {
            await service.CreateHoldAsync(classId, student.Id);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    });

    // Execute all 10 requests concurrently.
    var results = await Task.WhenAll(tasks);

    var successfulRequests = results.Count(success => success);

    // Exactly 3 students should receive reservations.
    Assert.Equal(3, successfulRequests);

    // Verify the actual database state.
    await using var verificationContext = fixture.CreateDbContext();

    var occupiedSeats = await verificationContext.AdmissionHolds
        .CountAsync(h =>
            h.AdmissionClassId == classId &&
            (
                h.Status == HoldStatus.Confirmed ||
                (h.Status == HoldStatus.Active &&
                 h.ExpiresAt > DateTime.UtcNow)
            ));

    Assert.Equal(3, occupiedSeats);
}

}
