import { describe, expect, it } from 'vitest'
import { count, money, percent } from './format.ts'

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
