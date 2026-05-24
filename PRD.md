# PRD: CHIRP → FTM-400D CSV Converter

**Project codename:** `chirp2ftm400`
**Owner:** Alex
**Status:** Draft v1.0
**Target stack:** .NET 10, C# 13, Spectre.Console, Spectre.Console.Cli

---

## 1. Overview

A cross-platform C# command-line utility that converts CHIRP-formatted memory channel CSV files into the CSV format required by Yaesu's ADMS-4 programming software for the FTM-400D/DR dual-band mobile transceiver.

The tool reads a CHIRP export (typically generated from CHIRP-next, RepeaterBook downloads, or hand-edited spreadsheets), maps fields to the FTM-400D's flat 17-column row format, validates and warns on lossy conversions, and writes a 500-channel CSV that ADMS-4 can import without manual cleanup.

## 2. Background

CHIRP is the de facto open-source tool for managing amateur radio memory channels, with broad community support and CSV import/export. Yaesu's ADMS-4 software is the only officially supported way to bulk-program the FTM-400D and uses a proprietary, undocumented CSV layout. Manually re-keying repeaters between the two is tedious and error-prone — especially for users maintaining large repeater lists (200+ channels) across multiple radios.

This tool eliminates the manual step.

## 3. Goals

- **G1:** Faithfully map every CHIRP field that has an FTM-400D equivalent.
- **G2:** Produce a CSV that imports into ADMS-4 without errors or manual fix-up.
- **G3:** Surface lossy or ambiguous conversions to the user via warnings.
- **G4:** Pleasant, modern CLI UX using Spectre.Console (tables, progress, color).
- **G5:** Single self-contained executable for Windows, Linux, and macOS.

## 4. Non-Goals

- GUI. CLI only.
- Reverse conversion (FTM-400D → CHIRP).
- Support for other Yaesu radios (FTM-300D, FT3D, FT5D, etc.) — may be a future expansion.
- Direct radio programming (no serial/USB cable communication).
- Editing/merging multiple CHIRP files.
- D-Star (DV mode) support — the FTM-400D is a C4FM/analog radio, not D-Star.

## 5. Users

Licensed amateur radio operators with an FTM-400D/DR who maintain channel lists in CHIRP-compatible repositories (RepeaterBook, RFinder, hand-maintained CSVs) and want to load them into ADMS-4 with a single command.

## 6. Functional Requirements

### 6.1 CLI Commands

Primary command (default if no command specified):

```
chirp2ftm400 convert <input.csv> [output.csv] [options]
```

Additional commands:

```
chirp2ftm400 validate <input.csv>     # Parse + validate CHIRP file, no output
chirp2ftm400 inspect <input.csv>      # Show preview table of parsed channels
chirp2ftm400 --version
chirp2ftm400 --help
```

### 6.2 Options for `convert`

| Option | Default | Description |
|---|---|---|
| `-o, --output <path>` | `<input>.ftm400.csv` | Output file path |
| `--max-channels <n>` | `500` | Output row count (FTM-400D supports 500) |
| `--renumber` | `false` | Renumber channels sequentially 1..N instead of using CHIRP Location |
| `--start-channel <n>` | `1` | First output channel number (only with `--renumber`) |
| `--strict` | `false` | Treat warnings as errors (non-zero exit) |
| `--default-power <H/M/L>` | `HIGH` | Power level when CHIRP has none |
| `--default-step <khz>` | `5.0` | Step when CHIRP has none |
| `--overwrite` | `false` | Allow overwriting an existing output file |
| `-v, --verbose` | `false` | Print every channel conversion |
| `-q, --quiet` | `false` | Suppress non-error output |

### 6.3 Exit Codes

- `0` — Success, zero warnings
- `1` — Success with warnings (or strict-mode warnings escalated)
- `2` — Input file error (missing, unreadable, malformed)
- `3` — Output file error (write failure, exists w/o --overwrite)
- `4` — Validation failure (no recoverable channels)

## 7. Format Specifications

### 7.1 CHIRP CSV (Source)

UTF-8, comma-separated, with header row. 21 columns:

