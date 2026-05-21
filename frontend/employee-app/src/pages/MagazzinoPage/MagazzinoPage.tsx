import { FormEvent, useEffect, useMemo, useState } from 'react'
import {
  inventoryApi,
  type CreateGoodsReceiptRequest,
  type CreateInventoryAdjustmentRequest,
  type CreateInventoryItemRequest,
  type CreatePurchaseOrderRequest,
  type CreateSupplierRequest,
  type EmployeeInventoryOverview,
  type InventoryBranchOption,
  type InventoryItem,
  type InventoryMovement,
  type LowStockRow,
  type PurchaseOrder,
  type Supplier,
  type UpdateInventoryItemRequest,
  type UpdateSupplierRequest,
} from '../../lib/api/inventory'
import type { FeatureAccessLevel } from '../../App'
import './MagazzinoPage.css'

interface Props {
  /** Livello di accesso del dipendente alla feature Magazzino. */
  accessLevel: FeatureAccessLevel
}

type TabKey = 'overview' | 'items' | 'movements' | 'suppliers' | 'orders'

interface ItemFormState {
  sku: string
  name: string
  barcode: string
  description: string
  unitOfMeasure: string
  reorderPoint: string
  isActive: boolean
}

interface SupplierFormState {
  name: string
  contactName: string
  email: string
  phone: string
  vatNumber: string
  notes: string
  isActive: boolean
}

interface AdjustmentFormState {
  branchId: string
  itemId: string
  quantityDelta: string
  unitCost: string
  reason: string
}

interface OrderLineFormState {
  itemId: string
  quantityOrdered: string
  unitCost: string
}

interface OrderFormState {
  branchId: string
  supplierId: string
  expectedDeliveryDate: string
  notes: string
  lines: OrderLineFormState[]
}

interface ReceiptLineFormState {
  purchaseOrderLineId: number
  quantityReceived: string
  unitCost: string
}

const ALL_TABS: Array<{ key: TabKey; label: string }> = [
  { key: 'overview', label: 'Panoramica' },
  { key: 'items', label: 'Articoli' },
  { key: 'movements', label: 'Movimenti' },
  { key: 'suppliers', label: 'Fornitori' },
  { key: 'orders', label: 'Ordini acquisto' },
]

const emptyItemForm: ItemFormState = {
  sku: '',
  name: '',
  barcode: '',
  description: '',
  unitOfMeasure: 'pz',
  reorderPoint: '0',
  isActive: true,
}

const emptySupplierForm: SupplierFormState = {
  name: '',
  contactName: '',
  email: '',
  phone: '',
  vatNumber: '',
  notes: '',
  isActive: true,
}

const defaultOrderLine: OrderLineFormState = { itemId: '', quantityOrdered: '1', unitCost: '0' }

/** Ordinamento dei livelli di accesso per i confronti di gating. */
const LEVEL_RANK: Record<FeatureAccessLevel, number> = {
  ReadOnly: 1,
  Operator: 2,
  Manager: 3,
}

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

