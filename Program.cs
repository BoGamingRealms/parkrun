using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ParkrunScraper.Models;
using ParkrunScraper.Services;

namespace ParkrunScraper;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=========================================================================================");
        Console.WriteLine("                PARKRUN CONSOLIDATED CLUB RESULTS SCRAPER (C# .NET)                      ");
        Console.WriteLine("=========================================================================================");

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string configPath = Path.Combine(baseDir, "appsettings.json");
        if (!File.Exists(configPath))
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        string defaultClubNum = "21925";
        string defaultClubName = "Birmingham Swifts";
        string downloadFolder = "~/Downloads";
        string outputPattern = "parkrun_{0}_{1}.pdf";
        string singleOutputFilename = "parkrun_club_results.pdf";
        bool overwriteSingleFile = false;

        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("DefaultClubNum", out var cn)) defaultClubNum = cn.GetString() ?? defaultClubNum;
                if (root.TryGetProperty("DefaultClubName", out var cname)) defaultClubName = cname.GetString() ?? defaultClubName;
                if (root.TryGetProperty("DownloadFolder", out var df)) downloadFolder = df.GetString() ?? downloadFolder;
                if (root.TryGetProperty("OutputFilenamePattern", out var op)) outputPattern = op.GetString() ?? outputPattern;
                if (root.TryGetProperty("SingleOutputFilename", out var sof)) singleOutputFilename = sof.GetString() ?? singleOutputFilename;
                if (root.TryGetProperty("OverwriteSingleFile", out var osf)) overwriteSingleFile = osf.GetBoolean();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to read appsettings.json: {ex.Message}");
            }
        }

        string clubInput = defaultClubNum;
        string? eventDate = null;
        string? customOutput = null;
        bool singleFileFlag = overwriteSingleFile;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if ((arg.Equals("--club", StringComparison.OrdinalIgnoreCase) || arg.Equals("-c", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                clubInput = args[++i];
            }
            else if ((arg.Equals("--date", StringComparison.OrdinalIgnoreCase) || arg.Equals("-d", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                eventDate = args[++i];
            }
            else if ((arg.Equals("--output", StringComparison.OrdinalIgnoreCase) || arg.Equals("-o", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                customOutput = args[++i];
            }
            else if (arg.Equals("--single-file", StringComparison.OrdinalIgnoreCase))
            {
                singleFileFlag = true;
            }
            else if (!arg.StartsWith("-"))
            {
                clubInput = arg;
            }
        }

        var scraperService = new ParkrunScraperService();
        string requestUrl = scraperService.BuildUrl(clubInput, eventDate);

        Console.WriteLine($"Fetching consolidated club report from:\n  {requestUrl}\n");

        try
        {
            var (meta, records) = await scraperService.ScrapeConsolidatedClubAsync(clubInput, eventDate);

            string effectiveClubName = !string.IsNullOrEmpty(meta.ClubName) ? meta.ClubName : defaultClubName;
            string effectiveDate = !string.IsNullOrEmpty(meta.EventDate) ? meta.EventDate : (eventDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd"));
            int totalRunners = records.Count;
            int totalEvents = records.Select(r => r.EventName).Distinct().Count();
            int totalMembers = int.TryParse(meta.TotalMembers, out int tm) ? tm : 0;

            Console.WriteLine($"Club Name:                       {effectiveClubName}");
            Console.WriteLine($"Event Date:                      {effectiveDate}");
            Console.WriteLine($"Total Club Members Registered:   {(string.IsNullOrEmpty(meta.TotalMembers) ? "N/A" : meta.TotalMembers)}");
            Console.WriteLine($"Total Club Runners on Date:      {totalRunners}");
            Console.WriteLine($"Distinct Events Attended:        {totalEvents:N0}");
            Console.WriteLine($"Total Runner Records Parsed:     {records.Count:N0}\n");

            // Save Snapshot into History
            var historyService = new ParkrunHistoryService();
            historyService.SaveSnapshot(new WeeklyClubSnapshot
            {
                EventDate = effectiveDate,
                ClubName = effectiveClubName,
                TotalRunners = totalRunners,
                DistinctEvents = totalEvents,
                TotalMembersRegistered = totalMembers
            });

            // Get historical trends and generate chart
            var (trends, recentHistory) = historyService.GetTrends(effectiveClubName, effectiveDate);
            byte[]? trendChartBytes = null;
            if (recentHistory.Count >= 2)
            {
                trendChartBytes = ParkrunChartGenerator.GenerateWeeklyTrendChart(recentHistory);
            }

            string destinationPdf;
            if (!string.IsNullOrEmpty(customOutput))
            {
                destinationPdf = customOutput;
            }
            else
            {
                string resolvedDownloadDir = ParkrunScraperService.ResolvePath(downloadFolder);
                if (singleFileFlag)
                {
                    destinationPdf = Path.Combine(resolvedDownloadDir, singleOutputFilename);
                }
                else
                {
                    string slug = Regex.Replace(effectiveClubName, @"[^a-zA-Z0-9_-]", "_").Trim('_');
                    if (string.IsNullOrEmpty(slug)) slug = "Club";
                    string fileName = string.Format(outputPattern, slug, effectiveDate);
                    destinationPdf = Path.Combine(resolvedDownloadDir, fileName);
                }
            }

            // Exclusively generate PDF Report with trend charts at the end
            ParkrunPdfGenerator.GeneratePdf(meta, records, destinationPdf, trendChartBytes);

            if (records.Count > 0)
            {
                Console.WriteLine("\nPreview of extracted records (first 5):");
                Console.WriteLine(new string('-', 95));
                Console.WriteLine($"{"Event Name",-30} | {"Pos",-5} | {"Parkrunner",-26} | {"Time",-8}");
                Console.WriteLine(new string('-', 95));
                foreach (var r in records.Take(5))
                {
                    Console.WriteLine($"{r.EventName,-30} | {r.OverallPosition,-5} | {r.Parkrunner,-26} | {r.Time,-8}");
                }
                Console.WriteLine(new string('-', 95));
            }

            Console.WriteLine("\nExtraction completed successfully!");
            Console.WriteLine("=========================================================================================");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error] Scraper failed: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}
