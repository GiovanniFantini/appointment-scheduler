import { useEffect, useMemo, useState } from 'react'
import {
  inventoryApi,
  type EmployeeInventoryOverview,
  type InventoryItem,
  type InventoryMovement,
  type LowStockRow,
} from '../../lib/api/inventory'
import './MagazzinoPage.css'

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

function formatDate(value?: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleDateString('it-IT', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

function movementTypeLabel(type: number): string {
  switch (type) {
    case 1: return 'Saldo iniziale'
    case 2: return 'Ricezione acquisto'
    case 3: return 'Rettifica positiva'
    case 4: return 'Rettifica negativa'
    default: return 'Movimento'
  }
}

export default function MagazzinoPage() {
  const [overview, setOverview] = useState<EmployeeInventoryOverview | null>(null)
  const [selectedBranchId, setSelectedBranchId] = useState<number | null>(null)
  const [items, setItems] = useState<InventoryItem[]>([])
  const [movements, setMovements] = useState<InventoryMovement[]>([])
  const [lowStockRows, setLowStockRows] = useState<LowStockRow[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const loadInventory = async () => {
    setLoading(true)
    setError('')

    const [overviewRes, itemsRes, movementsRes, lowStockRes] = await Promise.allSettled([
      inventoryApi.getOverview(selectedBranchId),
      inventoryApi.getItems(selectedBranchId),
      inventoryApi.getMovements(selectedBranchId),
      inventoryApi.getLowStock(selectedBranchId),
    ])

    const nextOverview = overviewRes.status === 'fulfilled' ? overviewRes.value : null
    setOverview(nextOverview)
    setItems(itemsRes.status === 'fulfilled' ? itemsRes.value : [])
    setMovements(movementsRes.status === 'fulfilled' ? movementsRes.value : [])
    setLowStockRows(lowStockRes.status === 'fulfilled' ? lowStockRes.value : [])

    if (selectedBranchId == null && nextOverview?.selectedBranchId != null) {
      setSelectedBranchId(nextOverview.selectedBranchId)
    }

    const failed = [overviewRes, itemsRes, movementsRes, lowStockRes].some(result => result.status === 'rejected')
    if (failed) {
      setError('Alcuni dati del magazzino non sono disponibili al momento.')
    }

    setLoading(false)
  }

  useEffect(() => {
    loadInventory()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedBranchId])

  const filteredItems = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return items
    return items.filter(item =>
      item.sku.toLowerCase().includes(term) ||
      item.name.toLowerCase().includes(term) ||
      (item.barcode ?? '').toLowerCase().includes(term),
    )
  }, [items, search])

  const selectedBranchName = overview?.accessibleBranches.find(branch => branch.id === selectedBranchId)?.name ?? 'Filiale assegnata'

  return (
    <div className="employee-magazzino-page">
      <div className="employee-magazzino-header">
        <div>
          <h1 className="employee-magazzino-title">Magazzino</h1>
          <p className="employee-magazzino-subtitle">
            Consultazione operativa di stock, articoli sotto soglia e ultimi movimenti sulla filiale assegnata.
          </p>
        </div>

        <div className="employee-magazzino-filter-card">
          <span>Filiale operativa</span>
          <select
            value={selectedBranchId ?? ''}
            onChange={event => setSelectedBranchId(event.target.value ? Number(event.target.value) : null)}
          >
            {(overview?.accessibleBranches ?? []).map(branch => (
              <option key={branch.id} value={branch.id}>
                {branch.name}{branch.isHomeBranch ? ' · principale' : ''}{!branch.isActive ? ' · inattiva' : ''}
              </option>
            ))}
          </select>
        </div>
      </div>

      {error && <div className="employee-magazzino-alert">{error}</div>}

      {loading ? (
        <div className="employee-magazzino-loading">
          <div className="employee-magazzino-spinner" />
          <span>Caricamento stock e movimenti...</span>
        </div>
      ) : (
        <>
          <div className="employee-magazzino-stats">
            <div className="employee-magazzino-stat-card accent-teal">
              <span>Articoli visibili</span>
              <strong>{overview?.dashboard.totalItems ?? 0}</strong>
            </div>
            <div className="employee-magazzino-stat-card accent-amber">
              <span>Sotto scorta</span>
              <strong>{overview?.dashboard.lowStockItems ?? 0}</strong>
            </div>
            <div className="employee-magazzino-stat-card accent-blue">
              <span>Ordini aperti</span>
              <strong>{overview?.dashboard.openPurchaseOrders ?? 0}</strong>
            </div>
            <div className="employee-magazzino-stat-card accent-slate">
              <span>Valore stock</span>
              <strong>{formatCurrency(overview?.dashboard.totalStockValue ?? 0)}</strong>
            </div>
          </div>

          <div className="employee-magazzino-toolbar">
            <div className="employee-magazzino-scope-pill">Scope operativo: {selectedBranchName}</div>
            <input
              className="employee-magazzino-search"
              placeholder="Cerca per SKU, nome o barcode"
              value={search}
              onChange={event => setSearch(event.target.value)}
            />
          </div>

          <div className="employee-magazzino-grid">
            <section className="employee-magazzino-panel employee-magazzino-panel--wide">
              <div className="employee-panel-head">
                <div>
                  <h2>Stock disponibile</h2>
                  <p>Vista rapida degli articoli consultabili sul tuo perimetro.</p>
                </div>
              </div>

              {filteredItems.length === 0 ? (
                <div className="employee-magazzino-empty">
                  <div className="employee-magazzino-empty-icon">📦</div>
                  <strong>Nessun articolo da mostrare</strong>
                  <p>Prova a cambiare filiale oppure rimuovi il filtro di ricerca.</p>
                </div>
              ) : (
                <div className="employee-item-list">
                  {filteredItems.map(item => (
                    <article key={item.id} className="employee-item-card">
                      <div className="employee-item-head">
                        <div>
                          <span className="employee-item-sku">{item.sku}</span>
                          <h3>{item.name}</h3>
                        </div>
                        {item.totalQuantityOnHand <= item.reorderPoint && item.reorderPoint > 0 && (
                          <span className="employee-item-warning">Da riordinare</span>
                        )}
                      </div>

                      <p className="employee-item-meta">
                        {item.barcode ? `Barcode ${item.barcode}` : 'Barcode non disponibile'} · {item.unitOfMeasure}
                      </p>

                      <div className="employee-item-stats">
                        <div>
                          <span>Quantità</span>
                          <strong>{formatQuantity(item.totalQuantityOnHand)}</strong>
                        </div>
                        <div>
                          <span>Valore</span>
                          <strong>{formatCurrency(item.totalInventoryValue)}</strong>
                        </div>
                        <div>
                          <span>Soglia</span>
                          <strong>{formatQuantity(item.reorderPoint)}</strong>
                        </div>
                      </div>

                      <div className="employee-balance-list">
                        {item.balances.map(balance => (
                          <div key={balance.id} className="employee-balance-row">
                            <div>
                              <strong>{balance.branchName}</strong>
                              <span>Aggiornato {formatDate(balance.updatedAt)}</span>
                            </div>
                            <div className="employee-balance-value">{formatQuantity(balance.quantityOnHand)}</div>
                          </div>
                        ))}
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>

            <section className="employee-magazzino-side">
              <section className="employee-magazzino-panel">
                <div className="employee-panel-head">
                  <div>
                    <h2>Sotto scorta</h2>
                    <p>Priorità operative per il riordino.</p>
                  </div>
                </div>
                {lowStockRows.length === 0 ? (
                  <div className="employee-inline-empty">Nessun articolo sotto soglia.</div>
                ) : (
                  <div className="employee-mini-list">
                    {lowStockRows.map(row => (
                      <div key={`${row.itemId}-${row.branchId}`} className="employee-mini-row">
                        <div>
                          <strong>{row.itemName}</strong>
                          <span>{row.sku}</span>
                        </div>
                        <div className="employee-mini-metric">
                          {formatQuantity(row.quantityOnHand)} / {formatQuantity(row.reorderPoint)}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </section>

              <section className="employee-magazzino-panel">
                <div className="employee-panel-head">
                  <div>
                    <h2>Ultimi movimenti</h2>
                    <p>Feed operativo delle variazioni più recenti.</p>
                  </div>
                </div>
                {movements.length === 0 ? (
                  <div className="employee-inline-empty">Nessun movimento recente.</div>
                ) : (
                  <div className="employee-mini-list">
                    {movements.slice(0, 8).map(movement => (
                      <div key={movement.id} className="employee-movement-row">
                        <div>
                          <strong>{movementTypeLabel(movement.type)}</strong>
                          <span>{formatDate(movement.createdAt)} · {movement.branchName}</span>
                        </div>
                        <div className={`employee-movement-qty ${movement.quantityDelta >= 0 ? 'is-positive' : 'is-negative'}`}>
                          {movement.quantityDelta >= 0 ? '+' : ''}{formatQuantity(movement.quantityDelta)}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            </section>
          </div>
        </>
      )}
    </div>
  )
}