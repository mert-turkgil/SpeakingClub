namespace SpeakingClub.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    /// <summary>404 for a missing page, 500 for an error, other codes as they come.</summary>
    public int StatusCode { get; set; } = 500;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
