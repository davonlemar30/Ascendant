#!/usr/bin/env python3
"""Fail closed on empty, partial, or unsuccessful fixture/browser evidence."""
import argparse
import re
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('report', type=Path)
parser.add_argument('--fixture-log', type=Path)
parser.add_argument('--browser-log', type=Path)
args = parser.parse_args()
text = args.report.read_text()
lines = text.splitlines()
if not lines or not all(line.startswith('PASS: ') for line in lines):
    raise SystemExit('FAIL: missing, empty or unsuccessful validation evidence')
if args.browser_log and args.browser_log.read_text().rstrip('\n') != text.rstrip('\n'):
    raise SystemExit('FAIL: browser log differs from its report')
if args.fixture_log:
    counts = re.findall(r'\[SlicePlayValidation\] PASS: (\d+) checks\.', args.fixture_log.read_text())
    if counts != [str(len(lines))]:
        raise SystemExit('FAIL: fixture completion count differs from report')
print(f'PASS: verified {len(lines)} checks in {args.report}')
