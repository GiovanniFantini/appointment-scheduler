import apiClient from '../axios'

export interface InventoryStockBalance {
  id: number
  branchId: number
  branchName: string
  itemId: number
  quantityOnHand: number
  weightedAverageCost: number
  inventoryValue: number
  updatedAt: string
}

export interface InventoryMovement {
  id: number
  branchId: number
  branchName: string
  itemId: number
  type: number
  quantityDelta: number
  unitCost: number
  totalValue: number
  reason?: string | null
  referenceNumber?: string | null
  createdAt: string
}

export interface InventoryItem {
  id: number
  merchantId: number
  sku: string
  name: string
  barcode?: string | null
  description?: string | null
  unitOfMeasure: string
  reorderPoint: number
  averageUnitCost: number
  valuationMethod: number
  isActive: boolean
  createdAt: string
  totalQuantityOnHand: number
  totalInventoryValue: number
  balances: InventoryStockBalance[]
  recentMovements: InventoryMovement[]
}

export interface InventoryDashboard {
  totalItems: number
  activeSuppliers: number
  openPurchaseOrders: number
  totalStockValue: number
  lowStockItems: number
}

export interface Supplier {
  id: number
  merchantId: number
  name: string
  contactName?: string | null
  email?: string | null
  phone?: string | null
  vatNumber?: string | null
  notes?: string | null
  isActive: boolean
  createdAt: string
  updatedAt?: string | null
}

export interface PurchaseOrderLine {
  id: number
  itemId: number
  itemSku: string
  itemName: string
  quantityOrdered: number
  quantityReceived: number
  unitCost: number
  lineTotal: number
}

export interface GoodsReceiptLine {
  id: number
  itemId: number
  itemSku: string
  itemName: string
  purchaseOrderLineId?: number | null
  quantityReceived: number
  unitCost: number
  lineTotal: number
}

export interface GoodsReceipt {
  id: number
  merchantId: number
  branchId: number
  branchName: string
  supplierId: number
  supplierName: string
  purchaseOrderId?: number | null
  receiptNumber: string
  receivedAt: string
  notes?: string | null
  totalAmount: number
  lines: GoodsReceiptLine[]
}

export interface PurchaseOrder {
  id: number
  merchantId: number
  branchId: number
  branchName: string
  supplierId: number
  supplierName: string
  orderNumber: string
  status: number
  orderedAt: string
  expectedDeliveryDate?: string | null
  sentAt?: string | null
  closedAt?: string | null
  cancelledAt?: string | null
  notes?: string | null
  totalAmount: number
  lines: PurchaseOrderLine[]
  receipts: GoodsReceipt[]
}

export interface InventoryValuationRow {
  itemId: number
  sku: string
  itemName: string
  branchId: number
  branchName: string
  quantityOnHand: number
  weightedAverageCost: number
  inventoryValue: number
}

export interface LowStockRow {
  itemId: number
  sku: string
  itemName: string
  branchId: number
  branchName: string
  quantityOnHand: number
  reorderPoint: number
  suggestedReorderQuantity: number
}

export interface CreateInventoryItemRequest {
  sku: string
  name: string
  barcode?: string
  description?: string
  unitOfMeasure?: string
  reorderPoint: number
}

export interface UpdateInventoryItemRequest extends CreateInventoryItemRequest {
  isActive: boolean
}

export interface CreateInventoryAdjustmentRequest {
  branchId: number
  itemId: number
  quantityDelta: number
  unitCost?: number
  reason: string
}

export interface CreateSupplierRequest {
  name: string
  contactName?: string
  email?: string
  phone?: string
  vatNumber?: string
  notes?: string
}

export interface UpdateSupplierRequest extends CreateSupplierRequest {
  isActive: boolean
}

export interface CreatePurchaseOrderLineRequest {
  itemId: number
  quantityOrdered: number
  unitCost: number
}

export interface CreatePurchaseOrderRequest {
  branchId: number
  supplierId: number
  expectedDeliveryDate?: string
  notes?: string
  lines: CreatePurchaseOrderLineRequest[]
}

export interface CreateGoodsReceiptLineRequest {
  purchaseOrderLineId: number
  quantityReceived: number
  unitCost?: number
}

