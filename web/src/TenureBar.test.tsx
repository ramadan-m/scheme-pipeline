// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import type { TenureLine } from './api.ts'
import { TenureBar } from './TenureBar.tsx'

afterEach(cleanup)

const segments = () => Array.from(screen.getByRole('img').children) as HTMLElement[]

describe('TenureBar', () => {
  it('sizes each tenure by its share of homes and skips tenures with none', () => {
    const tenures: TenureLine[] = [
      { tenure: 'SocialRent', homes: 20, averageValue: null },
      { tenure: 'AffordableRent', homes: 0, averageValue: null },
      { tenure: 'MarketSale', homes: 60, averageValue: 300000 },
    ]
    render(<TenureBar tenures={tenures} />)

    expect(segments().map((s) => [s.dataset.tenure, s.style.width])).toEqual([
      ['SocialRent', '25%'],
      ['MarketSale', '75%'],
    ])
  })

  it('marks unvalued tenures (hatched) and says so to screen readers', () => {
    render(
      <TenureBar
        tenures={[
          { tenure: 'SocialRent', homes: 20, averageValue: null },
          { tenure: 'MarketSale', homes: 10, averageValue: 300000 },
        ]}
      />,
    )

    expect(segments()[0].className).toBe('unvalued')
    expect(segments()[1].className).toBe('')
    expect(screen.getByRole('img').getAttribute('aria-label')).toBe(
      'Tenure mix: Social rent 20 (not valued), Market sale 10',
    )
  })

  it('shows a plain note instead of an empty bar when there are no homes', () => {
    render(<TenureBar tenures={[{ tenure: 'SocialRent', homes: 0, averageValue: null }]} />)

    expect(screen.queryByRole('img')).toBeNull()
    expect(screen.getByText('No homes yet')).toBeTruthy()
  })
})
