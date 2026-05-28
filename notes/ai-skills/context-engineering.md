**What “context engineering” actually means** 

In that snippet, “context engineering” is about **designing everything around the model** so it can reliably do high‑quality work: files, structure, briefs, tools, and how it finds what it needs. Think: solution architecture, but for AI workflows instead of microservices.

Let’s walk through it step by step using a concrete example.

**Example project: Weekly Competitive Intelligence Brief** 

Goal: Every Monday morning, Claude produces a 1‑page brief on 3 competitors (sites, pricing, features) for your team.

**Step 1: Define the job precisely** 

Write a 3–5 line spec (this is _for humans_ and later goes into the AI brief):

- Business goal: “Give me a concise weekly view of 3 named competitors: positioning, pricing, recent changes, risks.”
- Inputs: “Competitor URLs, last week’s brief, our product overview.”
- Output: “1‑page markdown brief + risk/opportunity list, same structure every week.”

This is like your high‑level design doc.

**Step 2: Design the “context folders”** 

Now you design where things live so the model can reason over them.

For our example:

- `/ai/competitor-briefs/CLAUDE.md` – master briefing doc
- `/ai/competitor-briefs/system/` – stable docs the model should always see

- `product_overview.md`
- `positioning_guidelines.md`
- `tone_and_style.md`

- `/ai/competitor-briefs/history/` – last 10 briefs
- `/ai/competitor-briefs/raw/` – dumped PDFs / HTML snapshots of competitor pages

The point: consistent, predictable layout so you can tell Claude, “You have access to system docs, history, and raw inputs in this structure…”

**Step 3: Write the CLAUDE.md brief** 

This is the “environment spec” Claude reads every time.

Example skeleton:

```markdown
# Role
You are a Competitive Intelligence Analyst for [Our Product].

# Goal
Every Monday, create a concise brief on 3 competitors for our leadership team.

# Inputs
- System docs in /system: product_overview, positioning_guidelines, tone_and_style.
- Historical briefs in /history: use them to keep format and avoid repetition.
- Raw inputs in /raw: latest snapshots of competitor sites and pricing pages.

# Output Requirements
- Length: 1 page in markdown.
- Sections (in this order):
  1. Executive Summary (3 bullets)
  2. Competitor Snapshots (one subsection per competitor)
  3. Risks & Opportunities (5–7 bullets)
  4. Notable Changes vs Last Week (3 bullets)

# Constraints
- Use neutral, analyst tone.
- Do not hallucinate: if a fact isn't in the raw inputs, say "unknown".
```

That’s context engineering: you’re not “prompting” a one‑off; you’re designing a standing environment.
**Step 4: Decide the connectors / skills** 
What does the agent need to _reach_?
For a simple version:
- Connector to your file store (where `/ai/competitor-briefs/...` lives).
- Optional: a web fetch skill that can snapshot given URLs into `/raw` before each run.
- Optional: a calendar/cron trigger to run every Monday 6am and drop output into a folder or Notion page.
In Claude terms, this is where you’d define tools/skills like:
- `read_file(path)`
- `list_dir(path)`
- `write_file(path, content)`
- `fetch_url_to_file(url, target_path)`

You’re deciding _how_ the agent finds / writes stuff, not just what it says.

**Step 5: Design the agent’s workflow** 

Now specify the multi‑step internal process. For example:

1. Read system docs from `/system`.
2. Read last week’s brief from `/history`.
3. Read latest raw inputs in `/raw`.
4. Generate this week’s brief.
5. Save it to `/history/brief-YYYY-MM-DD.md`.
6. Output a short confirmation summary.

You encode that in a “playbook” prompt or in the agent’s system instructions, e.g.:

```text
Workflow:
1. Load all files in /system.
2. Load the most recent file in /history as "last_week".
3. Load all files in /raw for this week.
4. Draft a new brief following CLAUDE.md Output Requirements.
5. Save the new brief to /history with today's date in the filename.
6. Reply with only the markdown content of the new brief.
```

  

**Step 6: Test and refine the environment (not just the wording)** 

Run a few dry runs and then tweak **context**, not just phrasing:

- If it’s missing key facts → adjust what goes in `/raw` or update CLAUDE.md to emphasize those sources.
- If tone is off → refine `tone_and_style.md` rather than rewriting the whole user prompt.
- If structure drifts → tighten the “Output Requirements” and add examples in CLAUDE.md.

This is where you earn the “context engineer” title: you improve the _setup_ until outputs lock in.

**Step 7: Make it reusable and scalable** 

Once the pattern works, you generalize it:

- Clone the same structure for “Weekly Engineering Risk Report”, “Monthly Investor Update”, etc.
- Swap out CLAUDE.md and system docs; keep the folder/schema, skills, and workflow patterns.

At that point, you’re doing **AI operations architecture**: a catalog of well‑designed environments and playbooks that different teams can plug models into and get predictable, high‑quality results.

In short, context engineering is taking what you already do as a solution architect—domains, folders, interfaces, workflows—and applying it to AI so the model is never staring at a blank box; it’s operating inside a well‑designed system.