function statusLabel(status: number): string {
  switch (status) {
    case 1: return 'Bozza'
    case 2: return 'Inviato'
    case 3: return 'Parzialmente ricevuto'
    case 4: return 'Chiuso'
    case 5: return 'Annullato'
    default: return 'Sconosciuto'
  }
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

function getApiErrorMessage(error: unknown, fallback: string): string {
  const axiosErr = error as { response?: { data?: { message?: string } } }
  return axiosErr.response?.data?.message ?? fallback
}

export default function MagazzinoPage({ accessLevel }: Props) {
  // Gating per livello: cosa il dipendente può fare.
  const canOperate = LEVEL_RANK[accessLevel] >= LEVEL_RANK.Operator
  const canManage = LEVEL_RANK[accessLevel] >= LEVEL_RANK.Manager

  const [activeTab, setActiveTab] = useState<TabKey>('overview')
  const [overview, setOverview] = useState<EmployeeInventoryOverview | null>(null)
  const [selectedBranchId, setSelectedBranchId] = useState<number | null>(null)
  const [items, setItems] = useState<InventoryItem[]>([])
  const [movements, setMovements] = useState<InventoryMovement[]>([])
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [orders, setOrders] = useState<PurchaseOrder[]>([])
  const [lowStockRows, setLowStockRows] = useState<LowStockRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const [itemSearch, setItemSearch] = useState('')
  const [editingItem, setEditingItem] = useState<InventoryItem | null>(null)
  const [itemModalOpen, setItemModalOpen] = useState(false)
  const [itemForm, setItemForm] = useState<ItemFormState>(emptyItemForm)
  const [itemSaving, setItemSaving] = useState(false)

  const [adjustmentForm, setAdjustmentForm] = useState<AdjustmentFormState>({
    branchId: '',
    itemId: '',
    quantityDelta: '',
    unitCost: '',
    reason: '',
  })
  const [adjustmentSaving, setAdjustmentSaving] = useState(false)

  const [editingSupplier, setEditingSupplier] = useState<Supplier | null>(null)
  const [supplierModalOpen, setSupplierModalOpen] = useState(false)
  const [supplierForm, setSupplierForm] = useState<SupplierFormState>(emptySupplierForm)
  const [supplierSaving, setSupplierSaving] = useState(false)

  const [orderModalOpen, setOrderModalOpen] = useState(false)
  const [orderForm, setOrderForm] = useState<OrderFormState>({
    branchId: '',
    supplierId: '',
    expectedDeliveryDate: '',
    notes: '',
    lines: [defaultOrderLine],
  })
  const [orderSaving, setOrderSaving] = useState(false)

  const [receivingOrder, setReceivingOrder] = useState<PurchaseOrder | null>(null)
  const [receiptNotes, setReceiptNotes] = useState('')
  const [receiptLines, setReceiptLines] = useState<ReceiptLineFormState[]>([])
  const [receiptSaving, setReceiptSaving] = useState(false)

  const accessibleBranches: InventoryBranchOption[] = overview?.accessibleBranches ?? []

  const loadInventory = async () => {
    setLoading(true)
    setError('')

    // Le richieste operative (suppliers, orders) vengono caricate solo se il
    // dipendente ha almeno il livello che ne consente la consultazione utile.
    const [overviewRes, itemsRes, movementsRes, lowStockRes, suppliersRes, ordersRes] = await Promise.allSettled([
      inventoryApi.getOverview(selectedBranchId),
      inventoryApi.getItems(selectedBranchId),
      inventoryApi.getMovements(selectedBranchId),
      inventoryApi.getLowStock(selectedBranchId),
      inventoryApi.getSuppliers(true),
      inventoryApi.getOrders(selectedBranchId),
    ])

    const nextOverview = overviewRes.status === 'fulfilled' ? overviewRes.value : null
    setOverview(nextOverview)
    setItems(itemsRes.status === 'fulfilled' ? itemsRes.value : [])
    setMovements(movementsRes.status === 'fulfilled' ? movementsRes.value : [])
    setLowStockRows(lowStockRes.status === 'fulfilled' ? lowStockRes.value : [])
    setSuppliers(suppliersRes.status === 'fulfilled' ? suppliersRes.value : [])
    setOrders(ordersRes.status === 'fulfilled' ? ordersRes.value : [])

    if (selectedBranchId == null && nextOverview?.selectedBranchId != null) {
      setSelectedBranchId(nextOverview.selectedBranchId)
    }

    const failed = [overviewRes, itemsRes, movementsRes, lowStockRes, suppliersRes, ordersRes]
      .some(result => result.status === 'rejected')
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
    const term = itemSearch.trim().toLowerCase()
    if (!term) return items
    return items.filter(item =>
      item.sku.toLowerCase().includes(term) ||
      item.name.toLowerCase().includes(term) ||
      (item.barcode ?? '').toLowerCase().includes(term),
    )
  }, [items, itemSearch])

  const draftOrders = orders.filter(order => order.status === 1)
  const openOrders = orders.filter(order => order.status === 1 || order.status === 2 || order.status === 3)
  const recentMovements = movements.slice(0, 8)

  const selectedBranchName = accessibleBranches.find(branch => branch.id === selectedBranchId)?.name
    ?? 'Filiale assegnata'

  // ── Articoli (Manager) ──────────────────────────────────────────────────

  const openCreateItem = () => {
    setEditingItem(null)
    setItemForm(emptyItemForm)
    setItemModalOpen(true)
  }

  const openEditItem = (item: InventoryItem) => {
    setEditingItem(item)
    setItemForm({
      sku: item.sku,
      name: item.name,
      barcode: item.barcode ?? '',
      description: item.description ?? '',
      unitOfMeasure: item.unitOfMeasure,
      reorderPoint: String(item.reorderPoint),
      isActive: item.isActive,
    })
    setItemModalOpen(true)
  }

  const handleItemSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setItemSaving(true)

    try {
      const basePayload: CreateInventoryItemRequest = {
        sku: itemForm.sku.trim(),
        name: itemForm.name.trim(),
        barcode: itemForm.barcode.trim() || undefined,
        description: itemForm.description.trim() || undefined,
        unitOfMeasure: itemForm.unitOfMeasure.trim() || 'pz',
        reorderPoint: Number(itemForm.reorderPoint || 0),
      }

      if (editingItem) {
        const payload: UpdateInventoryItemRequest = { ...basePayload, isActive: itemForm.isActive }
        await inventoryApi.updateItem(editingItem.id, payload)
        setNotice(`Articolo ${editingItem.sku} aggiornato.`)
      } else {
        await inventoryApi.createItem(basePayload)
        setNotice(`Articolo ${basePayload.sku} creato.`)
      }

      setItemModalOpen(false)
      await loadInventory()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Errore durante il salvataggio dell’articolo.'))
    } finally {
      setItemSaving(false)
    }
  }

  // ── Fornitori (Manager) ─────────────────────────────────────────────────

  const openCreateSupplier = () => {
    setEditingSupplier(null)
    setSupplierForm(emptySupplierForm)
    setSupplierModalOpen(true)
  }

  const openEditSupplier = (supplier: Supplier) => {
    setEditingSupplier(supplier)
    setSupplierForm({
      name: supplier.name,
      contactName: supplier.contactName ?? '',
      email: supplier.email ?? '',
      phone: supplier.phone ?? '',
      vatNumber: supplier.vatNumber ?? '',
      notes: supplier.notes ?? '',
      isActive: supplier.isActive,
    })
    setSupplierModalOpen(true)
  }

  const handleSupplierSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setSupplierSaving(true)

    try {
      const basePayload: CreateSupplierRequest = {
        name: supplierForm.name.trim(),
        contactName: supplierForm.contactName.trim() || undefined,
        email: supplierForm.email.trim() || undefined,
        phone: supplierForm.phone.trim() || undefined,
        vatNumber: supplierForm.vatNumber.trim() || undefined,
        notes: supplierForm.notes.trim() || undefined,
      }

      if (editingSupplier) {
        const payload: UpdateSupplierRequest = { ...basePayload, isActive: supplierForm.isActive }
        await inventoryApi.updateSupplier(editingSupplier.id, payload)
        setNotice(`Fornitore ${editingSupplier.name} aggiornato.`)
      } else {
        await inventoryApi.createSupplier(basePayload)
        setNotice(`Fornitore ${basePayload.name} creato.`)
      }

      setSupplierModalOpen(false)
      await loadInventory()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Errore durante il salvataggio del fornitore.'))
    } finally {
      setSupplierSaving(false)
    }
  }

  // ── Rettifiche stock (Operator) ─────────────────────────────────────────

  const handleAdjustmentSubmit = async (event: FormEvent) => {
    event.preventDefault()

    // Guardia: filiale e articolo devono essere scelte valide. Il select vuoto
    // dà Number('') = 0; senza questo controllo partirebbe una richiesta a vuoto.
    const branchId = Number(adjustmentForm.branchId)
    const itemId = Number(adjustmentForm.itemId)
    const quantityDelta = Number(adjustmentForm.quantityDelta)
    if (!branchId || !itemId) {
      setError('Seleziona una filiale e un articolo validi prima di registrare la rettifica.')
      return
    }
    if (!quantityDelta) {
      setError('La quantità di rettifica deve essere diversa da zero.')
      return
    }

    setAdjustmentSaving(true)

    try {
      const payload: CreateInventoryAdjustmentRequest = {
        branchId,
        itemId,
        quantityDelta,
        unitCost: adjustmentForm.unitCost ? Number(adjustmentForm.unitCost) : undefined,
        reason: adjustmentForm.reason.trim(),
      }

      await inventoryApi.createAdjustment(payload)
      setNotice('Rettifica stock registrata.')
      setAdjustmentForm({
        branchId: adjustmentForm.branchId,
        itemId: '',
        quantityDelta: '',
        unitCost: '',
        reason: '',
      })
      await loadInventory()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Errore durante la rettifica stock.'))
    } finally {
      setAdjustmentSaving(false)
    }
  }

  // ── Ordini di acquisto (Manager, ricezione Operator) ────────────────────

  const addOrderLine = () => {
    setOrderForm(prev => ({ ...prev, lines: [...prev.lines, { ...defaultOrderLine }] }))
  }

  const updateOrderLine = (index: number, patch: Partial<OrderLineFormState>) => {
    setOrderForm(prev => ({
      ...prev,
      lines: prev.lines.map((line, lineIndex) => (lineIndex === index ? { ...line, ...patch } : line)),
    }))
  }

  const removeOrderLine = (index: number) => {
    setOrderForm(prev => ({
      ...prev,
      lines: prev.lines.filter((_, lineIndex) => lineIndex !== index),
    }))
  }

  const openCreateOrder = () => {
    setOrderForm({
      branchId: selectedBranchId != null ? String(selectedBranchId) : String(accessibleBranches[0]?.id ?? ''),
      supplierId: suppliers.find(supplier => supplier.isActive)?.id?.toString() ?? '',
      expectedDeliveryDate: '',
      notes: '',
      lines: [{ ...defaultOrderLine }],
    })
    setOrderModalOpen(true)
  }

  const handleOrderSubmit = async (event: FormEvent) => {
    event.preventDefault()

    // Guardia: filiale, fornitore e ogni riga devono essere scelte valide.
    const branchId = Number(orderForm.branchId)
    const supplierId = Number(orderForm.supplierId)
    if (!branchId || !supplierId) {
      setError('Seleziona una filiale e un fornitore validi.')
      return
    }
    const invalidLine = orderForm.lines.some(line =>
      !Number(line.itemId) || Number(line.quantityOrdered) <= 0 || Number(line.unitCost) < 0,
    )
    if (orderForm.lines.length === 0 || invalidLine) {
      setError('Ogni riga ordine deve avere un articolo, quantità maggiore di zero e costo non negativo.')
      return
    }

    setOrderSaving(true)

    try {
      const payload: CreatePurchaseOrderRequest = {
        branchId,
        supplierId,
        expectedDeliveryDate: orderForm.expectedDeliveryDate || undefined,
        notes: orderForm.notes.trim() || undefined,
        lines: orderForm.lines.map(line => ({
          itemId: Number(line.itemId),
          quantityOrdered: Number(line.quantityOrdered),
          unitCost: Number(line.unitCost),
        })),
      }

      await inventoryApi.createOrder(payload)
      setOrderModalOpen(false)
      setNotice('Ordine acquisto creato.')
      await loadInventory()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Errore durante la creazione dell’ordine acquisto.'))
    } finally {
      setOrderSaving(false)
    }
  }

  const handleSendOrder = async (order: PurchaseOrder) => {
    try {
      await inventoryApi.sendOrder(order.id)
      setNotice(`Ordine ${order.orderNumber} confermato.`)
      await loadInventory()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Errore durante la conferma dell’ordine.'))
    }
  }

  const handleCancelOrder = async (order: PurchaseOrder) => {
    if (!window.confirm(`Annullare l'ordine ${order.orderNumber}?`)) return

    try {
      await inventoryApi.cancelOrder(order.id)
      setNotice(`Ordine ${order.orderNumber} annullato.`)
      await loadInventory()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Errore durante l’annullamento dell’ordine.'))
    }
  }

  const openReceiveOrder = (order: PurchaseOrder) => {
    setReceivingOrder(order)
    setReceiptNotes('')
    setReceiptLines(order.lines
      .filter(line => line.quantityReceived < line.quantityOrdered)
      .map(line => ({
        purchaseOrderLineId: line.id,
        quantityReceived: String(line.quantityOrdered - line.quantityReceived),
        unitCost: String(line.unitCost),
      })))
  }

  const updateReceiptLine = (lineId: number, patch: Partial<ReceiptLineFormState>) => {
    setReceiptLines(prev => prev.map(line => (
      line.purchaseOrderLineId === lineId ? { ...line, ...patch } : line
    )))
  }

  const handleReceiptSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (!receivingOrder) return

    const validLines = receiptLines
      .filter(line => Number(line.quantityReceived) > 0)
      .map(line => ({
        purchaseOrderLineId: line.purchaseOrderLineId,
        quantityReceived: Number(line.quantityReceived),
        unitCost: line.unitCost ? Number(line.unitCost) : undefined,
      }))

    // Guardia: una ricezione senza righe con quantità > 0 non ha senso.
    if (validLines.length === 0) {
      setError('Indica almeno una riga con quantità ricevuta maggiore di zero.')
      return
    }

    setReceiptSaving(true)
    try {
      const payload: CreateGoodsReceiptRequest = {
        notes: receiptNotes.trim() || undefined,
        lines: validLines,
      }

      await inventoryApi.receiveOrder(receivingOrder.id, payload)
      setNotice(`Ricezione registrata per ${receivingOrder.orderNumber}.`)
      setReceivingOrder(null)
      await loadInventory()
    } catch (err) {
      setError(getApiErrorMessage(err, 'Errore durante la ricezione dell’ordine.'))
    } finally {
      setReceiptSaving(false)
    }
  }

  // ── Render tab ──────────────────────────────────────────────────────────

  const renderOverview = () => (
    <div className="inventory-stack">
      <div className="inventory-panel-grid">
        <section className="inventory-panel inventory-panel--accent">
          <div className="inventory-panel-head">
            <div>
              <h2>Stato operativo</h2>
              <p>Vista consolidata di stock, ordini e fornitori sul tuo perimetro.</p>
            </div>
            <span className="inventory-context-pill">{selectedBranchName}</span>
          </div>
          <div className="inventory-stat-grid">
            <div className="inventory-stat-card">
              <span className="inventory-stat-label">Articoli visibili</span>
              <strong>{overview?.dashboard.totalItems ?? 0}</strong>
            </div>
            <div className="inventory-stat-card">
              <span className="inventory-stat-label">Fornitori attivi</span>
              <strong>{overview?.dashboard.activeSuppliers ?? 0}</strong>
            </div>
            <div className="inventory-stat-card">
              <span className="inventory-stat-label">Ordini aperti</span>
              <strong>{overview?.dashboard.openPurchaseOrders ?? 0}</strong>
            </div>
            <div className="inventory-stat-card">
              <span className="inventory-stat-label">Valore stock</span>
              <strong>{formatCurrency(overview?.dashboard.totalStockValue ?? 0)}</strong>
            </div>
          </div>
        </section>

        <section className="inventory-panel">
          <div className="inventory-section-head">
            <div>
              <h2>Sotto scorta</h2>
              <p>Articoli che richiedono riordino o verifica immediata.</p>
            </div>
          </div>
          {lowStockRows.length === 0 ? (
            <div className="inventory-empty-inline">Nessuna anomalia di riordino sul filtro corrente.</div>
          ) : (
            <div className="inventory-mini-list">
              {lowStockRows.slice(0, 5).map(row => (
                <div key={`${row.itemId}-${row.branchId}`} className="inventory-mini-row">
                  <div>
                    <strong>{row.itemName}</strong>
                    <span>{row.branchName}</span>
                  </div>
                  <div className="inventory-mini-metric">
                    {formatQuantity(row.quantityOnHand)} / {formatQuantity(row.reorderPoint)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>

      <div className="inventory-panel-grid inventory-panel-grid--split">
        <section className="inventory-panel">
          <div className="inventory-section-head">
            <div>
              <h2>Ordini aperti</h2>
              <p>Monitoraggio acquisti in lavorazione e ricezioni parziali.</p>
            </div>
            <button className="inventory-link-btn" onClick={() => setActiveTab('orders')}>Gestisci ordini</button>
          </div>
          {openOrders.length === 0 ? (
            <div className="inventory-empty-inline">Nessun ordine aperto.</div>
          ) : (
            <div className="inventory-card-list">
              {openOrders.slice(0, 4).map(order => (
                <article key={order.id} className="inventory-info-card">
                  <div className="inventory-info-card-head">
                    <strong>{order.orderNumber}</strong>
                    <span className={`inventory-status-badge status-${order.status}`}>{statusLabel(order.status)}</span>
                  </div>
                  <p>{order.supplierName} · {order.branchName}</p>
                  <div className="inventory-info-card-foot">
                    <span>{order.lines.length} righe</span>
                    <span>{formatCurrency(order.totalAmount)}</span>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="inventory-panel">
          <div className="inventory-section-head">
            <div>
              <h2>Ultimi movimenti</h2>
              <p>Storico rapido delle ultime rettifiche e ricezioni.</p>
            </div>
            <button className="inventory-link-btn" onClick={() => setActiveTab('movements')}>Apri movimenti</button>
          </div>
          {recentMovements.length === 0 ? (
            <div className="inventory-empty-inline">Nessun movimento disponibile.</div>
          ) : (
            <div className="inventory-timeline">
              {recentMovements.map(movement => (
                <div key={movement.id} className="inventory-timeline-row">
                  <div className="inventory-timeline-icon">📦</div>
                  <div>
                    <strong>{movementTypeLabel(movement.type)}</strong>
                    <span>{movement.branchName} · {formatDate(movement.createdAt)}</span>
                  </div>
                  <div className={`inventory-quantity-pill ${movement.quantityDelta >= 0 ? 'is-positive' : 'is-negative'}`}>
                    {movement.quantityDelta >= 0 ? '+' : ''}{formatQuantity(movement.quantityDelta)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  )

  const renderItems = () => (
    <div className="inventory-stack">
      <section className="inventory-panel">
        <div className="inventory-section-head">
          <div>
            <h2>Catalogo articoli</h2>
            <p>SKU, barcode, soglie di riordino e distribuzione stock per filiale.</p>
          </div>
          <div className="inventory-toolbar-row">
            <input
              className="inventory-search"
              placeholder="Cerca per SKU, nome o barcode"
              value={itemSearch}
              onChange={event => setItemSearch(event.target.value)}
            />
            {canManage && (
              <button className="inventory-primary-btn" onClick={openCreateItem}>+ Nuovo articolo</button>
            )}
          </div>
        </div>

        {filteredItems.length === 0 ? (
          <div className="inventory-empty-state">
            <div className="inventory-empty-icon">📦</div>
            <strong>Nessun articolo da mostrare</strong>
            <p>Prova a cambiare filiale oppure rimuovi il filtro di ricerca.</p>
          </div>
        ) : (
          <div className="inventory-card-grid">
            {filteredItems.map(item => (
              <article key={item.id} className="inventory-item-card">
                <div className="inventory-item-card-head">
                  <div>
                    <span className="inventory-item-sku">{item.sku}</span>
                    <h3>{item.name}</h3>
                  </div>
                  {canManage && (
                    <button className="inventory-link-btn" onClick={() => openEditItem(item)}>Modifica</button>
                  )}
                </div>

                <p className="inventory-item-meta">
                  {item.barcode ? `Barcode ${item.barcode}` : 'Barcode non configurato'} · {item.unitOfMeasure}
                </p>

                <div className="inventory-item-metrics">
                  <div>
                    <span>Stock totale</span>
                    <strong>{formatQuantity(item.totalQuantityOnHand)}</strong>
                  </div>
                  <div>
                    <span>Valore</span>
                    <strong>{formatCurrency(item.totalInventoryValue)}</strong>
                  </div>
                  <div>
                    <span>Riordino</span>
                    <strong>{formatQuantity(item.reorderPoint)}</strong>
                  </div>
                </div>

                <div className="inventory-balance-list">
                  {item.balances.length === 0 ? (
                    <div className="inventory-empty-inline">Nessun saldo registrato.</div>
                  ) : (
                    item.balances.map(balance => (
                      <div key={balance.id} className="inventory-balance-row">
                        <div>
                          <strong>{balance.branchName}</strong>
                          <span>Aggiornato {formatDate(balance.updatedAt)}</span>
                        </div>
                        <div className="inventory-balance-values">
                          <span>{formatQuantity(balance.quantityOnHand)}</span>
                          <span>{formatCurrency(balance.inventoryValue)}</span>
                        </div>
                      </div>
                    ))
                  )}
                </div>

                {canOperate && (
                  <div className="inventory-item-actions">
                    <button
                      className="inventory-secondary-btn"
                      onClick={() => {
                        setActiveTab('movements')
                        setAdjustmentForm(prev => ({
                          ...prev,
                          branchId: String(item.balances[0]?.branchId ?? selectedBranchId ?? ''),
                          itemId: String(item.id),
                          quantityDelta: '',
                        }))
                      }}
                    >
                      Rettifica stock
                    </button>
                    {!item.isActive && <span className="inventory-muted-chip">Disattivo</span>}
                  </div>
                )}
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  )

  const renderMovements = () => (
    <div className="inventory-stack">
      {canOperate && accessibleBranches.length === 0 && (
        <div className="inventory-empty-inline">
          Nessuna filiale assegnata: non è possibile registrare rettifiche. Contatta l'amministratore.
        </div>
      )}

      {canOperate && accessibleBranches.length > 0 && (
        <section className="inventory-panel inventory-panel--form">
          <div className="inventory-section-head">
            <div>
              <h2>Rettifica stock</h2>
              <p>Ogni rettifica scrive ledger e aggiorna il saldo della filiale selezionata.</p>
            </div>
          </div>

          <form className="inventory-form-grid" onSubmit={handleAdjustmentSubmit}>
            <label>
              Filiale
              <select
                value={adjustmentForm.branchId}
                onChange={event => setAdjustmentForm(prev => ({ ...prev, branchId: event.target.value }))}
                required
              >
                <option value="">Seleziona filiale</option>
                {accessibleBranches.map(branch => (
                  <option key={branch.id} value={branch.id}>{branch.name}</option>
                ))}
              </select>
            </label>

            <label>
              Articolo
              <select
                value={adjustmentForm.itemId}
                onChange={event => setAdjustmentForm(prev => ({ ...prev, itemId: event.target.value }))}
                required
              >
                <option value="">Seleziona articolo</option>
                {items.map(item => (
                  <option key={item.id} value={item.id}>{item.sku} · {item.name}</option>
                ))}
              </select>
            </label>

            <label>
              Delta quantità
              <input
                type="number"
                step="0.001"
                value={adjustmentForm.quantityDelta}
                onChange={event => setAdjustmentForm(prev => ({ ...prev, quantityDelta: event.target.value }))}
                placeholder="Es. 12 oppure -3"
                required
              />
            </label>

            <label>
              Costo unitario
              <input
                type="number"
                step="0.0001"
                value={adjustmentForm.unitCost}
                onChange={event => setAdjustmentForm(prev => ({ ...prev, unitCost: event.target.value }))}
                placeholder="Facoltativo per rettifiche negative"
              />
            </label>

            <label className="inventory-form-grid-full">
              Motivo (obbligatorio)
              <textarea
                value={adjustmentForm.reason}
                onChange={event => setAdjustmentForm(prev => ({ ...prev, reason: event.target.value }))}
                placeholder="Motivo della rettifica"
                required
              />
            </label>

            <div className="inventory-form-actions inventory-form-grid-full">
              <button className="inventory-primary-btn" type="submit" disabled={adjustmentSaving}>
                {adjustmentSaving ? 'Salvataggio...' : 'Registra rettifica'}
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="inventory-panel">
        <div className="inventory-section-head">
          <div>
            <h2>Ledger movimenti</h2>
            <p>Storico cronologico di ricezioni, rettifiche e variazioni stock.</p>
          </div>
        </div>

        {movements.length === 0 ? (
          <div className="inventory-empty-inline">Nessun movimento disponibile sul filtro selezionato.</div>
        ) : (
          <div className="inventory-table-wrap">
            <table className="inventory-table">
              <thead>
                <tr>
                  <th>Data</th>
                  <th>Tipo</th>
                  <th>Filiale</th>
                  <th>Quantità</th>
                  <th>Valore</th>
                  <th>Dettaglio</th>
                </tr>
              </thead>
              <tbody>
                {movements.map(movement => (
                  <tr key={movement.id}>
                    <td>{formatDate(movement.createdAt)}</td>
                    <td>{movementTypeLabel(movement.type)}</td>
                    <td>{movement.branchName}</td>
                    <td className={movement.quantityDelta >= 0 ? 'inventory-positive' : 'inventory-negative'}>
                      {movement.quantityDelta >= 0 ? '+' : ''}{formatQuantity(movement.quantityDelta)}
                    </td>
                    <td>{formatCurrency(movement.totalValue)}</td>
                    <td>{movement.reason ?? movement.referenceNumber ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )

  const renderSuppliers = () => (
    <section className="inventory-panel">
      <div className="inventory-section-head">
        <div>
          <h2>Fornitori</h2>
          <p>Rubrica acquisti con stato attivo e contatti operativi.</p>
        </div>
        {canManage && (
          <button className="inventory-primary-btn" onClick={openCreateSupplier}>+ Nuovo fornitore</button>
        )}
      </div>

      {suppliers.length === 0 ? (
        <div className="inventory-empty-state">
          <div className="inventory-empty-icon">🏭</div>
          <strong>Nessun fornitore configurato</strong>
          <p>{canManage ? 'Crea i fornitori prima di emettere ordini acquisto.' : 'Nessun fornitore disponibile al momento.'}</p>
        </div>
      ) : (
        <div className="inventory-card-grid inventory-card-grid--compact">
          {suppliers.map(supplier => (
            <article key={supplier.id} className="inventory-info-card">
              <div className="inventory-info-card-head">
                <strong>{supplier.name}</strong>
                {!supplier.isActive && <span className="inventory-muted-chip">Disattivo</span>}
              </div>
              <p>{supplier.contactName || 'Nessun referente'} · {supplier.phone || supplier.email || 'Contatto non definito'}</p>
              <div className="inventory-info-card-foot">
                <span>{supplier.vatNumber || 'P.IVA non inserita'}</span>
                {canManage && (
                  <button className="inventory-link-btn" onClick={() => openEditSupplier(supplier)}>Modifica</button>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )

  const renderOrders = () => (
    <section className="inventory-panel">
      <div className="inventory-section-head">
        <div>
          <h2>Ordini acquisto</h2>
          <p>Dalla bozza alla ricezione parziale o totale senza uscire dal modulo.</p>
        </div>
        <div className="inventory-toolbar-row">
          {draftOrders.length > 0 && <span className="inventory-muted-chip">{draftOrders.length} in bozza</span>}
          {canManage && (
            <button className="inventory-primary-btn" onClick={openCreateOrder}>+ Nuovo ordine</button>
          )}
        </div>
      </div>

      {orders.length === 0 ? (
        <div className="inventory-empty-state">
          <div className="inventory-empty-icon">🧾</div>
          <strong>Nessun ordine acquisto</strong>
          <p>Nessun ordine sul perimetro corrente.</p>
        </div>
      ) : (
        <div className="inventory-order-list">
          {orders.map(order => {
            const canReceiveOrder = canOperate && (order.status === 2 || order.status === 3)
            const canSend = canManage && order.status === 1
            const canCancel = canManage && (order.status === 1 || order.status === 2)
            const hasActions = canReceiveOrder || canSend || canCancel

            return (
              <article key={order.id} className="inventory-order-card">
                <div className="inventory-order-head">
                  <div>
                    <div className="inventory-order-title-row">
                      <strong>{order.orderNumber}</strong>
                      <span className={`inventory-status-badge status-${order.status}`}>{statusLabel(order.status)}</span>
                    </div>
                    <p>{order.supplierName} · {order.branchName}</p>
                  </div>
                  <div className="inventory-order-total">{formatCurrency(order.totalAmount)}</div>
                </div>

                <div className="inventory-table-wrap">
                  <table className="inventory-table inventory-table--dense">
                    <thead>
                      <tr>
                        <th>Articolo</th>
                        <th>Q.ta ord.</th>
                        <th>Q.ta ric.</th>
                        <th>Costo</th>
                      </tr>
                    </thead>
                    <tbody>
                      {order.lines.map(line => (
                        <tr key={line.id}>
                          <td>{line.itemSku} · {line.itemName}</td>
                          <td>{formatQuantity(line.quantityOrdered)}</td>
                          <td>{formatQuantity(line.quantityReceived)}</td>
                          <td>{formatCurrency(line.lineTotal)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {order.receipts.length > 0 && (
                  <div className="inventory-receipt-summary">
                    <strong>Ricezioni registrate</strong>
                    <div className="inventory-mini-list">
                      {order.receipts.map(receipt => (
                        <div key={receipt.id} className="inventory-mini-row">
                          <div>
                            <strong>{receipt.receiptNumber}</strong>
                            <span>{formatDate(receipt.receivedAt)}</span>
                          </div>
                          <div className="inventory-mini-metric">{formatCurrency(receipt.totalAmount)}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {hasActions && (
                  <div className="inventory-order-actions">
                    {canSend && <button className="inventory-secondary-btn" onClick={() => handleSendOrder(order)}>Conferma ordine</button>}
                    {canReceiveOrder && <button className="inventory-primary-btn" onClick={() => openReceiveOrder(order)}>Registra ricezione</button>}
                    {canCancel && <button className="inventory-danger-btn" onClick={() => handleCancelOrder(order)}>Annulla</button>}
                  </div>
                )}
              </article>
            )
          })}
        </div>
      )}
    </section>
  )

  return (
    <div className="magazzino-page">
      <div className="magazzino-hero">
        <div>
          <h1 className="page-title">Magazzino</h1>
          <p className="page-subtitle">
            Operatività di stock, articoli, fornitori, acquisti e ricezioni sul tuo perimetro.
          </p>
        </div>

        <div className="magazzino-hero-actions">
          <span className="inventory-context-pill" title="Livello di accesso del tuo ruolo sul Magazzino">
            {canManage
              ? 'Livello: Manager'
              : canOperate
                ? 'Livello: Operatore'
                : 'Livello: Sola lettura'}
          </span>
          <div className="inventory-context-box">
            <span>Filiale operativa</span>
            <select
              value={selectedBranchId ?? ''}
              onChange={event => setSelectedBranchId(event.target.value ? Number(event.target.value) : null)}
            >
              {accessibleBranches.length === 0 && <option value="">Nessuna filiale assegnata</option>}
              {accessibleBranches.map(branch => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}{branch.isHomeBranch ? ' · principale' : ''}{!branch.isActive ? ' · inattiva' : ''}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {!canOperate && (
        <div className="inventory-notice" role="status">
          <span>
            Il tuo ruolo consente la <strong>sola consultazione</strong> del Magazzino.
            Per registrare rettifiche, ricezioni o gestire anagrafiche serve un livello superiore.
          </span>
        </div>
      )}

      {notice && (
        <div className="inventory-notice" role="status">
          <span>{notice}</span>
          <button onClick={() => setNotice('')}>Chiudi</button>
        </div>
      )}
      {error && (
        <div className="inventory-error" role="alert">
          <span>{error}</span>
          <button onClick={() => setError('')}>Chiudi</button>
        </div>
      )}

      <div className="magazzino-tabs">
        {ALL_TABS.map(tab => (
          <button
            key={tab.key}
            className={`magazzino-tab ${activeTab === tab.key ? 'is-active' : ''}`}
            onClick={() => setActiveTab(tab.key)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="inventory-loading-state">
          <div className="inventory-spinner" />
          <span>Caricamento del modulo Magazzino...</span>
        </div>
      ) : (
        <>
          {activeTab === 'overview' && renderOverview()}
          {activeTab === 'items' && renderItems()}
          {activeTab === 'movements' && renderMovements()}
          {activeTab === 'suppliers' && renderSuppliers()}
          {activeTab === 'orders' && renderOrders()}
        </>
      )}

      {itemModalOpen && canManage && (
        <div className="inventory-modal-overlay" onClick={() => setItemModalOpen(false)}>
          <div className="inventory-modal" onClick={event => event.stopPropagation()}>
            <div className="inventory-modal-head">
              <div>
                <h2>{editingItem ? 'Modifica articolo' : 'Nuovo articolo'}</h2>
                <p>Definisci SKU, barcode, unità di misura e soglia di riordino.</p>
              </div>
              <button className="inventory-link-btn" onClick={() => setItemModalOpen(false)}>Chiudi</button>
            </div>
            <form className="inventory-form-grid" onSubmit={handleItemSubmit}>
              <label>
                SKU
                <input value={itemForm.sku} onChange={event => setItemForm(prev => ({ ...prev, sku: event.target.value }))} required />
              </label>
              <label>
                Nome
                <input value={itemForm.name} onChange={event => setItemForm(prev => ({ ...prev, name: event.target.value }))} required />
              </label>
              <label>
                Barcode
                <input value={itemForm.barcode} onChange={event => setItemForm(prev => ({ ...prev, barcode: event.target.value }))} />
              </label>
              <label>
                Unità di misura
                <input value={itemForm.unitOfMeasure} onChange={event => setItemForm(prev => ({ ...prev, unitOfMeasure: event.target.value }))} />
              </label>
              <label>
                Soglia di riordino
                <input type="number" step="0.001" value={itemForm.reorderPoint} onChange={event => setItemForm(prev => ({ ...prev, reorderPoint: event.target.value }))} />
              </label>
              {editingItem && (
                <label className="inventory-toggle-label">
                  <input type="checkbox" checked={itemForm.isActive} onChange={event => setItemForm(prev => ({ ...prev, isActive: event.target.checked }))} />
                  Articolo attivo
                </label>
              )}
              <label className="inventory-form-grid-full">
                Descrizione
                <textarea value={itemForm.description} onChange={event => setItemForm(prev => ({ ...prev, description: event.target.value }))} />
              </label>
              <div className="inventory-form-actions inventory-form-grid-full">
                <button type="button" className="inventory-secondary-btn" onClick={() => setItemModalOpen(false)}>Annulla</button>
                <button type="submit" className="inventory-primary-btn" disabled={itemSaving}>{itemSaving ? 'Salvataggio...' : 'Salva articolo'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {supplierModalOpen && canManage && (
        <div className="inventory-modal-overlay" onClick={() => setSupplierModalOpen(false)}>
          <div className="inventory-modal" onClick={event => event.stopPropagation()}>
            <div className="inventory-modal-head">
              <div>
                <h2>{editingSupplier ? 'Modifica fornitore' : 'Nuovo fornitore'}</h2>
                <p>Rubrica acquisti e dati di contatto per gli ordini.</p>
              </div>
              <button className="inventory-link-btn" onClick={() => setSupplierModalOpen(false)}>Chiudi</button>
            </div>
            <form className="inventory-form-grid" onSubmit={handleSupplierSubmit}>
              <label>
                Ragione sociale
                <input value={supplierForm.name} onChange={event => setSupplierForm(prev => ({ ...prev, name: event.target.value }))} required />
              </label>
              <label>
                Referente
                <input value={supplierForm.contactName} onChange={event => setSupplierForm(prev => ({ ...prev, contactName: event.target.value }))} />
              </label>
              <label>
                Email
                <input value={supplierForm.email} onChange={event => setSupplierForm(prev => ({ ...prev, email: event.target.value }))} />
              </label>
              <label>
                Telefono
                <input value={supplierForm.phone} onChange={event => setSupplierForm(prev => ({ ...prev, phone: event.target.value }))} />
              </label>
              <label>
                P.IVA
                <input value={supplierForm.vatNumber} onChange={event => setSupplierForm(prev => ({ ...prev, vatNumber: event.target.value }))} />
              </label>
              {editingSupplier && (
                <label className="inventory-toggle-label">
                  <input type="checkbox" checked={supplierForm.isActive} onChange={event => setSupplierForm(prev => ({ ...prev, isActive: event.target.checked }))} />
                  Fornitore attivo
                </label>
              )}
              <label className="inventory-form-grid-full">
                Note
                <textarea value={supplierForm.notes} onChange={event => setSupplierForm(prev => ({ ...prev, notes: event.target.value }))} />
              </label>
              <div className="inventory-form-actions inventory-form-grid-full">
                <button type="button" className="inventory-secondary-btn" onClick={() => setSupplierModalOpen(false)}>Annulla</button>
                <button type="submit" className="inventory-primary-btn" disabled={supplierSaving}>{supplierSaving ? 'Salvataggio...' : 'Salva fornitore'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {orderModalOpen && canManage && (
        <div className="inventory-modal-overlay" onClick={() => setOrderModalOpen(false)}>
          <div className="inventory-modal inventory-modal--wide" onClick={event => event.stopPropagation()}>
            <div className="inventory-modal-head">
              <div>
                <h2>Nuovo ordine acquisto</h2>
                <p>Componi righe articolo e costi unitari per filiale.</p>
              </div>
              <button className="inventory-link-btn" onClick={() => setOrderModalOpen(false)}>Chiudi</button>
            </div>
            <form className="inventory-form-grid" onSubmit={handleOrderSubmit}>
              <label className="inventory-form-field">
                Filiale
                <select value={orderForm.branchId} onChange={event => setOrderForm(prev => ({ ...prev, branchId: event.target.value }))} required>
                  <option value="">Seleziona filiale</option>
                  {accessibleBranches.map(branch => (
                    <option key={branch.id} value={branch.id}>{branch.name}</option>
                  ))}
                </select>
              </label>
              <label className="inventory-form-field">
                Fornitore
                <select value={orderForm.supplierId} onChange={event => setOrderForm(prev => ({ ...prev, supplierId: event.target.value }))} required>
                  <option value="">Seleziona fornitore</option>
                  {suppliers.filter(supplier => supplier.isActive).map(supplier => (
                    <option key={supplier.id} value={supplier.id}>{supplier.name}</option>
                  ))}
                </select>
              </label>
              <label className="inventory-form-field">
                Consegna prevista
                <input type="date" value={orderForm.expectedDeliveryDate} onChange={event => setOrderForm(prev => ({ ...prev, expectedDeliveryDate: event.target.value }))} />
              </label>
              <label className="inventory-form-field inventory-form-grid-full">
                Note
                <textarea value={orderForm.notes} onChange={event => setOrderForm(prev => ({ ...prev, notes: event.target.value }))} />
              </label>

              <div className="inventory-line-items inventory-form-grid-full">
                <div className="inventory-line-items-head">
                  <strong>Righe ordine</strong>
                  <button type="button" className="inventory-secondary-btn" onClick={addOrderLine}>+ Riga</button>
                </div>

                {orderForm.lines.map((line, index) => (
                  <div key={`line-${index}`} className="inventory-line-item-row">
                    <label className="inventory-line-item-field">
                      Articolo
                      <select value={line.itemId} onChange={event => updateOrderLine(index, { itemId: event.target.value })} required>
                        <option value="">Seleziona articolo</option>
                        {items.filter(item => item.isActive).map(item => (
                          <option key={item.id} value={item.id}>{item.sku} · {item.name}</option>
                        ))}
                      </select>
                    </label>
                    <label className="inventory-line-item-field">
                      Quantità
                      <input type="number" step="0.001" value={line.quantityOrdered} onChange={event => updateOrderLine(index, { quantityOrdered: event.target.value })} required />
                    </label>
                    <label className="inventory-line-item-field">
                      Costo unitario
                      <input type="number" step="0.0001" value={line.unitCost} onChange={event => updateOrderLine(index, { unitCost: event.target.value })} required />
                    </label>
                    <button type="button" className="inventory-danger-btn inventory-danger-btn--ghost" onClick={() => removeOrderLine(index)} disabled={orderForm.lines.length === 1}>Rimuovi</button>
                  </div>
                ))}
              </div>

              <div className="inventory-form-actions inventory-form-grid-full">
                <button type="button" className="inventory-secondary-btn" onClick={() => setOrderModalOpen(false)}>Annulla</button>
                <button type="submit" className="inventory-primary-btn" disabled={orderSaving}>{orderSaving ? 'Creazione...' : 'Crea ordine'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {receivingOrder && canOperate && (
        <div className="inventory-modal-overlay" onClick={() => setReceivingOrder(null)}>
          <div className="inventory-modal inventory-modal--wide" onClick={event => event.stopPropagation()}>
            <div className="inventory-modal-head">
              <div>
                <h2>Ricezione ordine {receivingOrder.orderNumber}</h2>
                <p>{receivingOrder.supplierName} · {receivingOrder.branchName}</p>
              </div>
              <button className="inventory-link-btn" onClick={() => setReceivingOrder(null)}>Chiudi</button>
            </div>

            <form className="inventory-form-grid" onSubmit={handleReceiptSubmit}>
              <div className="inventory-line-items inventory-form-grid-full">
                {receivingOrder.lines.map(line => {
                  const remaining = line.quantityOrdered - line.quantityReceived
                  const draft = receiptLines.find(receiptLine => receiptLine.purchaseOrderLineId === line.id)
                  if (remaining <= 0 || !draft) return null

                  return (
                    <div key={line.id} className="inventory-receipt-row">
                      <div>
                        <strong>{line.itemSku} · {line.itemName}</strong>
                        <span>Residuo {formatQuantity(remaining)}</span>
                      </div>
                      <label className="inventory-receipt-field">
                        Quantità ricevuta
                        <input type="number" step="0.001" value={draft.quantityReceived} onChange={event => updateReceiptLine(line.id, { quantityReceived: event.target.value })} required />
                      </label>
                      <label className="inventory-receipt-field">
                        Costo unitario
                        <input type="number" step="0.0001" value={draft.unitCost} onChange={event => updateReceiptLine(line.id, { unitCost: event.target.value })} required />
                      </label>
                    </div>
                  )
                })}
              </div>

              <label className="inventory-form-field inventory-form-grid-full">
                Note ricezione
                <textarea value={receiptNotes} onChange={event => setReceiptNotes(event.target.value)} />
              </label>

              <div className="inventory-form-actions inventory-form-grid-full">
                <button type="button" className="inventory-secondary-btn" onClick={() => setReceivingOrder(null)}>Annulla</button>
                <button type="submit" className="inventory-primary-btn" disabled={receiptSaving}>{receiptSaving ? 'Registrazione...' : 'Registra ricezione'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
