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

  it('treats a 400 without field errors as a plain failure', async () => {
    respond(400, { title: 'Bad Request' })

    await expect(api.summary()).rejects.not.toBeInstanceOf(ValidationError)
  })
})
