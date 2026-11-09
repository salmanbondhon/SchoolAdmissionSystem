using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.API.DTOs.Holds;
using SchoolAdmission.API.Services;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("classes/{classId:guid}/holds")]
public class HoldsController : ControllerBase
{
    private readonly AdmissionHoldService _holdService;
    private readonly StudentIdentificationService _studentService;

    public HoldsController(
        AdmissionHoldService holdService,
        StudentIdentificationService studentService)
    {
        _holdService = holdService;
        _studentService = studentService;
    }

    // POST: /classes/{classId}/holds
    [HttpPost]
    public async Task<ActionResult<HoldResponse>> CreateHold(
        Guid classId,
        [FromHeader(Name = "X-Student-Id")] string registrationNumber,
        [FromBody] CreateHoldRequest request)
    {
        // Make sure the class ID in the URL and body match.
        if (classId != request.AdmissionClassId)
        {
            return BadRequest(new
            {
                message = "Admission class ID in the URL and request body must match."
            });
        }

        try
        {
            // Identify or create the student.
            var student = await _studentService
                .GetOrCreateStudentAsync(registrationNumber);

            // Create the seat reservation.
            var hold = await _holdService.CreateHoldAsync(
                classId,
                student.Id);

            return CreatedAtAction(
                nameof(CreateHold),
                new { classId },
                hold);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // GET: /holds/{holdId}
    [HttpGet("/holds/{holdId:guid}")]
    public async Task<ActionResult<HoldResponse>> GetHold(
        Guid holdId,
        [FromHeader(Name = "X-Student-Id")] string registrationNumber)
    {
        try
        {
            // Identify the student.
            var student = await _studentService
                .GetOrCreateStudentAsync(registrationNumber);

            // Retrieve the student's hold.
            var hold = await _holdService.GetHoldAsync(
                holdId,
                student.Id);

            if (hold == null)
            {
                return NotFound(new
                {
                    message = "Admission hold not found."
                });
            }

            return Ok(hold);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    // DELETE: /holds/{holdId}
[HttpDelete("/holds/{holdId:guid}")]
public async Task<IActionResult> CancelHold(
    Guid holdId,
    [FromHeader(Name = "X-Student-Id")] string registrationNumber)
{
    try
    {
        // Identify the student.
        var student = await _studentService
            .GetOrCreateStudentAsync(registrationNumber);

        // Cancel the reservation.
        var cancelled = await _holdService.CancelHoldAsync(
            holdId,
            student.Id);

        if (!cancelled)
        {
            return NotFound(new
            {
                message = "Admission hold not found."
            });
        }

        return Ok(new
        {
            message = "Admission reservation cancelled successfully."
        });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message });
    }
}





// POST: /holds/{holdId}/confirm
[HttpPost("/holds/{holdId:guid}/confirm")]
public async Task<ActionResult<ConfirmHoldResponse>> ConfirmHold(
    Guid holdId,
    [FromHeader(Name = "X-Student-Id")] string registrationNumber)
{
    try
    {
        // Identify the student.
        var student = await _studentService
            .GetOrCreateStudentAsync(registrationNumber);

        // Confirm the reservation and process simulated payment.
        var result = await _holdService.ConfirmHoldAsync(
            holdId,
            student.Id);

        return Ok(result);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message });
    }
}



// GET: /me/holds
[HttpGet("/me/holds")]
public async Task<ActionResult<List<HoldResponse>>> GetMyHolds(
    [FromHeader(Name = "X-Student-Id")] string registrationNumber)
{
    try
    {
        // Identify the student.
        var student = await _studentService
            .GetOrCreateStudentAsync(registrationNumber);

        // Retrieve all applications belonging to this student.
        var holds = await _holdService.GetMyHoldsAsync(
            student.Id);

        return Ok(holds);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
}