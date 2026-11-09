using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.API.DTOs.Classes;

public class CreateClassRequest
{
    [Required(ErrorMessage = "Class name is required.")]
    [MaxLength(150, ErrorMessage = "Class name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Total seats are required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Total seats must be at least 1.")]
    public int TotalSeats { get; set; }
}