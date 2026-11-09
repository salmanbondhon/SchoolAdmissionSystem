using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.API.DTOs.Holds;

public class CreateHoldRequest
{
    [Required]
    public Guid AdmissionClassId { get; set; }
}