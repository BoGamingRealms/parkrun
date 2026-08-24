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

                // Header (Repeats on each page cleanly)
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

                    // Summary Stats Badges
                    column.Item().PaddingTop(8).PaddingBottom(6).Row(statRow =>
                    {
                        statRow.Spacing(10);

                        // Card 1: Runners
                        statRow.RelativeItem().Background(Colors.Indigo.Lighten5).Border(1).BorderColor(Colors.Indigo.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Total Club Runners").FontSize(8).FontColor(Colors.Indigo.Darken2).SemiBold();
                            c.Item().Text($"{totalRunners}").FontSize(14).Bold().FontColor(Colors.Indigo.Darken4);
                        });

                        // Card 2: Events
                        statRow.RelativeItem().Background(Colors.Teal.Lighten5).Border(1).BorderColor(Colors.Teal.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Events Attended").FontSize(8).FontColor(Colors.Teal.Darken2).SemiBold();
                            c.Item().Text($"{totalEvents}").FontSize(14).Bold().FontColor(Colors.Teal.Darken4);
                        });

                        // Card 3: Registered Members
                        statRow.RelativeItem().Background(Colors.Orange.Lighten5).Border(1).BorderColor(Colors.Orange.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("Club Members Reg.").FontSize(8).FontColor(Colors.Orange.Darken2).SemiBold();
                            string memberStr = string.IsNullOrEmpty(meta.TotalMembers) ? "N/A" : meta.TotalMembers;
                            c.Item().Text(memberStr).FontSize(14).Bold().FontColor(Colors.Orange.Darken4);
                        });
                    });

                    column.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content (Members Table followed by Trends Graph at the end)
                page.Content().PaddingTop(8).Column(col =>
                {
                    // Table
                    col.Item().Table(table =>
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

                    // Trend Graph Section at the END of the report (after all members)
                    if (trendChartBytes != null)
                    {
                        col.Item().PaddingTop(16).Column(chartCol =>
                        {
                            chartCol.Item().Row(cr =>
                            {
                                cr.RelativeItem().Text("Weekly Trends & Participation History (Last 10 Weeks)").FontSize(10f).Bold().FontColor(Colors.Indigo.Darken3);
                            });
                            chartCol.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Image(trendChartBytes).FitWidth();

                            // Legend Panel positioned below the graph
                            chartCol.Item().PaddingTop(5).AlignCenter().Row(lr =>
                            {
                                lr.Spacing(24);

                                lr.AutoItem().Row(r1 =>
                                {
                                    r1.Spacing(4);
                                    r1.AutoItem().Text("●").FontSize(10).FontColor(Colors.Indigo.Darken3);
                                    r1.AutoItem().Text("Club Runners").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken3);
                                });

                                lr.AutoItem().Row(r2 =>
                                {
                                    r2.Spacing(4);
                                    r2.AutoItem().Text("●").FontSize(10).FontColor(Colors.Teal.Darken2);
                                    r2.AutoItem().Text("Events Attended").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken3);
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
        }).GeneratePdf(fullPath);

        Console.WriteLine($"[Success] Successfully generated PDF report ({records.Count:N0} records) at: {fullPath}");
    }
}
