# Reviewer chat excerpts (Claude, claude.ai)

A separate Claude chat reviewed each phase before I committed it. It never saw the builder's reasoning, only the code and the builder's reports that I pasted in. These are the exchanges that mattered, quoted from that chat (trimmed, not reworded). A share link to the full chat can be provided on request.

---

## 1. Refining a prompt: scoping the data-model review

**My first prompt:** "do u agree with this domain" (followed by `Scheme.cs`)

**Reviewer:** six points mixing the model, where calculations live, and layering, including:
> "2. **Missing calculations.** The derived figures belong in the domain so the API and UI can't drift apart."

**Me:** "but this is the domain model proposal only"

**Reviewer:** "Fair point. As a data model, it's fine; point 2 isn't a flaw in it."

**My second prompt:** "as a data model only, do u agree with it or uu have any notes ?"

**Reviewer:** "Yes, I agree with it. Just four small notes:" (summarised: use `IReadOnlyList<TenureLine>`; decide tenure uniqueness; use `Guid Id`; use `DateTimeOffset` or document UTC).

**What changed:** stating the scope. The vague prompt got a review of everything, including a point that wasn't about the model; the scoped prompt got four notes I could act on. I accepted `Guid`, deferred tenure uniqueness to the validator, and kept `List<>` and `DateTime` (UTC).

---

## 2. Spec before code: the calculation table

I posted a table of test cases with expected values before any maths code existed and asked: "Please check rows 1 and 9 by hand."

**Reviewer:**
> "Row 1: 2.4M (SR) + 1.5M (AR) + 3.9M (SO) + 17.6M (MS) = £25,400,000, and 45 affordable out of 100 homes is 45.0%. ✓
> Row 9: 1 ÷ 16 = 6.25% exactly … Away-from-zero rounding gives 6.3; to-even would give 6.2. ✓"

It also suggested an extra row (0 homes with a non-zero value → value 0), which I added later.

---

## 3. A crash the builder missed

Reviewing the validator (step 1.4), the reviewer found:
> "**Bug: a null tenure line crashes it.** JSON `"tenures": [null]` deserialises fine, then `line.Tenure` throws a `NullReferenceException`, so the API returns a 500 instead of a 400."

The builder had listed three "questions to challenge" about its own code; none covered this. The fix came with a test and a mutation check (commit `a95c639`).

---

## 4. A domain pushback (mine), improved by the builder

I challenged the rule "average value required when homes > 0": at appraisal a developer knows the home count before the values, so the rule forces a fake price. The builder's answer is in `screenshots/04-appraisal-pushback.png`: it agreed and proposed `null` for "not valued yet" plus an `UnvaluedHomes` count.

**Reviewer:**
> "Approve it. The proposal is better than your original 'allow 0' idea … you spotted the problem, and the AI improved your fix."

It asked for two extra test rows, which I added: unvalued homes still count as homes and as affordable (SR 20 unvalued + MS 10 @ £300k → 30 homes, 20 affordable, 66.7%), and 0 homes with a value → value 0, unvalued 0 (commit `dcb5608`).

---

## 5. Where the reviewer was wrong

- **Fractional homes.** The reviewer said typing `2.5` homes would give "a plain 400 that has no field errors". When I questioned it, the builder ran curl against the real API: an **HTTP 500**. Both AIs had reasoned about it; only running it showed the truth (fixed in `06fac9c`).
- **Decimal storage.** The reviewer warned that the Mongo C# driver stores `decimal` as a string by default. That was true of driver 2.x; 3.x stores `Decimal128`. A raw-BSON test now proves it rather than trusting either AI.
- **Interfaces.** The reviewer first said storage should sit behind an interface, then reversed once it saw the store tested against a real Mongo container: "That's defensible, arguably better." I kept the concrete class.
- **An overclaim I chose not to pursue.** The builder wrote "Multiplying before dividing keeps 1/16 an exact 6.25"; the reviewer pointed out that 1/16 is exact in `decimal` either way. The code was right, only the explanation was wrong, so I left it.
