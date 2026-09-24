#!/usr/bin/env python3
"""Prints every player-facing Turkish string literal (UI + generated content) with
interpolations replaced by samples, one per line, for the --loc-check pass."""
import re, pathlib
root = pathlib.Path(__file__).resolve().parents[2] / "Assets/_Project/Scripts"
files = list((root / "UI").glob("*.cs")) + [root / "Editor/ContentBuilder.cs"]
lit = re.compile(r'(\$?)"((?:[^"\\]|\\.)*)"')
samples = [("displayName", "Tüy Blaster"), ("perkText", "Kedi. Sersemletme süresi 2 kat."),
           ("description", "+%3 hasar"), ("chapterName", "Şekerkamışı"), ("state", "  (açık)")]
skip_calls = ("Node(", "Panel(", "FindProperty", "Debug.", "Tooltip", "Header", "SaveAsset", "PathFor",
              "Sprites[", "Shader.Find", "GameObject(", "Find(\"", "WeaponPrefab", "EnemyPrefab")
turkish = re.compile(r"[çğıöşüÇĞİÖŞÜâ]")
seen = set()
for f in files:
    for line in f.read_text(encoding="utf-8").split("\n"):
        s = line.strip()
        if s.startswith("//") or s.startswith("///") or s.startswith("["):
            continue
        for m in lit.finditer(line):
            text = m.group(2)
            before = line[:m.start()]
            if any(c in before[-40:] for c in skip_calls) or "Set(" in before[-12:] or "SetArray(" in before[-16:]:
                continue
            if m.group(1):
                def sub(mm):
                    expr = mm.group(1)
                    for key, val in samples:
                        if key in expr:
                            return val
                    return "7"
                text = re.sub(r"\{([^{}]+)\}", sub, text)
            if not turkish.search(text) or text in seen:
                continue
            seen.add(text)
            print(text)
