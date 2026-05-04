# Sync `tabula-sharp` with upstream `tabula-java` — Design

**Date:** 2026-05-04
**Branch target:** `v1` (commits land directly)
**Scope:** Port behavioral changes from `tabula-java` between commits `ebc83ac2bb1a1cbe54ab8081d70f3c9fe81886ea` (last ported) and `2cdf3b4f` (current `master`, Mar 2025).
**Out of scope:** Java dependency bumps, CI migrations, JSON schema additions, PdfPig version bump, public-API deprecations, `NurminenDetectionAlgorithm` revival.

---

## 1. Background

`tabula-sharp` is a C# port of `tabula-java`. The C# port has already diverged in several deliberate ways:

- Uses **PdfPig** instead of PDFBox.
- Coordinate origin is **bottom-left** (PdfPig convention) instead of top-left (PDFBox/Tabula convention). Existing inversion comments are tagged `// bobld:` throughout the code.
- `ObjectExtractor` is a **static** class (made static in `4eef723`).
- `NurminenDetectionAlgorithm` is replaced by `SimpleNurminenDetectionAlgorithm` because it requires image processing.
- Various classes are `sealed`; classes follow C# naming and PdfPig types.
- `PageArea` is the C# rename of Java `Page` (avoids clash with PdfPig's `Page`).

Result outputs may differ from Java because of how PdfPig builds Letter bounding boxes (documented in README).

This document plans the port of 105 upstream commits, of which roughly 104 are skipped as Java-only or reclassified N/A after audit, and 1 commit is ported (audits collapsed the rest).

## 2. Inventory & triage of upstream commits

### Bucket SKIP — Java-only or N/A (~104 commits)

The following categories are skipped wholesale; full SHA ledger in §6.

- All **dependency bumps**: PDFBox (×7), junit (×2), jts-core (×3), gson (×3), slf4j (×4), bouncycastle (×3), commons-csv (×2), commons-cli, jai-imageio.
- All **Maven plugin bumps** (×7).
- **Build / CI**: jbang catalog, Travis → GitHub Actions, AppVeyor removal, LF enforcement, release prep, branch merges.
- **Java logo / README** edits (English-language non-API doc).
- **CLI-only**: `e5d8ca8` (batch-mode logging), `a747549e` (-b README), `6591241b` (batch-processing PR merge). The C# port has no CLI front-end. Additional CLI-only commits found during pre-execution audit: `00bee45`, `2ab8579`, `6ea9d3a` (Issue #379 series).
- **N/A after audit**:
    - `a6c322e6` + `6ba8ad89` + `96ac1829` — `ObjectExtractor implements Closeable` plumbing. Audited (see §3 audit notes); no behavior change. C# `ObjectExtractor` is `static`; tests own `PdfDocument` lifetime via `using`. Skip.
    - `20b1053a` + `a0173066` — OOM fix in `NurminenDetectionAlgorithm.removeText(PDPage)`. Audited; OOM trigger is unbounded growth in PDFBox token-stream rewriting. C# `SimpleNurminenDetectionAlgorithm` works on already-parsed PdfPig data and has no token rewriting (`grep -r removeText|parseNextToken|newTokens` returns no matches). Trigger condition does not exist. Skip.
    - Pre-execution audit reclassifications: `f7e1976`, `14b3d26`, `d0241fb`, `84aef7f`, `3c2af18` — all refactor-only or N/A (see §6).
    - Task 2 audit (Page-class refactor): `008c395`, `c059262`, `62e6b5f`, `c3c3b9d`, `8a78148`, `6e81297`, `4519596`, `b8d44f6`, `d498a5e`, `1deb2c9`, `9c219de`, `8c4a002` — all REFACTOR-ONLY or N/A (see §6).
    - Task 3 audit (SpreadsheetExtractionAlgorithm cleanup): `df3653b`, `cbb6d73`, `3452fe1`, `6923895`, `6286f85`, `1192887` — all REFACTOR-ONLY or N/A (see §6).
    - Task 4 audit: `2cdf3b4f` — test assertion change driven by PDFBox 3.0.4 `/ActualText` support; no algorithmic change (see §6).
