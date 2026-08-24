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
        TrendComparison? trends = null,
        byte[]? trendChartBytes = null)
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

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                // Header
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text(clubName)
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Indigo.Darken3);

                            titleCol.Item().Text("Parkrun Club Results")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        row.AutoItem().Column(dateCol =>
                        {
                            dateCol.Item().AlignRight().Text($"Event Date: {eventDate}")
                                .FontSize(10)
                                .Bold()
                                .FontColor(Colors.Grey.Darken3);

                            dateCol.Item().AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });

                    // Summary Stats Badges with Week-over-Week Trend Indicators
                    column.Item().PaddingTop(8).PaddingBottom(6).Row(statRow =>
                    {
                        statRow.Spacing(10);

                        // Card 1: Runners
                        statRow.RelativeItem().Background(Colors.Indigo.Lighten5).Border(1).BorderColor(Colors.Indigo.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Total Club Runners").FontSize(8).FontColor(Colors.Indigo.Darken2).SemiBold();
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{totalRunners}").FontSize(14).Bold().FontColor(Colors.Indigo.Darken4);
                                if (trends != null && trends.HasPreviousRunners)
                                {
                                    string deltaText = trends.RunnersDelta > 0 ? $"+{trends.RunnersDelta}" : $"{trends.RunnersDelta}";
                                    string symbol = trends.RunnersDelta > 0 ? "▲" : (trends.RunnersDelta < 0 ? "▼" : "—");
                                    string deltaColor = trends.RunnersDelta > 0 ? Colors.Green.Darken2 : (trends.RunnersDelta < 0 ? Colors.Red.Darken2 : Colors.Grey.Darken1);
                                    r.AutoItem().AlignBottom().Text($"{symbol} {deltaText} vs last wk").FontSize(7.5f).Bold().FontColor(deltaColor);
                                }
                            });
                        });

                        // Card 2: Events
                        statRow.RelativeItem().Background(Colors.Teal.Lighten5).Border(1).BorderColor(Colors.Teal.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Events Attended").FontSize(8).FontColor(Colors.Teal.Darken2).SemiBold();
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{totalEvents}").FontSize(14).Bold().FontColor(Colors.Teal.Darken4);
                                if (trends != null && trends.HasPreviousEvents)
                                {
                                    string deltaText = trends.EventsDelta > 0 ? $"+{trends.EventsDelta}" : $"{trends.EventsDelta}";
                                    string symbol = trends.EventsDelta > 0 ? "▲" : (trends.EventsDelta < 0 ? "▼" : "—");
                                    string deltaColor = trends.EventsDelta > 0 ? Colors.Green.Darken2 : (trends.EventsDelta < 0 ? Colors.Red.Darken2 : Colors.Grey.Darken1);
                                    r.AutoItem().AlignBottom().Text($"{symbol} {deltaText} vs last wk").FontSize(7.5f).Bold().FontColor(deltaColor);
                                }
                            });
                        });

                        // Card 3: Registered Members
                        statRow.RelativeItem().Background(Colors.Orange.Lighten5).Border(1).BorderColor(Colors.Orange.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Club Members Reg.").FontSize(8).FontColor(Colors.Orange.Darken2).SemiBold();
                            c.Item().Row(r =>
                            {
                                string memberStr = string.IsNullOrEmpty(meta.TotalMembers) ? "N/A" : meta.TotalMembers;
                                r.RelativeItem().Text(memberStr).FontSize(14).Bold().FontColor(Colors.Orange.Darken4);
                                if (trends != null && trends.HasPreviousMembers)
                                {
                                    string deltaText = trends.MembersDelta > 0 ? $"+{trends.MembersDelta}" : $"{trends.MembersDelta}";
                                    string symbol = trends.MembersDelta > 0 ? "▲" : (trends.MembersDelta < 0 ? "▼" : "—");
                                    string deltaColor = trends.MembersDelta > 0 ? Colors.Green.Darken2 : (trends.MembersDelta < 0 ? Colors.Red.Darken2 : Colors.Grey.Darken1);
                                    r.AutoItem().AlignBottom().Text($"{symbol} {deltaText}").FontSize(7.5f).Bold().FontColor(deltaColor);
                                }
                            });
                        });
                    });

                    // Optional Trend Chart (if multiple weeks of history exist)
                    if (trendChartBytes != null)
                    {
                        column.Item().PaddingTop(2).PaddingBottom(6).Column(chartCol =>
                        {
                            chartCol.Item().Row(cr =>
                            {
                                cr.RelativeItem().Text("Weekly Trends (Past Weeks)").FontSize(8f).Bold().FontColor(Colors.Grey.Darken2);
                            });
                            chartCol.Item().PaddingTop(2).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Image(trendChartBytes).FitWidth();
                        });
                    }

                    column.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content Table
                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3.5f); // Event Name
                        columns.ConstantColumn(45);   // Pos
                        columns.RelativeColumn(3.0f); // Runner Name
                        columns.ConstantColumn(60);   // Time
                        columns.ConstantColumn(65);   // Total Part.
                        columns.RelativeColumn(2.5f); // Profile Link
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Indigo.Darken3).Padding(5).Text("Event Name").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken3).Padding(5).AlignCenter().Text("Pos").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken3).Padding(5).Text("Parkrunner").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken3).Padding(5).AlignCenter().Text("Time").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken3).Padding(5).AlignCenter().Text("Finishers").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken3).Padding(5).Text("Profile").Bold().FontColor(Colors.White);
                    });

                    // Table Rows
                    for (int i = 0; i < records.Count; i++)
                    {
                        var r = records[i];
                        string bgColor = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                        table.Cell().Background(bgColor).Padding(4).Text(r.EventName).FontSize(8.5f);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(r.OverallPosition).FontSize(8.5f).Bold();
                        table.Cell().Background(bgColor).Padding(4).Text(r.Parkrunner).FontSize(8.5f).Medium();
                        table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(r.Time).FontSize(8.5f).Bold().FontColor(Colors.Indigo.Darken2);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(r.EventTotalParticipants).FontSize(8f).FontColor(Colors.Grey.Darken1);

                        if (!string.IsNullOrEmpty(r.ProfileUrl))
                        {
                            table.Cell().Background(bgColor).Padding(4).Hyperlink(r.ProfileUrl).Text("View Profile").FontSize(7.5f).FontColor(Colors.Blue.Darken2).Underline();
                        }
                        else
                        {
                            table.Cell().Background(bgColor).Padding(4).Text("-").FontSize(8f).FontColor(Colors.Grey.Lighten1);
                        }
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
        }).GeneratePdf(fullPath);

        Console.WriteLine($"[Success] Successfully generated PDF report ({records.Count:N0} records) at: {fullPath}");
    }
}
