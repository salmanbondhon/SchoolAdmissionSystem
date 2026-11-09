using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Data;
using SchoolAdmission.API.Models;

namespace SchoolAdmission.API.Services;

public class StudentIdentificationService
{
    private readonly ApplicationDbContext _context;

    public StudentIdentificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Student> GetOrCreateStudentAsync(
        string registrationNumber)
    {
        if (!Regex.IsMatch(registrationNumber, @"^[0-9]{17}$"))
        {
            throw new ArgumentException(
                "Student registration number must contain exactly 17 digits.");
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.RegistrationNumber == registrationNumber);

        if (student != null)
        {
            return student;
        }

        student = new Student
        {
            RegistrationNumber = registrationNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.Students.Add(student);

        await _context.SaveChangesAsync();

        return student;
    }
}