```
Location,Name,Frequency,Duplex,Offset,Tone,rToneFreq,cToneFreq,
DtcsCode,DtcsPolarity,RxDtcsCode,CrossMode,Mode,TStep,Skip,Power,
Comment,URCALL,RPT1CALL,RPT2CALL,DVCODE
```

Notable column semantics:

- **Location:** Integer channel index. May be sparse (0, 1, 2, 11, 12, ...).
- **Frequency:** RX frequency in MHz with 6 decimals (e.g., `147.200000`).
- **Duplex:** `""` (simplex), `+`, `-`, `split`, or `off`.
- **Offset:** Offset in MHz with 6 decimals. For `split`, this field holds the absolute TX frequency.
- **Tone:** `""` (none), `Tone`, `TSQL`, `DTCS`, or `Cross`.
- **rToneFreq:** TX CTCSS tone in Hz (e.g., `141.3`).
- **cToneFreq:** RX CTCSS tone in Hz.
- **DtcsCode:** TX DCS code, 3-digit zero-padded octal (e.g., `023`).
- **CrossMode:** Active when `Tone=Cross`. Examples: `Tone->Tone`, `->Tone`, `Tone->DTCS`.
- **Mode:** `FM`, `NFM`, `WFM`, `AM`, `DN` (C4FM Digital Narrow), `DV` (D-Star, not supported).
- **TStep:** Step in kHz with 2 decimals (e.g., `5.00`, `12.50`).
- **Skip:** `""`, `S` (skip), `P` (priority/PSCAN).
- **Power:** Wattage string (`50W`, `25W`, `5W`, `1W`).
- **URCALL/RPT1CALL/RPT2CALL/DVCODE:** D-Star fields, ignored by this tool.

### 7.2 FTM-400D ADMS-4 CSV (Target)

UTF-8, comma-separated, **no header row**, always exactly 500 rows (one per channel slot).

17 columns per row:

| # | Field | Example (populated) | Example (empty) | Notes |
|---|---|---|---|---|
| 1 | Channel Number | `1` | `4` | 1–500 |
| 2 | RX Frequency (MHz) | `144.39000` | `` | 5 decimal places |
| 3 | TX Frequency (MHz) | `144.39000` | `` | 5 decimal places; same as RX unless split |
| 4 | Offset Frequency (MHz) | `0.60000` | `` | 5 decimal places |
| 5 | Offset Direction | `OFF` / `-RPT` / `+RPT` / `-/+` | `` | |
| 6 | Operating Mode | `FM` / `AM` / `NFM` / `AUTO` / `DN` | `` | |
| 7 | Tag/Name | `KARS2M` | `` | Max 8 chars, alphanumeric + space |
| 8 | Tone Mode | `OFF` / `TONE` / `T SQL` / `DCS` / `REV TONE` | `` | Note the space in `T SQL` |
| 9 | CTCSS Frequency | `141.3 Hz` | `` | Trailing ` Hz`, one decimal |
| 10 | DCS Code | `023` | `` | 3-digit zero-padded |
| 11 | User CTCSS | `1500 Hz` | `` | Default `1500 Hz` unless customized |
| 12 | TX Power | `HIGH` / `MID` / `LOW` | `` | |
| 13 | Scan Skip | `OFF` / `SKIP` / `SCAN` | `` | `SCAN` = PSCAN priority |
| 14 | Step | `5.0KHz` / `12.5KHz` / `25.0KHz` | `` | One decimal, suffix `KHz` (no space) |
| 15 | Reserved | `0` | `0` | Always `0` |
| 16 | Comment | `` | `` | Usually empty; not surfaced in radio UI |
| 17 | Reserved | `0` | `0` | Always `0` |

Empty channel slots **must** still emit a row containing: channel number, 13 empty fields, `0`, empty, `0`.

## 8. Field Mapping Specification

### 8.1 Channel Number

- Default: FTM-400D channel = CHIRP Location + 1 (CHIRP is 0-indexed, FTM-400D is 1-indexed).
- With `--renumber`: assign sequential channel numbers starting at `--start-channel`, in input order.
- Channels beyond `--max-channels` produce a warning and are dropped.