export interface CreateGoodsReceiptRequest {
  notes?: string
  lines: CreateGoodsReceiptLineRequest[]
}

export const inventoryApi = {
  async getDashboard(branchId?: number | null): Promise<InventoryDashboard> {
    const res = await apiClient.get<InventoryDashboard>('/merchant/inventory/reports/dashboard', {
      params: { branchId: branchId ?? undefined },
    })
    return res.data
  },

  async getItems(branchId?: number | null, search?: string | null): Promise<InventoryItem[]> {
    const res = await apiClient.get<InventoryItem[]>('/merchant/inventory/items', {
      params: {
        branchId: branchId ?? undefined,
        search: search?.trim() || undefined,
      },
    })
    return res.data
  },

  async createItem(payload: CreateInventoryItemRequest): Promise<InventoryItem> {
    const res = await apiClient.post<InventoryItem>('/merchant/inventory/items', payload)
    return res.data
  },

  async updateItem(id: number, payload: UpdateInventoryItemRequest): Promise<InventoryItem> {
    const res = await apiClient.put<InventoryItem>(`/merchant/inventory/items/${id}`, payload)
    return res.data
  },

  async getMovements(branchId?: number | null, itemId?: number | null): Promise<InventoryMovement[]> {
    const res = await apiClient.get<InventoryMovement[]>('/merchant/inventory/movements', {
      params: {
        branchId: branchId ?? undefined,
        itemId: itemId ?? undefined,
      },
    })
    return res.data
  },

  async createAdjustment(payload: CreateInventoryAdjustmentRequest): Promise<InventoryMovement> {
    const res = await apiClient.post<InventoryMovement>('/merchant/inventory/adjustments', payload)
    return res.data
  },

  async getSuppliers(includeInactive = true): Promise<Supplier[]> {
    const res = await apiClient.get<Supplier[]>('/merchant/inventory/suppliers', {
      params: { includeInactive },
    })
    return res.data
  },

  async createSupplier(payload: CreateSupplierRequest): Promise<Supplier> {
    const res = await apiClient.post<Supplier>('/merchant/inventory/suppliers', payload)
    return res.data
  },

  async updateSupplier(id: number, payload: UpdateSupplierRequest): Promise<Supplier> {
    const res = await apiClient.put<Supplier>(`/merchant/inventory/suppliers/${id}`, payload)
    return res.data
  },

  async getOrders(branchId?: number | null, status?: number | null): Promise<PurchaseOrder[]> {
    const res = await apiClient.get<PurchaseOrder[]>('/merchant/inventory/purchase-orders', {
      params: {
        branchId: branchId ?? undefined,
        status: status ?? undefined,
      },
    })
    return res.data
  },

  async createOrder(payload: CreatePurchaseOrderRequest): Promise<PurchaseOrder> {
    const res = await apiClient.post<PurchaseOrder>('/merchant/inventory/purchase-orders', payload)
    return res.data
  },

  async sendOrder(id: number): Promise<PurchaseOrder> {
    const res = await apiClient.post<PurchaseOrder>(`/merchant/inventory/purchase-orders/${id}/send`)
    return res.data
  },

  async cancelOrder(id: number): Promise<PurchaseOrder> {
    const res = await apiClient.post<PurchaseOrder>(`/merchant/inventory/purchase-orders/${id}/cancel`)
    return res.data
  },

  async receiveOrder(id: number, payload: CreateGoodsReceiptRequest): Promise<PurchaseOrder> {
    const res = await apiClient.post<PurchaseOrder>(`/merchant/inventory/purchase-orders/${id}/receive`, payload)
    return res.data
  },

  async getValuation(branchId?: number | null): Promise<InventoryValuationRow[]> {
    const res = await apiClient.get<InventoryValuationRow[]>('/merchant/inventory/reports/valuation', {
      params: { branchId: branchId ?? undefined },
    })
    return res.data
  },

  async getLowStock(branchId?: number | null): Promise<LowStockRow[]> {
    const res = await apiClient.get<LowStockRow[]>('/merchant/inventory/reports/low-stock', {
      params: { branchId: branchId ?? undefined },
    })
    return res.data
  },
}