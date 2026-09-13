using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ParkrunScraper.Models;

namespace ParkrunScraper.Services;

public class ParkrunPdfGenerator
{
    static ParkrunPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void GeneratePdf(
        ConsolidatedReportMetadata meta,
        List<ParkrunRecord> records,
        string outputPath,
        byte[]? trendChartBytes = null,
        List<ParkrunVolunteerProfile>? volunteerProfiles = null)
    {
        string fullPath = ParkrunScraperService.ResolvePath(outputPath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string clubName = !string.IsNullOrEmpty(meta.ClubName) ? meta.ClubName : "Parkrun Club";
        string eventDate = !string.IsNullOrEmpty(meta.EventDate) ? meta.EventDate : DateTime.UtcNow.ToString("yyyy-MM-dd");
        int totalRunners = records.Count;
        int totalEvents = records.Select(r => r.EventName).Distinct().Count();

        // Check for club logo image
        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "birmingham_swifts_logo.jpg");
        if (!File.Exists(logoPath))
        {
            logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "birmingham_swifts_logo.jpg");
        }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(18);
                page.MarginVertical(15);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                // Header (Repeats on each page cleanly)
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            if (File.Exists(logoPath) && (clubName.Contains("Birmingham Swifts", StringComparison.OrdinalIgnoreCase) || clubName.Contains("Swifts", StringComparison.OrdinalIgnoreCase)))
                            {
                                titleCol.Item().MaxHeight(32).MaxWidth(140).Image(logoPath).FitArea();
                            }
                            else
                            {
                                titleCol.Item().Text(clubName)
                                    .FontSize(17)
                                    .Bold()
                                    .FontColor(Colors.Indigo.Darken3);
                            }

                            titleCol.Item().PaddingTop(1).Text("Parkrun Club Results")
                                .FontSize(10.5f)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        row.AutoItem().Column(dateCol =>
                        {
                            dateCol.Item().AlignRight().Text($"Event Date: {eventDate}")
                                .FontSize(9.5f)
                                .Bold()
                                .FontColor(Colors.Grey.Darken3);

                            dateCol.Item().AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });

                    // Summary Stats Badges
                    column.Item().PaddingTop(4).PaddingBottom(4).Row(statRow =>
                    {
                        statRow.Spacing(8);

                        // Card 1: Runners
                        statRow.RelativeItem().Background(Colors.Indigo.Lighten5).Border(1).BorderColor(Colors.Indigo.Lighten3).Padding(5).Column(c =>
                        {
                            c.Item().Text("Total Club Runners").FontSize(7.5f).FontColor(Colors.Indigo.Darken2).SemiBold();
                            c.Item().Text($"{totalRunners}").FontSize(13).Bold().FontColor(Colors.Indigo.Darken4);
                        });

                        // Card 2: Events
                        statRow.RelativeItem().Background(Colors.Teal.Lighten5).Border(1).BorderColor(Colors.Teal.Lighten3).Padding(5).Column(c =>
                        {
                            c.Item().Text("Events Attended").FontSize(7.5f).FontColor(Colors.Teal.Darken2).SemiBold();
                            c.Item().Text($"{totalEvents}").FontSize(13).Bold().FontColor(Colors.Teal.Darken4);
                        });

                        // Card 3: Registered Members
                        statRow.RelativeItem().Background(Colors.Orange.Lighten5).Border(1).BorderColor(Colors.Orange.Lighten3).Padding(5).Column(c =>
                        {
                            c.Item().Text("Club Members Reg.").FontSize(7.5f).FontColor(Colors.Orange.Darken2).SemiBold();
                            string memberStr = string.IsNullOrEmpty(meta.TotalMembers) ? "N/A" : meta.TotalMembers;
                            c.Item().Text(memberStr).FontSize(13).Bold().FontColor(Colors.Orange.Darken4);
                        });
                    });