### 8.2 Frequencies

| CHIRP Duplex | RX Freq | TX Freq | Offset Freq | Offset Direction |
|---|---|---|---|---|
| `""` (simplex) | Frequency | Frequency | `0.00000` | `OFF` |
| `+` | Frequency | Frequency | Offset | `+RPT` |
| `-` | Frequency | Frequency | Offset | `-RPT` |
| `split` | Frequency | **Offset value** | `\|TX − RX\|` | `+RPT` if TX>RX else `-RPT` |
| `off` | Frequency | Frequency | `0.00000` | `OFF` + emit warning (TX-disabled not directly representable) |

All frequencies formatted to **5 decimal places**.

### 8.3 Operating Mode

| CHIRP Mode | FTM-400D Mode | Notes |
|---|---|---|
| `FM` | `FM` | Wide FM analog |
| `NFM` | `NFM` | Narrow FM |
| `WFM` | `FM` | Warn — FTM-400D has no broadcast wideband |
| `AM` | `AM` | Air band etc. |
| `DN` | `AUTO` | C4FM digital; AUTO lets radio fall back to analog |
| `DV` | `FM` | **Warn — D-Star not supported on FTM-400D; falls back to FM** |
| (unknown) | `FM` | Warn |

### 8.4 Tone Mode and Tones

CHIRP `Tone` column drives FTM-400D `Tone Mode`:

| CHIRP Tone | FTM-400D Tone Mode | Tones Used |
|---|---|---|
| `""` | `OFF` | none |
| `Tone` | `TONE` | rToneFreq → CTCSS field |
| `TSQL` | `T SQL` | cToneFreq → CTCSS field |
| `DTCS` | `DCS` | DtcsCode → DCS field |
| `Cross` | (see 8.4.1) | depends on CrossMode |

#### 8.4.1 Cross Mode Handling

CHIRP `Cross` supports asymmetric encode/decode combinations. Map to the closest FTM-400D equivalent and emit a warning:

| CrossMode | FTM-400D Tone Mode | Behavior |
|---|---|---|
| `Tone->Tone` | `T SQL` | Use rToneFreq for both TX and RX (lossy if cToneFreq differs) |
| `->Tone` | `T SQL` | Use cToneFreq; warn that TX has no tone (not exactly representable) |
| `Tone->` | `TONE` | Use rToneFreq; RX squelch open |
| `DTCS->DTCS` | `DCS` | Use DtcsCode |
| `Tone->DTCS` / `DTCS->Tone` / mixed | `T SQL` | Warn — fall back to TSQL with rToneFreq |

**CTCSS Frequency** (col 9): Always emit `{freq} Hz` with one decimal place. Default `100.0 Hz` if empty.

**DCS Code** (col 10): Pass-through 3-digit `DtcsCode`. Default `023` if empty.

**User CTCSS** (col 11): Always `1500 Hz` (FTM-400D factory default). Not derivable from CHIRP.

### 8.5 Power

| CHIRP Power | FTM-400D Power |
|---|---|
| `50W`, blank, missing | `HIGH` |
| `25W`, `20W`, `10W` | `MID` |
| `5W`, `1W`, anything ≤ 5W | `LOW` |

Override default with `--default-power`.

### 8.6 Step

CHIRP `TStep` like `5.00` → FTM-400D `5.0KHz` (one decimal, no space, `KHz` suffix).

Valid FTM-400D steps: `5.0`, `6.25`, `8.33`, `10.0`, `12.5`, `15.0`, `20.0`, `25.0`, `50.0`, `100.0` kHz. If CHIRP value doesn't match, snap to nearest valid and warn.

### 8.7 Skip / Scan

| CHIRP Skip | FTM-400D Skip |
|---|---|
| `""` | `OFF` |
| `S` | `SKIP` |
| `P` | `SCAN` (priority scan) |

### 8.8 Tag/Name

- Trim CHIRP `Name` to first 8 characters.
- Replace any disallowed characters (anything outside `A-Z 0-9 -`) with `-` after uppercasing.
- Emit a warning when truncation or substitution occurs.

