import React from 'react'
import ReactDOM from 'react-dom/client'
import { initTheme } from '@scheduler/ui'
import App from './App.tsx'
import { OnlineOnly } from './components/OnlineOnly'
import './index.css'

initTheme()

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <OnlineOnly><App /></OnlineOnly>
  </React.StrictMode>,
)
