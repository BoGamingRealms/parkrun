# Parkrun Consolidated Club Results Scraper (.NET 9 / C#)

A C# .NET 9 console application that scrapes and extracts **Parkrun Consolidated Club Results** across all worldwide parkrun events for **Birmingham Swifts (Club #21925)** (or any other club) and automatically generates a styled **PDF report** directly in your `~/Downloads` folder.

---

## Features

- **Worldwide Coverage**: Extracts all parkruns attended by Birmingham Swifts members worldwide for a given week.
- **PDF Report Layout**:
  - Club Header with dynamic branding and date subtitle.
  - Summary metrics cards (Total Club Runners, Events Attended, Total Registered Members).
  - Multi-page responsive table with alternating row colors.
  - Interactive profile links to parkrunner profiles.
  - Running footer with page numbering and timestamp.
- **Detailed Extracted Fields in Report**:
  - `Event Name` (e.g. *Edgbaston Reservoir parkrun*, *Cannon Hill parkrun*)
  - `Overall Position`
  - `Parkrunner` (Runner's Full Name)
  - `Time` (Finish Time)
  - `Event Finishers`
  - `Profile Link`
- **Fast & Modern**: Built on .NET 9, `HtmlAgilityPack`, and `QuestPDF`.

---

## Quick Start

### 1. Run with default configuration (Generates PDF for Birmingham Swifts):
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper
```

### 2. Specifying a Different Club ID or URL:
Pass another club's numerical ID or full URL with `-c` or `--club`:
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper -- --club 21925
```

### 3. Specifying a Historical Date:
Extract results for a specific weekend event date (`YYYY-MM-DD`):
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper -- --date 2026-08-22
```

### 4. Also Exporting a CSV File:
Pass `--csv` to generate both a PDF and a CSV file simultaneously:
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper -- --csv
```

---

## Configuration (`appsettings.json`)

Pre-configured for Birmingham Swifts in [appsettings.json](appsettings.json):
```json
{
  "DefaultClubNum": "21925",
  "DefaultClubName": "Birmingham Swifts",
  "DownloadFolder": "~/Downloads",
  "OutputFilenamePattern": "parkrun_consolidated_{0}_{1}.pdf",
  "SingleOutputFilename": "parkrun_consolidated_club_results.pdf",
  "OverwriteSingleFile": false
}
```

---

## Pushing to GitHub as an Independent Repository

This project is configured as its own independent Git repository. To push to GitHub:

```bash
cd /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper
git push origin main
```
