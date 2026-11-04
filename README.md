# Scheme Pipeline

A small app for a housing developer to track the schemes in their pipeline. Each scheme has a name, a site address, a status (Appraisal, Planning, On site, Complete) and a mix of homes across four tenures (social rent, affordable rent, shared ownership, market sale), each with a number of homes and an average value. The app shows, per scheme, the total homes, the affordable percentage and the total value, plus a summary across all schemes.

**Where to start:** run it (below), then read `api/SchemePipeline.Api/Domain/` (the business rules, about 70 lines) and `api/SchemePipeline.Tests/SchemeMathsTests.cs` (the rules as examples). `TESTING.md` is a short manual test guide.

## How to run it

**Prerequisites:** .NET SDK 10, Node.js 22.12 or later, Docker (for MongoDB and the integration tests). Google Chrome for the end-to-end test.

**Windows, one click:** `start.bat` starts MongoDB, the API and the web app, then opens http://localhost:5173.

**Any OS, by hand** (three terminals, from the repo root):

```bash
docker compose up -d                      # MongoDB on 127.0.0.1:27017
dotnet run --project api/SchemePipeline.Api   # API on http://localhost:5080
cd web && npm ci && npm run dev           # web app on http://localhost:5173
```

`schemes.http` has ready-made API requests for VS Code or Visual Studio.

## How to run the tests

| What | Command | Needs |
|---|---|---|
| .NET unit and integration tests (84) | `dotnet test` | Docker running (Testcontainers starts a throwaway MongoDB) |
| Web unit tests (39) | `cd web && npm test` | |
| End-to-end journey (Playwright) | `docker compose up -d`, then `cd web && npm run e2e` | Chrome; uses its own API, port and database, so dev data is untouched |
| Lint and type-check | `cd web && npm run lint && npm run build` | |

## Structure

| Folder | What's in it |
|---|---|
| `api/SchemePipeline.Api/Domain/` | The model and the pure business rules: affordability, totals, sorting. No HTTP or database code. |
| `api/SchemePipeline.Api/Http/` | Request and response contracts, the validator, the endpoints. |
| `api/SchemePipeline.Api/Data/` | MongoDB storage and background index creation. |
| `api/SchemePipeline.Tests/` | xUnit: domain, validator, store and API tests (the last two against a real MongoDB). |
| `web/src/` | React + TypeScript: typed API client (zod-checked), table, summary, form, tenure mix bar. |
| `web/e2e/` | One Playwright test of the whole add, edit, filter and sort journey. |
| `docs/` | How AI was used, with the raw evidence. |

## Data design and validation

**Stored:** a scheme is one MongoDB document: `id` (a time-ordered GUID), `name`, `siteAddress`, `status`, `tenures`, `createdAt`, `updatedAt` (UTC). `tenures` always holds all four tenures in a fixed order; a missing tenure is stored as 0 homes. Each line has `homes` (whole number) and `averageValue` (decimal, or **null meaning "not valued yet"**, common at appraisal). Enums are stored as strings and money as Decimal128, so the documents read naturally and reordering an enum can't corrupt data.

**Calculated, never stored:** totals are computed on every read from one function (`SchemeMaths`), so they can't go stale.
- **Affordable** = social rent + affordable rent + shared ownership (one allow-list).
- **Affordable %** = affordable ÷ total homes, to 1 decimal place, halves rounded up (6.25 → 6.3). It is null ("—"), not 0%, when there are no homes.
- **Value** = Σ homes × average value over the valued lines. `unvaluedHomes` says how many homes the value leaves out.
- **Portfolio summary** = the same calculation over every line of every scheme, so the percentage is weighted by homes, not an average of percentages.

