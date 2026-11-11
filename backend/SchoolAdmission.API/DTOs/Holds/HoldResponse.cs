
namespace SchoolAdmission.API.DTOs.Holds;

public class HoldResponse
{
    public Guid Id { get; set; }

    public Guid AdmissionClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public int RemainingSeconds { get; set; }

    // Payment information
    public decimal? PaymentAmount { get; set; }

    public string? TransactionReference { get; set; }

    public bool? PaymentSuccessful { get; set; }

    public DateTime? PaidAt { get; set; }
}
