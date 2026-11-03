import { useEffect, useState } from 'react'
import { api, STATUSES, type Scheme, type SchemeStatus, type SortKey, type Totals } from './api.ts'
import { STATUS_LABELS } from './format.ts'
import { SchemeTable, type Sort } from './SchemeTable.tsx'
import { Summary } from './Summary.tsx'

export default function App() {
  const [schemes, setSchemes] = useState<Scheme[] | null>(null)
  const [totals, setTotals] = useState<Totals | null>(null)
  const [status, setStatus] = useState<SchemeStatus | null>(null)
  const [sort, setSort] = useState<Sort>({ key: 'status', desc: false })
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    // Aborting on cleanup means a slow, stale response can never overwrite a newer one.
    const controller = new AbortController()
    Promise.all([api.list(status, sort.key, sort.desc, controller.signal), api.summary(controller.signal)])
      .then(([list, summary]) => {
        setSchemes(list)
        setTotals(summary)
        setError(null)
      })
      .catch(() => {
        if (!controller.signal.aborted) setError("Couldn't load schemes. Check the API is running, then reload.")
      })
    return () => controller.abort()
  }, [status, sort])

  function changeSort(key: SortKey) {
    setSort((current) => ({ key, desc: current.key === key ? !current.desc : false }))
  }

  return (
    <main>
      <h1>Scheme pipeline</h1>
      {totals && <Summary totals={totals} />}

      <div className="toolbar">
        <label>
          Status
          <select
            value={status ?? ''}
            onChange={(e) => setStatus(e.target.value === '' ? null : (e.target.value as SchemeStatus))}
          >
            <option value="">All</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {STATUS_LABELS[s]}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error ? (
        <p className="message error" role="alert">
          {error}
        </p>
      ) : schemes === null ? (
        <p className="message">Loading schemes…</p>
      ) : schemes.length === 0 ? (
        <p className="message">
          {status ? 'No schemes at this status.' : 'No schemes yet. Add your first scheme to start tracking the pipeline.'}
        </p>
      ) : (
        <SchemeTable schemes={schemes} sort={sort} onSort={changeSort} />
      )}
    </main>
  )
}
