import { useEffect, useState } from 'react'

// Temporary: proves the browser -> Vite proxy -> API path works. Replaced in step 3.3.
export default function App() {
  const [status, setStatus] = useState('Checking API…')

  useEffect(() => {
    fetch('/health')
      .then((r) => setStatus(r.ok ? 'API reachable' : `API error ${r.status}`))
      .catch(() => setStatus('API unreachable'))
  }, [])

  return (
    <main>
      <h1>Scheme Pipeline</h1>
      <p>{status}</p>
    </main>
  )
}
