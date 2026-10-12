# Security Policy

## Reporting a vulnerability

Open a **private security advisory** on GitHub. Don't open a public
issue for security reports.

What helps: the app version, Windows version, and steps to reproduce.
This is a local, read-only diagnostic tool, so describe what you found
plainly — most issues here will be about data handling rather than
remote exploitation.

## What counts

This app scans your device and exports reports containing device
identifiers (serials, MACs, UUIDs). The security-relevant surface:

- **Masking failures.** The app masks identifiers on a copy of the
  report before export bytes are written. If masked data leaks true
  values into an export, that's in scope.
- **Snapshot storage.** Scan history lives in a local SQLite database.
  If another local user can read snapshots they shouldn't see, that's
  in scope.
- **Unsafe handling of exported files** (path traversal on import,
  if an import path ever exists).

Not security issues:

- A scanner reporting `Unavailable` or low-confidence data. That's
  the honesty policy working, not a vulnerability.
- Anything in third-party NuGet packages — report upstream.
- Social engineering or anything targeting me personally.

## Scope

In scope: this repo's own code — the app, scanners, exporters,
persistence.

Out of scope: third-party libraries, the .NET runtime / Windows
itself, the GitHub release infrastructure.

## What to expect

Indie project, no bug bounty, no SLA. I'll acknowledge your report,
fix confirmed issues, and credit you unless you'd rather stay
anonymous.
