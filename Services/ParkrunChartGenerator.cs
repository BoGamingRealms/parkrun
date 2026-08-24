using System;
using System.Collections.Generic;
using System.Linq;
using ParkrunScraper.Models;
using ScottPlot;

namespace ParkrunScraper.Services;

public class ParkrunChartGenerator
{
    public static byte[]? GenerateWeeklyTrendChart(List<WeeklyClubSnapshot> history, int width = 520, int height = 140)
    {
        if (history == null || history.Count < 2)
        {
            return null; // Not enough data points to render a meaningful trend chart
        }

        try
        {
            var plot = new Plot();

            // Background & Layout
            plot.FigureBackground.Color = Color.FromHex("#FFFFFF");
            plot.DataBackground.Color = Color.FromHex("#F8F9FA");

            double[] xs = Enumerable.Range(0, history.Count).Select(i => (double)i).ToArray();
            double[] runnerCounts = history.Select(h => (double)h.TotalRunners).ToArray();
            double[] eventCounts = history.Select(h => (double)h.DistinctEvents).ToArray();

            // Line 1: Runners
            var runnerLine = plot.Add.Scatter(xs, runnerCounts);
            runnerLine.Color = Color.FromHex("#283593"); // Deep Indigo
            runnerLine.LineWidth = 2.5f;
            runnerLine.MarkerSize = 7;
            runnerLine.LegendText = "Runners";

            // Line 2: Events
            var eventLine = plot.Add.Scatter(xs, eventCounts);
            eventLine.Color = Color.FromHex("#00796B"); // Deep Teal
            eventLine.LineWidth = 2.5f;
            eventLine.MarkerSize = 7;
            eventLine.LegendText = "Events";

            // Add value markers on data points
            for (int i = 0; i < history.Count; i++)
            {
                var rText = plot.Add.Text($"{history[i].TotalRunners}", xs[i], runnerCounts[i] + 1.2);
                rText.LabelFontColor = Color.FromHex("#283593");
                rText.LabelBold = true;
                rText.LabelFontSize = 9;
                rText.LabelAlignment = Alignment.LowerCenter;

                var eText = plot.Add.Text($"{history[i].DistinctEvents}", xs[i], eventCounts[i] + 1.2);
                eText.LabelFontColor = Color.FromHex("#00796B");
                eText.LabelBold = true;
                eText.LabelFontSize = 9;
                eText.LabelAlignment = Alignment.LowerCenter;
            }

            // Format X Axis ticks
            Tick[] ticks = new Tick[history.Count];
            for (int i = 0; i < history.Count; i++)
            {
                string label = history[i].EventDate;
                if (DateTime.TryParse(history[i].EventDate, out var dt))
                {
                    label = dt.ToString("dd MMM");
                }
                ticks[i] = new Tick(i, label);
            }
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
            plot.Axes.Bottom.TickLabelStyle.FontSize = 8.5f;
            plot.Axes.Bottom.TickLabelStyle.ForeColor = Color.FromHex("#424242");

            plot.Axes.Left.TickLabelStyle.FontSize = 8f;
            plot.Axes.Left.TickLabelStyle.ForeColor = Color.FromHex("#757575");

            // Expand Y limits slightly so numbers don't clip
            double maxY = Math.Max(runnerCounts.Max(), eventCounts.Max()) + 5;
            double minY = Math.Max(0, Math.Min(runnerCounts.Min(), eventCounts.Min()) - 4);
            plot.Axes.SetLimitsY(minY, maxY);
            plot.Axes.SetLimitsX(-0.5, history.Count - 0.5);

            // Hide in-plot legend so it does not obstruct any lines/numbers
            plot.HideLegend();

            return plot.GetImageBytes(width, height, ImageFormat.Png);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Failed to generate trend chart: {ex.Message}");
            return null;
        }
    }
}
