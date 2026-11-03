import { expect, test, type Page } from '@playwright/test'

async function addScheme(page: Page, name: string, fill: (panel: ReturnType<Page['getByRole']>) => Promise<void>) {
  await page.getByRole('button', { name: 'Add scheme' }).click()
  const panel = page.getByRole('complementary', { name: 'Add scheme' })
  await panel.getByLabel('Name').fill(name)
  await panel.getByLabel('Site address').fill(`${name} site`)
  await fill(panel)
  await panel.getByRole('button', { name: 'Save scheme' }).click()
  await expect(page.getByRole('status')).toHaveText('Scheme saved.')
}

test('add, edit, filter and sort schemes through the real stack', async ({ page }) => {
  const summary = page.getByRole('region', { name: 'Portfolio summary' })
  const row = (name: string) => page.getByRole('row', { name: new RegExp(name) })

  await page.goto('/')
  await expect(page.getByText('No schemes yet.')).toBeVisible()

  // Add: social rent homes known but not yet valued.
  await addScheme(page, 'Riverside', async (panel) => {
    await panel.getByLabel('Social rent homes').fill('20')
    await panel.getByLabel('Market sale homes').fill('10')
    await panel.getByLabel('Market sale average value').fill('300,000')
  })
  await expect(row('Riverside')).toContainText('66.7%')
  await expect(row('Riverside')).toContainText('£3,000,000')
  await expect(row('Riverside')).toContainText('20 unvalued')
  await expect(summary).toContainText('excludes 20 unvalued homes')

  await addScheme(page, 'Canal Wharf', async (panel) => {
    await panel.getByLabel('Status').selectOption('OnSite')
    await panel.getByLabel('Market sale homes').fill('100')
    await panel.getByLabel('Market sale average value').fill('500000')
  })
  await expect(summary).toContainText('£53,000,000')

  // Edit: value the social rent homes and move to Planning; totals recalculate on the server.
  await page.getByRole('button', { name: 'Riverside' }).click()
  const panel = page.getByRole('complementary', { name: 'Edit scheme' })
  await panel.getByLabel('Social rent average value').fill('120000')
  await panel.getByLabel('Status').selectOption('Planning')
  await panel.getByRole('button', { name: 'Save scheme' }).click()
  await expect(page.getByRole('status')).toHaveText('Scheme saved.')
  await expect(row('Riverside')).toContainText('Planning')
  await expect(row('Riverside')).toContainText('£5,400,000')
  await expect(row('Riverside')).not.toContainText('unvalued')
  await expect(summary).toContainText('£55,400,000')
  await expect(summary).not.toContainText('excludes')

  // Filter by status.
  await page.getByLabel('Status').selectOption('OnSite')
  await expect(page.getByRole('row')).toHaveCount(2) // header + Canal Wharf
  await expect(row('Canal Wharf')).toBeVisible()
  await page.getByLabel('Status').selectOption('')

  // Sort by value, highest first (first click ascending, second descending).
  const value = page.getByRole('button', { name: 'Value' })
  await value.click()
  await value.click()
  await expect(page.getByRole('row').nth(1)).toContainText('Canal Wharf')
  await expect(page.getByRole('columnheader', { name: 'Value' })).toHaveAttribute('aria-sort', 'descending')
})
