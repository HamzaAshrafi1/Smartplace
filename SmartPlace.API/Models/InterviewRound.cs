namespace SmartPlace.API.Models;

public class InterviewRound
{
    public int InterviewRoundId { get; set; }

    public string RoundName { get; set; } = string.Empty;

    public DateTime ScheduledDate { get; set; }

    public string Mode { get; set; } = "Online";

    public string? LocationOrLink { get; set; }

    public string Status { get; set; } = "Scheduled";

    public string? Result { get; set; }

    public string? Remarks { get; set; }

    // Foreign Key
    public int ApplicationId { get; set; }

    // Navigation Property
    public Application? Application { get; set; }
}