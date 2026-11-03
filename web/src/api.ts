// Types mirror the API's JSON contract (see schemes.http).

export const STATUSES = ['Appraisal', 'Planning', 'OnSite', 'Complete'] as const
export type SchemeStatus = (typeof STATUSES)[number]

export const TENURES = ['SocialRent', 'AffordableRent', 'SharedOwnership', 'MarketSale'] as const
export type Tenure = (typeof TENURES)[number]

export type SortKey = 'status' | 'name' | 'homes' | 'affordable' | 'value'

export interface TenureLine {
  tenure: Tenure
  homes: number
  averageValue: number | null
}

export interface Totals {
  homes: number
  affordableHomes: number
  affordablePercent: number | null
  value: number
  unvaluedHomes: number
}

export interface Scheme {
  id: string
  name: string
  siteAddress: string
  status: SchemeStatus
  tenures: TenureLine[]
  totals: Totals
  createdAt: string
  updatedAt: string
}

export interface SchemeInput {
  name: string
  siteAddress: string
  status: SchemeStatus
  tenures: TenureLine[]
}

export type FieldErrors = Record<string, string[]>

// A 400 from the API, carrying its per-field messages so the form can show them.
export class ValidationError extends Error {
  readonly errors: FieldErrors

  constructor(errors: FieldErrors) {
    super('The scheme has validation errors.')
    this.errors = errors
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  if (response.status === 400) {
    const body = await response.json()
    if (body.errors) throw new ValidationError(body.errors)
  }
  if (!response.ok) throw new Error(`The server returned HTTP ${response.status}.`)
  return response.json() as Promise<T>
}

export const api = {
  list(status: SchemeStatus | null, sort: SortKey, desc: boolean, signal?: AbortSignal) {
    const query = new URLSearchParams({ sort, desc: String(desc) })
    if (status) query.set('status', status)
    return request<Scheme[]>(`/api/schemes?${query}`, { signal })
  },
  create: (input: SchemeInput) =>
    request<Scheme>('/api/schemes', { method: 'POST', body: JSON.stringify(input) }),
  update: (id: string, input: SchemeInput) =>
    request<Scheme>(`/api/schemes/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  summary: (signal?: AbortSignal) => request<Totals>('/api/summary', { signal }),
}
