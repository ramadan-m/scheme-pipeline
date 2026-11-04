import { useEffect, useState } from 'react'
import { api, STATUSES, type Scheme, type SchemeInput, type SchemeStatus, type SortKey, type Totals } from './api.ts'
import { STATUS_LABELS } from './format.ts'
import { SchemeForm } from './SchemeForm.tsx'
import { SchemeTable, type Sort } from './SchemeTable.tsx'
import { Summary } from './Summary.tsx'
import { TenureKey } from './TenureBar.tsx'

export default function App() {
  const [schemes, setSchemes] = useState<Scheme[] | null>(null)
  const [totals, setTotals] = useState<Totals | null>(null)
  const [status, setStatus] = useState<SchemeStatus | null>(null)
  const [sort, setSort] = useState<Sort>({ key: 'status', desc: false })
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState<Scheme | 'new' | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [reloads, setReloads] = useState(0)
  const [saving, setSaving] = useState(false)

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
  }, [status, sort, reloads])

  function changeSort(key: SortKey) {
    setSort((current) => ({ key, desc: current.key === key ? !current.desc : false }))
  }

  function open(target: Scheme | 'new') {
    setNotice(null)
    setEditing(target)
  }

  // The panel is locked while saving: otherwise a late response could close a different form,
  // and a late error would go to a form that is no longer on screen.
  async function save(input: SchemeInput) {
    setSaving(true)
    try {
      if (editing === 'new') await api.create(input)
      else if (editing) await api.update(editing.id, input)
    } finally {
      setSaving(false)
    }
    // Errors propagate to the form, which shows them; only success closes the panel.
    setEditing(null)
    setNotice('Scheme saved.')
    setReloads((n) => n + 1)
  }

  return (
    <main>
      <header className="page-header">
        <h1>Scheme pipeline</h1>
        <button type="button" className="primary" onClick={() => open('new')} disabled={saving}>
          Add scheme
        </button>
      </header>
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
        <p className="notice" role="status">
          {notice}
        </p>
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
        <>
          <TenureKey />
          <SchemeTable schemes={schemes} sort={sort} onSort={changeSort} onEdit={open} editLocked={saving} />
        </>
      )}

      {editing && (
        <SchemeForm
          key={editing === 'new' ? 'new' : editing.id}
          scheme={editing === 'new' ? null : editing}
          saving={saving}
          onSave={save}
          onCancel={() => !saving && setEditing(null)}
        />
      )}
    </main>
  )
}
