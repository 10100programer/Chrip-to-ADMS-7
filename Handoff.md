# Handoff: chirp2ftm400

> **Read `PRD.md` in this directory first.** This file is the implementation kickoff and assumes you've read the PRD.

## TL;DR

Build a .NET 10 / C# 13 CLI tool that converts CHIRP CSV memory channel files into the Yaesu FTM-400D ADMS-4 CSV format. Use **Spectre.Console** + **Spectre.Console.Cli** for the UX. Use **CsvHelper** for parsing.

Sample input/output files are in `samples/`:
- `samples/FTM-400-Repeater-CanadateV2_1_.csv` — real CHIRP export (~240 channels). This is the **primary fixture** for snapshot testing.
- `samples/FTM-400D_Untitled1.csv` — the FTM-400D target format (3 populated rows + 497 empty rows). This is the **schema reference** — your output's empty-row formatting must match this byte-for-byte.

## Plan of Attack

Build it in this order. Don't move to the next phase until the prior one has tests passing.

### Phase 1 — Scaffolding
1. Create the solution and project structure per PRD §12.
2. `dotnet new sln`, `dotnet new console`, `dotnet new xunit` for tests.
3. Add NuGet refs: `Spectre.Console`, `Spectre.Console.Cli`, `CsvHelper`. Test project: `FluentAssertions`.
4. Wire up `CommandApp` in `Program.cs` with three placeholder commands: `convert`, `validate`, `inspect`.
5. Set `TargetFramework` to `net10.0`. `Nullable enable`, `ImplicitUsings enable`, `LangVersion latest`.

### Phase 2 — Models + Parser
1. Build `ChirpChannel` model — all 21 columns from CHIRP CSV (PRD §7.1). Use nullable strings where CHIRP can leave blanks.
2. Build `Ftm400Channel` model — 17 fields (PRD §7.2). Use `Channel = int` and remaining fields as nullable typed values where it helps formatting.
3. `ChirpCsvReader`: use `CsvHelper` with `CsvConfiguration` set for header validation. Return `IReadOnlyList<ChirpChannel>` + list of parse warnings.
4. `Ftm400CsvWriter`: emit exactly `--max-channels` rows. Empty rows are `{n},,,,,,,,,,,,,,0,,0`. Populated rows formatted per PRD §7.2 — pay special attention to: 5-decimal frequencies, `KHz` suffix with no space, `T SQL` with space, ` Hz` suffix with space, `1500 Hz` default for User CTCSS.
5. Tests: round-trip the provided sample through reader → writer, parse the writer output back into a generic CSV row reader, assert column counts and sentinel values.

### Phase 3 — Mappers
Implement one mapper class per concern, each fully unit-tested before integration:

1. `FrequencyMapper` — handles RX, TX, Offset, OffsetDirection from CHIRP Frequency + Duplex + Offset. Cover all 5 Duplex variants (PRD §8.2).
2. `ModeMapper` — CHIRP Mode → FTM-400D Mode (PRD §8.3). Returns mapped value + optional warning.
3. `ToneMapper` — CHIRP Tone + CrossMode + rToneFreq + cToneFreq + DtcsCode → FTM-400D ToneMode + CTCSS + DCS (PRD §8.4).
4. `PowerMapper` — CHIRP Power string → HIGH/MID/LOW (PRD §8.5).
5. `StepMapper` — CHIRP TStep → FTM-400D Step with snap-to-valid (PRD §8.6).
6. `SkipMapper` — CHIRP Skip → FTM-400D Skip (PRD §8.7).
7. `NameMapper` — CHIRP Name → 8-char sanitized tag with substitution tracking (PRD §8.8).

Each mapper returns a small result type containing the value + zero-or-more `ValidationWarning`s. Don't throw for recoverable problems.

### Phase 4 — Channel Orchestration
1. `ChannelMapper` composes the field mappers into a full `ChirpChannel` → `Ftm400Channel` conversion. Aggregates warnings.
2. `ChirpValidator` enforces PRD §9 rules V1–V9. Run before mapping; can drop rows.
3. Channel number assignment: default = `Location + 1`; `--renumber` overrides with sequential.

### Phase 5 — CLI / Spectre.Console UX
1. `ConvertCommand : Command<ConvertSettings>` with all options from PRD §6.2.
2. Banner via `FigletText` with muted color (e.g., `Color.SteelBlue`). Suppress on `--quiet`.
3. File-read phase: `AnsiConsole.Status().Start(...)`.
4. Conversion phase: `AnsiConsole.Progress()` with one task showing channel count.
5. Warning rendering: collect during conversion, print at the end as a `Table` with columns `Ch | Name | Warning`. Yellow `⚠` glyph.
6. Final summary `Table` (PRD §10.2). Exit code logic per PRD §6.3.
7. `InspectCommand`: render parsed channels as a paged `Table`. Use `--page-size`.
8. `ValidateCommand`: same parse + validate pipeline as `convert`, but no writer.

