using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ParkrunScraper.Models;

namespace ParkrunScraper.Services;

public class ParkrunHistoryService
{
    private readonly string _historyFilePath;

    public ParkrunHistoryService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _historyFilePath = ParkrunScraperService.ResolvePath(customPath);
        }
        else
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _historyFilePath = Path.Combine(baseDir, "data", "history.json");
        }
    }

    public List<WeeklyClubSnapshot> LoadHistory()
    {
        if (!File.Exists(_historyFilePath))
        {
            // Check fallback in current working dir
            string localFallback = Path.Combine(Directory.GetCurrentDirectory(), "data", "history.json");
            if (File.Exists(localFallback))
            {
                return ReadFromFile(localFallback);
            }
            return new List<WeeklyClubSnapshot>();
        }

        return ReadFromFile(_historyFilePath);
    }

    private static List<WeeklyClubSnapshot> ReadFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<WeeklyClubSnapshot>>(json);
            return list ?? new List<WeeklyClubSnapshot>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Failed to read history file: {ex.Message}");
            return new List<WeeklyClubSnapshot>();
        }
    }

    public void SaveSnapshot(WeeklyClubSnapshot snapshot)
    {
        var history = LoadHistory();

        // Update existing or add new
        int existingIdx = history.FindIndex(h =>
            h.EventDate.Equals(snapshot.EventDate, StringComparison.OrdinalIgnoreCase) &&
            h.ClubName.Equals(snapshot.ClubName, StringComparison.OrdinalIgnoreCase));

        if (existingIdx >= 0)
        {
            history[existingIdx] = snapshot;
        }
        else
        {
            history.Add(snapshot);
        }

        // Sort chronologically by date
        history = history.OrderBy(h => h.EventDate).ToList();

        string? dir = Path.GetDirectoryName(_historyFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_historyFilePath, json);
    }

    public (TrendComparison Comparison, List<WeeklyClubSnapshot> RecentHistory) GetTrends(string clubName, string currentDate)
    {
        var allHistory = LoadHistory()
            .Where(h => h.ClubName.Equals(clubName, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(clubName))
            .OrderBy(h => h.EventDate)
            .ToList();

        var comparison = new TrendComparison();

        var current = allHistory.FirstOrDefault(h => h.EventDate.Equals(currentDate, StringComparison.OrdinalIgnoreCase));
        if (current == null && allHistory.Count > 0)
        {
            current = allHistory.Last();
        }

        if (current != null)
        {
            comparison.CurrentRunners = current.TotalRunners;
            comparison.CurrentEvents = current.DistinctEvents;
            comparison.CurrentMembers = current.TotalMembersRegistered;

            // Find previous historical snapshot prior to this date
            var previous = allHistory
                .Where(h => string.Compare(h.EventDate, current.EventDate, StringComparison.OrdinalIgnoreCase) < 0)
                .OrderBy(h => h.EventDate)
                .LastOrDefault();

            if (previous != null)
            {
                comparison.HasPreviousRunners = true;
                comparison.RunnersDelta = current.TotalRunners - previous.TotalRunners;

                comparison.HasPreviousEvents = true;
                comparison.EventsDelta = current.DistinctEvents - previous.DistinctEvents;

                if (current.TotalMembersRegistered > 0 && previous.TotalMembersRegistered > 0)
                {
                    comparison.HasPreviousMembers = true;
                    comparison.MembersDelta = current.TotalMembersRegistered - previous.TotalMembersRegistered;
                }
            }
        }

        // Return up to the last 15 weekly snapshots leading up to current date
        var recent = allHistory
            .Where(h => string.Compare(h.EventDate, currentDate, StringComparison.OrdinalIgnoreCase) <= 0)
            .TakeLast(15)
            .ToList();

        return (comparison, recent);
    }
}
