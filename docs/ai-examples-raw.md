# AI examples: raw log

Everything worth keeping from the build, in order. "Caught by" says honestly who spotted it.
Commit hashes are in `git log` (updated after the author-email history rewrite on 2026-11-04).

## 1. Pushbacks and catches by me

| # | What happened | Caught by | Outcome | Evidence |
|---|---|---|---|---|
| 1 | The build plan had me write the calculation tests by hand, without AI. I asked whether an AI would actually get that arithmetic wrong. | Me | Agreed the risk is the spec (null vs 0, rounding mode, weighting), not the sums. Switched to: I sign off the cases and expected values before the code exists, hand-check one example, then a mutation check. | Plan change before Phase 1 |
| 2 | Asked why the id was a `string` and not a `Guid`. | Me | It was a Mongo ObjectId. Switched to `Guid.CreateVersion7()`: strongly typed, the `{id:guid}` route gives a free 404, time-ordered like ObjectId. | `8239b66` |
| 3 | The validator required an average value whenever homes > 0. At appraisal, developers know the home count before the values, so the rule forces a fake price that corrupts totals. | Me (domain thinking, not code reading) | Value became nullable ("not valued yet"); totals gained `UnvaluedHomes`. I added two test rows: unvalued homes still count as homes and affordable; 0 homes with a value gives value 0 and unvalued 0. | `dcb5608` |
| 4 | Asked why business rules like "one line per tenure" live in `Http/`, not the domain. | Me | AI conceded the validator mixes input parsing with domain invariants. Kept it as the single gate (one entry point, YAGNI), with a stated condition for moving it: a second entry point such as an import job. | Discussion before step 1.5 |
| 5 | Asked: PUT with an unknown id and an invalid body, which wins, 404 or 400? "You must specify it for the test." | Me | Defined it: 400 wins (validate before touching the DB; the update is one atomic call). A malformed id is still 404 because the route never matches. 4-row test table; mutation (lookup before validate) failed exactly the 400 test. | `cfb0b66` |
| 6 | Asked whether `type="number"` with `inputMode="numeric"` would send decimals for an int field. The AI had earlier claimed this would be "a plain 400" and that number inputs prevent it. | Me | Proved with curl against the real API: **HTTP 500** (`BadHttpRequestException` turned into 500 by our handler). Also found `type="number"` reports unreadable text as empty, so `12abc` was **silently saved as 0 homes**. Fixed: decimal `Homes` plus a validator field error; the handler keeps the 400; text inputs with a "this isn't a number" check only. Mutations on both fixes failed the right tests. | `06fac9c` |
| 7 | Separate reviewer chat (Claude, claude.ai) flagged that `"tenures": [null]` crashes the validator (`NullReferenceException`, so a 500). | Reviewer, relayed by me | Guard added, list element type made nullable so the compiler enforces the check; test plus mutation proved it. | `a95c639` |
| 8 | Reviewer flagged: no test proving a 500 doesn't leak internals; `/health` can hang ~30s when Mongo is down; health failures aren't logged. | Reviewer, relayed by me | 500 ProblemDetails test (stores unreadable data); ping capped at 2s (measured: 503 in 2.05s with Mongo stopped); warning log line. | `eedac96` |
| 9 | Asked for Anthropic's `frontend-design` skill, and later the Zod skill (secondsky/claude-skills). | Me | Both read in full before installing, licences kept, committed under `.claude/skills/`. Design plan approved before UI code; Zod applied at the response boundary only. | `31628c8`, `6db3feb` |

## 2. AI mistakes or overclaims caught by verification (AI's own checks)

