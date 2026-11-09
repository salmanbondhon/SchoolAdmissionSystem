namespace SchoolAdmission.API.DTOs.Holds;

public class ConfirmHoldResponse
{
    public Guid HoldId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TransactionReference { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime ConfirmedAt { get; set; }

    public string Message { get; set; } = string.Empty;
}
