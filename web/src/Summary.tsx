import type { Totals } from './api.ts'
import { count, percent, value } from './format.ts'

export function Summary({ totals }: { totals: Totals }) {
  return (
    <section className="summary" aria-label="Portfolio summary">
      <dl>
        <div>
          <dt>Homes</dt>
          <dd>{count(totals.homes)}</dd>
        </div>
        <div>
          <dt>Affordable homes</dt>
          <dd>
            {count(totals.affordableHomes)} <span className="secondary">{percent(totals.affordablePercent)}</span>
          </dd>
        </div>
        <div>
          <dt>Total value</dt>
          <dd>
            {value(totals)}
            {/* Only when some homes are valued: with none valued there is no total to exclude from. */}
            {totals.unvaluedHomes > 0 && totals.unvaluedHomes < totals.homes && (
              <span className="caution">excludes {count(totals.unvaluedHomes)} unvalued homes</span>
            )}
          </dd>
        </div>
      </dl>
    </section>
  )
}
