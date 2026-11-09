
using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.API.Models;

public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(17, MinimumLength = 17)]
    [RegularExpression(@"^[0-9]{17}$",
        ErrorMessage = "Registration number must contain exactly 17 digits.")]
    public string RegistrationNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<AdmissionHold> Holds { get; set; }
        = new List<AdmissionHold>();
}
