import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

interface Employee {
  id: number
  firstName: string
  lastName: string
  fullName: string
  email: string
  phoneNumber: string | null
  badgeCode: string | null
  role: string | null
  shiftsConfiguration: string | null
  isActive: boolean
  createdAt: string
}

interface EmployeesProps {
  user: any
  onLogout: () => void
}

/**
 * Pagina per gestire i dipendenti del merchant (CRUD completo)
 */
function Employees({ user, onLogout }: EmployeesProps) {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    badgeCode: '',
    role: '',
    isActive: true
  })

  useEffect(() => {
    fetchEmployees()
  }, [])

  const fetchEmployees = async () => {
    try {
      const response = await apiClient.get('/employees/my-employees')
      setEmployees(response.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    try {
      if (editingId) {
        await apiClient.put(`/employees/${editingId}`, formData)
      } else {
        await apiClient.post('/employees', formData)
      }
      fetchEmployees()
      setShowForm(false)
      setEditingId(null)
      resetForm()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nel salvataggio')
    }
  }

  const handleEdit = (employee: Employee) => {
    setFormData({
      firstName: employee.firstName,
      lastName: employee.lastName,
      email: employee.email,
      phoneNumber: employee.phoneNumber || '',
      badgeCode: employee.badgeCode || '',
      role: employee.role || '',
      isActive: employee.isActive
    })
    setEditingId(employee.id)
    setShowForm(true)
  }

  const handleDelete = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questo dipendente?')) return

    try {
      await apiClient.delete(`/employees/${id}`)
      fetchEmployees()
    } catch (err: any) {
      alert(err.response?.data?.message || 'Errore nell\'eliminazione')
    }
  }

  const resetForm = () => {
    setFormData({
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      badgeCode: '',
      role: '',
      isActive: true
    })
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Gestione Dipendenti">
      <div className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <button
            onClick={() => { setShowForm(!showForm); setEditingId(null); resetForm(); }}
            className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
          >
            {showForm ? '❌ Annulla' : '➕ Nuovo Dipendente'}
          </button>
        </div>

        {showForm && (
          <div className="glass-card rounded-3xl shadow-lg p-6 mb-6 border border-white/10 animate-scale-in">
            <h2 className="text-2xl font-bold gradient-text mb-6">{editingId ? '✏️ Modifica Dipendente' : '✨ Nuovo Dipendente'}</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Nome *</label>
                  <input
                    type="text"
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="Mario"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Cognome *</label>
                  <input
                    type="text"
                    value={formData.lastName}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="Rossi"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Email *</label>
                  <input
                    type="email"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="mario.rossi@esempio.com"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Telefono</label>
                  <input
                    type="tel"
                    value={formData.phoneNumber}
                    onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="+39 123 456 7890"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Codice Badge</label>
                  <input
                    type="text"
                    value={formData.badgeCode}
                    onChange={(e) => setFormData({ ...formData, badgeCode: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="Es: EMP001"
                  />
                </div>
                <div className="space-y-2">
                  <label className="block text-sm font-bold text-white/90">Ruolo</label>
                  <input
                    type="text"
                    value={formData.role}
                    onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                    className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                    placeholder="Es: Receptionist, Stylist"
                  />
                </div>
              </div>

              <div>
                <label className="flex items-center glass-card-dark p-3 rounded-xl cursor-pointer border border-white/10">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="mr-2"
                  />
                  <span className="text-gray-300">✅ Dipendente attivo</span>
                </label>
              </div>
              <button type="submit" className="bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold">
                💾 Salva
              </button>
            </form>
          </div>
        )}

        {loading ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 animate-scale-in">
            <div className="flex items-center justify-center gap-3 text-neon-cyan">
              <svg className="animate-spin h-8 w-8" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span className="text-xl font-semibold">Caricamento...</span>
            </div>
          </div>
        ) : employees.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            <p className="text-2xl text-gray-400 mb-2">Nessun dipendente ancora creato</p>
            <p className="text-gray-500">Clicca su "Nuovo Dipendente" per iniziare</p>
          </div>
        ) : (
          <div className="grid gap-6">
            {employees.map((employee, index) => (
              <div key={employee.id} className="glass-card rounded-3xl shadow-lg p-6 border border-white/10 hover:border-neon-green/50 transition-all hover:shadow-glow-green animate-slide-up" style={{ animationDelay: `${index * 0.1}s` }}>
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-3">
                      <h3 className="text-2xl font-bold gradient-text">{employee.fullName}</h3>
                      <span className={`px-4 py-1.5 rounded-xl text-xs font-semibold border ${employee.isActive ? 'bg-neon-green/20 text-neon-green border-neon-green/30' : 'bg-gray-500/20 text-gray-400 border-gray-500/30'}`}>
                        {employee.isActive ? '✅ Attivo' : '⏸️ Non attivo'}
                      </span>
                    </div>
                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div className="glass-card-dark p-3 rounded-xl border border-neon-cyan/20">
                        <span className="text-gray-500 block mb-1">📧 Email:</span>
                        <span className="text-neon-cyan font-semibold">{employee.email}</span>
                      </div>
                      {employee.phoneNumber && (
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-blue/20">
                          <span className="text-gray-500 block mb-1">📱 Telefono:</span>
                          <span className="text-neon-blue font-semibold">{employee.phoneNumber}</span>
                        </div>
                      )}
                      {employee.badgeCode && (
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-purple/20">
                          <span className="text-gray-500 block mb-1">🎫 Badge:</span>
                          <span className="text-neon-purple font-semibold font-mono">{employee.badgeCode}</span>
                        </div>
                      )}
                      {employee.role && (
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-green/20">
                          <span className="text-gray-500 block mb-1">💼 Ruolo:</span>
                          <span className="text-neon-green font-semibold">{employee.role}</span>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
                <div className="mt-6 pt-6 border-t border-white/10 flex gap-3">
                  <button
                    onClick={() => handleEdit(employee)}
                    className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 hover:text-neon-blue border border-white/10 transform hover:scale-105"
                  >
                    ✏️ Modifica
                  </button>
                  <button
                    onClick={() => handleDelete(employee.id)}
                    className="glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105"
                  >
                    🗑️ Elimina
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </AppLayout>
  )
}

export default Employees
