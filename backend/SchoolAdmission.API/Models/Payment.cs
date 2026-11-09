using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.API.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // One payment belongs to one admission hold
    public Guid AdmissionHoldId { get; set; }

    // Payment information
    public decimal Amount { get; set; }

    [MaxLength(100)]
    public string TransactionReference { get; set; } = string.Empty;

    public bool IsSuccessful { get; set; } = false;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    // Navigation property
    public AdmissionHold AdmissionHold { get; set; } = null!;
}