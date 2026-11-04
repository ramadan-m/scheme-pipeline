You are reviewing a small take-home project for a developer job interview. I want an honest, evidence-based review, not a long list of generic advice.

## Ground rules
- READ-ONLY. Do not edit, create or delete any file. Do not install packages, change config, or run git commands.
- The only commands you may run are `dotnet test` (repo root) and `npm test` (in `web/`), and only if you need them to confirm a finding.
- If you are not sure something is a real problem, leave it out or mark it Low confidence. Reporting nothing is better than guessing.

## The project
A housing-scheme pipeline app: users add, edit and list housing schemes, filter or sort them by status (each with a mix of tenures, home counts and values) and see computed totals.
- `api/SchemePipeline.Api/`: .NET 10 Minimal API. `Http/` = request/response contracts, validator, endpoints. `Domain/` = scheme model, maths, sorting. `Data/` = MongoDB store.
- `api/SchemePipeline.Tests/`: xUnit unit tests + integration tests against a real Mongo (Testcontainers).
- `web/src/`: React 19 + TypeScript + Vite front end; zod validates API responses.
- `web/e2e/`: one Playwright end-to-end journey.

## Threat model and context
- Runs locally on one developer machine. API and MongoDB are bound to 127.0.0.1. There are no user accounts and no authentication, by design (it's an exercise, not production).
- So do NOT report "add authentication", "add rate limiting", "add HTTPS", "add logging/monitoring" or "use a repository pattern / clean architecture" unless you can show a concrete bug they would prevent here.
- Out of scope: formatting, naming taste, comment style, package versions.

## What I want you to look for (in this priority order)
1. **Correctness:** wrong totals or rounding, decimal/precision loss, null or empty handling, off-by-one, sorting/filtering mistakes, create vs update inconsistencies, wrong HTTP status codes.
2. **Validation gaps:** input the API accepts but should reject, or rejects but should accept. Check the validator against what the store and UI actually assume.
3. **API contract drift:** anywhere the C# contracts, the zod schemas in `web/src/api.ts` and the UI disagree.
4. **Security that applies even on localhost:** injection into Mongo queries, unsafe deserialisation, CORS mistakes, secrets in the repo, XSS in the React UI.
5. **Test weaknesses:** important behaviour with no test, or tests that would still pass if the code were broken (name the mutation that would survive).
6. **Design challenges:** if you think a deliberate-looking choice is wrong, list it separately and argue the case. Don't mix these into the bug list.

## Output format
Start with a one-paragraph overall verdict. Then one table per section:

**Findings** (sections 1–5), most severe first:

| # | Severity | Confidence | File:line | Problem | How to reproduce (exact input/steps) | Expected vs actual | Suggested fix | Test that would catch it |
|---|---|---|---|---|---|---|---|---|

- Severity: High = wrong data or crash a user can hit; Medium = wrong behaviour in an edge case; Low = robustness/clarity issue with no user-visible effect today.
- Confidence: High = you traced the code path or ran it; Medium = likely from reading; Low = suspicion.
- Every finding needs a real file and line number from this repo. No finding without a reproduction.

**Design challenges** (section 6):

| # | Choice | Why you'd do it differently | Trade-off you accept |
|---|---|---|---|

Finish with a short list of things you checked and found to be fine, so I know what was covered.
