using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        string defaultClubNum = "947";
        string defaultClubName = "Ranelagh Harriers";
        string downloadFolder = "~/Downloads";
        string outputPattern = "parkrun_consolidated_{0}_{1}.csv";
        string singleOutputFilename = "parkrun_consolidated_club_results.csv";
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
            string totalPart = !string.IsNullOrEmpty(meta.TotalParticipants) ? meta.TotalParticipants : records.Count.ToString();

            Console.WriteLine($"Club Name:                       {effectiveClubName}");
            Console.WriteLine($"Event Date:                      {effectiveDate}");
            Console.WriteLine($"Total Club Members Registered:   {(string.IsNullOrEmpty(meta.TotalMembers) ? "N/A" : meta.TotalMembers)}");
            Console.WriteLine($"Total Club Runners on Date:      {totalPart}");
            Console.WriteLine($"Distinct Events Attended:        {records.Select(r => r.EventName).Distinct().Count():N0}");
            Console.WriteLine($"Total Runner Records Parsed:     {records.Count:N0}\n");

            string destinationCsv;
            if (!string.IsNullOrEmpty(customOutput))
            {
                destinationCsv = customOutput;
            }
            else
            {
                string resolvedDownloadDir = ParkrunScraperService.ResolvePath(downloadFolder);
                if (singleFileFlag)
                {
                    destinationCsv = Path.Combine(resolvedDownloadDir, singleOutputFilename);
                }
                else
                {
                    string slug = Regex.Replace(effectiveClubName, @"[^a-zA-Z0-9_-]", "_").Trim('_');
                    if (string.IsNullOrEmpty(slug)) slug = "Club";
                    string fileName = string.Format(outputPattern, slug, effectiveDate);
                    destinationCsv = Path.Combine(resolvedDownloadDir, fileName);
                }
            }

            ParkrunScraperService.SaveToCsv(records, destinationCsv);

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
