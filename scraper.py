#!/usr/bin/env python3
"""
Parkrun Consolidated Club Results Scraper
-----------------------------------------
Extracts parkrun consolidated club results across all worldwide events for a specific club
and automatically exports them into a single CSV file in the Downloads folder.
"""

import argparse
import csv
import json
import os
import re
import sys
import urllib.request
import urllib.parse
from datetime import datetime
from html.parser import HTMLParser

try:
    from lxml import html as lxml_html
    HAS_LXML = True
except ImportError:
    HAS_LXML = False


DEFAULT_CONFIG_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "config.json")


def load_config():
    """Load configuration from config.json if present."""
    if os.path.exists(DEFAULT_CONFIG_PATH):
        try:
            with open(DEFAULT_CONFIG_PATH, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception as e:
            print(f"[Warning] Could not parse config.json: {e}", file=sys.stderr)
    return {
        "default_club_num": "947",
        "default_club_name": "",
        "download_folder": "~/Downloads",
        "output_filename_pattern": "parkrun_consolidated_{club_slug}_{date}.csv",
        "single_output_filename": "parkrun_consolidated_club_results.csv",
        "overwrite_single_file": False,
    }


def build_url(club_num_or_url, event_date=None):
    """Constructs the full parkrun consolidated club URL."""
    club_str = str(club_num_or_url).strip()

    if club_str.startswith("http://") or club_str.startswith("https://"):
        url = club_str
        if event_date and "eventdate=" not in url:
            separator = "&" if "?" in url else "?"
            url += f"{separator}eventdate={event_date}"
        return url

    # Extract digits if user passed 'clubNum=XXXX' or similar
    match = re.search(r"\d+", club_str)
    if not match:
        raise ValueError(f"Invalid club number or URL: {club_num_or_url}")
    club_num = match.group(0)

    url = f"https://www.parkrun.com/results/consolidatedclub/?clubNum={club_num}"
    if event_date:
        url += f"&eventdate={event_date}"
    return url


def fetch_html(url):
    """Fetches HTML content from parkrun with browser headers."""
    headers = {
        "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 "
                      "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
        "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
        "Accept-Language": "en-US,en;q=0.9",
    }
    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            return response.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"HTTP {e.code} error fetching URL {url}: {e.reason}")
    except urllib.error.URLError as e:
        raise RuntimeError(f"Network error fetching URL {url}: {e.reason}")


def parse_with_lxml(html_content):
    """Parses the consolidated club report using lxml."""
    tree = lxml_html.fromstring(html_content)

    # 1. Extract metadata from header
    meta = {
        "club_name": "",
        "event_date": "",
        "total_members": "",
        "total_participants": ""
    }

    # Find intro paragraph (e.g. "This is a list of members of Ranelagh Harriers who participated at a parkrun on 2026-08-22...")
    for p in tree.xpath("//p"):
        p_text = p.text_content().strip()
        if "This is a list of members of" in p_text:
            match_club = re.search(r"members of (.*?) who participated", p_text)
            if match_club:
                meta["club_name"] = match_club.group(1).strip()
            
            match_date = re.search(r"on (\d{4}-\d{2}-\d{2})", p_text)
            if match_date:
                meta["event_date"] = match_date.group(1).strip()

            match_counts = re.search(r"total (\d+) members, (\d+) took part", p_text)
            if match_counts:
                meta["total_members"] = match_counts.group(1).strip()
                meta["total_participants"] = match_counts.group(2).strip()
            break

    # 2. Extract results by event
    results = []
    
    for h2 in tree.xpath("//h2"):
        event_name = h2.text_content().strip()
        if not event_name:
            continue

        event_number = ""
        total_event_participants = ""
        table_node = None

        # Scan following sibling tags until next h2
        curr = h2.getnext()
        while curr is not None and curr.tag != "h2":
            if curr.tag == "p":
                p_text = curr.text_content().strip()
                m_part = re.search(r"total of (\d+) parkrunners", p_text, re.IGNORECASE)
                if m_part:
                    total_event_participants = m_part.group(1)

                m_ev = re.search(r"event #(\d+)", p_text, re.IGNORECASE)
                if m_ev:
                    event_number = m_ev.group(1)

            elif curr.tag == "table":
                table_node = curr
            curr = curr.getnext()

        if table_node is None:
            continue

        # Parse table rows
        for tr in table_node.xpath(".//tr"):
            tds = tr.xpath(".//td")
            if not tds:
                continue

            row_data = [td.text_content().strip() for td in tds]
            if len(row_data) < 5:
                continue

            overall_pos = row_data[0]
            gender_pos = row_data[1]
            runner_name = row_data[2]
            club_name = row_data[3]
            finish_time = row_data[4]

            # Extract parkrunner ID and link if available
            parkrunner_id = ""
            profile_url = ""
            a_tags = tds[2].xpath(".//a")
            if a_tags:
                profile_url = a_tags[0].get("href", "")
                m_id = re.search(r"/parkrunner/(\d+)", profile_url)
                if m_id:
                    parkrunner_id = m_id.group(1)

            results.append({
                "Event Date": meta["event_date"],
                "Club Name": club_name or meta["club_name"],
                "Event Name": event_name,
                "Event Number": event_number,
                "Overall Position": overall_pos,
                "Gender Position": gender_pos,
                "Parkrunner": runner_name,
                "Parkrunner ID": parkrunner_id,
                "Time": finish_time,
                "Event Total Participants": total_event_participants,
                "Profile URL": profile_url,
            })

    return meta, results


