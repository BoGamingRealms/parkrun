namespace ParkrunScraper.Models;

public class WeeklyClubSnapshot
{
    public string EventDate { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public int TotalRunners { get; set; }
    public int DistinctEvents { get; set; }
    public int TotalMembersRegistered { get; set; }
}

public class TrendComparison
{
    public int CurrentRunners { get; set; }
    public int RunnersDelta { get; set; }
    public bool HasPreviousRunners { get; set; }

    public int CurrentEvents { get; set; }
    public int EventsDelta { get; set; }
    public bool HasPreviousEvents { get; set; }

    public int CurrentMembers { get; set; }
    public int MembersDelta { get; set; }
    public bool HasPreviousMembers { get; set; }
}
