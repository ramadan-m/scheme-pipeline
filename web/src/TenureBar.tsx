import { TENURES, type TenureLine } from './api.ts'
import { count, TENURE_LABELS } from './format.ts'

// Like a site-plan tenure key: one segment per tenure, sized by homes; hatched where not yet valued.
export function TenureBar({ tenures }: { tenures: TenureLine[] }) {
  const lines = tenures.filter((l) => l.homes > 0)
  const total = lines.reduce((sum, l) => sum + l.homes, 0)
  if (total === 0) return <span className="mix-empty">No homes yet</span>

  const describe = (l: TenureLine) =>
    `${TENURE_LABELS[l.tenure]} ${count(l.homes)}${l.averageValue === null ? ' (not valued)' : ''}`

  return (
    <div className="tenure-bar" role="img" aria-label={`Tenure mix: ${lines.map(describe).join(', ')}`}>
      {lines.map((l) => (
        <span
          key={l.tenure}
          data-tenure={l.tenure}
          className={l.averageValue === null ? 'unvalued' : undefined}
          style={{ width: `${(l.homes / total) * 100}%` }}
          title={describe(l)}
        />
      ))}
    </div>
  )
}

export function TenureKey() {
  return (
    <ul className="tenure-key" aria-label="Tenure key">
      {TENURES.map((tenure) => (
        <li key={tenure}>
          <span className="swatch" data-tenure={tenure} aria-hidden="true" />
          {TENURE_LABELS[tenure]}
        </li>
      ))}
      <li>
        <span className="swatch unvalued" data-tenure="MarketSale" aria-hidden="true" />
        Not valued yet
      </li>
    </ul>
  )
}