### 8.9 Comment

Drop CHIRP `Comment` (column 16 in FTM-400D is reserved/unused in the radio UI and including arbitrary text risks ADMS-4 parse issues). Emit a debug-level note when verbose, not a warning.

## 9. Validation Rules

The tool MUST validate the following and emit warnings (or errors in `--strict` mode):

- **V1:** Frequency within amateur or general coverage bands the FTM-400D supports (RX: 108–999 MHz with gaps; TX: ham segments only). Out-of-range emits warning, channel still written.
- **V2:** Required CHIRP columns present in header. Missing core columns (Location, Frequency, Mode) → fatal.
- **V3:** Duplicate Location values → warn, last wins.
- **V4:** Location < 0 or ≥ max-channels → drop with warning.
- **V5:** Empty Frequency → drop row with warning.
- **V6:** DV mode → warn (downgraded to FM).
- **V7:** Cross mode → warn (best-effort mapping).
- **V8:** Name truncation/substitution → warn.
- **V9:** Step snap to nearest valid → warn.

## 10. CLI UX with Spectre.Console

### 10.1 Banner

On startup print a small ASCII banner (use `FigletText` with a muted color) showing program name and version. Suppress with `--quiet`.

### 10.2 Conversion Flow

```
  ┌─ chirp2ftm400 v1.0.0 ──────────────────────────────────┐

  ⌛ Reading CHIRP file: repeaters.csv
  ✓ Parsed 137 channels from 21-column CHIRP CSV

  ⌛ Converting channels...
  [████████████████████████████] 100% (137/137)

  ⚠ Warnings (3):
    Ch 18  W5DOC     Name truncated: "W5DOC-AB" → "W5DOC-A"
    Ch 33  W5TFD     Tone mode 'DTCS' with TX/RX cross — using DCS
    Ch 75  AUSTIN    Mode 'DN' → AUTO (C4FM digital)

  ✓ Wrote 500 rows to repeaters.ftm400.csv

  Summary
  ┌──────────────────────┬───────┐
  │ Channels converted   │   137 │
  │ Channels skipped     │     0 │
  │ Warnings             │     3 │
  │ Output rows (total)  │   500 │
  └──────────────────────┴───────┘
```

### 10.3 Spectre.Console Components to Use

- **`AnsiConsole.Status()`** for the "Reading..." and "Converting..." phases.
- **`AnsiConsole.Progress()`** with one task per phase for the channel loop.
- **`Table`** for the summary and for `inspect` command output.
- **`Markup`** with semantic colors: green ✓ for success, yellow ⚠ for warnings, red ✗ for errors.
- **`FigletText`** for the banner.
- **`Spectre.Console.Cli`** for command structure (`CommandApp`, `Command<TSettings>`).

### 10.4 `inspect` Command Output

Render a table showing: Channel, Name, RX, TX, Mode, Tone, Power. Pagination via `--page-size <n>` if more than 50 rows.

## 11. Error Handling

| Class | Examples | Behavior |
|---|---|---|
| Fatal input | File not found, unreadable, missing required columns | Red ✗ message, exit 2 |
| Fatal output | Output exists w/o `--overwrite`, write failure, no disk space | Red ✗ message, exit 3 |
| Per-row warning | All cases in §9 | Yellow ⚠ message under conversion progress, continue |
| Per-row error | Frequency parse failure | Yellow ⚠ message, skip row, continue |
| Unexpected | Anything else | Red ✗ stack trace (only with `--verbose`), exit 1 |

All warnings and errors include the channel number and CHIRP Name for traceability.

## 12. Project Structure