class SimpleParkrunParser(HTMLParser):
    """Fallback parser using standard library html.parser if lxml is unavailable."""
    def __init__(self):
        super().__init__()
        self.meta = {"club_name": "", "event_date": "", "total_members": "", "total_participants": ""}
        self.results = []
        self.current_tag = ""
        self.current_event = ""
        self.current_event_num = ""
        self.current_event_part = ""
        self.in_table = False
        self.in_tr = False
        self.in_td = False
        self.in_h2 = False
        self.in_p = False
        self.current_p_text = []
        self.current_h2_text = []
        self.current_row = []
        self.current_cell = []
        self.current_a_href = ""

    def handle_starttag(self, tag, attrs):
        self.current_tag = tag
        attr_dict = dict(attrs)
        if tag == "h2":
            self.in_h2 = True
            self.current_h2_text = []
        elif tag == "p":
            self.in_p = True
            self.current_p_text = []
        elif tag == "table":
            self.in_table = True
        elif tag == "tr" and self.in_table:
            self.in_tr = True
            self.current_row = []
        elif tag == "td" and self.in_tr:
            self.in_td = True
            self.current_cell = []
            self.current_a_href = ""
        elif tag == "a" and self.in_td:
            if "href" in attr_dict:
                self.current_a_href = attr_dict["href"]

    def handle_endtag(self, tag):
        if tag == "h2":
            self.in_h2 = False
            self.current_event = "".join(self.current_h2_text).strip()
            self.current_event_num = ""
            self.current_event_part = ""
        elif tag == "p":
            self.in_p = False
            p_text = "".join(self.current_p_text).strip()
            if "This is a list of members of" in p_text:
                m_club = re.search(r"members of (.*?) who participated", p_text)
                if m_club: self.meta["club_name"] = m_club.group(1).strip()
                m_date = re.search(r"on (\d{4}-\d{2}-\d{2})", p_text)
                if m_date: self.meta["event_date"] = m_date.group(1).strip()
                m_counts = re.search(r"total (\d+) members, (\d+) took part", p_text)
                if m_counts:
                    self.meta["total_members"] = m_counts.group(1).strip()
                    self.meta["total_participants"] = m_counts.group(2).strip()
            else:
                m_part = re.search(r"total of (\d+) parkrunners", p_text, re.IGNORECASE)
                if m_part: self.current_event_part = m_part.group(1)
                m_ev = re.search(r"event #(\d+)", p_text, re.IGNORECASE)
                if m_ev: self.current_event_num = m_ev.group(1)
        elif tag == "td" and self.in_tr:
            self.in_td = False
            cell_text = "".join(self.current_cell).strip()
            self.current_row.append((cell_text, self.current_a_href))
        elif tag == "tr" and self.in_table:
            self.in_tr = False
            if len(self.current_row) >= 5:
                pos = self.current_row[0][0]
                g_pos = self.current_row[1][0]
                runner = self.current_row[2][0]
                link = self.current_row[2][1]
                club = self.current_row[3][0]
                time_str = self.current_row[4][0]
                p_id = ""
                m_id = re.search(r"/parkrunner/(\d+)", link)
                if m_id: p_id = m_id.group(1)

                self.results.append({
                    "Event Date": self.meta["event_date"],
                    "Club Name": club or self.meta["club_name"],
                    "Event Name": self.current_event,
                    "Event Number": self.current_event_num,
                    "Overall Position": pos,
                    "Gender Position": g_pos,
                    "Parkrunner": runner,
                    "Parkrunner ID": p_id,
                    "Time": time_str,
                    "Event Total Participants": self.current_event_part,
                    "Profile URL": link,
                })
        elif tag == "table":
            self.in_table = False

    def handle_data(self, data):
        if self.in_h2:
            self.current_h2_text.append(data)
        elif self.in_p:
            self.current_p_text.append(data)
        elif self.in_td:
            self.current_cell.append(data)


