# Parkrun Consolidated Club Results Scraper

A lightweight Python tool that scrapes and extracts **Parkrun Consolidated Club Results** across all worldwide parkrun events for any club and exports the data into a single CSV file directly in your `~/Downloads` folder.

---

## Features

- **Worldwide Coverage**: Extracts all parkruns attended by your club's members worldwide for a given week.
- **Detailed Extracted Fields**:
  - `Event Date`
  - `Club Name`
  - `Event Name` (e.g. *Richmond parkrun*)
  - `Event Number` (e.g. *#929*)
  - `Overall Position`
  - `Gender Position`
  - `Parkrunner` (Runner's Full Name)
  - `Parkrunner ID` (e.g. *4662650*)
  - `Time` (Finish Time, formatted)
  - `Event Total Participants`
  - `Profile URL`
- **Excel-Ready**: Writes UTF-8 with BOM (`utf-8-sig`) so special characters and times render cleanly in Microsoft Excel or Google Sheets.
- **Zero Heavy Dependencies**: Uses Python standard library by default, with automatic acceleration via `lxml` if present.

---

## Quick Start

Run the scraper with default club configuration:
```bash
python3 scraper.py
```

### Specifying Club ID / Name
Pass your club's numerical ID or full URL with `-c` or `--club`:
```bash
python3 scraper.py --club 947
```
Or pass the full parkrun consolidated URL directly:
```bash
python3 scraper.py --club "https://www.parkrun.com/results/consolidatedclub/?clubNum=947"
```

### Specifying a Historical Date
Extract results for a specific weekend event date (`YYYY-MM-DD`):
```bash
python3 scraper.py --club 947 --date 2026-08-22
```

### Overwriting a Single Constant CSV
To overwrite a fixed single file (`~/Downloads/parkrun_consolidated_club_results.csv`) instead of date-stamped files:
```bash
python3 scraper.py --single-file
```

---

## Configuration (`config.json`)

You can set your default club number, name, and output directory in [config.json](config.json):
```json
{
  "default_club_num": "947",
  "default_club_name": "Ranelagh Harriers",
  "download_folder": "~/Downloads",
  "output_filename_pattern": "parkrun_consolidated_{club_slug}_{date}.csv",
  "single_output_filename": "parkrun_consolidated_club_results.csv",
  "overwrite_single_file": false
}
```

---

## Pushing to GitHub as an Independent Repository

This project is in its own isolated Git repository. To push to a new GitHub repository:

```bash
cd /Users/bowang/.gemini/antigravity-ide/scratch/parkrun-scraper
git remote add origin https://github.com/<YOUR_USERNAME>/<NEW_REPO_NAME>.git
git branch -M main
git push -u origin main
```
