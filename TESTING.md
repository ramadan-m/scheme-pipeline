# Manual test guide

Start the app (see README: `start.bat`, or the three commands). Open http://localhost:5173. Each step says what you should see.

1. **Empty state.** With no data you see "No schemes yet." and an **Add scheme** button.
2. **Add a scheme with unvalued homes.** Add scheme → Name `Riverside`, Site address `12 Mill Lane`, Social rent homes `20` (leave its value blank), Market sale homes `10` at `300000` → Save scheme. The row shows 30 homes, **66.7%** affordable, **£3,000,000** and "20 unvalued". The summary says "excludes 20 unvalued homes".
3. **Validation.** Add scheme → leave Name blank, type `2.5` in a homes field → Save. Each field shows its own message; nothing is saved. Type `abc` or `Infinity` in an average value field: it says "Enter an amount in pounds, for example 250,000, or leave it blank."
4. **Edit.** Click `Riverside`, give social rent a value of `120000`, set Status to Planning → Save. The row shows **£5,400,000**, Planning, and no "unvalued" note.
5. **Filter.** Add a second scheme with status On site. Set the Status filter to On site: only that scheme is listed. Set it back to All.
6. **Sort.** The list starts in pipeline order (Appraisal → Planning → On site → Complete, not alphabetical); clicking the Status header reverses it. Click Value once for lowest first, again for highest first.
7. **Persistence.** Reload the page: everything is still there.
8. **API down.** Stop the API, reload: the page says it couldn't load schemes, instead of showing a blank table. `http://localhost:5080/health` returns 503 within about 2 s if MongoDB is stopped.

API-level checks are in `schemes.http`.
