using System;
using System.Collections.Generic;

namespace ParkrunScraper.Models;

public class ParkrunVolunteerProfile
{
    public string ParkrunnerId { get; set; } = "";
    public string ParkrunnerName { get; set; } = "";
    public int TotalCredits { get; set; }
    public string HighestMilestone { get; set; } = "-";
    public List<string> Milestones { get; set; } = new();
    public Dictionary<string, int> Roles { get; set; } = new();
    public string TopRolesSummary { get; set; } = "-";
    public string EventName { get; set; } = "";
    public string RoleThisWeek { get; set; } = "";
    public bool VolunteeredThisWeek { get; set; } = false;
    public DateTime LastFetched { get; set; } = DateTime.UtcNow;
    public int DeltaCredits { get; set; } = 0;
}

public class VolunteerStore
{
    public Dictionary<string, ParkrunVolunteerProfile> Profiles { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> Snapshots { get; set; } = new();
}