                    column.Item().PaddingTop(1).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content (Members Table followed by Trends Graph at the end)
                page.Content().PaddingTop(5).Column(col =>
                {
                    // Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(5.0f); // Event Name
                            columns.ConstantColumn(42);   // Pos
                            columns.RelativeColumn(4.0f); // Runner Name
                            columns.ConstantColumn(72);   // Time
                            columns.ConstantColumn(70);   // Finishers
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Darken3).PaddingVertical(3).PaddingHorizontal(3).Text("Event Name").Bold().FontColor(Colors.White).FontSize(13f);
                            header.Cell().Background(Colors.Indigo.Darken3).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text("Pos").Bold().FontColor(Colors.White).FontSize(13f);
                            header.Cell().Background(Colors.Indigo.Darken3).PaddingVertical(3).PaddingHorizontal(3).Text("Parkrunner").Bold().FontColor(Colors.White).FontSize(13f);
                            header.Cell().Background(Colors.Indigo.Darken3).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text("Time").Bold().FontColor(Colors.White).FontSize(13f);
                            header.Cell().Background(Colors.Indigo.Darken3).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text("Finishers").Bold().FontColor(Colors.White).FontSize(13f);
                        });

                        // Table Rows
                        for (int i = 0; i < records.Count; i++)
                        {
                            var r = records[i];
                            string bgColor = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bgColor).PaddingVertical(1.2f).PaddingHorizontal(3).Text(r.EventName).FontSize(11.5f);
                            table.Cell().Background(bgColor).PaddingVertical(1.2f).PaddingHorizontal(2).AlignCenter().Text(r.OverallPosition).FontSize(13f).Bold();

                            if (!string.IsNullOrEmpty(r.ProfileUrl))
                            {
                                table.Cell().Background(bgColor).PaddingVertical(1.2f).PaddingHorizontal(3).Hyperlink(r.ProfileUrl).Text(r.Parkrunner).FontSize(13f).SemiBold().FontColor(Colors.Indigo.Darken4);
                            }
                            else
                            {
                                table.Cell().Background(bgColor).PaddingVertical(1.2f).PaddingHorizontal(3).Text(r.Parkrunner).FontSize(13f).SemiBold();
                            }

                            table.Cell().Background(bgColor).PaddingVertical(1.2f).PaddingHorizontal(2).AlignCenter().Text(r.Time).FontSize(13f).Bold().FontColor(Colors.Indigo.Darken2);
                            table.Cell().Background(bgColor).PaddingVertical(1.2f).PaddingHorizontal(2).AlignCenter().Text(r.EventTotalParticipants).FontSize(15f).FontColor(Colors.Grey.Darken2);
                        }
                    });

                    // =========================================================================
                    // Trend Graph Section (Last 15 Weeks) - right after weekly runner section
                    // =========================================================================
                    if (trendChartBytes != null)
                    {
                        col.Item().ShowEntire().PaddingTop(10).Column(chartCol =>
                        {
                            chartCol.Item().Row(cr =>
                            {
                                cr.RelativeItem().Text("Weekly Trends & Participation History (Last 15 Weeks)").FontSize(10f).Bold().FontColor(Colors.Indigo.Darken3);
                            });
                            chartCol.Item().PaddingTop(3).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Image(trendChartBytes).FitWidth();

                            // Legend Panel positioned below the graph
                            chartCol.Item().PaddingTop(3).AlignCenter().Row(lr =>
                            {
                                lr.Spacing(24);

                                lr.AutoItem().Row(r1 =>
                                {
                                    r1.Spacing(4);
                                    r1.AutoItem().Text("●").FontSize(9).FontColor(Colors.Indigo.Darken3);
                                    r1.AutoItem().Text("Club Runners").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken3);
                                });

                                lr.AutoItem().Row(r2 =>
                                {
                                    r2.Spacing(4);
                                    r2.AutoItem().Text("●").FontSize(9).FontColor(Colors.Teal.Darken2);
                                    r2.AutoItem().Text("Events Attended").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken3);
                                });
                            });
                        });
                    }

                    // =========================================================================
                    // Volunteering Section (ONLY members who volunteered this specific week)
                    // =========================================================================
                    if (volunteerProfiles != null && volunteerProfiles.Count > 0)
                    {
                        int totalWeeklyVolunteers = volunteerProfiles.Count;
                        int distinctEventsSupported = volunteerProfiles.Select(v => v.EventName).Where(e => !string.IsNullOrEmpty(e)).Distinct().Count();
                        int totalVolCredits = volunteerProfiles.Sum(v => v.TotalCredits);
                        int milestoneHolders = volunteerProfiles.Count(v => !string.IsNullOrEmpty(v.HighestMilestone) && v.HighestMilestone != "-");

                        col.Item().ShowEntire().PaddingTop(8).Column(volCol =>
                        {
                            // Section Header
                            volCol.Item().Row(vr =>
                            {
                                vr.RelativeItem().Column(vc =>
                                {
                                    vc.Item().Row(r =>
                                    {
                                        r.Spacing(6);
                                        r.AutoItem().Text("💜").FontSize(10.5f);
                                        r.AutoItem().Text("Club Volunteers This Week & Community Champions")
                                            .FontSize(11f).Bold().FontColor(Colors.Purple.Darken3);
                                    });
                                    vc.Item().PaddingTop(1).Text($"Celebrating {totalWeeklyVolunteers} Birmingham Swifts members who volunteered at parkrun events this weekend ({eventDate}).")
                                        .FontSize(8f).FontColor(Colors.Grey.Darken1);
                                });
                            });

                            // Summary Tiles
                            volCol.Item().PaddingTop(3).PaddingBottom(3).Row(statRow =>
                            {
                                statRow.Spacing(8);

                                // Card 1: Volunteers This Week
                                statRow.RelativeItem().Background(Colors.Purple.Lighten5).Border(1).BorderColor(Colors.Purple.Lighten3).Padding(4).Column(c =>
                                {
                                    c.Item().Text("Volunteers This Week").FontSize(7.5f).FontColor(Colors.Purple.Darken2).SemiBold();
                                    c.Item().Text($"{totalWeeklyVolunteers} Members").FontSize(11.5f).Bold().FontColor(Colors.Purple.Darken4);
                                });

                                // Card 2: Events Supported
                                statRow.RelativeItem().Background(Colors.Teal.Lighten5).Border(1).BorderColor(Colors.Teal.Lighten3).Padding(4).Column(c =>
                                {
                                    c.Item().Text("Events Supported").FontSize(7.5f).FontColor(Colors.Teal.Darken2).SemiBold();
                                    c.Item().Text($"{distinctEventsSupported} Events").FontSize(11.5f).Bold().FontColor(Colors.Teal.Darken4);
                                });

                                // Card 3: Combined Lifetime Credits
                                statRow.RelativeItem().Background(Colors.Indigo.Lighten5).Border(1).BorderColor(Colors.Indigo.Lighten3).Padding(4).Column(c =>
                                {
                                    c.Item().Text("Combined Volunteer Credits").FontSize(7.5f).FontColor(Colors.Indigo.Darken2).SemiBold();
                                    c.Item().Text($"{totalVolCredits:N0} Credits").FontSize(11.5f).Bold().FontColor(Colors.Indigo.Darken4);
                                });

                                // Card 4: Milestone Club Achievers
                                statRow.RelativeItem().Background(Colors.Amber.Lighten5).Border(1).BorderColor(Colors.Amber.Lighten3).Padding(4).Column(c =>
                                {
                                    c.Item().Text("Milestone Club Achievers").FontSize(7.5f).FontColor(Colors.Amber.Darken3).SemiBold();
                                    c.Item().Text($"{milestoneHolders} Members").FontSize(11.5f).Bold().FontColor(Colors.Amber.Darken4);
                                });
                            });

                            // Volunteer Table
                            volCol.Item().PaddingTop(2).Table(vTable =>
                            {
                                vTable.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4.2f); // Volunteer Name & ID
                                    columns.RelativeColumn(3.2f); // Event
                                    columns.RelativeColumn(4.6f); // Role(s) This Week
                                    columns.ConstantColumn(62);   // Milestone
                                    columns.ConstantColumn(62);   // Credits
                                });

                                // Table Header
                                vTable.Header(header =>
                                {
                                    header.Cell().Background(Colors.Purple.Darken3).PaddingVertical(3).PaddingHorizontal(3).Text("Volunteer").Bold().FontColor(Colors.White).FontSize(13f);
                                    header.Cell().Background(Colors.Purple.Darken3).PaddingVertical(3).PaddingHorizontal(3).Text("Event").Bold().FontColor(Colors.White).FontSize(13f);
                                    header.Cell().Background(Colors.Purple.Darken3).PaddingVertical(3).PaddingHorizontal(3).Text("Role(s) This Week").Bold().FontColor(Colors.White).FontSize(13f);
                                    header.Cell().Background(Colors.Purple.Darken3).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text("Milestone").Bold().FontColor(Colors.White).FontSize(13f);
                                    header.Cell().Background(Colors.Purple.Darken3).PaddingVertical(3).PaddingHorizontal(2).AlignCenter().Text("Credits").Bold().FontColor(Colors.White).FontSize(13f);
                                });

                                // Table Rows
                                for (int vi = 0; vi < volunteerProfiles.Count; vi++)
                                {
                                    var vp = volunteerProfiles[vi];
                                    string rowBg = (vi % 2 == 0) ? Colors.White : Colors.Grey.Lighten5;

                                    // Name (15pt name, clickable link, no ID)
                                    string profileUrl = $"https://www.parkrun.org.uk/parkrunner/{vp.ParkrunnerId}/";
                                    vTable.Cell().Background(rowBg).PaddingVertical(1.2f).PaddingHorizontal(3).Hyperlink(profileUrl).Text(vp.ParkrunnerName).FontSize(15f).SemiBold().FontColor(Colors.Purple.Darken4);

                                    // Event Name (13pt)
                                    string cleanEvent = vp.EventName.Replace(" parkrun", "", StringComparison.OrdinalIgnoreCase).Trim();
                                    vTable.Cell().Background(rowBg).PaddingVertical(1.2f).PaddingHorizontal(3).Text(cleanEvent).FontSize(13f).FontColor(Colors.Grey.Darken3);

                                    // Role(s) This Week (13pt semi-bold)
                                    vTable.Cell().Background(rowBg).PaddingVertical(1.2f).PaddingHorizontal(3).Text(vp.RoleThisWeek).FontSize(13f).SemiBold().FontColor(Colors.Purple.Darken3);

                                    // Milestone Badge
                                    var msCell = vTable.Cell().Background(rowBg).PaddingVertical(1.2f).PaddingHorizontal(2).AlignCenter();
                                    if (!string.IsNullOrEmpty(vp.HighestMilestone) && vp.HighestMilestone != "-")
                                    {
                                        string badgeBg;
                                        string badgeTextColor = Colors.White;
                                        string ms = vp.HighestMilestone.ToUpperInvariant();
                                        if (ms == "V1000") badgeBg = Colors.Amber.Darken3;
                                        else if (ms == "V500") badgeBg = Colors.Blue.Darken3;
                                        else if (ms == "V250") badgeBg = Colors.Green.Darken3;
                                        else if (ms == "V100") badgeBg = Colors.Grey.Darken4;
                                        else if (ms == "V50") badgeBg = Colors.Red.Darken2;
                                        else if (ms == "V25") badgeBg = Colors.Purple.Darken2;
                                        else { badgeBg = Colors.Grey.Lighten2; badgeTextColor = Colors.Purple.Darken3; }

                                        msCell.Container().Background(badgeBg).PaddingHorizontal(5).PaddingVertical(2).Text(ms).FontSize(11.5f).Bold().FontColor(badgeTextColor);
                                    }
                                    else
                                    {
                                        msCell.Text("-").FontSize(13f).FontColor(Colors.Grey.Lighten1);
                                    }

                                    // Total Credits (13pt bold)
                                    vTable.Cell().Background(rowBg).PaddingVertical(1.2f).PaddingHorizontal(2).AlignCenter().Text($"{vp.TotalCredits:N0}").FontSize(13f).Bold().FontColor(Colors.Purple.Darken4);
                                }
                            });

                            // Callout Thank You Banner - right after the volunteer section (13pt font)
                            volCol.Item().PaddingTop(5).Background(Colors.Purple.Lighten5).Border(1).BorderColor(Colors.Purple.Lighten3).Padding(6).Row(banner =>
                            {
                                banner.Spacing(8);
                                banner.AutoItem().Text("💜").FontSize(14);
                                banner.RelativeItem().Text(t =>
                                {
                                    t.Span("Celebrating Our Volunteers: ").FontSize(13f).Bold().FontColor(Colors.Purple.Darken3);
                                    t.Span("Every parkrun event is 100% volunteer-led. Huge thanks to all Birmingham Swifts members who volunteered their time to support the running community this weekend! If you would like to help at an upcoming event, chat with our club team or sign up on your local parkrun roster.").FontSize(13f).FontColor(Colors.Purple.Darken4);
                                });
                            });
                        });
                    }
                });

                // Footer
                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem();
                        row.AutoItem().Text(text =>
                        {
                            text.Span("Page ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                            text.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken2).Bold();
                            text.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                            text.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken2).Bold();
                        });
                    });
                });
            });
        });

        doc.GeneratePdf(fullPath);

        Console.WriteLine($"[Success] Successfully generated PDF report ({records.Count:N0} records) at: {fullPath}");
    }
}
