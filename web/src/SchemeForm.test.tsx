// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ValidationError, type Scheme, type SchemeInput } from './api.ts'
import { SchemeForm } from './SchemeForm.tsx'

afterEach(cleanup)

const input = (label: string) => screen.getByLabelText(label) as HTMLInputElement

describe('SchemeForm', () => {
  it('sends a blank average value as null (not yet known) and blank homes as 0', async () => {
    const onSave = vi.fn<(input: SchemeInput) => Promise<void>>().mockResolvedValue()
    render(<SchemeForm scheme={null} onSave={onSave} onCancel={() => {}} />)

    await userEvent.type(input('Name'), 'Orchard Fields')
    await userEvent.type(input('Site address'), 'Hollow Lane')
    await userEvent.type(input('Social rent homes'), '12')
    await userEvent.click(screen.getByRole('button', { name: 'Save scheme' }))

    const sent = onSave.mock.calls[0][0]
    expect(sent.tenures[0]).toEqual({ tenure: 'SocialRent', homes: 12, averageValue: null })
    expect(sent.tenures[3]).toEqual({ tenure: 'MarketSale', homes: 0, averageValue: null })
  })

  it('shows each server error under the field it belongs to', async () => {
    const onSave = vi.fn().mockRejectedValue(
      new ValidationError({
        name: ['Name is required.'],
        'tenures[2].averageValue': ['Average value, if given, must be more than 0 and at most 100,000,000.'],
      }),
    )
    render(<SchemeForm scheme={null} onSave={onSave} onCancel={() => {}} />)

    await userEvent.click(screen.getByRole('button', { name: 'Save scheme' }))

    const name = input('Name')
    expect(name.getAttribute('aria-invalid')).toBe('true')
    expect(document.getElementById(name.getAttribute('aria-describedby')!)?.textContent).toBe('Name is required.')
    const sharedValue = input('Shared ownership average value')
    expect(document.getElementById(sharedValue.getAttribute('aria-describedby')!)?.textContent).toContain(
      'must be more than 0',
    )
    expect((screen.getByRole('button', { name: 'Save scheme' }) as HTMLButtonElement).disabled).toBe(false)
  })

  it('stops text that is not a number before calling the API, instead of saving it as 0', async () => {
    const onSave = vi.fn()
    render(<SchemeForm scheme={null} onSave={onSave} onCancel={() => {}} />)

    await userEvent.type(input('Social rent homes'), '12abc')
    await userEvent.click(screen.getByRole('button', { name: 'Save scheme' }))

    expect(onSave).not.toHaveBeenCalled()
    const homes = input('Social rent homes')
    expect(document.getElementById(homes.getAttribute('aria-describedby')!)?.textContent).toBe(
      'Enter a number, for example 12.',
    )
  })

  it('sends 2.5 homes unchanged so the API can reject it, and accepts "250,000" as a value', async () => {
    const onSave = vi.fn<(input: SchemeInput) => Promise<void>>().mockResolvedValue()
    render(<SchemeForm scheme={null} onSave={onSave} onCancel={() => {}} />)

    await userEvent.type(input('Market sale homes'), '2.5')
    await userEvent.type(input('Market sale average value'), '250,000')
    await userEvent.click(screen.getByRole('button', { name: 'Save scheme' }))

    expect(onSave.mock.calls[0][0].tenures[3]).toEqual({ tenure: 'MarketSale', homes: 2.5, averageValue: 250000 })
  })

  it('prefills an existing scheme for editing', () => {
    const scheme: Scheme = {
      id: '1',
      name: 'Canal Wharf',
      siteAddress: 'Wharf Road',
      status: 'OnSite',
      tenures: [
        { tenure: 'SocialRent', homes: 40, averageValue: 135000 },
        { tenure: 'AffordableRent', homes: 0, averageValue: null },
        { tenure: 'SharedOwnership', homes: 30, averageValue: null },
        { tenure: 'MarketSale', homes: 120, averageValue: 295000 },
      ],
      totals: { homes: 190, affordableHomes: 70, affordablePercent: 36.8, value: 40800000, unvaluedHomes: 30 },
      createdAt: '',
      updatedAt: '',
    }
    render(<SchemeForm scheme={scheme} onSave={vi.fn()} onCancel={() => {}} />)

    expect(screen.getByRole('heading', { name: 'Edit scheme' })).toBeTruthy()
    expect(input('Name').value).toBe('Canal Wharf')
    expect(input('Social rent average value').value).toBe('135000')
    expect(input('Shared ownership average value').value).toBe('')
    expect(input('Affordable rent homes').value).toBe('')
  })
})