-------------
You can. Below is a full, runnable Python “weekly competitor brief” agent wired to a generic DeepSeek-style chat API, plus the filesystem tools we discussed.

I’ll assume this directory layout on your machine:

- `ai/competitor-briefs/CLAUDE.md`
- `ai/competitor-briefs/system/` (product docs, tone guide, etc.)
- `ai/competitor-briefs/history/` (past briefs)
- `ai/competitor-briefs/raw/` (latest competitor snapshots)

The agent will:

1. Ensure the folders exist.
2. Fetch competitor URLs into `raw/`.
3. Load `CLAUDE.md`, system docs, last week’s brief, and raw files.
4. Call DeepSeek to generate this week’s brief.
5. Save the brief into `history/brief-YYYY-MM-DD.md`.

You can later put this behind a cron or scheduled job.

```python
# file: competitor_brief_agent.py

import os
import json
import glob
import datetime
from pathlib import Path
from typing import List, Optional

import requests  # pip install requests

# ==========
# Configuration
# ==========

# Base directory for this agent's context
BASE_DIR = Path("ai/competitor-briefs")
SYSTEM_DIR = BASE_DIR / "system"
HISTORY_DIR = BASE_DIR / "history"
RAW_DIR = BASE_DIR / "raw"
CLAUDE_DOC = BASE_DIR / "CLAUDE.md"

# DeepSeek-style API config (adjust for your environment)
DEEPSEEK_API_KEY = os.environ.get("DEEPSEEK_API_KEY")
DEEPSEEK_BASE_URL = os.environ.get("DEEPSEEK_BASE_URL")  # e.g. set in env
DEEPSEEK_MODEL = os.environ.get("DEEPSEEK_MODEL", "deepseek-chat")


# ==========
# File + HTTP utilities
# ==========

def ensure_directories() -> None:
    """
    Ensure the expected folder structure exists.
    """
    for d in [BASE_DIR, SYSTEM_DIR, HISTORY_DIR, RAW_DIR]:
        d.mkdir(parents=True, exist_ok=True)


def read_file(path: Path) -> str:
    """
    Read a text file and return its content.
    """
    with path.open("r", encoding="utf-8") as f:
        return f.read()


def write_file(path: Path, content: str) -> None:
    """
    Write text content to a file, creating parent dirs if needed.
    """
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        f.write(content)


def list_dir(path: Path) -> List[Path]:
    """
    List files (not directories) in a given path.
    """
    if not path.exists():
        return []
    return [p for p in path.iterdir() if p.is_file()]


def fetch_url_to_file(url: str, target_path: Path) -> None:
    """
    Fetch the contents of a URL and write it to target_path.
    This is a simple snapshot (HTML or JSON, whatever the server returns).
    """
    resp = requests.get(url, timeout=30)
    resp.raise_for_status()
    # Save as text, ignoring binary edge cases for simplicity.
    write_file(target_path, resp.text)


def get_most_recent_file(directory: Path) -> Optional[Path]:
    """
    Return the most recently modified file in a directory, or None.
    """
    files = list_dir(directory)
    if not files:
        return None
    return max(files, key=lambda p: p.stat().st_mtime)


# ==========
# DeepSeek Chat wrapper
# ==========

class DeepSeekClient:
    """
    Minimal DeepSeek-style chat client.
    Adjust the request format to match the actual API you use.
    """

    def __init__(self, api_key: str, base_url: str, model: str) -> None:
        if not api_key or not base_url:
            raise RuntimeError(
                "DEEPSEEK_API_KEY and DEEPSEEK_BASE_URL must be set in the environment."
            )
        self.api_key = api_key
        self.base_url = base_url.rstrip("/")
        self.model = model

    def chat(self, messages: List[dict], temperature: float = 0.3) -> str:
        """
        Send a chat completion request and return the assistant's message content.
        `messages` is a list of {role: "system"/"user"/"assistant", content: "..."}.
        """
        url = f"{self.base_url}/v1/chat/completions"  # adjust path if needed

        payload = {
            "model": self.model,
            "messages": messages,
            "temperature": temperature,
        }

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
        }

        resp = requests.post(url, headers=headers, data=json.dumps(payload), timeout=60)
        resp.raise_for_status()
        data = resp.json()

        # Adjust this to match DeepSeek's exact response schema
        try:
            return data["choices"][0]["message"]["content"]
        except (KeyError, IndexError) as e:
            raise RuntimeError(f"Unexpected DeepSeek response: {data}") from e


# ==========
# Context loading and prompt construction
# ==========

def load_system_docs() -> str:
    """
    Concatenate all system docs into one block the model can read.
    """
    docs = []
    for path in sorted(list_dir(SYSTEM_DIR)):
        docs.append(f"# SYSTEM DOC: {path.name}\n{read_file(path)}\n")
    return "\n\n".join(docs)


def load_last_week_brief() -> str:
    """
    Load the most recent brief from HISTORY_DIR, or an empty string if none.
    """
    latest = get_most_recent_file(HISTORY_DIR)
    if not latest:
        return ""
    return read_file(latest)


def load_raw_inputs() -> str:
    """
    Concatenate all raw competitor snapshots into one block.
    """
    parts = []
    for path in sorted(list_dir(RAW_DIR)):
        parts.append(f"# RAW INPUT: {path.name}\n{read_file(path)}\n")
    return "\n\n".join(parts)


def build_messages(
    competitor_urls: List[str],
) -> List[dict]:
    """
    Build the messages payload for DeepSeek based on the current context.
    """
    claude_md = read_file(CLAUDE_DOC) if CLAUDE_DOC.exists() else ""
    system_docs = load_system_docs()
    last_week = load_last_week_brief()
    raw_inputs = load_raw_inputs()

    today = datetime.date.today().isoformat()

    system_prompt = (
        "You are a Competitive Intelligence Analyst AI agent.\n\n"
        "You operate inside a structured environment with:\n"
        "- A CLAUDE.md brief that defines your role, goals, and output format.\n"
        "- System docs that describe our product, positioning, tone, and style.\n"
        "- Historical briefs to keep continuity and avoid repetition.\n"
        "- Raw snapshots of competitor websites and pricing pages.\n\n"
        "Always follow the output requirements in CLAUDE.md exactly.\n"
        "If a fact is not supported by the inputs, say 'unknown' rather than guessing.\n"
    )

    # User content bundles all context. In a more advanced setup, you could
    # expose these as tools/files instead of stuffing into text.
    user_content = f"""
Today: {today}

## CLAUDE.md (Environment Spec)

{claude_md}

## System Docs (Reference)

{system_docs}

## Last Week's Brief (if any)

{last_week or "[None – this is the first brief]"} 

## Raw Inputs (Competitor Snapshots)

{raw_inputs or "[None yet – raw directory is empty]"}

## Task

Create this week's competitor intelligence brief.

Competitor URLs for this run:
{chr(10).join(f"- {u}" for u in competitor_urls)}

Follow the workflow encoded in CLAUDE.md:
- Read system docs and last week's brief.
- Use the raw inputs as your primary evidence.
- Produce a single markdown brief with the sections and format defined in CLAUDE.md.
- Highlight notable changes vs last week where possible.

Reply with only the markdown content of this week's brief, nothing else.
"""

    return [
        {"role": "system", "content": system_prompt},
        {"role": "user", "content": user_content},
    ]


# ==========
# Main agent routine
# ==========

def run_competitor_brief_agent(competitor_urls: List[str]) -> Path:
    """
    Main entry point:
    - Ensure directories exist.
    - Fetch competitor URLs into RAW_DIR.
    - Call DeepSeek with all context.
    - Save the generated brief into HISTORY_DIR.
    - Return the path to the new brief.
    """
    ensure_directories()

    # 1) Fetch competitor pages into RAW_DIR
    for idx, url in enumerate(competitor_urls, start=1):
        filename = f"competitor_{idx}.txt"
        target_path = RAW_DIR / filename
        print(f"[agent] Fetching {url} -> {target_path}")
        fetch_url_to_file(url, target_path)

    # 2) Build messages for the model
    messages = build_messages(competitor_urls)

    # 3) Call DeepSeek
    client = DeepSeekClient(
        api_key=DEEPSEEK_API_KEY,
        base_url=DEEPSEEK_BASE_URL,
        model=DEEPSEEK_MODEL,
    )
    print("[agent] Calling DeepSeek chat API...")
    brief_markdown = client.chat(messages)

    # 4) Save the brief
    today = datetime.date.today().isoformat()
    brief_path = HISTORY_DIR / f"brief-{today}.md"
    write_file(brief_path, brief_markdown)
    print(f"[agent] Saved brief to {brief_path}")

    return brief_path


# ==========
# CLI entry
# ==========

if __name__ == "__main__":
    """
    Example usage:

    export DEEPSEEK_API_KEY="..."
    export DEEPSEEK_BASE_URL="..."  # e.g. your DeepSeek endpoint
    python competitor_brief_agent.py
    """
    # Example competitor URLs (replace with real ones)
    competitors = [
        "https://example.com/competitor-a",
        "https://example.com/competitor-b",
        "https://example.com/competitor-c",
    ]
    run_competitor_brief_agent(competitors)
```

  

How to wire this up in practice:

1. Create the folders and a first version of `ai/competitor-briefs/CLAUDE.md` with the role, goal, output format, and constraints (like in the earlier message).
2. Drop your product docs, tone guide, etc. into `ai/competitor-briefs/system/`.
3. Set `DEEPSEEK_API_KEY`, `DEEPSEEK_BASE_URL`, and optionally `DEEPSEEK_MODEL` in your environment to match DeepSeek.
4. Replace the three example competitor URLs in `__main__` with real ones.
5. Run `python competitor_brief_agent.py`. It will fetch pages, call DeepSeek, and write a `brief-YYYY-MM-DD.md` file into `history/`.

From there you can:

- Put this behind a cron or GitHub Actions job for a real weekly cadence.
- Extend the agent to also write into Notion, send email, or update a dashboard using more tools.