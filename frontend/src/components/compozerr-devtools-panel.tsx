import { useState } from 'react'
import { apiBaseUrl } from '@/api-client'

export function CompozerrDevtoolsPanel() {
  const [status, setStatus] = useState<{ type: 'idle' | 'loading' | 'success' | 'error'; message: string }>({
    type: 'idle',
    message: '',
  })

  async function callSeedEndpoint(method: 'POST' | 'DELETE') {
    setStatus({ type: 'loading', message: method === 'POST' ? 'Seeding...' : 'Clearing...' })
    try {
      const res = await fetch(`${apiBaseUrl}/v1/dev/seed`, {
        method,
        credentials: 'include',
      })
      const data = await res.json()
      if (!res.ok) {
        setStatus({ type: 'error', message: `${res.status}: ${JSON.stringify(data)}` })
        return
      }
      setStatus({ type: 'success', message: JSON.stringify(data, null, 2) })
    } catch (e) {
      setStatus({ type: 'error', message: e instanceof Error ? e.message : String(e) })
    }
  }

  return (
    <div style={{ padding: 16, fontFamily: 'monospace', fontSize: 13, color: '#e2e8f0' }}>
      <h3 style={{ margin: '0 0 12px', fontSize: 14, fontWeight: 600 }}>Database Seeding</h3>
      <p style={{ margin: '0 0 12px', fontSize: 12, color: '#94a3b8' }}>
        Seed or clear development data for the current user.
      </p>
      <div style={{ display: 'flex', gap: 8 }}>
        <button
          onClick={() => callSeedEndpoint('POST')}
          disabled={status.type === 'loading'}
          style={{
            padding: '6px 12px',
            borderRadius: 4,
            border: 'none',
            background: '#3b82f6',
            color: '#fff',
            cursor: status.type === 'loading' ? 'wait' : 'pointer',
            fontSize: 12,
            fontWeight: 500,
            opacity: status.type === 'loading' ? 0.6 : 1,
          }}
        >
          Seed Database
        </button>
        <button
          onClick={() => callSeedEndpoint('DELETE')}
          disabled={status.type === 'loading'}
          style={{
            padding: '6px 12px',
            borderRadius: 4,
            border: '1px solid #475569',
            background: 'transparent',
            color: '#e2e8f0',
            cursor: status.type === 'loading' ? 'wait' : 'pointer',
            fontSize: 12,
            fontWeight: 500,
            opacity: status.type === 'loading' ? 0.6 : 1,
          }}
        >
          Clear Seed Data
        </button>
      </div>
      {status.message && (
        <pre
          style={{
            marginTop: 12,
            padding: 8,
            borderRadius: 4,
            background: status.type === 'error' ? '#1e1215' : '#0f172a',
            border: `1px solid ${status.type === 'error' ? '#7f1d1d' : '#1e293b'}`,
            color: status.type === 'error' ? '#fca5a5' : '#94a3b8',
            fontSize: 11,
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-all',
            maxHeight: 200,
            overflow: 'auto',
          }}
        >
          {status.message}
        </pre>
      )}
    </div>
  )
}