**Validation** happens in one place, the API (`SchemeValidator`). It returns every error at once, keyed by field (`tenures[2].homes`) so the form shows each message under its input.
- Name and site address are required, trimmed, at most 200 and 500 characters.
- Status and tenure must be exact names (`"2"` or `"onsite"` are rejected in the body); each tenure may appear at most once; an empty tenure line is an error.
- Homes: a whole number from 0 to 100,000. Average value: optional; if given, more than £0 and at most £100,000,000 (a £0 home is treated as a typo now that "unknown" is null).
- Unknown filter or sort values in the query return 400 rather than silently returning everything.
- The form only checks that number fields hold plain numbers JavaScript can send exactly; all other rules come from the API, so there is one source of messages.

## Decisions and trade-offs

- **Unvalued homes are allowed.** The first version required a value whenever homes > 0. At appraisal a developer knows the home count before the values, so that forced fake prices. Value became nullable, and the UI marks partly valued totals.
- **Pure domain, concrete store.** The rules are static pure functions, tested directly. The store has no interface: it's tested against a real MongoDB container, which catches what a fake would hide (decimal and GUID storage, enum mapping). The cost is that `dotnet test` needs Docker.
- **Validation in the HTTP layer.** It mixes input parsing with domain rules. Fine with one entry point; I'd move the invariants into the domain when a second one appears (an import job, say).
- **Sorting and totals in memory**, because the totals are computed rather than stored. Fine at this scale.
- **PUT to an unknown id with an invalid body returns 400**, because the body is validated before the database is touched.
- **Hand-written TypeScript types, checked at runtime with zod.** Drift fails loudly in the browser instead of showing `NaN`; the trade-off is that one bad record makes the list unreadable.
- **The edit panel locks while saving**, so a late response can't close a different form. A save gives up after 15 s so a hung API can't keep it locked; the message says the save may still have gone through, so the user reloads rather than retrying into a duplicate.
- **Deliberately left out:** delete (the brief asks for add, edit and list), authentication, paging, conflict detection for simultaneous edits, CORS (the Vite proxy makes it same-origin), Dockerfiles and a non-Windows start script.

## What I'd do next

1. A version check on update (409 on conflict) before more than one person uses it.
2. Generate the TypeScript types from the API's OpenAPI spec so a contract change fails the build.
3. Move the domain invariants out of the validator into the domain.
4. Delete or archive, paging, and stored totals or a MongoDB aggregation once there are thousands of schemes.
5. Focus handling for the side panel (a native `<dialog>`), and Dockerfiles plus a cross-platform start script.

## How I used AI

**Tools and why**

| Tool | Used for | Why this one |
|---|---|---|
| Claude Code (Opus 5.5) | Building, phase by phase, with tests | Works in the repo: runs the tests, Docker and the API, so it can prove its own claims |
| Claude (claude.ai chat) | An independent review of every phase; explaining housing terms (in Arabic); drafting this README, `TESTING.md`, the reviewer excerpts and first-draft triage verdicts from my logs, which I then checked and edited | A separate context that never saw the builder's reasoning, so it wasn't grading its own work |
| Codex (GPT 6.1-sol), Gemini (Antigravity), Claude Code `/code-review` and `/security-review` | The final review before submitting | Different models miss different things; I compared them on the same commit |
| Skills: `frontend-design` (Anthropic, Apache-2.0), `zod` (community, MIT) | UI design process; zod patterns | Read and checked for commands and URLs before adding, because a skill runs with the agent's permissions |

**How I worked.** Small phases, each ending at a gate where I accepted, rejected or deferred every point with a reason. For the logic that matters, I signed off the test cases and expected values before the code existed (the calculation table, the API contract, the design plan). The builder then proved its tests with mutation checks: break the code on purpose and confirm the right test fails.

**What I kept and what I rejected**
- **My pushback that changed the design:** the appraisal-stage value rule (above). The builder then improved my fix (null plus a count instead of "allow 0").
- **Caught by the reviewer, missed by the builder:** `"tenures": [null]` crashed the validator with a 500.
- **Both AIs wrong, caught by running it:** both predicted `2.5` homes would give a 400; curl against the API showed a 500, and `type="number"` silently saved `12abc` as 0 homes.

