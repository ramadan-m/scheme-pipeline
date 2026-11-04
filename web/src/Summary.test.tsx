// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { Summary } from './Summary.tsx'

afterEach(cleanup)

const totals = (homes: number, unvaluedHomes: number, value: number) => ({
  homes,
  affordableHomes: 0,
  affordablePercent: 0,
  value,
  unvaluedHomes,
})

const caution = () => screen.queryByText(/unvalued homes/)

describe('Summary total value', () => {
  it('notes the unvalued homes the total leaves out', () => {
    render(<Summary totals={totals(30, 20, 3_000_000)} />)

    expect(caution()?.textContent).toBe('excludes 20 unvalued homes')
  })

  it('says only "Not valued" when no home has a value, since there is no total to exclude from', () => {
    render(<Summary totals={totals(20, 20, 0)} />)

    expect(screen.getByText('Not valued')).toBeDefined()
    expect(caution()).toBeNull()
  })

  it('shows no note when every home is valued', () => {
    render(<Summary totals={totals(30, 0, 9_000_000)} />)

    expect(caution()).toBeNull()
  })
})
