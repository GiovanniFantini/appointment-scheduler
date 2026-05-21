import { useEffect, useState } from 'react'
import { useBranch } from '../../contexts/BranchContext'
import {
  inventoryApi,
  type InventoryDashboard,
  type InventoryValuationRow,
  type LowStockRow,
} from '../../lib/api/inventory'
import './MagazzinoPage.css'

// L'app Merchant è un configuratore: il Magazzino lato Merchant mostra solo i
// report (panoramica, valorizzazione, sotto scorta). L'operatività — articoli,
// movimenti, rettifiche, fornitori, ordini di acquisto — è sull'app Employee,
// modulata sul livello di accesso del ruolo (ReadOnly / Operator / Manager).

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('it-IT', {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
  }).format(value)
}

function formatQuantity(value: number): string {
  return new Intl.NumberFormat('it-IT', {
    minimumFractionDigits: Number.isInteger(value) ? 0 : 2,
    maximumFractionDigits: 3,
  }).format(value)
}

export default function MagazzinoPage() {
  const { activeBranches } = useBranch()

  const [branchFilterId, setBranchFilterId] = useState<number | null>(null)
  const [dashboard, setDashboard] = useState<InventoryDashboard | null>(null)
  const [valuationRows, setValuationRows] = useState<InventoryValuationRow[]>([])
  const [lowStockRows, setLowStockRows] = useState<LowStockRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const loadReports = async () => {
    setLoading(true)
    setError('')

    const [dashboardRes, valuationRes, lowStockRes] = await Promise.allSettled([
      inventoryApi.getDashboard(branchFilterId),
      inventoryApi.getValuation(branchFilterId),
      inventoryApi.getLowStock(branchFilterId),
    ])

    setDashboard(dashboardRes.status === 'fulfilled' ? dashboardRes.value : null)
    setValuationRows(valuationRes.status === 'fulfilled' ? valuationRes.value : [])
    setLowStockRows(lowStockRes.status === 'fulfilled' ? lowStockRes.value : [])

    const failed = [dashboardRes, valuationRes, lowStockRes].some(result => result.status === 'rejected')
    if (failed) {
      setError('Alcuni report del modulo Magazzino non sono stati caricati correttamente.')
    }

    setLoading(false)
  }

  useEffect(() => {
    loadReports()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [branchFilterId])

  const branchFilterLabel = branchFilterId == null
    ? 'Tutte le filiali'
    : activeBranches.find(branch => branch.id === branchFilterId)?.name ?? 'Filiale selezionata'

  return (
    <div className="magazzino-page">
      <div className="magazzino-hero">
        <div>
          <h1 className="page-title">Magazzino — Report</h1>
          <p className="page-subtitle">
            Panoramica, valorizzazione e sotto scorta. L'operatività di magazzino è gestita dai
            dipendenti dall'app Employee.
          </p>
        </div>

        <div className="magazzino-hero-actions">
          <div className="inventory-context-box">
            <span>Filtro filiale</span>
            <select
              value={branchFilterId ?? ''}
              onChange={event => setBranchFilterId(event.target.value ? Number(event.target.value) : null)}
            >
              <option value="">Tutte le filiali</option>
              {activeBranches.map(branch => (
                <option key={branch.id} value={branch.id}>{branch.name}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {error && (
        <div className="inventory-error" role="alert">
          <span>{error}</span>
          <button onClick={() => setError('')}>Chiudi</button>
        </div>
      )}

      {loading ? (
        <div className="inventory-loading-state">
          <div className="inventory-spinner" />
          <span>Caricamento dei report Magazzino...</span>
        </div>
      ) : (
        <div className="inventory-stack">
          <section className="inventory-panel inventory-panel--accent">
            <div className="inventory-panel-head">
              <div>
                <h2>Panoramica</h2>
                <p>Vista consolidata di stock, ordini e fornitori per il filtro attivo.</p>
              </div>
              <span className="inventory-context-pill">{branchFilterLabel}</span>
            </div>
            <div className="inventory-stat-grid">
              <div className="inventory-stat-card">
                <span className="inventory-stat-label">Articoli attivi</span>
                <strong>{dashboard?.totalItems ?? 0}</strong>
              </div>
              <div className="inventory-stat-card">
                <span className="inventory-stat-label">Fornitori attivi</span>
                <strong>{dashboard?.activeSuppliers ?? 0}</strong>
              </div>
              <div className="inventory-stat-card">
                <span className="inventory-stat-label">Ordini aperti</span>
                <strong>{dashboard?.openPurchaseOrders ?? 0}</strong>
              </div>
              <div className="inventory-stat-card">
                <span className="inventory-stat-label">Valore stock</span>
                <strong>{formatCurrency(dashboard?.totalStockValue ?? 0)}</strong>
              </div>
            </div>
          </section>

          <section className="inventory-panel">
            <div className="inventory-section-head">
              <div>
                <h2>Valorizzazione stock</h2>
                <p>Costo medio ponderato e valore di magazzino per filiale.</p>
              </div>
            </div>

            <div className="inventory-table-wrap">
              <table className="inventory-table">
                <thead>
                  <tr>
                    <th>Articolo</th>
                    <th>Filiale</th>
                    <th>Quantità</th>
                    <th>Costo medio</th>
                    <th>Valore</th>
                  </tr>
                </thead>
                <tbody>
                  {valuationRows.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="inventory-table-empty">Nessun dato disponibile.</td>
                    </tr>
                  ) : valuationRows.map(row => (
                    <tr key={`${row.itemId}-${row.branchId}`}>
                      <td>{row.sku} · {row.itemName}</td>
                      <td>{row.branchName}</td>
                      <td>{formatQuantity(row.quantityOnHand)}</td>
                      <td>{formatCurrency(row.weightedAverageCost)}</td>
                      <td>{formatCurrency(row.inventoryValue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="inventory-panel">
            <div className="inventory-section-head">
              <div>
                <h2>Sotto scorta</h2>
                <p>Azioni di riordino suggerite sul filtro filiale attivo.</p>
              </div>
            </div>

            <div className="inventory-table-wrap">
              <table className="inventory-table">
                <thead>
                  <tr>
                    <th>Articolo</th>
                    <th>Filiale</th>
                    <th>Giacenza</th>
                    <th>Soglia di riordino</th>
                    <th>Riordino suggerito</th>
                  </tr>
                </thead>
                <tbody>
                  {lowStockRows.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="inventory-table-empty">Nessun articolo sotto soglia.</td>
                    </tr>
                  ) : lowStockRows.map(row => (
                    <tr key={`${row.itemId}-${row.branchId}`}>
                      <td>{row.sku} · {row.itemName}</td>
                      <td>{row.branchName}</td>
                      <td>{formatQuantity(row.quantityOnHand)}</td>
                      <td>{formatQuantity(row.reorderPoint)}</td>
                      <td>{formatQuantity(row.suggestedReorderQuantity)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      )}
    </div>
  )
}
