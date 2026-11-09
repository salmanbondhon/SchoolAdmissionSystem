
using SchoolAdmission.API.Enums;

namespace SchoolAdmission.API.Models;

public class AdmissionHold
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Keys
    public Guid AdmissionClassId { get; set; }
    public Guid StudentId { get; set; }

    // Application status
    public HoldStatus Status { get; set; } = HoldStatus.Active;

    // Application timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    // Navigation properties
    public AdmissionClass AdmissionClass { get; set; } = null!;

    public Student Student { get; set; } = null!;

    public Payment? Payment { get; set; }
}