```
chirp2ftm400/
├── src/
│   └── Chirp2Ftm400/
│       ├── Chirp2Ftm400.csproj
│       ├── Program.cs
│       ├── Commands/
│       │   ├── ConvertCommand.cs
│       │   ├── ValidateCommand.cs
│       │   └── InspectCommand.cs
│       ├── Models/
│       │   ├── ChirpChannel.cs
│       │   └── Ftm400Channel.cs
│       ├── Parsing/
│       │   ├── ChirpCsvReader.cs
│       │   └── Ftm400CsvWriter.cs
│       ├── Mapping/
│       │   ├── ChannelMapper.cs
│       │   ├── ToneMapper.cs
│       │   ├── PowerMapper.cs
│       │   └── ModeMapper.cs
│       ├── Validation/
│       │   ├── ChirpValidator.cs
│       │   └── ValidationWarning.cs
│       └── Ui/
│           ├── Banner.cs
│           └── ConsoleReporter.cs
├── tests/
│   └── Chirp2Ftm400.Tests/
│       ├── Chirp2Ftm400.Tests.csproj
│       ├── Parsing/
│       ├── Mapping/
│       └── Fixtures/
│           ├── sample_chirp_small.csv
│           └── expected_ftm400_small.csv
├── samples/
│   ├── FTM-400-Repeater-CanadateV2.csv      (provided)
│   └── FTM-400D_Untitled1.csv               (provided, target reference)
├── README.md
└── chirp2ftm400.sln
```

## 13. Dependencies

- `Spectre.Console` (latest, 0.49+)
- `Spectre.Console.Cli` (latest)
- `CsvHelper` (33+) for robust CSV parsing
- `xunit` + `FluentAssertions` for tests

## 14. Testing Requirements

- **Unit tests** for every mapper (mode, tone, power, frequency, skip, step, name).
- **Snapshot test** comparing converted output of provided sample (`FTM-400-Repeater-CanadateV2.csv`) against a hand-verified golden file (`expected_ftm400_full.csv`) — generate this file once during initial development and commit it.
- **Round-trip test** for representative rows: build a `ChirpChannel`, convert to `Ftm400Channel`, assert field values.
- **Edge case tests:**
  - Sparse Location values (0, 1, 11, 12, ...)
  - All Duplex variants including `split` and `off`
  - All Tone variants including all CrossMode combinations
  - Names with special characters, with > 8 chars, with leading/trailing spaces
  - Frequencies at band edges
  - Empty fields, malformed rows
- **CLI smoke test** exercising each command via `CommandApp.Run`.

Target ≥ 85% line coverage on `src/Chirp2Ftm400` (excluding `Program.cs`).

## 15. Packaging & Distribution

- `dotnet publish -c Release -r <rid> --self-contained true -p:PublishSingleFile=true` for `win-x64`, `linux-x64`, `osx-arm64`.
- Output binary named `chirp2ftm400` (`.exe` on Windows).
- README with install + usage examples.

## 16. Acceptance Criteria

- [ ] Convert provided `FTM-400-Repeater-CanadateV2_1_.csv` and successfully import the result into ADMS-4 with no manual edits.
- [ ] All channel names, frequencies, offsets, tones, and modes match expected values in ADMS-4 visual inspection.
- [ ] CLI displays banner, progress, warnings table, and summary as specified in §10.
- [ ] All 9 validation rules implemented and tested.
- [ ] Unit + snapshot tests passing in CI.
- [ ] Self-contained single-file binaries built for win-x64, linux-x64, osx-arm64.

## 17. Open Questions / Assumptions

1. **User CTCSS (col 11):** Assumed always `1500 Hz` (radio default). If real-world users customize this in ADMS, we'll need a CLI override.
2. **Empty channel field 15/17 sentinels:** Assumed always `0` based on provided sample. Verify ADMS-4 accepts non-`0` values are not required.
3. **Tag character set:** Assumed alphanumeric, dash, space. Will verify against ADMS-4 import on first round-trip test.
4. **AM step:** FTM-400D may force AM channels to specific step values; needs verification with air-band sample channels.
5. **Negative offset on +800 MHz UHF in CHIRP `split` mode:** Edge case for split channels with TX < RX. Mapping defined in §8.2 but needs real-world test.
6. **CHIRP Mode `DN` to FTM-400D `AUTO` vs `DN`:** `AUTO` is the safer default (analog fallback); user may want to flag a future `--digital-strict` option to force `DN`.

These do not block initial implementation but should be confirmed during the first end-to-end test on real hardware.
