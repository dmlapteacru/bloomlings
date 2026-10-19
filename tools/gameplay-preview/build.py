#!/usr/bin/env python3
"""Build the standalone product preview with repository-owned assets and no network.

This does not alter game assets, level data, or any generated catalog. Run from any cwd:
    python3 tools/gameplay-preview/build.py
"""
from __future__ import annotations

import base64
import html
import json
import re
from pathlib import Path
from urllib.parse import quote

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
PLAN = ROOT / "product/plans/gameplay-evolution-v2.md"
OUT = ROOT / "product/previews/gameplay-evolution/index.html"
REPOSITORY = "https://github.com/dmlapteacru/bloomlings/blob/claude/great-darwin-6qrpj8/"


def data_uri(path: Path, mime: str) -> str:
    return f"data:{mime};base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def link_target(target: str) -> str:
    if target.startswith(("https://", "http://", "#")):
        return target
    filename, _, fragment = target.partition("#")
    resolved = (PLAN.parent / filename).resolve()
    if resolved == OUT:
        return "#journey"
    if not resolved.is_relative_to(ROOT):
        raise ValueError(f"Link outside repository: {target}")
    result = REPOSITORY + quote(resolved.relative_to(ROOT).as_posix(), safe="/")
    return result + ("#" + quote(fragment) if fragment else "")


def inline(text: str) -> str:
    # Tokenize before escaping; markup is only introduced by this renderer.
    pattern = r"(`[^`]+`|\*\*.+?\*\*|\[[^\]]+\]\([^)]+\))"
    parts = re.split(pattern, text)
    result = []
    for part in parts:
        if part.startswith("`") and part.endswith("`"):
            result.append("<code>" + html.escape(part[1:-1]) + "</code>")
        elif part.startswith("**") and part.endswith("**"):
            result.append("<strong>" + inline(part[2:-2]) + "</strong>")
        elif match := re.fullmatch(r"\[([^\]]+)\]\(([^)]+)\)", part):
            label, target = match.groups()
            result.append('<a href="' + html.escape(link_target(target), quote=True) + '">' + inline(label) + "</a>")
        else:
            result.append(html.escape(part))
    return "".join(result)


def render_markdown(source: str) -> str:
    """Render the small Markdown subset used in this exact plan, including every block."""
    lines = source.splitlines()
    blocks: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        if not line.strip():
            i += 1
            continue
        if line.startswith("```"):
            language = line[3:].strip()
            code = []
            i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                code.append(lines[i])
                i += 1
            if language == "mermaid":
                labels = re.findall(r"\w+\[([^\]]+)\]", "\n".join(code))
                labels = list(dict.fromkeys(labels))
                blocks.append('<div class="diagram" aria-label="Области и зависимости">' + '<b aria-hidden="true">→</b>'.join("<span>" + html.escape(x) + "</span>" for x in labels) + "</div>")
                blocks.append("<details><summary>Точная схема зависимостей</summary><pre>" + html.escape("\n".join(code)) + "</pre></details>")
            else:
                blocks.append("<pre><code>" + html.escape("\n".join(code)) + "</code></pre>")
            i += 1
            continue
        if match := re.match(r"^(#{1,6})\s+(.+)$", line):
            level, text = len(match[1]), match[2]
            slug = "plan-" + re.sub(r"[^\w\-]+", "-", text.lower()).strip("-")
            blocks.append(f'<h{level} id="{html.escape(slug)}">{inline(text)}</h{level}>')
            i += 1
            continue
        if line.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                row = [x.strip() for x in lines[i].strip().strip("|").split("|")]
                if not all(re.fullmatch(r":?-+:?", x.replace(" ", "")) for x in row):
                    rows.append(row)
                i += 1
            head, *body = rows
            table = '<div class="table-scroll" tabindex="0" role="region" aria-label="Таблица плана"><table><thead><tr>'
            table += "".join("<th>" + inline(c) + "</th>" for c in head) + "</tr></thead><tbody>"
            table += "".join("<tr>" + "".join("<td>" + inline(c) + "</td>" for c in row) + "</tr>" for row in body)
            blocks.append(table + "</tbody></table></div>")
            continue
        if re.match(r"^(?:- |\d+\. )", line):
            ordered = bool(re.match(r"^\d+\.", line))
            kind = "ol" if ordered else "ul"
            items = []
            while i < len(lines) and re.match(r"^\d+\. " if ordered else r"^- ", lines[i]):
                items.append(re.sub(r"^(?:- |\d+\. )", "", lines[i]))
                i += 1
            blocks.append(f"<{kind}>" + "".join("<li>" + inline(x) + "</li>" for x in items) + f"</{kind}>")
            continue
        paragraph = [line]
        i += 1
        while i < len(lines) and lines[i].strip() and not re.match(r"^(?:#|\||```|- |\d+\. )", lines[i]):
            paragraph.append(lines[i])
            i += 1
        blocks.append("<p>" + inline(" ".join(paragraph)) + "</p>")
    return "\n".join(blocks)


def script_json(value: object) -> str:
    # A closing script tag must not be emitted inside the JSON text node.
    return json.dumps(value, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c")


def main() -> None:
    hero_root = ROOT / "client/Assets/Bloomlings/Art/Heroes/Resources/HeroMotion"
    assets = {name: data_uri(hero_root / f"homehero-{name}-001.png", "image/png")
              for name in ("sprig", "bloom", "drop", "twig")}
    assets["garden"] = data_uri(ROOT / "client/Assets/Bloomlings/Art/Backgrounds/Resources/Backgrounds/gameplay-orchard.jpg", "image/jpeg")
    fonts = ROOT / "client/Assets/Bloomlings/UI/Fonts/Resources"
    css = (SOURCE / "style.css").read_text().replace("__FONT__", data_uri(fonts / "Nunito-SemiBold.ttf", "font/ttf"))
    css = css.replace("__FONT_BOLD__", data_uri(fonts / "Nunito-ExtraBold.ttf", "font/ttf"))
    plan = PLAN.read_text()
    replacements = {
        "/*__STYLE__*/": css,
        "/*__SCRIPT__*/": (SOURCE / "app.js").read_text(),
        "/*__ASSETS__*/": script_json(assets),
        "/*__PLAN_SOURCE__*/": script_json(plan),
        "/*__PLAN__*/": render_markdown(plan),
    }
    page = (SOURCE / "page.html").read_text()
    for marker, replacement in replacements.items():
        assert page.count(marker) == 1, marker
        page = page.replace(marker, replacement)
    assert not re.search(r"/\*__(?:STYLE|SCRIPT|ASSETS|PLAN|PLAN_SOURCE)__\*/", page)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(page)
    print(f"Built {OUT.relative_to(ROOT)}: {OUT.stat().st_size:,} bytes; no external runtime dependencies.")


if __name__ == "__main__":
    main()
