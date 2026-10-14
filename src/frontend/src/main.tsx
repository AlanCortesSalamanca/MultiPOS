import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router'
import { App } from '@/app/app'
import '@/styles/globals.css'

// Infraestructura de server state, sin queries, mutations ni endpoints inventados.
const queryClient = new QueryClient({
  defaultOptions: { mutations: { retry: false } },
})

const root = document.getElementById('root')
if (!root) throw new Error('No se encontró el contenedor de la aplicación.')

createRoot(root).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
