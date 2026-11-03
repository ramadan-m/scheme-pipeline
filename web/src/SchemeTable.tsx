import type { Scheme, SortKey } from './api.ts'
import { count, percent, STATUS_LABELS, value } from './format.ts'

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
}

export function SchemeTable({ schemes, sort, onSort }: Props) {
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
          </tr>
        </thead>
        <tbody>
          {schemes.map((scheme) => (
            <tr key={scheme.id}>
              <td>
                {scheme.name}
                <span className="address">{scheme.siteAddress}</span>
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
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
