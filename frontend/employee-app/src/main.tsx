import React from 'react'
import ReactDOM from 'react-dom/client'
import { initTheme } from '@scheduler/ui'
import App from './App.tsx'
import './index.css'

initTheme()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
