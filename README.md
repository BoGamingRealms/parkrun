# Parkrun Consolidated Club Results Scraper (.NET 9 / C#)

A C# .NET 9 console application that scrapes and extracts **Parkrun Consolidated Club Results** across all worldwide parkrun events for **Birmingham Swifts (Club #21925)** (or any other club) and exports the data into a single CSV file directly in your `~/Downloads` folder.

---

## Features

- **Worldwide Coverage**: Extracts all parkruns attended by Birmingham Swifts members worldwide for a given week.
- **Detailed Extracted Fields**:
  - `Event Date`
  - `Club Name`
  - `Event Name` (e.g. *Edgbaston Reservoir parkrun*, *Cannon Hill parkrun*)
  - `Overall Position`
  - `Parkrunner` (Runner's Full Name)
  - `Time` (Finish Time, formatted)
  - `Event Total Participants`
  - `Profile URL`
- **Excel-Ready**: Writes UTF-8 with BOM (`utf-8-sig`) so names and finish times render cleanly in Microsoft Excel or Google Sheets.
- **Fast & Modern**: Built on .NET 9 and `HtmlAgilityPack`.

---

## Quick Start

### 1. Run with default configuration (Birmingham Swifts - Club #21925):
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper
```

### 2. Specifying a Different Club ID or URL:
Pass another club's numerical ID or full URL with `-c` or `--club`:
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper -- --club 21925
```
Or with full parkrun URL:
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper -- --club "https://www.parkrun.com/results/consolidatedclub/?clubNum=21925"
```

### 3. Specifying a Historical Date:
Extract results for a specific weekend event date (`YYYY-MM-DD`):
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper -- --date 2026-08-22
```

### 4. Overwriting a Single Constant CSV File:
To overwrite a fixed single file (`~/Downloads/parkrun_consolidated_club_results.csv`) instead of date-stamped files:
```bash
dotnet run --project /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper -- --single-file
```

---

## Configuration (`appsettings.json`)

Pre-configured for Birmingham Swifts in [appsettings.json](appsettings.json):
```json
{
  "DefaultClubNum": "21925",
  "DefaultClubName": "Birmingham Swifts",
  "DownloadFolder": "~/Downloads",
  "OutputFilenamePattern": "parkrun_consolidated_{0}_{1}.csv",
  "SingleOutputFilename": "parkrun_consolidated_club_results.csv",
  "OverwriteSingleFile": false
}
```

---

## Pushing to GitHub as an Independent Repository

This project is configured as its own independent Git repository. To push to GitHub:

```bash
cd /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper
git remote add origin https://github.com/<YOUR_USERNAME>/<NEW_REPO_NAME>.git
git branch -M main
git push -u origin main
```
