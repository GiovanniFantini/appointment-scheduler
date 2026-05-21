import apiClient from '../axios'

export interface InventoryBranchOption {
  id: number
  name: string
  isHomeBranch: boolean
  isActive: boolean
}

export interface InventoryDashboard {
  totalItems: number
  activeSuppliers: number
  openPurchaseOrders: number
  totalStockValue: number
  lowStockItems: number
}

export interface EmployeeInventoryOverview {
  homeBranchId: number
  selectedBranchId?: number | null
  accessibleBranches: InventoryBranchOption[]
  dashboard: InventoryDashboard
}

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

export const inventoryApi = {
  async getOverview(branchId?: number | null): Promise<EmployeeInventoryOverview> {
    const res = await apiClient.get<EmployeeInventoryOverview>('/employee/inventory/overview', {
      params: { branchId: branchId ?? undefined },
    })
    return res.data
  },

  async getItems(branchId?: number | null, search?: string | null): Promise<InventoryItem[]> {
    const res = await apiClient.get<InventoryItem[]>('/employee/inventory/items', {
      params: {
        branchId: branchId ?? undefined,
        search: search?.trim() || undefined,
      },
    })
    return res.data
  },

  async getMovements(branchId?: number | null): Promise<InventoryMovement[]> {
    const res = await apiClient.get<InventoryMovement[]>('/employee/inventory/movements', {
      params: { branchId: branchId ?? undefined },
    })
    return res.data
  },

  async getLowStock(branchId?: number | null): Promise<LowStockRow[]> {
    const res = await apiClient.get<LowStockRow[]>('/employee/inventory/low-stock', {
      params: { branchId: branchId ?? undefined },
    })
    return res.data
  },
}