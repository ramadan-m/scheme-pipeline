# Claude Security Review: scheme-pipeline (all tracked files)

Date: 2026-11-04

**No HIGH or MEDIUM vulnerabilities found.** I listed every tracked file with `git ls-files` and read all of the non-test source and config. Nothing came close to the 0.8 confidence bar, so there was nothing to send to the false-positive filtering step.

## What was checked

| Area | Result |
|---|---|
| **NoSQL injection** | No user input reaches a Mongo filter, sort or field name. The only filter is a typed `Eq` on an enum (`api/SchemePipeline.Api/Data/SchemeStore.cs:36-38`). Lookups use a typed lambda on a `Guid`, and the `{id:guid}` route constraint rejects anything else. Updates set only fixed, typed fields (`SchemeStore.cs:56-61`). Sorting happens in memory using an enum (`Domain/SchemeSorting.cs`). Enum parsing accepts only declared names (`Http/SchemeValidator.cs:75-80`). There are no regex, raw `BsonDocument` or JSON-string filters. |
| **Deserialization** | Request objects are plain records of strings and decimals, read with System.Text.Json. Nothing reads a type name from the request (no `$type` or `TypeNameHandling`). The database classes map to sealed records. |
| **Mass assignment** | The server always sets `Id`, `CreatedAt` and `UpdatedAt`. Requests don't carry them, and `UpdateAsync` writes only Name, SiteAddress, Status, Tenures and UpdatedAt. |
| **Auth and network exposure** | The API has no authentication, but as deployed it isn't reachable from outside the machine. Kestrel binds `localhost:5080`, and Mongo is published only on `127.0.0.1:27017` in `docker-compose.yml`. |
| **CORS and CSRF** | There is no CORS policy, so other sites can't read responses. JSON body binding requires `application/json`, which makes the browser send a preflight check that fails. A `text/plain` request gets a 415. |
| **Error and debug exposure** | `UseExceptionHandler` returns a generic 500. `/health` returns a fixed "Database unavailable" title and logs details only on the server. 404 responses repeat only the GUID. |
| **Secrets** | The only connection string is `mongodb://localhost:27017/scheme_pipeline`, which has no credentials. No keys or tokens are in the repo. |
| **XSS** | `web/src` contains no `dangerouslySetInnerHTML`, `innerHTML`, `eval`, `new Function` or `window.open`. All rendering is plain React text. `api.ts` builds paths from fixed strings plus a server-issued UUID and checks responses with zod. |
| **Command execution** | `web/e2e/reset-database.ts` runs a fixed command string and is test-only. `start.bat` takes no untrusted input. The `vite.config.ts` proxy target comes from an environment variable, which is trusted. |

## Considered and rejected

- **DNS rebinding against the localhost API** (`AllowedHosts: "*"` and no auth). In theory, a malicious website could reach the local API this way. It depends on the browser (recent Chrome versions block it), and the data is low-sensitivity housing-scheme records. Confidence is about 0.5, which is below the reporting bar.
