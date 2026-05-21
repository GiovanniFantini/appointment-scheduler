import apiClient from '../axios'

// L'app Merchant è un configuratore: il modulo Magazzino lato Merchant espone
// solo i report. Tutta l'operatività (articoli, movimenti, fornitori, ordini)
// vive sull'app Employee, sotto /api/employee/inventory.

export interface InventoryDashboard {
  totalItems: number
  activeSuppliers: number
  openPurchaseOrders: number
  totalStockValue: number
  lowStockItems: number
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

export const inventoryApi = {
  async getDashboard(branchId?: number | null): Promise<InventoryDashboard> {
    const res = await apiClient.get<InventoryDashboard>('/merchant/inventory/reports/dashboard', {
      params: { branchId: branchId ?? undefined },
    })
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
