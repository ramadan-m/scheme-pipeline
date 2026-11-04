import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ValidationError } from './api.ts'

const respond = (status: number, body: unknown) =>
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(body), { status })))

afterEach(() => vi.unstubAllGlobals())

const totals = { homes: 30, affordableHomes: 20, affordablePercent: 66.7, value: 3000000, unvaluedHomes: 20 }

describe('api responses are validated at the boundary', () => {
  it('returns data that matches the contract', async () => {
    respond(200, totals)

    await expect(api.summary()).resolves.toEqual(totals)
  })

  it('rejects a response with the wrong shape instead of passing it to the page', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    respond(200, { ...totals, affordablePercent: '66.7%' })

    await expect(api.summary()).rejects.toThrow('does not understand')
  })

  it('turns a 400 with field errors into a ValidationError', async () => {
    respond(400, { title: 'One or more validation errors occurred.', errors: { name: ['Name is required.'] } })

    const error = await api.summary().catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ValidationError)
    expect((error as ValidationError).errors).toEqual({ name: ['Name is required.'] })
  })

  // Shaped like real .NET output: a v7 Guid and an ISO timestamp with a Z.
  const scheme = {
    id: '019a4f2c-7c3a-7b4e-9a1d-2f3e4d5c6b7a',
    name: 'Riverside',
    siteAddress: '12 Mill Lane',
    status: 'Planning',
    tenures: [{ tenure: 'MarketSale', homes: 10, averageValue: 300000 }],
    totals,
    createdAt: '2026-11-03T13:20:34.062Z',
    updatedAt: '2026-11-03T13:20:34.062Z',
  }

  it('accepts a scheme list that matches the contract', async () => {
    respond(200, [scheme])

    await expect(api.list(null, 'status', false)).resolves.toEqual([scheme])
  })

  it.each([
    ['id is not a uuid', { id: '1' }],
    ['createdAt is not an ISO date', { createdAt: 'yesterday' }],
    ['status is unknown', { status: 'Paused' }],
    ['homes is not whole', { tenures: [{ tenure: 'MarketSale', homes: 2.5, averageValue: null }] }],
  ])('rejects a scheme whose %s', async (_, change) => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    respond(200, [{ ...scheme, ...change }])

    await expect(api.list(null, 'status', false)).rejects.toThrow('does not understand')
  })

  it('treats a 400 without field errors as a plain failure', async () => {
    respond(400, { title: 'Bad Request' })

    await expect(api.summary()).rejects.not.toBeInstanceOf(ValidationError)
  })
})
