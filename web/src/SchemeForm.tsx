import { useState, type FormEvent } from 'react'
import {
  STATUSES,
  TENURES,
  ValidationError,
  type FieldErrors,
  type Scheme,
  type SchemeInput,
  type SchemeStatus,
  type Tenure,
} from './api.ts'
import { STATUS_LABELS, TENURE_LABELS } from './format.ts'

// Inputs hold text; conversion to numbers happens once, on save.
interface Row {
  homes: string
  averageValue: string
}

interface Props {
  scheme: Scheme | null
  onSave: (input: SchemeInput) => Promise<void>
  onCancel: () => void
}

function initialRows(scheme: Scheme | null) {
  const rows = {} as Record<Tenure, Row>
  for (const tenure of TENURES) {
    const line = scheme?.tenures.find((l) => l.tenure === tenure)
    rows[tenure] = {
      homes: line && line.homes > 0 ? String(line.homes) : '',
      averageValue: line?.averageValue == null ? '' : String(line.averageValue),
    }
  }
  return rows
}

// Blank homes means 0; blank value means "not yet known" (null), never 0.
function toLines(rows: Record<Tenure, Row>) {
  return TENURES.map((tenure) => ({
    tenure,
    homes: rows[tenure].homes === '' ? 0 : Number(rows[tenure].homes),
    averageValue: rows[tenure].averageValue === '' ? null : Number(rows[tenure].averageValue),
  }))
}

function FieldError({ id, message }: { id: string; message?: string }) {
  return message ? (
    <p id={id} className="field-error">
      {message}
    </p>
  ) : null
}

export function SchemeForm({ scheme, onSave, onCancel }: Props) {
  const [name, setName] = useState(scheme?.name ?? '')
  const [siteAddress, setSiteAddress] = useState(scheme?.siteAddress ?? '')
  const [status, setStatus] = useState<SchemeStatus>(scheme?.status ?? 'Appraisal')
  const [rows, setRows] = useState(() => initialRows(scheme))
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const error = (key: string) => errors[key]?.[0]
  const described = (key: string) => (error(key) ? `${key}-error` : undefined)

  function setRow(tenure: Tenure, field: keyof Row, value: string) {
    setRows((current) => ({ ...current, [tenure]: { ...current[tenure], [field]: value } }))
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setErrors({})
    setFormError(null)
    try {
      await onSave({ name, siteAddress, status, tenures: toLines(rows) })
    } catch (e) {
      if (e instanceof ValidationError) setErrors(e.errors)
      else setFormError("Couldn't save the scheme. Check the API is running, then try again.")
      setSaving(false)
    }
  }

  return (
    <aside className="panel" aria-labelledby="panel-title" onKeyDown={(e) => e.key === 'Escape' && onCancel()}>
      {/* noValidate: the API is the single source of validation messages. */}
      <form onSubmit={submit} noValidate>
        <h2 id="panel-title">{scheme ? 'Edit scheme' : 'Add scheme'}</h2>

        <label htmlFor="name">Name</label>
        <input
          id="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          aria-invalid={!!error('name')}
          aria-describedby={described('name')}
          autoFocus
        />
        <FieldError id="name-error" message={error('name')} />

        <label htmlFor="siteAddress">Site address</label>
        <input
          id="siteAddress"
          value={siteAddress}
          onChange={(e) => setSiteAddress(e.target.value)}
          aria-invalid={!!error('siteAddress')}
          aria-describedby={described('siteAddress')}
        />
        <FieldError id="siteAddress-error" message={error('siteAddress')} />

        <label htmlFor="status">Status</label>
        <select id="status" value={status} onChange={(e) => setStatus(e.target.value as SchemeStatus)}>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {STATUS_LABELS[s]}
            </option>
          ))}
        </select>

        <fieldset>
          <legend>Homes by tenure</legend>
          <div className="tenure-grid">
            <span aria-hidden="true" />
            <span className="column-label">Homes</span>
            <span className="column-label">Average value (£)</span>
            {TENURES.map((tenure, i) => {
              const homesKey = `tenures[${i}].homes`
              const valueKey = `tenures[${i}].averageValue`
              return (
                <div className="tenure-row" key={tenure}>
                  <span>{TENURE_LABELS[tenure]}</span>
                  <input
                    type="number"
                    inputMode="numeric"
                    placeholder="0"
                    aria-label={`${TENURE_LABELS[tenure]} homes`}
                    value={rows[tenure].homes}
                    onChange={(e) => setRow(tenure, 'homes', e.target.value)}
                    aria-invalid={!!error(homesKey)}
                    aria-describedby={described(homesKey)}
                  />
                  <input
                    type="number"
                    inputMode="decimal"
                    placeholder="Not yet known"
                    aria-label={`${TENURE_LABELS[tenure]} average value`}
                    value={rows[tenure].averageValue}
                    onChange={(e) => setRow(tenure, 'averageValue', e.target.value)}
                    aria-invalid={!!error(valueKey)}
                    aria-describedby={described(valueKey)}
                  />
                  <FieldError id={`${homesKey}-error`} message={error(homesKey)} />
                  <FieldError id={`${valueKey}-error`} message={error(valueKey)} />
                </div>
              )
            })}
          </div>
        </fieldset>

        {formError && (
          <p className="field-error" role="alert">
            {formError}
          </p>
        )}

        <div className="actions">
          <button type="submit" className="primary" disabled={saving}>
            {saving ? 'Saving…' : 'Save scheme'}
          </button>
          <button type="button" onClick={onCancel}>
            Cancel
          </button>
        </div>
      </form>
    </aside>
  )
}
