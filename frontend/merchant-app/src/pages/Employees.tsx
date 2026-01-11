import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'

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
  onLogout: () => void
}

/**
 * Pagina per gestire i dipendenti del merchant (CRUD completo)
 */
function Employees({ onLogout }: EmployeesProps) {
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
    shiftsConfiguration: '',
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
      shiftsConfiguration: employee.shiftsConfiguration || '',
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
      shiftsConfiguration: '',
      isActive: true
    })
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-md">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-gray-800">Gestione Dipendenti</h1>
          <div className="flex items-center gap-4">
            <Link to="/" className="text-gray-600 hover:text-gray-800">Dashboard</Link>
            <button onClick={onLogout} className="bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300">
              Esci
            </button>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <button
            onClick={() => { setShowForm(!showForm); setEditingId(null); resetForm(); }}
            className="bg-green-600 text-white px-6 py-3 rounded-lg hover:bg-green-700 font-semibold"
          >
            {showForm ? 'Annulla' : '+ Nuovo Dipendente'}
          </button>
        </div>

        {showForm && (
          <div className="bg-white rounded-lg shadow-lg p-6 mb-6">
            <h2 className="text-xl font-bold mb-4">{editingId ? 'Modifica Dipendente' : 'Nuovo Dipendente'}</h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-bold mb-2">Nome *</label>
                  <input
                    type="text"
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Cognome *</label>
                  <input
                    type="text"
                    value={formData.lastName}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-bold mb-2">Email *</label>
                  <input
                    type="email"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Telefono</label>
                  <input
                    type="tel"
                    value={formData.phoneNumber}
                    onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-bold mb-2">Codice Badge</label>
                  <input
                    type="text"
                    value={formData.badgeCode}
                    onChange={(e) => setFormData({ ...formData, badgeCode: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder="Es: EMP001"
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold mb-2">Ruolo</label>
                  <input
                    type="text"
                    value={formData.role}
                    onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                    className="w-full px-3 py-2 border rounded-lg"
                    placeholder="Es: Receptionist, Stylist"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-bold mb-2">Configurazione Turni (JSON)</label>
                <textarea
                  value={formData.shiftsConfiguration}
                  onChange={(e) => setFormData({ ...formData, shiftsConfiguration: e.target.value })}
                  className="w-full px-3 py-2 border rounded-lg font-mono text-sm"
                  rows={5}
                  placeholder='{"shifts": [{"day": "Monday", "start": "09:00", "end": "17:00"}]}'
                />
                <p className="text-xs text-gray-600 mt-1">
                  Formato JSON per configurare i turni del dipendente
                </p>
              </div>

              <div>
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="mr-2"
                  />
                  Dipendente attivo
                </label>
              </div>
              <button type="submit" className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700">
                Salva
              </button>
            </form>
          </div>
        )}

        {loading ? (
          <div className="text-center py-12">Caricamento...</div>
        ) : employees.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-12 text-center">
            <p className="text-xl text-gray-600">Nessun dipendente ancora creato</p>
            <p className="text-gray-500 mt-2">Clicca su "Nuovo Dipendente" per iniziare</p>
          </div>
        ) : (
          <div className="grid gap-6">
            {employees.map((employee) => (
              <div key={employee.id} className="bg-white rounded-lg shadow-lg p-6">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <div className="flex items-center gap-3">
                      <h3 className="text-xl font-bold">{employee.fullName}</h3>
                      <span className={`px-3 py-1 rounded-full text-xs font-semibold ${employee.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                        {employee.isActive ? 'Attivo' : 'Non attivo'}
                      </span>
                    </div>
                    <div className="mt-3 grid grid-cols-2 gap-4 text-sm">
                      <div>
                        <span className="text-gray-500">Email:</span>
                        <span className="ml-2 text-gray-700">{employee.email}</span>
                      </div>
                      {employee.phoneNumber && (
                        <div>
                          <span className="text-gray-500">Telefono:</span>
                          <span className="ml-2 text-gray-700">{employee.phoneNumber}</span>
                        </div>
                      )}
                      {employee.badgeCode && (
                        <div>
                          <span className="text-gray-500">Badge:</span>
                          <span className="ml-2 font-mono text-gray-700">{employee.badgeCode}</span>
                        </div>
                      )}
                      {employee.role && (
                        <div>
                          <span className="text-gray-500">Ruolo:</span>
                          <span className="ml-2 text-gray-700">{employee.role}</span>
                        </div>
                      )}
                    </div>
                    {employee.shiftsConfiguration && (
                      <div className="mt-3">
                        <details className="bg-gray-50 rounded p-3">
                          <summary className="cursor-pointer text-sm font-semibold text-gray-700">
                            Visualizza Turni
                          </summary>
                          <pre className="mt-2 text-xs font-mono text-gray-600 overflow-x-auto">
                            {JSON.stringify(JSON.parse(employee.shiftsConfiguration), null, 2)}
                          </pre>
                        </details>
                      </div>
                    )}
                  </div>
                </div>
                <div className="mt-4 flex gap-2">
                  <button
                    onClick={() => handleEdit(employee)}
                    className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
                  >
                    Modifica
                  </button>
                  <button
                    onClick={() => handleDelete(employee.id)}
                    className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700"
                  >
                    Elimina
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default Employees
