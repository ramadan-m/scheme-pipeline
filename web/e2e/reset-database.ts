import { execSync } from 'node:child_process'
import { resolve } from 'node:path'

// Each run starts from an empty e2e database (uses the docker-compose Mongo; dev data is untouched).
export default function resetDatabase() {
  execSync('docker compose exec -T mongo mongosh scheme_pipeline_e2e --quiet --eval "db.dropDatabase()"', {
    // The repo root, found from this file rather than from wherever Playwright was started.
    cwd: resolve(import.meta.dirname, '..', '..'),
    stdio: 'inherit',
  })
}