- **JSON schema / public API**:
    - `19ddc51b` + `56cd7131` — adds `pageNumber` field to JSON output. Excluded by user decision (keep current C# JSON shape stable).
    - `4fd6cafe` — keep public ctors deprecated. Excluded by user decision (don't churn C# public API).

### Bucket PORT — 1 commit ported (`21a4932b`)

After Tasks 2–4 audits and pre-execution patch fetches, only `21a4932b` (whitespace heuristic) was actually ported to C#. All other original PORT candidates were reclassified SKIP/N/A during audit. Full ledger in §6.

## 3. Audit findings (already performed during brainstorming)

| Upstream SHA | Audit conclusion |
|---|---|
| `a6c322e6` | One line: `class ObjectExtractor implements java.io.Closeable`. Pure plumbing. No behavior change. |
| `6ba8ad89` | Adds `page.getPDDoc().close()` in 9 test methods. Resource cleanup only. |
| `96ac1829` | Adds `page.getPDDoc().close()` in ~17 test methods. Resource cleanup only. |
| `20b1053a` | Refactors `NurminenDetectionAlgorithm.removeText(PDPage)`. Replaces `while (page.hasContents())` infinite loop with proper `while (token != null)` token iteration; extracts `createTokensWithoutText()`. The OOM-prone token list lives inside PDFBox token-stream rewriting; PdfPig path in C# does not perform this operation. |

## 4. Port plan — topic-grouped C# commits

Each item below becomes one commit on `v1`. Commit messages reference the upstream SHA(s).

### 4.1. Whitespace heuristic for text chunking — `21a4932b`
Refines the heuristic that filters tall-ish whitespace elements affecting text chunking by considering realistic font sizes. Audit Java diff to find the C# equivalent (likely in `TextChunk.cs` or `TextElement.cs`), port the heuristic.

### 4.2. Issue #379 fix series — `00bee45` + `2ab8579` + `6ea9d3a`
~~Three related commits (initial fix, follow-up bugfix, code removal). Read all three diffs in date order, apply the resulting net change as one C# commit. Test PDF from upstream `src/test/resources/...` to be copied to `Tabula.Tests/Resources/` if the upstream test references one.~~

**AUDIT RESULT (pre-execution): CLI-only. All three commits touch only `CommandLineApp.java`. C# port has no CLI. Moved to §6 "CLI-only — found during pre-execution audit" bucket. No port required.**

### 4.3. Path-by-segment-type filtering — `f7e19764`
~~Behaviorally-relevant slice of the Page-class refactor. Affects how clipping/non-stroke path segments are filtered when building rulings. Lives in `PageArea.cs` / `ObjectExtractor.cs` path-extraction code.~~

**AUDIT RESULT (pre-execution): Extract-method refactor only; equivalent C# logic already inline in `ObjectExtractor.cs:80–95`. No behavior change. Moved to §6 "Refactor-only or N/A — found during pre-execution audit" bucket. No port required.**

### 4.4. Page-class internal refactor (behavior-preserving slice) — `008c395..9c219de5` + `8c4a0027`
~~Port the helper extractions, point-attribute encapsulation, and method grouping that improve internal readability. Skip the parts that exist solely to support the `4fd6cafe` deprecated-ctor migration. Public C# `PageArea` API stays unchanged.~~

**AUDIT RESULT (Task 2): All 12 commits in this cluster are REFACTOR-ONLY or N/A (`4519596`, `b8d44f6`, `d498a5e`, `1deb2c9`, `9c219de` relate to CohenSutherlandClipping which C# replaces with Clipper; `8c4a002` is a Builder pattern addition that per Q2=A causes no public-API churn). Moved to §6 "Page-class refactor cluster — Task 2 audit" bucket. No port required.**

### 4.5. `SpreadsheetExtractionAlgorithm` cleanup + extracted rounded comparator — `df3653b1` + `cbb6d73a` + `3452fe14` + `6923895e` + `6286f85` + `11928877`
~~Six interrelated cleanup commits. Apply only the parts that improve C# code we'll keep; drop pure Java-isms (e.g., `computeIfAbsent`, dependency-injection-style refactors that don't add value in C#) to the skipped ledger.~~

**AUDIT RESULT (Task 3): All 6 commits are REFACTOR-ONLY or N/A. `df3653b` renames a comparator constant; `cbb6d73` uses Java labeled-break (C# keeps `doBreak`); `3452fe1`/`6923895` encapsulate TextStripper fields with no C# counterpart; `6286f85` refactors NurminenDetectionAlgorithm (C# uses SimpleNurminenDetectionAlgorithm); `1192887` moves textElements to base class (C# hierarchy diverges). Moved to §6 "SpreadsheetExtractionAlgorithm cleanup cluster — Task 3 audit" bucket. No port required.**

### 4.6. Light cleanup — `14b3d261` + `d0241fb5`
~~Opportunistic, applied while in those files for other commits.~~

**AUDIT RESULT (pre-execution): `14b3d26` is Java idiom modernization (Collections.sort, Float.compare) with no behavior change; `d0241fb` removes a variable that doesn't exist in C# `SpreadsheetDetectionAlgorithm.cs`. Moved to §6 "Refactor-only or N/A — found during pre-execution audit" bucket. No port required.**

### 4.7. Investigate "Adjust test" — `2cdf3b4f`
~~Read the Java test diff. If it reveals a behavior fix (not just a PDFBox-3 API rename), apply the analogous C# change. Otherwise document as N/A and add to the skipped ledger.~~

**AUDIT RESULT (Task 4): N/A. The assertion change is driven by PDFBox 3.0.4 `/ActualText` rendering support; there is no algorithmic change in tabula extraction logic. PdfPig produces matching output to PDFBox 3.0.3, so existing C# assertions pass unchanged. Moved to §6 "Investigate-then-N/A — Task 4 audit" bucket. No port required.**

### 4.8. README API usage example — `84aef7f0` + `3c2af18f`
~~Port the spirit of the upstream `SpreadsheetExtractionAlgorithm` example to the C# `README.md`, adapted to PdfPig API.~~

**AUDIT RESULT (pre-execution): `84aef7f` adds a Java API example to the upstream README; C# README already has equivalent examples (lines 22–50). `3c2af18` is a markdown formatting fix to upstream README. Both N/A. Moved to §6 "Refactor-only or N/A — found during pre-execution audit" bucket. No port required.**

## 5. Per-commit-group execution recipe

The implementation plan instantiates this loop for each group in §4.

1. **Read Java diff(s)** — `Invoke-RestMethod "https://github.com/tabulapdf/tabula-java/commit/<sha>.patch"`. For multi-commit groups, read all in date order.
2. **Locate C# counterpart** — find file/method in `tabula-sharp` mirroring the Java change. Account for: PdfPig API, sealed classes, static `ObjectExtractor`, `PageArea` rename, bottom-left coords (`// bobld:` comments).
3. **Classify** — bug fix / behavioral / refactor-only / N/A. Refactor-only changes that don't improve C# code go to the skipped ledger.
4. **Apply** — C# style (PascalCase, properties, `using`, PdfPig types). No backward-compat shims; no `[Obsolete]`.
5. **Build** — `dotnet build tabula-sharp.sln -c Release` from repo root. Must succeed across all library TFMs (`netstandard2.0;net462;net471;net6.0;net8.0`). Test project currently targets `net8.0` only.
6. **Test** — `dotnet test tabula-sharp.sln -c Release --nologo`.
7. **Classify any test failures**:
    - **Real regression** → fix the code, re-run.
    - **PdfPig coord divergence** in expected value → update assertion + comment `// Updated: PdfPig coords differ from PDFBox; see upstream <sha>`.
    - **Pre-existing failure** unrelated to the port → leave, note in commit body.
8. **Commit** on `v1`:
    ```
    <Imperative subject>

    Ports tabula-java <sha[, sha, ...]>: <one-line summary>.
    <Optional: notes on C# divergences, test changes, etc.>
    ```

**Test-fixture PDFs from upstream**: when an upstream commit adds a test PDF, copy the binary from `tabula-java/src/test/resources/...` into `Tabula.Tests/Resources/`, register in `Tabula.Tests.csproj` with `<None Include=... CopyToOutputDirectory>`, reference from the new test. Existing pattern in `Tabula.Tests/GitHubIssues.cs` is the model.

**Halt condition**: if any commit group's audit reveals the change is risky/unclear or a test failure can't be classified, stop and report rather than guess.

## 6. Skipped-commit ledger

Every one of the 105 upstream SHAs is accounted for here.

### Dependency bumps (skip — Java-only)
- `47f784f` junit 4.13→4.13.1
- `f8cac70` jai-imageio-jpeg2000 1.3.0→1.4.0
- `e8f9c15` jts-core 1.17.0→1.18.0
- `7f3f039` pdfbox 2.0.22
- `ae9d2eb` junit 4.13.1→4.13.2
- `eac87d0` jts-core 1.18.0→1.18.1
- `39253c5` bcprov-jdk15on 1.66→1.68
- `92a69b4` bcmail-jdk15on 1.66→1.68
- `b0b0860` gson 2.8.6→2.8.7
- `6b7ec97` pdfbox 2.0.23→2.0.24
- `ae281f6` commons-csv 1.8→1.9.0
- `80042f5` bcprov-jdk15on 1.68→1.69
- `ce74f12` bcmail-jdk15on 1.68→1.69
- `1e0d751` slf4j-api 1.7.30→1.7.32
- `a3bba8b` slf4j-simple 1.7.30→1.7.32
- `01c2559` pdfbox 2.0.24→2.0.25
- `a5f59ed` pdfbox latest
- `fa9363b` slf4j-api 1.7.32→1.7.35
- `4e23be7` pdfbox 2.0.23
- `c65783d` pdfbox/bc/big2/plugins update
- `ab93da9` gson 2.8.7→2.9.0
- `8bfa3ad` pdfbox 2.0.28
- `bc60be2` pdfbox 3.0.1
- `e0ee072` pdfbox 3.0.2
- `2ef079f` jts-core 1.18.1→1.19.0
- `c8983110` GitHub-native Dependabot
- `c1e4e32` maven-javadoc-plugin 3.3.1→3.5.0
- `6d59cdd` maven-compiler-plugin 3.8.1→3.11.0
- `2bdeb95` maven-gpg-plugin 1.6→3.2.4
- `c831cf6` commons-cli 1.4→1.8.0
- `9dc64f8` slf4j-api 1.7.35→2.0.13
- `3f74453` slf4j-simple 1.7.32→2.0.13
- `5761334` nexus-staging-maven-plugin 1.6.8→1.7.0
- `ab7c4bd` maven-source-plugin 3.2.1→3.3.1
- `ebe8e30` commons-csv 1.9.0→1.11.0
- `db3f6df` maven-compiler-plugin
- `fd3a32c` gson 2.9.0→2.11.0
- `097559d` maven-javadoc-plugin 3.3.1→3.7.0
- `bde6d76` maven-surefire-plugin 2.22.2→3.3.1
- `0c73e69` maven-javadoc-plugin 3.7.0→3.8.0
- `818c9a2` pdfbox 3.0.2→3.0.3
- `5d91f1d` jts-core 1.19.0→1.20.0
- `971ae76` BouncyCastle dependencies upgrade
- `63de16a` exclude junit-jupiter from pdfbox
- `88154e2` Update PDFBox

### Build / CI / release plumbing (skip)
- `7fca22e` add jbang catalog
- `c355a34` fix logo
- `d175879` goodbye travis, hello github actions
- `1739fbf` goodbye appveyor
- `5f43a93` Remove Appveyor badge
- `ef23f62` cache maven deps
- `c6de348` windows test runner
- `b0fde49` Enforce checkout with LF
- `50ff2df` Run tests on pull request
- `20e3c2e` prepare release 1.0.5
- `adb7738` prepare for next release

### Merge commits (skip — no own diff)
- `e49d860`
- `df9bc34`
- `0256b36`
- `a9428e29`
- `4bde73c`
- `d2da27f`
- `346ac27`
- `7e024fc`

### CLI-only (skip — C# port has no CLI)
- `e5d8ca8` Add logging when an error occurs in batch mode
- `a747549` Add -b option to README
- `6591241` Merge PR #451 batch-processing

### N/A after audit (skip with reason)
- `a6c322e` `ObjectExtractor implements Closeable` — `ObjectExtractor` is `static` in C#
- `6ba8ad8` Fix unclosed document warnings — tests own `PdfDocument` via `using`
- `96ac182` Fix more warnings in tests — same reason
- `20b1053` OOM fix in `NurminenDetectionAlgorithm.removeText` — algorithm replaced by `SimpleNurminenDetectionAlgorithm`; no token rewriting in C# pipeline
- `a017306` Test for the OOM fix — same reason

### Excluded by user decision
- `19ddc51` add page number to JSON output — keep current C# JSON shape (Q5=C)
- `56cd713` tests passing for above
- `4fd6caf` Keep public ctors deprecated — don't churn C# public API (Q2=A)

### Refactor-only Java internals (skip — no behavior change, no improvement to C# code)
- `6fdf554` Refactoring JSON serializers — internal Java refactor of `Tabula.Json` writer; no output change. C# `Tabula.Json` is small and clean. Skip.
- `a9932a8` Refactoring writers — internal Java refactor of writer infrastructure; no behavior change. Skip.

### CLI-only — found during pre-execution audit (skip)
- `00bee45` Issue #379 fix — `CommandLineApp.java` only
- `2ab8579` Issue #379 follow-up — `CommandLineApp.java` only
- `6ea9d3a` Issue #379 cleanup — `CommandLineApp.java` only

### Refactor-only or N/A — found during pre-execution audit (skip)
- `f7e1976` filter path by segment type — extract-method refactor only; equivalent C# logic already inline in `ObjectExtractor.cs:80–95`
- `14b3d26` light Java cleanup — Java idiom modernization (Collections.sort, Float.compare); no behavior change
- `d0241fb` remove unused variable — variable doesn't exist in C# `SpreadsheetDetectionAlgorithm.cs`; already clean
- `84aef7f` README API example — C# README already has equivalent examples (lines 22–50)
- `3c2af18` README markdown fix — same; N/A

### Page-class refactor cluster — Task 2 audit (skip; all REFACTOR-ONLY or N/A)
- `008c395` Variable renaming + convenience constructor; no logic change
- `c059262` Field renames (texts→textElements etc.), method reorder
- `62e6b5f` Extract `getCollapsedVerticalRulings`/`getCollapsedHorizontalRulings` from `getRulings`; identical logic
- `c3c3b9d` Extract `getMinimumCharWidth/HeightFrom`, `addBorderRulingsTo` + `DEFAULT_MIN_CHAR_LENGTH=7`
- `8a78148` Constructor renames in ObjectExtractorStreamEngine; remove unused `debugClippingPaths`
- `6e81297` Extract `getStartPoint`, `getLineBetween`, `verifyLineIntersectsClipping`
- `4519596` New TestCohenSutherland.java; minor style; C# uses Clipper — N/A
- `b8d44f6` CohenSutherlandClipping variable renaming — N/A (C# uses Clipper)
- `d498a5e` Extract `delta()` helper with MINIMUM_DELTA=0.01f — N/A (C# uses Clipper)
- `1deb2c9` Encapsulate point into inner class — N/A (C# uses Clipper)
- `9c219de` `!= INSIDE` → `!= 0` (mathematical identity) — N/A (C# uses Clipper)
- `8c4a002` PageDims DTO + Builder pattern; per Q2=A no public-API churn

### SpreadsheetExtractionAlgorithm cleanup cluster — Task 3 audit (skip; all REFACTOR-ONLY or N/A)
- `df3653b` Extract `compareRounded` helper, rename `POINT_COMPARATOR` → `Y_FIRST_POINT_COMPARATOR`; identical logic
- `cbb6d73` Import cleanup, `doBreak` removal via Java labeled-break (C# idiom keeps `doBreak`); empty-table `return false` already in C# at line 190–194
- `3452fe1` Encapsulate TextStripper fields; no exact C# counterpart
- `6923895` Adapt Page.java to 3452fe1; no C# counterpart
- `6286f85` Refactor NurminenDetectionAlgorithm only; N/A (C# uses SimpleNurminenDetectionAlgorithm)
- `1192887` Move textElements up to base class; C# class hierarchy diverges

### Investigate-then-N/A — Task 4 audit (skip)
- `2cdf3b4` Test assertion updated to match PDFBox 3.0.4 `/ActualText` rendering; no tabula algorithm change. PdfPig produces matching output to PDFBox 3.0.3, so existing C# assertions pass.

### To port (1 commit)
- `21a4932` Whitespace heuristic for text chunking — ported in commit `53b8bd7` (tabula-sharp). Required updating `Issue30` assertion (2 → 1) per user authorization due to PdfPig PointSize semantics divergence from PDFBox.

**Tally**: 1 PORT (`21a4932`) + 104 SKIP/N/A = 105.

Breakdown of the 104 SKIP/N/A: 45 dependency bumps + 11 build/CI + 8 merge commits + 3 CLI-only (original) + 5 N/A-after-audit (original) + 3 excluded-by-user + 2 refactor-only-Java (original) + 3 CLI-only (pre-execution audit) + 5 refactor-only/N/A (pre-execution audit) + 12 Page-class refactor cluster (Task 2 audit) + 6 SpreadsheetExtractionAlgorithm cleanup (Task 3 audit) + 1 investigate-then-N/A (Task 4 audit) = 104.

## 7. Risks accepted

- **PdfPig vs PDFBox coord semantics**: assertion updates will be unavoidable. `// bobld:` convention applies to new ports too.
- **Page-class refactor scope creep**: 10 small commits to evaluate. Mitigation in recipe step 3 — drop refactor-only Java-isms.
- **Multi-target framework matrix**: any port using newer C# language features must compile across `netstandard2.0;net462;net471;net6.0;net8.0`.

## 8. Non-goals

- No PdfPig version bump.
- No re-introduction of `NurminenDetectionAlgorithm` (deliberately a stub).
- No new public API surface unless a behavioral fix demands it.
- No `[Obsolete]` deprecations.
- No JSON schema changes (Q5=C).
- No version / `<PackageVersion>` bump in this work.
- No CI / GitHub Actions changes.
- No `Tabula.Csv` / `Tabula.Json` API changes beyond what a behavioral port forces.

## 9. Success criteria

- All commits build across all library target frameworks (`netstandard2.0;net462;net471;net6.0;net8.0`).
- `dotnet test` green (test project targets `net8.0`). Updated assertions documented per §5 step 7.
- Skipped-commit ledger (§6) committed alongside this design doc; every one of the 105 upstream SHAs accounted for.
- `v1` branch ready to ship without further cleanup.
- Final outcome: 1 behavioral port (`21a4932b` → `tabula-sharp` commit `53b8bd7`); 104 commits classified SKIP/N/A across the categories in §6.
