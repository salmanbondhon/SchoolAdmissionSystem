
using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.API.Models;

public class AdmissionClass
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TotalSeats { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public ICollection<AdmissionHold> Holds { get; set; }
        = new List<AdmissionHold>();
}
