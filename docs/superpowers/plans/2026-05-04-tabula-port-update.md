# tabula-java upstream sync — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port behavioral changes from `tabula-java` between `ebc83ac2` and `2cdf3b4f` (105 upstream commits) into `tabula-sharp` `v1`, preserving the C# port's deliberate divergences (PdfPig, bottom-left coords, static `ObjectExtractor`, `SimpleNurminenDetectionAlgorithm`).

**Architecture:** Topic-grouped commits land directly on `v1`. Each commit references upstream SHA(s). Spec lives at `docs/superpowers/specs/2026-05-04-tabula-port-update-design.md`. After audit during plan-writing the actual port work is much smaller than initial inventory — only one confirmed behavioral fix (`21a4932b`); two clusters still need per-commit audit.

**Tech Stack:** C# / .NET (multi-target: `netstandard2.0;net462;net471;net6.0;net8.0` for library, `net8.0` for tests), PdfPig, xUnit, PowerShell on Windows for tooling.

---

## Pre-execution audit findings

Done during plan-writing by reading individual upstream patches. These reclassifications refine the spec's tally:

| Upstream SHA(s) | Original bucket (spec) | Refined bucket | Reason |
|---|---|---|---|
| `00bee45` + `2ab8579` + `6ea9d3a` | PORT (Issue #379) | **SKIP** — CLI-only | All three diffs touch only `CommandLineApp.java`. C# port has no CLI. |
| `f7e19764` | PORT (path-by-segment-type) | **SKIP** — refactor-only | Pure extract-method (`filterPathBySegmentType`). Equivalent C# logic already exists inline in `Tabula/ObjectExtractor.cs:80–95` per Q2=A (no refactor-only ports). |
| `14b3d261` | PORT (light cleanup) | **SKIP** — Java idioms | `Collections.sort` → instance, `Float.compare` modernization, `hashCode` removal. Java-specific cleanup, no behavior change. |
| `d0241fb5` | PORT (light cleanup) | **N/A** | Removes unused `SpreadsheetExtractionAlgorithm sea` variable in `SpreadsheetDetectionAlgorithm.java`. C# `Tabula/Detectors/SpreadsheetDetectionAlgorithm.cs` has no such variable — already clean. |
| `84aef7f0` + `3c2af18f` | PORT (README example) | **N/A** | Adds Java API usage example to upstream README. C# `README.md` already has equivalent C# examples for both Stream and Lattice modes (lines 22–50). |

**Net post-audit work**:
- 1 confirmed behavioral port (`21a4932b`).
- 2 audit-then-decide clusters (Page-class refactor, SpreadsheetExtractionAlgorithm cleanup).
- 1 single-commit audit-then-decide (`2cdf3b4f`).

The spec's skip ledger is updated by Task 6.

---

## Task 0: Verify baseline build & tests

Establishes the green baseline so any post-port failure is attributable to ported changes, not pre-existing state.

**Files:** none modified.

- [ ] **Step 0.1: Confirm working directory and branch**

```powershell
git rev-parse --abbrev-ref HEAD
git status --short
```
Expected: branch `v1`, no unexpected modifications. If `tabula-java-commits.txt` and `.claude/` show as untracked, that's fine — they're working notes.

- [ ] **Step 0.2: Restore + build full solution in Release**

```powershell
dotnet restore tabula-sharp.sln
dotnet build tabula-sharp.sln -c Release --nologo
```
Expected: build succeeds across all library TFMs (`netstandard2.0;net462;net471;net6.0;net8.0`) and the test project (`net8.0`). Warnings ok; errors not.

- [ ] **Step 0.3: Run full test suite, capture baseline**

```powershell
dotnet test tabula-sharp.sln -c Release --nologo --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath baseline-tests.log
```
Expected: prints summary like `Passed: N, Failed: 0`. Note `N` (the passing count) — every later task must keep at least `N` passing unless it explicitly reclassifies a failure per spec §5 step 7.

- [ ] **Step 0.4: Stop and report if baseline is not green**

If any test fails in Step 0.3, halt the plan. Investigate and fix the pre-existing failure separately, then restart the plan.

---

## Task 1: Port whitespace heuristic for text chunking — `21a4932b`

Refines the heuristic that filters tall-ish whitespace elements. The Java change adds font-size bounds (2pt ≤ blank ≤ 40pt) so very small/very large blank glyphs are filtered as artefacts.

**Upstream Java:** `src/main/java/technology/tabula/TextStripper.java`, method `writeString`.

**C# counterpart:** `Tabula/TextStripper.cs`, method `Process` at lines 31–64.

**Mapping notes:**
- Java `textPosition.getFontSizeInPt()` → C# `letter.PointSize`.
- Java `te.getText().trim().equals("")` → existing C# `te.GetText()?.Trim().Equals("") != false`.
- The current C# code combines all three conditions in one `if`; the port restructures to nested `if` matching the Java.

**Files:**
- Modify: `Tabula/TextStripper.cs:16` (add two constants), `Tabula/TextStripper.cs:57-60` (restructure filter)

- [ ] **Step 1.1: Add the two new constants**

Edit `Tabula/TextStripper.cs`. Locate the existing line `        private static float AVG_HEIGHT_MULT_THRESHOLD = 6.0f;` (currently line 16) and insert two new constant lines immediately after it. Do **not** modify the `NBSP` constant on line 15 (its value contains a literal U+00A0 character that some editors mangle).

Use the Edit tool:
- `old_string`: `        private static float AVG_HEIGHT_MULT_THRESHOLD = 6.0f;\n`
- `new_string`:

```csharp
        private static float AVG_HEIGHT_MULT_THRESHOLD = 6.0f;
        private static float MAX_BLANK_FONT_SIZE = 40.0f;
        private static float MIN_BLANK_FONT_SIZE = 2.0f;
```

After this step, the constants block at the top of the `TextStripper` class should contain four declarations: the unchanged `NBSP`, the unchanged `AVG_HEIGHT_MULT_THRESHOLD`, plus the two new font-size bounds.

- [ ] **Step 1.2: Restructure the filter**

Edit `Tabula/TextStripper.cs`. Replace lines 57–60:

```csharp
                if (avgHeight > 0 && te.Height >= (avgHeight * AVG_HEIGHT_MULT_THRESHOLD) && (te.GetText()?.Trim().Equals("") != false))
                {
                    continue;
                }
```

with:

```csharp
                if (te.GetText()?.Trim().Equals("") != false)
                {
                    if (avgHeight > 0 && te.Height >= (avgHeight * AVG_HEIGHT_MULT_THRESHOLD))
                    {
                        continue;
                    }

                    if (letter.PointSize > MAX_BLANK_FONT_SIZE || letter.PointSize < MIN_BLANK_FONT_SIZE)
                    {
                        continue;
                    }
                }
```

- [ ] **Step 1.3: Build**

```powershell
dotnet build tabula-sharp.sln -c Release --nologo
```
Expected: build succeeds; no new warnings about unused constants.

- [ ] **Step 1.4: Run tests**

```powershell
dotnet test tabula-sharp.sln -c Release --nologo --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath task1-tests.log
```
Expected: all baseline tests still pass. Tests likely affected: `TestBasicExtractor`, `TestSpreadsheetExtractor`, `TextStripper`-related tests, `TestsTabulaPy`, `TestsIcdar2013`.

- [ ] **Step 1.5: Classify any failures per spec §5 step 7**

For each new failure (failure not in `baseline-tests.log`):
- **Real regression** → revert and re-think: the Java fix may not match PdfPig's `letter.PointSize` semantics. Worst case skip the port and document.
- **PdfPig coord/font-size divergence in expected value** → update assertion + add comment `// Updated: PdfPig PointSize differs from PDFBox getFontSizeInPt; see upstream 21a4932b`.
- **Pre-existing failure** → leave; note in commit body.

If failures cannot be cleanly classified, halt and report.

- [ ] **Step 1.6: Commit**

```powershell
git add Tabula/TextStripper.cs
# include any test-assertion updates
git commit -m @'
Refine whitespace filter in TextStripper

Ports tabula-java 21a4932b: filter tall-ish whitespace elements by
considering realistic font sizes. Adds MIN_BLANK_FONT_SIZE = 2pt and
MAX_BLANK_FONT_SIZE = 40pt bounds; restructures the filter so the
height check and the font-size check are evaluated separately on blank
elements. C# uses Letter.PointSize where Java uses
TextPosition.getFontSizeInPt.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

---

## Task 2: Audit + selective port — Page-class refactor cluster

11 upstream commits between Nov 2020 and Jan 2021 refactor `Page.java`. Per Q2=A, port only behavior-affecting slices; skip pure refactoring. The C# counterpart is `Tabula/PageArea.cs`. The cluster:

- `008c395`, `c059262`, `62e6b5f`, `c3c3b9d`, `8a78148`, `6e81297`, `4519596`, `b8d44f6`, `d498a5e`, `1deb2c9`, `9c219de` (Nov 2020)
- `8c4a0027` (Jan 2021, data-clumps)

Already pre-classified as SKIP in pre-execution audit:
- `f7e19764` "Filter path by segment type" — pure extract-method.

**Files:**
- Possibly modify: `Tabula/PageArea.cs`
- Possibly modify: `Tabula/ObjectExtractor.cs` (some Java refactors crossed file boundaries)

- [ ] **Step 2.1: Fetch the 11 diffs into a local audit folder**

```powershell
$shas = @('008c395','c059262','62e6b5f','c3c3b9d','8a78148','6e81297','4519596','b8d44f6','d498a5e','1deb2c9','9c219de','8c4a002')
New-Item -ItemType Directory -Path .audit\page -Force | Out-Null
foreach ($s in $shas) {
    Invoke-WebRequest -Uri "https://github.com/tabulapdf/tabula-java/commit/$s.patch" -OutFile ".audit/page/$s.patch"
}
Get-ChildItem .audit\page
```
Expected: 12 `.patch` files written.

- [ ] **Step 2.2: Read each patch and classify**

For each patch, classify as one of: BEHAVIORAL (port), REFACTOR-ONLY (skip), TEST-ONLY (skip — fixtures live in Java test paths), or BUILDS-ON-DEPRECATED-CTOR (skip per Q2=A — covered by `4fd6cafe` exclusion).

Record findings inline in this task as you go (use a markdown table). A typical pattern: most patches in this cluster will be REFACTOR-ONLY or BUILDS-ON-DEPRECATED-CTOR. If the entire cluster is non-behavioral, skip the cluster and proceed to Step 2.6 (record-only commit).

- [ ] **Step 2.3: For each BEHAVIORAL patch, locate C# counterpart**

`grep` for affected method/symbol names in `Tabula/PageArea.cs` and `Tabula/ObjectExtractor.cs`. Account for: PdfPig types instead of PDFBox, bottom-left coords (`// bobld:` comments), C# property names (PascalCase).

If no clear C# counterpart exists (e.g., the Java method was already restructured differently in C#), note in audit table and skip.

- [ ] **Step 2.4: Apply BEHAVIORAL changes only**

For each patch with a real C# counterpart, apply the C#-equivalent edit. No `[Obsolete]`, no public-API churn.

- [ ] **Step 2.5: Build + test + classify failures**

```powershell
dotnet build tabula-sharp.sln -c Release --nologo
dotnet test tabula-sharp.sln -c Release --nologo --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath task2-tests.log
```
Apply spec §5 step 7 classification.

- [ ] **Step 2.6: Commit**

If any BEHAVIORAL changes were ported:

```powershell
git add Tabula/PageArea.cs Tabula/ObjectExtractor.cs
git commit -m @'
Apply behavioral slice of Page-class upstream refactor

Ports tabula-java <listed SHAs>: <one-line summary of behavioral changes>.
Other commits in the cluster (<list>) audited and skipped as
refactor-only per design-doc Q2=A.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

If no BEHAVIORAL changes were found:

```powershell
git commit --allow-empty -m @'
Audit Page-class refactor cluster — no behavioral ports

Audited tabula-java 008c395..9c219de5 + 8c4a0027 + f7e19764 (12 commits).
All commits classified refactor-only per spec Q2=A; skipped wholesale.
No C# changes required. Audit notes recorded in spec ledger update
(see following commit).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

- [ ] **Step 2.7: Cleanup audit folder**

```powershell
Remove-Item .audit\page -Recurse -Force
```

---

## Task 3: Audit + selective port — `SpreadsheetExtractionAlgorithm` cleanup cluster

6 upstream commits cleaning up `SpreadsheetExtractionAlgorithm.java` and extracting a rounded comparator: `df3653b1`, `cbb6d73a`, `3452fe14`, `6923895e`, `6286f85`, `11928877`.

Same recipe as Task 2.

**Files:**
- Possibly modify: `Tabula/Extractors/SpreadsheetExtractionAlgorithm.cs`
- Possibly modify: `Tabula/Utils.cs` (if a comparator gets extracted there)

- [ ] **Step 3.1: Fetch the 6 diffs**

```powershell
$shas = @('df3653b','cbb6d73','3452fe1','6923895','6286f85','1192887')
New-Item -ItemType Directory -Path .audit\spreadsheet -Force | Out-Null
foreach ($s in $shas) {
    Invoke-WebRequest -Uri "https://github.com/tabulapdf/tabula-java/commit/$s.patch" -OutFile ".audit/spreadsheet/$s.patch"
}
Get-ChildItem .audit\spreadsheet
```

- [ ] **Step 3.2: Read each patch and classify**

Same buckets as Task 2.2 (BEHAVIORAL / REFACTOR-ONLY / N/A). Many of these will be REFACTOR-ONLY (extract method, dependency-inversion code smell, data-class smell) — drop those.

- [ ] **Step 3.3: For each BEHAVIORAL patch, locate C# counterpart**

`Tabula/Extractors/SpreadsheetExtractionAlgorithm.cs` is the primary file. The "extract rounded comparator" (`df3653b1`) may also touch a utility class — `Tabula/Utils.cs` has rounding helpers (`Utils.Round`).

- [ ] **Step 3.4: Apply BEHAVIORAL changes only**

- [ ] **Step 3.5: Build + test + classify failures**

```powershell
dotnet build tabula-sharp.sln -c Release --nologo
dotnet test tabula-sharp.sln -c Release --nologo --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath task3-tests.log
```

- [ ] **Step 3.6: Commit**

If BEHAVIORAL changes:

```powershell
git add Tabula/Extractors/SpreadsheetExtractionAlgorithm.cs Tabula/Utils.cs
git commit -m @'
Apply behavioral slice of SpreadsheetExtractionAlgorithm cleanup

Ports tabula-java <SHAs>: <summary>.
Other commits in the cluster (<list>) audited and skipped as
refactor-only per design-doc Q2=A.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

Otherwise empty-commit form per Task 2.6.

- [ ] **Step 3.7: Cleanup**

```powershell
Remove-Item .audit\spreadsheet -Recurse -Force
```

---

## Task 4: Investigate "Adjust test" — `2cdf3b4f`

This commit accompanies the PDFBox 3 update (`88154e2c`). The PDFBox bump itself is N/A (the C# port uses PdfPig). The test adjustment may signal a real behavior change worth understanding.

**Files:** to be determined by audit.

- [ ] **Step 4.1: Fetch and read both diffs**

```powershell
Invoke-WebRequest -Uri "https://github.com/tabulapdf/tabula-java/commit/88154e2c.patch" -OutFile ".audit/88154e2c.patch"
Invoke-WebRequest -Uri "https://github.com/tabulapdf/tabula-java/commit/2cdf3b4f.patch" -OutFile ".audit/2cdf3b4f.patch"
Get-Content .audit\2cdf3b4f.patch
Get-Content .audit\88154e2c.patch
```

- [ ] **Step 4.2: Decide**

- If the test adjustment merely renames a PDFBox-3 API call or accommodates a different PDFBox return type → **N/A**, document and skip.
- If the test adjustment changes an *assertion value* (i.e., the algorithm now produces a different output worth investigating) → identify the algorithmic cause, find the C# equivalent in `Tabula/`, port the fix.

- [ ] **Step 4.3: If port required, apply + build + test**

Same `dotnet build` + `dotnet test` cycle as previous tasks.

- [ ] **Step 4.4: Commit**

If ported:

```powershell
git add <files>
git commit -m @'
Apply behavior change underlying tabula-java "Adjust test"

Ports tabula-java 2cdf3b4f: <one-line summary of behavior change found>.
Companion PDFBox 3.x update (88154e2c) skipped — C# port uses PdfPig.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

If skipped:

```powershell
git commit --allow-empty -m @'
Audit tabula-java 2cdf3b4f — no behavior change to port

Test adjustment is a PDFBox-3 API-rename only; no algorithmic change.
Companion 88154e2c (PDFBox version bump) is N/A for C# port (PdfPig).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

- [ ] **Step 4.5: Cleanup**

```powershell
Remove-Item .audit\88154e2c.patch, .audit\2cdf3b4f.patch -Force
if (Test-Path .audit) { try { Remove-Item .audit -Force } catch {} }
```

---

## Task 5: Update spec ledger with execution-time reclassifications

The spec was approved before per-commit pre-fetch audit. Update the spec's skip ledger to incorporate the new SKIP/N/A reclassifications discovered during plan-writing and Tasks 2–4.

**Files:**
- Modify: `docs/superpowers/specs/2026-05-04-tabula-port-update-design.md`

- [ ] **Step 5.1: Move pre-execution-audit reclassifications**

In the spec, edit the `### To port (see §4)` section to remove the SHAs that were reclassified in the "Pre-execution audit findings" table at the top of this plan: `00bee45`, `2ab8579`, `6ea9d3a`, `f7e19764`, `14b3d26`, `d0241fb`, `84aef7f`, `3c2af18`.

Add new sections to the ledger:

```markdown
### CLI-only — found during pre-execution audit (skip)
- `00bee45` Issue #379 fix — `CommandLineApp.java` only
- `2ab8579` Issue #379 follow-up — `CommandLineApp.java` only
- `6ea9d3a` Issue #379 cleanup — `CommandLineApp.java` only

### Refactor-only or already-clean — found during pre-execution audit (skip / N/A)
- `f7e1976` filter path by segment type — extract-method refactor only; equivalent C# logic already inline in `ObjectExtractor.cs`
- `14b3d26` light Java cleanup — Java idiom modernization (Collections.sort → instance, Float.compare); no behavior change
- `d0241fb` remove unused variable — variable doesn't exist in C# `SpreadsheetDetectionAlgorithm.cs`; already clean
- `84aef7f` README API example — C# README already has equivalent examples
- `3c2af18` README markdown fix — same; N/A
```

- [ ] **Step 5.2: Move execution-time reclassifications**

After Tasks 2, 3, and 4 complete, append additional reclassifications based on per-cluster audit findings (from each task's audit table). Use the same section format as Step 5.1.

- [ ] **Step 5.3: Update the tally**

Update the `**Tally**` line near the end of §6 to reflect the new bucket counts. Total must remain 105.

- [ ] **Step 5.4: Build + test (no code change so should be no-op)**

```powershell
dotnet build tabula-sharp.sln -c Release --nologo
dotnet test tabula-sharp.sln -c Release --nologo
```

- [ ] **Step 5.5: Commit**

```powershell
git add docs/superpowers/specs/2026-05-04-tabula-port-update-design.md
git commit -m @'
Update spec ledger with audit-driven reclassifications

Reclassifies upstream SHAs as SKIP/N/A based on pre-execution patch
audit and per-task cluster audits. Total remains 105 (PORT counts
shrink, SKIP counts grow). No code changes.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

---

## Task 6: Final verification

Confirm the `v1` branch is in a shippable state per spec §9 success criteria.

**Files:** none modified.

- [ ] **Step 6.1: Full clean build across all library TFMs**

```powershell
dotnet clean tabula-sharp.sln --nologo
dotnet build tabula-sharp.sln -c Release --nologo
```
Expected: zero errors. New warnings should be limited to those documented per task.

- [ ] **Step 6.2: Full test pass**

```powershell
dotnet test tabula-sharp.sln -c Release --nologo --logger "console;verbosity=normal" 2>&1 | Tee-Object -FilePath final-tests.log
```
Expected: pass count ≥ baseline pass count from Step 0.3. Any reduction must be attributable to an explicit assertion update documented in a port commit.

- [ ] **Step 6.3: Confirm `v1` branch state**

```powershell
git log --oneline 4096c8a..HEAD
git status --short
```
Expected: a sequence of ports (Tasks 1–4) plus the spec-ledger update (Task 5). No uncommitted changes (other than working notes like `tabula-java-commits.txt`, `baseline-tests.log`, `task*.log`, `final-tests.log`).

- [ ] **Step 6.4: Cleanup working logs (optional)**

```powershell
Remove-Item baseline-tests.log, task1-tests.log, task2-tests.log, task3-tests.log, final-tests.log, tabula-java-commits.txt -ErrorAction SilentlyContinue
```

- [ ] **Step 6.5: Report**

Print a short summary:
- Number of ports applied (1 confirmed + audited additions).
- Number of upstream SHAs accounted for in the ledger (must be 105).
- Final test pass count vs. baseline.
- Any tests with updated assertions (with file:line references and upstream-SHA citations).
