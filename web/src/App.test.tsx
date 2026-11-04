// @vitest-environment jsdom
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App.tsx'
import { api, ValidationError, type Scheme } from './api.ts'

const totals = { homes: 0, affordableHomes: 0, affordablePercent: null, value: 0, unvaluedHomes: 0 }

const scheme = (id: string, name: string): Scheme => ({
  id,
  name,
  siteAddress: `${name} site`,
  status: 'Planning',
  tenures: [],
  totals,
  createdAt: '2026-11-04T10:00:00Z',
  updatedAt: '2026-11-04T10:00:00Z',
})

// A save the test finishes by hand, so it can act while the request is still in flight.
function pendingUpdate() {
  let settle!: { resolve: (s: Scheme) => void; reject: (e: unknown) => void }
  vi.spyOn(api, 'update').mockImplementation(
    () => new Promise<Scheme>((resolve, reject) => (settle = { resolve, reject })),
  )
  return () => settle
}

const panel = () => screen.queryByRole('heading', { name: 'Edit scheme' })
const button = (name: string) => screen.getByRole('button', { name }) as HTMLButtonElement

beforeEach(() => {
  vi.spyOn(api, 'list').mockResolvedValue([scheme('a', 'Ash Court'), scheme('b', 'Beech Row')])
  vi.spyOn(api, 'summary').mockResolvedValue(totals)
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

describe('App while a save is in flight', () => {
  it('locks the panel so a late response cannot close a different form', async () => {
    const settle = pendingUpdate()
    render(<App />)
    await userEvent.click(await screen.findByRole('button', { name: 'Ash Court' }))
    await userEvent.click(button('Save scheme'))

    expect(button('Cancel').disabled).toBe(true)
    expect(button('Add scheme').disabled).toBe(true)
    expect(button('Beech Row').disabled).toBe(true)
    // The disabled Save button drops focus, so put it back in the panel before pressing Escape.
    await userEvent.click(screen.getByLabelText('Name'))
    await userEvent.keyboard('{Escape}')
    expect(panel()).not.toBeNull()

    settle().resolve(scheme('a', 'Ash Court'))
    await waitFor(() => expect(panel()).toBeNull())
    expect(screen.getByText('Scheme saved.')).toBeDefined()
    expect(button('Add scheme').disabled).toBe(false)
  })

  it('keeps the panel open and unlocks it when the save fails, so the error is seen', async () => {
    const settle = pendingUpdate()
    render(<App />)
    await userEvent.click(await screen.findByRole('button', { name: 'Ash Court' }))
    await userEvent.click(button('Save scheme'))

    settle().reject(new ValidationError({ name: ['Name is required.'] }))

    expect(await screen.findByText('Name is required.')).toBeDefined()
    expect(panel()).not.toBeNull()
    expect(button('Cancel').disabled).toBe(false)
    expect(button('Beech Row').disabled).toBe(false)
  })
})
