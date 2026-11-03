import type { SchemeStatus, Tenure, Totals } from './api.ts'

export const STATUS_LABELS: Record<SchemeStatus, string> = {
  Appraisal: 'Appraisal',
  Planning: 'Planning',
  OnSite: 'On site',
  Complete: 'Complete',
}

export const TENURE_LABELS: Record<Tenure, string> = {
  SocialRent: 'Social rent',
  AffordableRent: 'Affordable rent',
  SharedOwnership: 'Shared ownership',
  MarketSale: 'Market sale',
}

const pounds = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 0 })
const whole = new Intl.NumberFormat('en-GB')

// Whole pounds: totals run to millions, so pence would only add noise.
export const money = (value: number) => pounds.format(value)

export const count = (value: number) => whole.format(value)

// The API has already rounded to 1 dp; toFixed only pads 45 to "45.0". Null means no homes.
export const percent = (value: number | null) => (value === null ? '—' : `${value.toFixed(1)}%`)

// Never show "£0" for homes that simply haven't been valued yet.
export function value(totals: Totals) {
  if (totals.homes === 0) return '—'
  if (totals.unvaluedHomes === totals.homes) return 'Not valued'
  return money(totals.value)
}
