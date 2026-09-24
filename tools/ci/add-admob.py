#!/usr/bin/env python3
"""Adds Google Mobile Ads (and its External Dependency Manager) from OpenUPM to
Packages/manifest.json at build time, pinned to the latest published version.
Fails soft: without network or on any error the build proceeds without ads
(Loc/Ads code falls back to the instant-reward placeholder)."""
import json, sys, urllib.request

REGISTRY = "https://package.openupm.com"
PACKAGES = ["com.google.ads.mobile", "com.google.external-dependency-manager"]
PIN = {}  # e.g. {"com.google.ads.mobile": "10.4.0"} to freeze a known-good version

def latest(name):
    with urllib.request.urlopen(f"{REGISTRY}/{name}", timeout=30) as r:
        return json.load(r)["dist-tags"]["latest"]

try:
    path = "Packages/manifest.json"
    manifest = json.load(open(path))
    deps = manifest["dependencies"]
    for name in PACKAGES:
        deps[name] = PIN.get(name) or latest(name)
        print(f"{name} {deps[name]}")
    regs = manifest.setdefault("scopedRegistries", [])
    if not any(r.get("url") == REGISTRY for r in regs):
        regs.append({"name": "OpenUPM", "url": REGISTRY, "scopes": ["com.google"]})
    json.dump(manifest, open(path, "w"), indent=2)
except Exception as e:  # noqa: BLE001 — never block the APK on ads
    print(f"::warning::AdMob not added ({e}); building without ads.")
    sys.exit(0)