### Phase 6 — Testing
1. Unit tests for every mapper (PRD §14).
2. Snapshot test: convert `samples/FTM-400-Repeater-CanadateV2_1_.csv` → generate the expected output file ONCE manually, hand-verify against ADMS-4 expectations, commit as `tests/Chirp2Ftm400.Tests/Fixtures/expected_ftm400_full.csv`. Test asserts byte equality.
3. CLI smoke tests using `CommandApp.Run(...)` with arg arrays.
4. Target ≥ 85% line coverage on `src/`.

### Phase 7 — Packaging
1. `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
2. Repeat for `linux-x64` and `osx-arm64`.
3. README with installation + usage + sample command. Include an "Output format" section linking back to PRD §7.2.

## Implementation Notes & Gotchas

- **No spaces in `KHz` suffix**, but there IS a space in `T SQL` and before `Hz`. Triple-check by diffing against the sample file's row 1: `1,144.00000,144.00000,0.60000,OFF,FM,,OFF,100.0 Hz,023,1500 Hz,HIGH,OFF,5.0KHz,0,,0`.
- **Trailing empty fields:** CHIRP rows have trailing commas (4 D-Star fields, usually empty). CsvHelper handles this; don't strip them.
- **Decimal formatting:** Use `InvariantCulture` everywhere. A European locale would break the frequencies otherwise.
- **Sparse Locations:** CHIRP Location of 0 maps to FTM-400D channel 1. Gaps in CHIRP locations leave empty FTM-400D rows in the corresponding slots (this is the desired default; preserve the user's channel layout). With `--renumber`, pack them sequentially instead.
- **Channel 0 in CHIRP:** Real CHIRP files use Location starting at 0. The sample's row 1 is Location 0 ("APRS"). This must become FTM-400D channel 1.
- **Comments with embedded commas/quotes:** The sample CHIRP file has long comments with commas and HTML in them (see row for W5DOC at Location 18). `CsvHelper` handles this if quoted correctly. Drop these from the output.
- **The reserved `0` columns (15 and 17):** Always emit `0`, even for empty channels. Look at row 4 of the sample target: `4,,,,,,,,,,,,,,0,,0` — only the channel number, the two `0`s, and the empty comment field are non-blank.
- **`AUTO` vs `DN`:** PRD §8.3 maps CHIRP `DN` to `AUTO`. This is the safer choice (analog fallback). Don't second-guess — implement as specified.
- **Defensive defaults:** When CHIRP has empty cells for tone/power/step, fall back to PRD-specified defaults (`100.0 Hz`, `HIGH`, `5.0KHz`) — but only when the channel uses those features. Don't emit a CTCSS frequency for a Tone Mode of `OFF`.

## What "Done" Looks Like

You should be able to run:

```
chirp2ftm400 convert samples/FTM-400-Repeater-CanadateV2_1_.csv -o /tmp/out.csv --overwrite
```

…and see:

- The banner.
- A status indicator while reading.
- A progress bar during conversion.
- A warning table with the expected ~10–15 warnings (cross-mode rows, name truncations, etc.).
- A summary table.
- Exit code 1 (warnings present).
- An output file `/tmp/out.csv` with exactly 500 rows that loads cleanly into ADMS-4.

## Things NOT to Do

- Don't add a GUI.
- Don't add support for other Yaesu radios in this pass.
- Don't try to preserve the CHIRP Comment field in the output. The FTM-400D doesn't surface it.
- Don't depend on PowerShell-isms or platform-specific paths. This needs to run on Linux for the homelab and Windows for the work laptop.
- Don't use `Console.WriteLine` directly — go through `AnsiConsole` or a `IConsoleReporter` abstraction so it can be mocked in tests.
- Don't pull in Newtonsoft.Json, MediatR, AutoMapper, or any framework that's overkill for a CSV converter. Keep dependencies minimal.

## Reference Files in This Handoff

- `PRD.md` — the spec. Source of truth for behavior.
- `samples/FTM-400-Repeater-CanadateV2_1_.csv` — real CHIRP input (~240 rows).
- `samples/FTM-400D_Untitled1.csv` — target format reference (3 populated + 497 empty rows).

## Questions to Ping Back

If any of these surface during implementation, ask before assuming:

1. Should `--max-channels` ever exceed 500? (FTM-400D hardware limit is 500; PMS/scan channels are separate.)
2. Confirm ADMS-4 accepts the exact whitespace conventions in §7.2 (T SQL with space, KHz without). If it rejects either, file format may need adjustment.
3. PRD §17 lists open questions that may surface during first real-hardware import. None block initial work.

Good hunting. Ship phase 1 first, then iterate.
