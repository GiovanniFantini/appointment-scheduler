import apiClient from '../axios'

export interface Department {
  id: number
  branchId: number
  merchantId: number
  name: string
  color: string
  isActive: boolean
  createdAt: string
}

export interface Branch {
  id: number
  merchantId: number
  name: string
  code?: string | null
  address?: string | null
  city?: string | null
  postalCode?: string | null
  country?: string | null
  phone?: string | null
  isHeadquarters: boolean
  isActive: boolean
  createdAt: string
  departments: Department[]
  employeeCount: number
}

export interface CreateBranchRequest {
  name: string
  code?: string | null
  address?: string | null
  city?: string | null
  postalCode?: string | null
  country?: string | null
  phone?: string | null
}

export interface UpdateBranchRequest extends CreateBranchRequest {
  isActive: boolean
}

export interface CreateDepartmentRequest {
  name: string
  color: string
}

export interface UpdateDepartmentRequest extends CreateDepartmentRequest {
  isActive: boolean
}

export const branchesApi = {
  async list(includeInactive = true): Promise<Branch[]> {
    const res = await apiClient.get<Branch[]>('/branches', { params: { includeInactive } })
    return res.data
  },

  async getById(id: number): Promise<Branch> {
    const res = await apiClient.get<Branch>(`/branches/${id}`)
    return res.data
  },

  async create(payload: CreateBranchRequest): Promise<Branch> {
    const res = await apiClient.post<Branch>('/branches', payload)
    return res.data
  },

  async update(id: number, payload: UpdateBranchRequest): Promise<Branch> {
    const res = await apiClient.put<Branch>(`/branches/${id}`, payload)
    return res.data
  },

  async remove(id: number): Promise<void> {
    await apiClient.delete(`/branches/${id}`)
  },

  async setHeadquarters(id: number): Promise<void> {
    await apiClient.post(`/branches/${id}/set-headquarters`)
  },

  async createDepartment(branchId: number, payload: CreateDepartmentRequest): Promise<Department> {
    const res = await apiClient.post<Department>(`/branches/${branchId}/departments`, payload)
    return res.data
  },

  async updateDepartment(departmentId: number, payload: UpdateDepartmentRequest): Promise<Department> {
    const res = await apiClient.put<Department>(`/branches/departments/${departmentId}`, payload)
    return res.data
  },

  async removeDepartment(departmentId: number): Promise<void> {
    await apiClient.delete(`/branches/departments/${departmentId}`)
  },
}
