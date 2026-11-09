using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.API.Data;
using SchoolAdmission.API.DTOs.Classes;
using SchoolAdmission.API.Models;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClassesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: /classes
    [HttpPost]
    public async Task<ActionResult<ClassResponse>> CreateClass(
        CreateClassRequest request)
    {
        var admissionClass = new AdmissionClass
        {
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdmissionClasses.Add(admissionClass);

        await _context.SaveChangesAsync();

        var response = new ClassResponse
        {
            Id = admissionClass.Id,
            Name = admissionClass.Name,
            TotalSeats = admissionClass.TotalSeats,
            ConfirmedSeats = 0,
            HeldSeats = 0,
            AvailableSeats = admissionClass.TotalSeats,
            CreatedAt = admissionClass.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetClass),
            new { classId = admissionClass.Id },
            response);
    }

    // GET: /classes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassResponse>>> GetClasses()
    {
        var classes = await _context.AdmissionClasses
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClassResponse
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                ConfirmedSeats = c.Holds.Count(
                    h => h.Status == Enums.HoldStatus.Confirmed),
                HeldSeats = c.Holds.Count(
                    h => h.Status == Enums.HoldStatus.Active &&
                         h.ExpiresAt > DateTime.UtcNow),
                AvailableSeats = c.TotalSeats -
                    c.Holds.Count(
                        h => h.Status == Enums.HoldStatus.Confirmed) -
                    c.Holds.Count(
                        h => h.Status == Enums.HoldStatus.Active &&
                             h.ExpiresAt > DateTime.UtcNow),
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(classes);
    }

    // GET: /classes/{classId}
    [HttpGet("{classId:guid}")]
    public async Task<ActionResult<ClassResponse>> GetClass(Guid classId)
    {
        var admissionClass = await _context.AdmissionClasses
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new ClassResponse
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                ConfirmedSeats = c.Holds.Count(
                    h => h.Status == Enums.HoldStatus.Confirmed),
                HeldSeats = c.Holds.Count(
                    h => h.Status == Enums.HoldStatus.Active &&
                         h.ExpiresAt > DateTime.UtcNow),
                AvailableSeats = c.TotalSeats -
                    c.Holds.Count(
                        h => h.Status == Enums.HoldStatus.Confirmed) -
                    c.Holds.Count(
                        h => h.Status == Enums.HoldStatus.Active &&
                             h.ExpiresAt > DateTime.UtcNow),
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (admissionClass == null)
        {
            return NotFound(new
            {
                message = "Admission class not found."
            });
        }

        return Ok(admissionClass);
    }
}