| # | Claim or assumption | How it was disproved | Outcome |
|---|---|---|---|
| 1 | "The pennies test protects decimal, not double." | Ran the same sum in double: `300000.30000000005` converts to decimal as `300000.3`, so a double version would pass the test anyway. | Kept the test, corrected the claim: the compiler, not the test, enforces decimal. |
| 2 | The plan said the Mongo driver may store decimal as a string, so register Decimal128. | Removed the registration: all tests still passed. Driver 3.13 already defaults to Decimal128. The nullable-decimal line was also redundant. | Both lines deleted; the raw-BSON test kept as the guard. The Guid registration, by contrast, broke 5 tests when removed, so it stays. |
| 3 | "The 500 test must run in Production, because Development shows the stack trace." | Ran it in Development: passed. Our `UseExceptionHandler` catches the exception before the developer page sees it. | Production override and wrong comment removed. |
| 4 | Wrote a test (`Put_to_unknown_id`) with a parameter that was always true. | Self-review before committing. | Parameter removed. |
| 5 | The first UI showed "£0" for a scheme whose homes were all unvalued, and "£0" for a scheme with no homes. | Screenshot review (frontend-design skill's "critique with screenshots"). | "Not valued" and "—", with a `value()` helper and 4 tests. Same screenshot found the summary running off the screen on mobile; fixed. |
| 6 | Planned `public partial class Program;` for the test host. | Tests compiled and ran without it on .NET 10. | Not added. |

## 3. Things real infrastructure caught that a fake would not

- The Guid storage format must be registered with driver 3.x (all 5 store tests failed without it).
- A database name in the Mongo URL also becomes the authentication database (the container user lives in `admin`).
- The stored document shape: `"OnSite"` as a string, camelCase fields, Decimal128 money, null for unvalued.
- `CreatedAt` survives an update (behaviour of `$set` in real Mongo).
- The Zod schemas accept the real .NET output: a v7 Guid passes `z.uuid()`, `2026-11-03T13:20:34.062Z` passes `z.iso.datetime()`.

## 4. How I checked AI output (practices)

- **Mutation checks:** break the code on purpose, confirm the right test fails, restore. Done for: affordable allow-list, rounding mode, status sort order, null-line guard, 400-before-404, exception handler, Guid/decimal registrations, blank value as null, text vs number inputs.
- **Sign-off before code:** calculation spec table, API contract, design plan; each approved by me before implementation.
- **Manual end-to-end runs:** `/health` timing with Mongo stopped; `schemes.http` replayed with curl; page loaded through the Vite proxy; screenshots at desktop and mobile widths.
- **Interactive visual review (step 3.5):** a small script drove headless Chrome (DevTools Protocol) to screenshot the list, the add form with real server errors (`2.5` homes, blank name), the edit panel, mobile, and keyboard focus; then checked against the frontend-design skill's list of AI tells.
- **I asked "did you do a full e2e test?"** The honest answer was no: API and components were tested separately, but a successful save through the real browser never was. Added one Playwright test of the whole journey (own API, Vite and database; uses installed Chrome). Mutation: removing the list refresh after save failed it at the first totals check.
- **Read before install:** both skills read in full and scanned for commands or hidden instructions before being added.

## 5. Deliberate judgement calls (including "not using" something)

- Followed the Zod skill's own "validate once" rule to **not** duplicate server validation in the form; Zod only where data enters the browser.
- Followed the frontend-design skill's process and avoid-list, but kept its "take aesthetic risk" to one element (the tenure bar), because this is an internal data tool.
- Kept validation in one gate (YAGNI) with a stated trigger to split it.
- Node: Vite 8 needed Node ≥ 22.12; I chose to upgrade Node via nvm rather than downgrade Vite.

## 6. Environment problems hit (not AI mistakes, but real)

- An interrupted session twice left a corrupted build cache in `obj/` (null bytes in a JSON file). Fixed by deleting `bin/` and `obj/`.
- Node 22.11 too old for Vite 8's native bundler; upgraded to 22.23.4.

## 7. Second-tool review: Antigravity (Gemini), refined prompt v2 (2026-11-04)

Raw output: `docs/ai/gemini-v2-review.txt`. 9 findings + 3 design challenges, every one checked against the code or a running API before triage.

| # | Gemini's claim | How it was checked | Verdict |
|---|---|---|---|
| 1 | POST with body `null` → NullReferenceException, HTTP 500 (High confidence) | Ran the API in Development, `curl -d 'null'` | **False.** 400: ASP.NET throws `BadHttpRequestException` before the validator runs, and our exception handler keeps its 400. It read the validator but not the framework behaviour. (Real but minor: the 400 has no field message.) |
| 2 | Summary shows "Not valued" + "excludes N unvalued homes" when all homes are unvalued | Read `Summary.tsx:22` vs `SchemeTable.tsx:68` and `format.ts value()` | **Valid.** Table has the `< homes` guard, summary doesn't. |
| 3 | DELETE endpoint missing "despite specification requirements" | Read the brief: "Add, edit and list schemes" | **False, and caused by Claude:** the v2 prompt (written by Claude) said "add, edit, list, filter and delete". Gemini trusted the prompt and called it a spec requirement without seeing the spec. Prompt corrected. |
| 4 | 500 test only passes because WebApplicationFactory runs in Production; Development leaks the stack trace | Ran the API in Development with a corrupt document | **False.** Clean ProblemDetails, no exception text. Same wrong belief Claude had earlier (section 2, #3), disproved the same way. |
| 5 | `Name` and `Homes` sort keys untested | grep tests: only Status, Value, Affordable sorts are exercised | **Valid** (mutation not yet run). |
| 6 | `EnsureIndexesAsync` never invoked in tests | It is invoked: every API test starts the host, which runs it. But no test asserts the index exists | **Half right:** a no-op mutation would survive. |
| 7 | `/health` 503 path untested | Only `Health_reports_ok_when_mongo_is_up` exists | **Valid.** |
| 8 | `MaxAddressLength` boundary untested | grep validator tests: name length only | **Valid** (mutation not yet run). |
| 9 | `SchemeSchema` (zod) not unit-tested | `api.test.ts` has 4 tests, summary + error paths | **To confirm** with a mutation. |
| D1 | Sorting/totals in memory | Deliberate (computed on read, small data) | Disagree for this scale; good "what next" item. |
| D2 | `Totals.Value` should be nullable | `UnvaluedHomes` already carries that state | Disagree; defendable either way. |
| D3 | `EnsureIndexesAsync` at startup blocks ~30s and crashes if Mongo is down, so `/health` can't report 503 then | Reading `Program.cs:27` confirms it | **Valid point.** /health 503 only works if Mongo dies after start. |

Lessons: 3 of 9 "High confidence" findings were false; two needed a running API to disprove, one was planted by our own prompt. It also read my AI-use notes in `docs/` (since merged into this file and the README), so it saw our earlier decisions.

## 8. Antigravity (Gemini) vague prompt v1, rerun on a clean clone (2026-11-04)

The first v1 run read `docs/ai/review-prompt-v2.md` from the workspace and followed it, so it was discarded. Rerun on a fresh `git clone` (no docs/, no prompts) with only "Review this project for issues." Raw: `docs/ai/gemini-v1-review.txt`.

With no rules it was not read-only: ran `npm install`, docker commands and the Playwright suite (the clone's git status stayed clean).

| Claim | Check | Verdict |
|---|---|---|
| 1.1 Startup crash if Mongo is down; "if Mongo takes 2-3 s to boot, the API crashes" | Program.cs:27 | **Valid core, overstated impact:** the driver waits up to 30 s for a server, so a slow boot is fine; only Mongo down >30 s crashes it. Same as v2's D3. |
| 1.2 No pagination, in-memory sort/summary, "DoS risk" | Deliberate, localhost | Generic scale advice; "what next" only. |
| 1.3 No optimistic concurrency on PUT | True | Real but single-user localhost; "what next". |
| 1.4a e2e reset uses `docker compose exec`, project name = folder name | `reset-database.ts`; its own e2e run failed in the clone | **Valid**, and it hit it for real. One-line fix: `name:` in docker-compose.yml. |
| 1.4b `channel: 'chrome'` fails on CI | Deliberate (uses installed Chrome) | Disagree for now; note for CI. |
| 2.1 Summary "Not valued" + "excludes N" | Same as v2 #2 | **Valid** (found by both prompts). |
| 2.2 Summary refetched on every sort; `Promise.all` couples failures | App.tsx | Valid, minor. |
| 2.3 "Infinity" passes the form check (`isNaN`), becomes `null` in JSON, API 400 without field errors, generic "check the API" message | SchemeForm.tsx `unreadableNumbers` | **Plausible, to confirm with a test.** Also `1e400`. Fix: `Number.isFinite`. |
| 2.4 No focus trap / focus return in the side panel | SchemeForm.tsx | Valid a11y point. |
| 2.5 Field errors stay while typing | `setErrors` only on submit | True; UX choice. |
| 2.6 Sub-pence values accepted | Validator has no scale check | Judgement call. |
| start.bat `curl` without `-f` treats a 503 as up | start.bat:38 | **Valid** small bug. |
| e2e/ and playwright.config.ts never type-checked | tsconfig includes only `src` and `vite.config.ts` | **Valid.** |
| No CORS | Vite proxy by design | **Reject:** adding CORS widens access for no need. |
| No DELETE | Brief: "Add, edit and list" | **Reject:** not required. (So v2's delete claim wasn't only from our prompt.) |
| No start.sh / Dockerfiles | | Left out deliberately (README, "Deliberately left out"). |

**v1 vs v2:** v1 found more real things in the UI and tooling (Infinity, curl -f, tsconfig, compose name) but mixed them with generic scale/security advice (pagination, CORS, DoS) and had no reproductions or confidence. v2 was tighter and found the test gaps via mutations, but 3 of its 9 "High confidence" findings were false. Lesson: the refined prompt improves the format, not the truth; verification is still the gate.

## 9. Codex (GPT 6.1-sol, high effort), clean clone (2026-11-04)

Raw output: `docs/ai/codex-v1.txt`. 2 findings, both checked against the code:

| Claim | Check | Verdict |
|---|---|---|
| A finished save closes whatever panel is open: save A, cancel, open B, A's response closes B and B's edits are lost | `App.tsx:44-50`: `save` awaits, then `setEditing(null)` regardless of what is open now. Only the Save button is disabled while saving; Cancel, Escape, "Add scheme" and row Edit still work | **Valid.** Neither Gemini run found it. Extra effect I found while checking: if A's save then *fails*, the error goes to A's form, which is already gone, so the user never sees it. |
| `Infinity` / `1e309` pass `Number.isNaN`, become `null` in JSON; for average value the API accepts null as "not valued", so a save can silently erase a valuation | `SchemeForm.tsx:51`; `TenureLineRequest(..., decimal? AverageValue)` | **Valid and sharper than Gemini's version.** Gemini only followed the homes path (non-nullable, so a 400). Codex found the worse path: average value is nullable, so no error at all, just lost data. To confirm with a test. |

Codex ran the tests, build and lint, changed nothing, and said what it did not run (e2e). 2 findings, 0 false: least noise of the three reviews.

## 10. Codex (GPT 6.1-sol, high effort), refined prompt v2, new session on the clean clone (2026-11-04)

Raw: `docs/ai/codex-v2.txt`. 4 findings, 0 design challenges, every one checked:

| # | Claim | Check | Verdict |
|---|---|---|---|
| 1 | Infinity / 1e309 in average value silently erases a valuation (High) | Same as Codex v1 #2 | **Valid.** Now rated High with a repro and a test plan. |
| 2 | A finished save closes whichever panel is open (Medium) | Same as Codex v1 #1 | **Valid.** |
| 3 | Affordable-sort test can't tell % from home count; swapping `.AffordablePercent` for `.Homes` survives | `SchemeSortingTests.cs:56`: empty (0 homes, 0%) vs half (2 homes, 50%), both keys give the same order | **Valid.** A mutation gap nobody else found. |
| 4 | Address changes on update are never tested; removing `.Set(SiteAddress)` survives | `Update_changes_editable_fields...` changes name and status only; API and e2e never change an address | **Valid.** |

Said it did not run tests this time, and its "checked and found sound" list matches what the other reviews and our own tests show. Did not repeat Gemini's false claims (null body, Development stack trace, DELETE). **4 for 4.**

## 11. Consolidated triage of all AI reviews

> **Rows 1–14:** confirmed by a failing test, a reproduction or a hand check, then fixed.
> **Rows 15–27:** drafted with the reviewer chat, then reviewed and confirmed by me (2026-11-04).

Evidence for each row is in sections 7-10.

**Accepted: bugs to fix**

| # | Finding | Found by | My verdict |
|---|---|---|---|
| 1 | A finished save closes whichever panel is open (newer draft lost); a failed save's error goes to a form that's gone | Codex v1+v2 (lost error: Claude) | Agree: reproduced by me 2026-11-04 in Chrome, 5000 ms throttling: saved A, cancelled, opened B, typed; A's response closed B and my draft was lost. |
| 2 | Infinity / 1e309: in average value silently erases a valuation; in homes gives a generic error | Codex v1+v2, Gemini v1 | Agree: confirmed by a failing test, fixed. |
| 3 | Summary shows "Not valued" + "excludes N unvalued homes" | Gemini v1+v2 | Agree: confirmed by a failing test, fixed. |
| 4 | `start.bat` health check without `curl -f` treats 503 as up | Gemini v1 | Agree: verified against a throwaway server returning 503, fixed. |
| 5 | e2e reset breaks when the repo folder has another name (compose `name:`) | Gemini v1 | Agree: reproduced in the clone folder, fixed. |
| 6 | Mongo down at startup crashes the API after ~30 s, so `/health` never gives its 503 (needs a design decision) | Gemini v1+v2 | Agree: chose background retry (option A); fixed and tested. |

**Accepted: test gaps (each proven with a mutation)**

| # | Gap | Found by | My verdict |
|---|---|---|---|
| 7 | Name and Homes sorts untested | Gemini v2 | Agree: test added, mutation-checked. |
| 8 | Affordable-sort test can't tell % from home count | Codex v2 | Agree: test added, mutation-checked. |
| 9 | Address change on update untested | Codex v2 | Agree: test added, mutation-checked. |
| 10 | `/health` 503 path untested | Gemini v2 | Agree: test added after #6 made it possible. |
| 11 | Address length limit (500) untested | Gemini v2 | Agree: test added, mutation-checked. |
| 12 | Index is created but never asserted | Gemini v2 (half right) | Agree: test added, mutation-checked. |
| 13 | zod `SchemeSchema` not unit-tested (to confirm) | Gemini v2 | Agree: confirmed, test added, mutation-checked. |
| 14 | e2e code and Playwright config never type-checked | Gemini v1 | Agree: fixed; planted type error now caught. |

**Valid but parked ("what next")**

| # | Finding | Found by | Why parked | My verdict |
|---|---|---|---|---|
| 15 | No optimistic concurrency on PUT | Gemini v1 | Single user, localhost | agree, park. Real lost-update risk with several users; add a version field and 409 when the app goes multi-user. |
| 16 | Summary refetched on every sort; its failure breaks the table | Gemini v1 | Minor at this size | agree, park. Split the summary into its own effect if it ever gets slow or flaky. |
| 17 | No focus trap / focus return in the side panel | Gemini v1 | Accessibility polish | agree, park. Worth doing with a native `<dialog>` next. |
| 18 | Field errors stay while typing | Gemini v1 | UX choice | agree, park. Clearing an error on edit is nicer; current behaviour is consistent (errors refresh on save). |
| 19 | No limit on decimal places for money | Gemini v1 | Judgement call | agree, park. Valuations are whole pounds in practice; a 2 dp rule is one line when needed. |
| 20 | Sorting and totals in memory | Gemini v1+v2 | Deliberate at this scale | agree, park. Fine at this size; store totals or use an aggregation at thousands of schemes. |
| 21 | Playwright uses installed Chrome, fails on CI | Gemini v1 | No CI in scope | agree, park. Switch to bundled Chromium when CI is added. |

**Disagreed**

| # | Claim | Found by | Why rejected | My verdict |
|---|---|---|---|---|
| 22 | Null body gives a 500 | Gemini v2 | Tested: it's a 400 (framework rejects it first) | reject. Tested with curl in Development: 400, not 500. |
| 23 | 500 leaks the stack trace in Development | Gemini v2 | Tested in Development: clean ProblemDetails | reject. Tested in Development with a corrupt document: clean ProblemDetails. |
| 24 | DELETE is a spec requirement | Gemini v1+v2 | Brief says "Add, edit and list"; v2 claim came from our prompt | reject. The brief asks for add, edit and list; delete is a deliberate omission. |
| 25 | Add CORS | Gemini v1 | Same-origin via Vite proxy; CORS would widen access | reject. Same-origin via the Vite proxy; CORS would only widen access. |
| 26 | `Totals.Value` should be nullable | Gemini v2 | `UnvaluedHomes` already carries that state | reject. `UnvaluedHomes` already says how much is missing; nullable totals would complicate every sum. |
| 27 | Pagination / DoS risk | Gemini v1 | Localhost, small data | reject for now. Localhost, small data; paging is a "next" item, not a fix. |

## 12. Fix log

**#1 save closes the wrong panel: fixed with option A (lock the panel while saving)**, chosen over Codex's "close only the panel that started it", which would have left the cancelled-but-still-saved surprise and the lost error. `App` owns `saving` (single source; the form's own copy removed); Add scheme, row Edit, Cancel and Escape are blocked until the save settles; `finally` unlocks on failure so the error shows in the still-open form. Tests: `web/src/App.test.tsx` (2), written first: the lock test failed before the fix.
Mutations: removing the table lock and removing the unlock-on-failure each fail the tests. **Removing the Escape guard first SURVIVED** (Claude's test was wrong): the disabled Save button drops focus out of the panel, so the test's Escape never reached it. Test fixed to click back into the Name field first, as a user would; the mutation now fails.

**#2 Infinity / 1e309 in number fields: fixed.** `unreadable()` in SchemeForm.tsx: blank is allowed, anything else must pass `Number.isFinite` (was `Number.isNaN`, which lets Infinity through). Tests: `it.each` over Infinity, -Infinity, 1e309 in homes and average value, written first; all 3 failed before the fix. Mutations: back to `isNaN` fails the 3 new tests; dropping the blank (null) check fails 3 existing tests (blank value must stay "not valued").

**#3 Summary "Not valued" + "excludes N": fixed.** Same guard as the table (`unvaluedHomes < homes`). `Summary.test.tsx` (3 cases: partly, none, fully valued), written first; the "none valued" case failed before the fix. Mutations: dropping `> 0` fails "fully valued"; `<=` instead of `<` fails "none valued".

**#4 start-up scripts: fixed.** (a) `start.bat`: `curl -f` on both readiness checks. Checked against a throwaway server returning 503: without `-f` exit 0 (counted as ready), with `-f` exit 22, nothing listening exit 7 (loop still waits). (b) `docker-compose.yml`: `name: scheme-pipeline`. In the clone folder, before: `docker compose exec mongo` → "service mongo is not running"; after: finds `scheme-pipeline-mongo-1`. Existing container/volume unchanged (project name was already the folder name). No unit tests: scripts, checked by hand.

**#5a backend test gaps (rows 7, 8, 9, 11, 12): tests added, 75 → 81.** Each checked by a mutation that fails exactly the new test: Name key removed; Name compare case-sensitive; Homes key removed; Affordable by home count (Codex's mutation); update drops address (fails store + API tests); update drops tenures (a gap **I found while adding the address check**: no test changed tenures on update either); EnsureIndexes a no-op; address limit 499. Sources restored with `git checkout` after each (avoids the old sed-restore indentation trap). Build used `--artifacts-path` because the dev API from start.bat had bin/ locked.

**#5b frontend test gaps (rows 13, 14).** Row 13 confirmed: the scheme list schema had no unit test. Added a valid .NET-shaped scheme plus 4 rejects (bad uuid, non-ISO date, unknown status, 2.5 homes); 4 mutations (`z.string()` for id/date/status, `z.number()` for homes) each fail exactly their test. Row 14: `tsconfig.node.json` now includes `e2e` and `playwright.config.ts`. Proof: a planted type error in `e2e/reset-database.ts` fails `tsc -b` now and passed silently with the old config. No hidden type errors in the existing e2e code. Frontend 27 → 32 tests.

**#6 startup crash when Mongo is down + row 10 (/health 503 untested): fixed with option A (background retry).** `Data/IndexInitializer.cs`: a `BackgroundService` that tries to create the indexes, each attempt capped at 5 s, retrying every 5 s until it succeeds, then stops. `Program.cs` no longer awaits index creation before `app.Run()`. Safe because the index only speeds up the status filter.
Tests (3 new, 81 → 84): `StartupTests` (API pointed at a closed port: /health gives 503 within 10 s), written first: it failed with the exact bug (30 s `TimeoutException`, host never started). `IndexInitializerTests` (fails twice, succeeds, then stops: 3 calls). `Startup_creates_the_status_index_in_the_background` (normal startup still creates the index).
Mutations: hosted service not registered; no retry; no stop after success; blocking startup restored. Each fails exactly one test. Not covered: the 5 s per-attempt cap (only affects how soon the warning is logged).
The 503 test was impossible before this fix: a missing test exposed the design flaw. Both AI reviews flagged the crash; neither linked it to the untested 503 path.

## 13. Claude reviews (2026-11-04)

Three runs, outputs in `docs/ai/`:
- **/code-review high on the 7 fix commits** (`claude-code-review.txt`). Asked for the whole project; it reviewed only the diff against origin. 10 findings.
- **/security-review, all tracked files** (`claude-security-review.md`). No High/Medium. DNS rebinding (`AllowedHosts: "*"`) considered and rejected at 0.5 confidence. Needed `git remote set-head origin main` first: the skill diffs against `origin/HEAD`, which a pushed-not-cloned repo doesn't have.
- **/code-review high, new session on the clean clone at c514c4d** (`claude-code-review-full.txt`), the same commit Codex and Gemini saw. 10 findings, no false ones. Independently re-found Codex #1 (save race), Codex #2 (Infinity) and the Summary caution (3 tools now).

**Found twice independently:** 1e30 (finite but beyond C# decimal) passes the #2 fix → API binding 400 with no field errors → form says "Check the API is running". Both Claude runs found it; it is a hole in my own #2 fix.

**AI claims checked and partly wrong:** diff review #5 said our comment claims "only plain numbers"; it doesn't (behaviour claim right, quote wrong). Diff review #9 said disabled links have "no :disabled style"; the general `button:disabled` rule already fades them; only the hover underline was wrong.

**Fix log (all frontend 36 tests green, lint + tsc clean):**
- **1e30 / hex / exponent input: fixed (3351f85).** `readNumber` accepts only plain decimals (regex), `unreadable` caps at `Number.MAX_SAFE_INTEGER` (bigger can't be sent exactly). Tests: 0x10, 1e5, 1e30, 20 nines added to the it.each, written first: 4 failed. Mutations: drop regex → 0x10 and 1e5 fail; back to `isFinite` only → 20 nines fails.
- **Shadowed `unreadable` local: renamed `numberErrors`** (same commit, refactor, no test).
- **Disabled link hover: fixed (0da8fc0).** `button.link:hover:not(:disabled)`. CSS, not unit tested.
- **e2e reset cwd: fixed (b1f9b0b).** `cwd: resolve(import.meta.dirname, '..', '..')`. Red by calling the reset from the repo root ("no configuration file provided"); green from root and from web/. Full Playwright run not done: the dev API from start.bat held bin/ locked.

## 14. Claude review findings (verdicts drafted with the reviewer chat, then reviewed and confirmed by me, 2026-11-04)

From `docs/ai/claude-code-review.txt` (D#: `/code-review` of my fix commits) and `docs/ai/claude-code-review-full.txt` (F#: `/code-review` of the whole repo on the clean clone). Already handled elsewhere: D2/F2 (1e30), D5 (hex/exponent), D9 (hover), D10 (shadowing), F1 and F4 (fixed in section 12), F3 (= row 15), F5 (= row 3), F7 (fixed in section 13), F9 (= row 16).

| # | Finding | Found by | My verdict |
|---|---|---|---|
| D1 | The save lock has no way out if the API hangs: Cancel and Escape are blocked, no timeout. Introduced by my fix for row 1. | Claude `/code-review`, fixes | **Fixed (ae0f589).** Create and update give up after 15 s; the panel unlocks and says the save may still have gone through, so the user reloads instead of retrying blind (a duplicate risk). Tests written first (3 failed); mutations: 14 s, 30 s and update without the timeout each fail. Node's `AbortSignal.timeout` ignores Vitest's fake clock, so the signal is built on `setTimeout`. |
| D3 | `IndexInitializer` treats every exception as transient: retries forever and logs a full stack trace at Warning about every 10 s. | Claude `/code-review`, fixes | Park: localhost, and retrying until Mongo is up is the intent. Next: back-off and a shorter log line. |
| D4 | The 5 s per-attempt cap also cancels an index build that is merely slow. | Claude `/code-review`, fixes | Reject for now: one index on a small collection builds in milliseconds. Note it for large data. |
| D6 | Background index creation may race the first insert in a fresh test database (possible flaky 500). | Claude `/code-review`, fixes | Reject: `dotnet test` run 10 times, 840/840 passed. Revisit if it ever appears. |
| D7 | The fixed compose project name orphans an old container started under another folder name. | Claude `/code-review`, fixes | Reject: the folder was already called `scheme-pipeline`, so nothing changed for this repo; a one-off `docker compose down` cleans any other clone. |
| D8 | The summary's "all homes unvalued" rule is duplicated outside `format.ts`. | Claude `/code-review`, fixes | Park: two short conditions; one helper if a third place needs it. |
| F6 | Portfolio totals sum `int` homes with checked arithmetic: about 5,400 maxed-out schemes overflow and `/api/summary` returns 500. | Claude `/code-review`, full repo | Park: unrealistic for one developer's pipeline (about 2 billion homes). Cheap fix if wanted: sum as `long`. |
| F8 | Playwright may start the API before the reset drops the e2e database, so the e2e run has no status index. | Claude `/code-review`, full repo | Park: the index only speeds up the filter; behaviour is the same. |
| F10 | `OneOf<T>()` exists but the body messages still inline `string.Join(...)`. | Claude `/code-review`, full repo | Park: same wording today; worth tidying with the next validator change. |
