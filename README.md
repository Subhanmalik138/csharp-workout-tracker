# Workout Tracker CLI

A small, dependency-free command-line workout tracker in C# (.NET 8). Define
exercises, build reusable routines, log training sessions, and answer the
questions lifters actually ask: what is my best lift, how much volume did I do
this month, and how long is my current streak?

## Problem

Spreadsheets work until they don't — a missed column breaks the totals, and
there is no easy way to ask "what did I bench last month?" This tool keeps one
JSON document as the source of truth and derives everything (personal records,
volume, streaks) from the logged sets, so the answers are always consistent
with the log.

## Design decisions

1. **One JSON document, written atomically.** The store writes to a temp file
   and moves it over the real file, so a crash mid-save can never corrupt the
   database. No server, no database setup — the data file is portable.
2. **Domain logic separated from the CLI.** `TrackerService` works against an
   `IWorkoutStore` interface, so every rule (duplicate names, unknown
   exercises, invalid sets) is unit-testable without touching a real file.
3. **Streaks are derived, not stored.** A streak is counted from session dates
   on demand, so backfilling an old session automatically corrects history —
   a stored counter would drift.
4. **Volume is reps × weight, grouped by muscle.** Grouping by muscle group
   instead of by exercise keeps monthly reports readable when you rotate
   variations of the same movement.

## Usage

```bash
dotnet run --project src/WorkoutTracker -- add-exercise "Bench Press" Chest
dotnet run --project src/WorkoutTracker -- create-routine "Push Day" "Bench Press:3:8"
dotnet run --project src/WorkoutTracker -- log 2026-10-28 "Bench Press" 8 80 "Bench Press" 8 82.5
dotnet run --project src/WorkoutTracker -- history "Bench Press"
dotnet run --project src/WorkoutTracker -- prs
dotnet run --project src/WorkoutTracker -- stats 2026-10-01 2026-10-31
dotnet run --project src/WorkoutTracker -- streak
```

Commands: `add-exercise`, `list-exercises`, `create-routine`, `log`,
`history [exercise]`, `prs`, `stats [from] [to]`, `streak [date]`.

By default data lives in `~/.workout-tracker/data.json`; set
`WORKOUT_TRACKER_DATA` to point elsewhere (useful for tests and backups).

## Tests

```bash
dotnet test tests/WorkoutTracker.Tests
```

9 xUnit tests cover duplicate exercises/routines, session validation,
personal records, volume windows, streak gaps, persistence round-trips, and
per-exercise history ordering.

## Tech stack

- C# / .NET 8, no runtime dependencies
- xUnit + Microsoft.NET.Test.Sdk for tests
- `System.Text.Json` for storage

## License

MIT — see `LICENSE`.
