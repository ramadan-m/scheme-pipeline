import type { Scheme, SortKey } from './api.ts'
import { count, percent, STATUS_LABELS, value } from './format.ts'
import { TenureBar } from './TenureBar.tsx'

export interface Sort {
  key: SortKey
  desc: boolean
}

const COLUMNS: { key: SortKey; label: string; numeric: boolean }[] = [
  { key: 'name', label: 'Scheme', numeric: false },
  { key: 'status', label: 'Status', numeric: false },
  { key: 'homes', label: 'Homes', numeric: true },
  { key: 'affordable', label: 'Affordable', numeric: true },
  { key: 'value', label: 'Value', numeric: true },
]

interface Props {
  schemes: Scheme[]
  sort: Sort
  onSort: (key: SortKey) => void
  onEdit: (scheme: Scheme) => void
}

export function SchemeTable({ schemes, sort, onSort, onEdit }: Props) {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            {COLUMNS.map((column) => {
              const active = sort.key === column.key
              return (
                <th
                  key={column.key}
                  className={column.numeric ? 'num' : undefined}
                  aria-sort={active ? (sort.desc ? 'descending' : 'ascending') : undefined}
                >
                  <button type="button" onClick={() => onSort(column.key)}>
                    {column.label}
                    <span aria-hidden="true">{active ? (sort.desc ? ' ↓' : ' ↑') : ''}</span>
                  </button>
                </th>
              )
            })}
            <th className="mix">Tenure mix</th>
          </tr>
        </thead>
        <tbody>
          {schemes.map((scheme) => (
            <tr key={scheme.id}>
              <td>
                <button type="button" className="link" onClick={() => onEdit(scheme)}>
                  {scheme.name}
                </button>
                <span className="address">{scheme.siteAddress}</span>
                {/* Narrow screens hide the mix column and show the bar here instead; display:none
                    also hides the unused copy from screen readers, so it is never announced twice. */}
                <div className="mix-inline">
                  <TenureBar tenures={scheme.tenures} />
                </div>
              </td>
              <td>{STATUS_LABELS[scheme.status]}</td>
              <td className="num">{count(scheme.totals.homes)}</td>
              <td className="num">{percent(scheme.totals.affordablePercent)}</td>
              <td className="num">
                {value(scheme.totals)}
                {scheme.totals.unvaluedHomes > 0 && scheme.totals.unvaluedHomes < scheme.totals.homes && (
                  <span className="caution">{count(scheme.totals.unvaluedHomes)} unvalued</span>
                )}
              </td>
              <td className="mix">
                <TenureBar tenures={scheme.tenures} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
