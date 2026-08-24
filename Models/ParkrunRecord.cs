namespace ParkrunScraper.Models;

public class ParkrunRecord
{
    public string EventDate { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventNumber { get; set; } = string.Empty;
    public string OverallPosition { get; set; } = string.Empty;
    public string GenderPosition { get; set; } = string.Empty;
    public string Parkrunner { get; set; } = string.Empty;
    public string ParkrunnerId { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string EventTotalParticipants { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
}

public class ConsolidatedReportMetadata
{
    public string ClubName { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public string TotalMembers { get; set; } = string.Empty;
    public string TotalParticipants { get; set; } = string.Empty;
}