def parse_consolidated_report(html_content):
    """Parses HTML using lxml if available, otherwise falls back to standard HTMLParser."""
    if HAS_LXML:
        return parse_with_lxml(html_content)
    parser = SimpleParkrunParser()
    parser.feed(html_content)
    return parser.meta, parser.results


def save_to_csv(results, output_path):
    """Writes results to a UTF-8 with BOM CSV file for Excel compatibility."""
    if not results:
        print("[Warning] No runner rows found to write.", file=sys.stderr)
        return

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)

    fieldnames = [
        "Event Date",
        "Club Name",
        "Event Name",
        "Event Number",
        "Overall Position",
        "Gender Position",
        "Parkrunner",
        "Parkrunner ID",
        "Time",
        "Event Total Participants",
        "Profile URL"
    ]

    with open(output_path, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(results)

    print(f"[Success] Extracted {len(results)} runner records to: {output_path}")


def main():
    config = load_config()

    parser = argparse.ArgumentParser(description="Scrape parkrun Consolidated Club Results into a single CSV.")
    parser.add_argument(
        "-c", "--club",
        default=config.get("default_club_num", "947"),
        help="Club number ID (e.g. 947) or full consolidated club URL."
    )
    parser.add_argument(
        "-d", "--date",
        default=None,
        help="Optional event date in YYYY-MM-DD format (defaults to latest available report)."
    )
    parser.add_argument(
        "-o", "--output",
        default=None,
        help="Custom output CSV file path."
    )
    parser.add_argument(
        "--single-file",
        action="store_true",
        help="Always overwrite a single fixed file 'parkrun_consolidated_club_results.csv' in Downloads."
    )

    args = parser.parse_args()

    url = build_url(args.club, args.date)
    print(f"Fetching consolidated club results from:\n  {url}\n")

    try:
        html_content = fetch_html(url)
    except Exception as e:
        print(f"[Error] {e}", file=sys.stderr)
        sys.exit(1)

    meta, results = parse_consolidated_report(html_content)

    club_name = meta.get("club_name") or config.get("default_club_name", "Club")
    event_date = meta.get("event_date") or args.date or datetime.now().strftime("%Y-%m-%d")
    total_participants = meta.get("total_participants") or str(len(results))

    print(f"Club: {club_name}")
    print(f"Event Date: {event_date}")
    print(f"Total Club Participants on Date: {total_participants}")
    print(f"Distinct Events Attended: {len(set(r['Event Name'] for r in results))}")
    print(f"Total Runner Results Parsed: {len(results)}\n")

    # Determine destination CSV path
    if args.output:
        dest_path = os.path.expanduser(args.output)
    else:
        downloads_dir = os.path.expanduser(config.get("download_folder", "~/Downloads"))
        if args.single_file or config.get("overwrite_single_file", False):
            filename = config.get("single_output_filename", "parkrun_consolidated_club_results.csv")
        else:
            club_slug = re.sub(r"[^a-zA-Z0-9_-]", "_", club_name).strip("_") or "club"
            pattern = config.get("output_filename_pattern", "parkrun_consolidated_{club_slug}_{date}.csv")
            filename = pattern.format(club_slug=club_slug, date=event_date)
        dest_path = os.path.join(downloads_dir, filename)

    save_to_csv(results, dest_path)

    # Print top preview
    if results:
        print("\nPreview of extracted records (first 5):")
        print("-" * 90)
        print(f"{'Event Name':<25} | {'Pos':<5} | {'GPos':<5} | {'Parkrunner':<25} | {'Time':<8}")
        print("-" * 90)
        for r in results[:5]:
            print(f"{r['Event Name']:<25} | {r['Overall Position']:<5} | {r['Gender Position']:<5} | {r['Parkrunner']:<25} | {r['Time']:<8}")
        print("-" * 90)


if __name__ == "__main__":
    main()
