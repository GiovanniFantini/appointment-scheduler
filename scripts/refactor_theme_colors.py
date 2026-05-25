"""
Refactor mirato: sostituisce i colori hex hardcoded nei CSS delle 3 app
con i CSS variables del design system shared-ui (--su-*).

Strategia: regex per propriet`a -> hex -> token. Niente sostituzioni cieche
per evitare di toccare gradient stops, rgba(), o classi del FullCalendar
che hanno semantica propria.

Uso:
  python scripts/refactor_theme_colors.py
  python scripts/refactor_theme_colors.py --dry  # mostra diff senza scrivere
"""
from __future__ import annotations
import re
import sys
from pathlib import Path

ROOT = Path(__file__).parent.parent / "frontend"
APPS = ["admin-app", "merchant-app", "employee-app"]

# Mappa hex (lowercase, normalizzato) -> token CSS var.
# Non vengono toccati gli accenti (#6366f1, #ef4444, #10b981, #f59e0b, #00d4ff, ecc.)
# perch'e funzionano sia su dark sia su light.
TOKEN_MAP: dict[str, str] = {
    # background scuri / superfici
    "#0a0a0f": "var(--su-surface-bg)",
    "#0f172a": "var(--su-surface-bg)",
    "#13131a": "var(--su-surface-bg)",
    "#111827": "var(--su-surface-bg)",
    "#1a1a24": "var(--su-surface-elevated)",
    "#1e293b": "var(--su-surface-elevated)",
    "#1f2937": "var(--su-surface-elevated)",
    # borders / divider scuri
    "#334155": "var(--su-border-strong)",
    "#2a2a3a": "var(--su-border-strong)",
    "#374151": "var(--su-border-strong)",
    "#475569": "var(--su-text-muted)",
    # text colors (slate scale chiara)
    "#64748b": "var(--su-text-muted)",
    "#94a3b8": "var(--su-text-secondary)",
    "#cbd5e1": "var(--su-text-secondary)",
    "#e2e8f0": "var(--su-border)",  # tipicamente usato come border chiaro
    "#f1f5f9": "var(--su-text-primary)",
    "#f8fafc": "var(--su-surface)",  # solo se usato come bg
}

# Propriet`a CSS dove `e sicuro sostituire: prendono color, non sono dentro a gradient
SAFE_PROPS = (
    r"(?P<prop>"
    r"(?:background|background-color|color|border|border-top|border-right|"
    r"border-bottom|border-left|border-color|border-top-color|border-right-color|"
    r"border-bottom-color|border-left-color|outline|outline-color|"
    r"caret-color|fill|stroke|column-rule|column-rule-color|"
    r"-webkit-text-fill-color|text-decoration-color)"
    r"\s*:\s*)"
)

# Hex token alla fine: solitamente seguito da ; o } o spazi, MA i border hanno
# anche width+style prima del color. Per sicurezza facciamo match flessibile.
HEX_RE = re.compile(SAFE_PROPS + r"(?P<val>[^;{}\n]*?)(?P<hex>#[0-9a-fA-F]{6})\b", re.IGNORECASE)


def replace_in_text(text: str) -> tuple[str, int]:
    count = 0

    def sub(m: re.Match[str]) -> str:
        nonlocal count
        hexval = m.group("hex").lower()
        if hexval not in TOKEN_MAP:
            return m.group(0)
        replacement = TOKEN_MAP[hexval]
        count += 1
        return f"{m.group('prop')}{m.group('val')}{replacement}"

    new_text = HEX_RE.sub(sub, text)
    return new_text, count


def main() -> int:
    dry = "--dry" in sys.argv
    total_files = 0
    total_subs = 0

    for app in APPS:
        app_root = ROOT / app / "src"
        if not app_root.exists():
            print(f"  SKIP {app}: dir not found")
            continue
        css_files = sorted(app_root.rglob("*.css"))
        for f in css_files:
            original = f.read_text(encoding="utf-8")
            new_text, n = replace_in_text(original)
            if n == 0:
                continue
            total_files += 1
            total_subs += n
            rel = f.relative_to(ROOT)
            print(f"  {n:4d}  {rel}")
            if not dry:
                f.write_text(new_text, encoding="utf-8")

    print()
    print(f"{'DRY RUN: ' if dry else ''}{total_subs} sostituzioni in {total_files} file")
    return 0


if __name__ == "__main__":
    sys.exit(main())
