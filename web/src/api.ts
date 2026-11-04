import { z } from 'zod'

// Schemas mirror the API's JSON contract (see schemes.http). Types are derived from them,
// so the shape is written once and also checked at runtime when responses arrive.

export const STATUSES = ['Appraisal', 'Planning', 'OnSite', 'Complete'] as const
export const TENURES = ['SocialRent', 'AffordableRent', 'SharedOwnership', 'MarketSale'] as const

const TenureLineSchema = z.object({
  tenure: z.enum(TENURES),
  homes: z.int().nonnegative(),
  averageValue: z.number().nullable(),
})

const TotalsSchema = z.object({
  homes: z.int().nonnegative(),
  affordableHomes: z.int().nonnegative(),
  affordablePercent: z.number().nullable(),
  value: z.number(),
  unvaluedHomes: z.int().nonnegative(),
})

const SchemeSchema = z.object({
  id: z.uuid(),
  name: z.string(),
  siteAddress: z.string(),
  status: z.enum(STATUSES),
  tenures: z.array(TenureLineSchema),
  totals: TotalsSchema,
  createdAt: z.iso.datetime(),
  updatedAt: z.iso.datetime(),
})

// Only the part of a 400 body the form needs: field name -> messages.
const ValidationProblemSchema = z.object({
  errors: z.record(z.string(), z.array(z.string())),
})

export type SchemeStatus = (typeof STATUSES)[number]
export type Tenure = (typeof TENURES)[number]
export type TenureLine = z.infer<typeof TenureLineSchema>
export type Totals = z.infer<typeof TotalsSchema>
export type Scheme = z.infer<typeof SchemeSchema>
export type SchemeInput = Pick<Scheme, 'name' | 'siteAddress' | 'status' | 'tenures'>
export type FieldErrors = z.infer<typeof ValidationProblemSchema>['errors']
export type SortKey = 'status' | 'name' | 'homes' | 'affordable' | 'value'

// A 400 from the API, carrying its per-field messages so the form can show them.
export class ValidationError extends Error {
  readonly errors: FieldErrors

  constructor(errors: FieldErrors) {
    super('The scheme has validation errors.')
    this.errors = errors
  }
}

async function request<T>(path: string, schema: z.ZodType<T>, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  const body: unknown = await response.json().catch(() => null)

  if (response.status === 400) {
    const problem = ValidationProblemSchema.safeParse(body)
    if (problem.success) throw new ValidationError(problem.data.errors)
  }
  if (!response.ok) throw new Error(`The server returned HTTP ${response.status}.`)

  const parsed = schema.safeParse(body)
  if (!parsed.success) {
    console.error(`Unexpected response from ${path}:\n${z.prettifyError(parsed.error)}`)
    throw new Error('The server sent a response this page does not understand.')
  }
  return parsed.data
}

// Like AbortSignal.timeout(15_000), but built on setTimeout so tests can run the clock forward.
function saveTimeout() {
  const controller = new AbortController()
  setTimeout(() => controller.abort(new DOMException('The save timed out.', 'TimeoutError')), 15_000)
  return controller.signal
}

export const api = {
  list(status: SchemeStatus | null, sort: SortKey, desc: boolean, signal?: AbortSignal) {
    const query = new URLSearchParams({ sort, desc: String(desc) })
    if (status) query.set('status', status)
    return request(`/api/schemes?${query}`, z.array(SchemeSchema), { signal })
  },
  // The edit panel is locked while saving, so a hung API must not keep it locked forever.
  create: (input: SchemeInput) =>
    request('/api/schemes', SchemeSchema, { method: 'POST', body: JSON.stringify(input), signal: saveTimeout() }),
  update: (id: string, input: SchemeInput) =>
    request(`/api/schemes/${id}`, SchemeSchema, { method: 'PUT', body: JSON.stringify(input), signal: saveTimeout() }),
  summary: (signal?: AbortSignal) => request('/api/summary', TotalsSchema, { signal }),
}