**Final AI review: what it found, what I fixed, what I disagreed with.** Codex and Gemini (each with a vague and a refined prompt) and Claude Code `/code-review` reviewed the same commit in a clean clone; `/security-review` read every tracked file (no high or medium issues); a second `/code-review` checked my fixes. Every finding was checked against the code or the running API (the builder ran the checks; I reviewed the evidence) before I decided. Full triage: `docs/ai-examples-raw.md`, sections 7–14.

| Finding | Found by | Verdict |
|---|---|---|
| A slow save closed whichever panel was open by then, losing the draft in it | Codex, Claude | **Fixed.** Reproduced in Chrome with throttling first; the panel now locks while saving. |
| That lock had no way out if the API hung | Claude (review of my fixes) | **Fixed.** A save gives up after 15 s and unlocks the panel. |
| `Infinity` in a value field was sent as `null`, silently erasing the valuation; `1e30` said "check the API is running" | Codex, Gemini, Claude | **Fixed.** The form accepts only plain numbers JavaScript holds exactly. The `1e30` hole was in my first fix; the review of the fixes found it. |
| The summary showed "Not valued" and "excludes N unvalued homes" together | Gemini, Claude | **Fixed.** |
| With MongoDB down, the API crashed at startup, so `/health` could never return its 503 | Gemini | **Fixed.** Indexes are now created in the background, with retries. |
| `start.bat` treated a 503 as ready; the e2e reset broke when run from another folder | Gemini, Claude | **Fixed.** |
| Test gaps: name and homes sorts, affordable sort (% vs count), address on update, address limit, the index, the zod list schema, `/health` 503, untyped e2e code | Gemini, Codex | **Fixed.** Each new test was shown to fail when the code is broken on purpose. |
| DELETE is missing "despite the specification" | Gemini | **Disagreed.** The brief asks for add, edit and list. In the refined-prompt run the claim came from my review prompt (drafted with Claude), which wrongly listed delete. |
| A `null` body gives a 500; Development leaks stack traces (both "High confidence") | Gemini | **Disagreed.** Ran the API: a 400, and a clean error in Development. |
| Add CORS; add paging against DoS | Gemini | **Disagreed.** Same-origin through the Vite proxy; localhost and small data. |
| Simultaneous edits silently overwrite each other | Gemini, Claude | **Parked:** first item under "What I'd do next". |

Three findings marked "High confidence" were false; two needed the running API to disprove, and one was planted by my review prompt (drafted with Claude; neither of us caught the error before the run).

**A prompt I refined.** With Claude's help, the review prompt went from "Review this project for issues" (v1) to `docs/ai/review-prompt-v2.md`: a threat model, what not to report, and a required reproduction, file and line for every finding. v2 removed the generic advice (CORS, DoS, pagination) and found the test gaps through mutations, but it didn't make the findings truer: 3 of Gemini's 9 v2 findings were still false. The format improved; verification was still the gate.

**Where I didn't use AI**
- Testing the UI by hand: I clicked through it in the browser myself rather than trusting tests the AI wrote, and reproduced the save race in Chrome with network throttling before accepting the fix.
- Confidentiality: no code or material from a current or former employer was given to any tool.

**Evidence** (all in `docs/`)
- `docs/ai-examples-raw.md`: the full log of pushbacks, AI mistakes, reviews, triage and fixes, with commit hashes.
- `docs/ai/screenshots/` (with captions) and `docs/ai/reviewer-chat-excerpts.md`: the build-phase interactions that mattered.
- `docs/ai/review-prompt-v2.md`, `codex-v1.txt`, `codex-v2.txt`, `gemini-v1-review.txt`, `gemini-v2-review.txt`, `claude-code-review.txt`, `claude-code-review-full.txt`, `claude-security-review.md`: the final review prompt and raw outputs.
