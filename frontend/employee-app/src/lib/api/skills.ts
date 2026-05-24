import apiClient from '../axios'

export interface Skill {
  id: number
  name: string
  color: string
  isActive: boolean
  employeeCount: number
  createdAt: string
}

export interface CreateSkillRequest {
  name: string
  color: string
  isActive: boolean
}

export type UpdateSkillRequest = CreateSkillRequest

export interface SuggestedEmployee {
  employeeId: number
  fullName: string
  isAvailable: boolean
  unavailableReason: string | null
}

export const skillsApi = {
  async list(): Promise<Skill[]> {
    const res = await apiClient.get<Skill[]>('/employee/skills')
    return res.data
  },

  async getById(id: number): Promise<Skill> {
    const res = await apiClient.get<Skill>(`/employee/skills/${id}`)
    return res.data
  },

  async getEmployees(skillId: number) {
    const res = await apiClient.get(`/employee/skills/${skillId}/employees`)
    return res.data as Array<{
      id: number
      firstName: string
      lastName: string
      email: string
    }>
  },

  async create(payload: CreateSkillRequest): Promise<Skill> {
    const res = await apiClient.post<Skill>('/employee/skills', payload)
    return res.data
  },

  async update(id: number, payload: UpdateSkillRequest): Promise<Skill> {
    const res = await apiClient.put<Skill>(`/employee/skills/${id}`, payload)
    return res.data
  },

  async remove(id: number): Promise<void> {
    await apiClient.delete(`/employee/skills/${id}`)
  },

  async getSuggested(
    skillId: number,
    params: { date: string; startTime?: string; endTime?: string; excludeEventId?: number }
  ): Promise<SuggestedEmployee[]> {
    const res = await apiClient.get<SuggestedEmployee[]>(`/employee/skills/${skillId}/suggested-employees`, {
      params
    })
    return res.data
  }
}
