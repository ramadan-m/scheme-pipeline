import { describe, expect, it } from 'vitest'
import type { Totals } from './api.ts'
import { count, money, percent, value } from './format.ts'

const totals = (homes: number, unvaluedHomes: number, total: number): Totals =>
  ({ homes, affordableHomes: 0, affordablePercent: null, value: total, unvaluedHomes })

describe('value', () => {
  it('shows a dash when there are no homes', () => {
    expect(value(totals(0, 0, 0))).toBe('—')
  })

  it('says "Not valued" rather than £0 when no homes have a value yet', () => {
    expect(value(totals(60, 60, 0))).toBe('Not valued')
  })

  it('shows the amount when some or all homes are valued', () => {
    expect(value(totals(90, 20, 21_500_000))).toBe('£21,500,000')
    expect(value(totals(40, 0, 11_260_000))).toBe('£11,260,000')
  })
})

describe('money', () => {
  it('shows whole pounds with thousands separators', () => {
    expect(money(25_400_000)).toBe('£25,400,000')
    expect(money(0)).toBe('£0')
  })
})

describe('percent', () => {
  it('pads to one decimal place', () => {
    expect(percent(45)).toBe('45.0%')
    expect(percent(66.7)).toBe('66.7%')
    expect(percent(0)).toBe('0.0%')
  })

  it('shows a dash when there are no homes, never 0%', () => {
    expect(percent(null)).toBe('—')
  })
})

describe('count', () => {
  it('uses thousands separators', () => {
    expect(count(100_000)).toBe('100,000')
  })
})
