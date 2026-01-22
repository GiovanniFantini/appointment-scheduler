import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import {
  EmployeeLeaveBalance,
  UpsertEmployeeLeaveBalanceRequest,
  LeaveType
} from '../types/leave';

interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

interface EmployeeLeaveBalancesProps {
  user: any;
  onLogout: () => void;
}

export default function EmployeeLeaveBalances({ user, onLogout }: EmployeeLeaveBalancesProps) {
  const [balances, setBalances] = useState<EmployeeLeaveBalance[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());
  const [showModal, setShowModal] = useState(false);
  const [editingBalance, setEditingBalance] = useState<EmployeeLeaveBalance | null>(null);

  // Form state
  const [formData, setFormData] = useState<UpsertEmployeeLeaveBalanceRequest>({
    employeeId: 0,
    leaveType: LeaveType.Ferie,
    year: new Date().getFullYear(),
    totalDays: 0,
    notes: ''
  });

  useEffect(() => {
    fetchBalances();
    fetchEmployees();
  }, [selectedYear]);

  const fetchBalances = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/leaverequests/balances', {
        params: { year: selectedYear }
      });
      setBalances(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento saldi:', error);
      alert(error.response?.data?.message || 'Errore nel caricamento dei saldi');
    } finally {
      setLoading(false);
    }
  };

  const fetchEmployees = async () => {
    try {
      const response = await axios.get('/employees/my-employees');
      setEmployees(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento dipendenti:', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await axios.post('/leaverequests/balances', formData);
      alert('Saldo salvato con successo!');
      setShowModal(false);
      setEditingBalance(null);
      resetForm();
      fetchBalances();
    } catch (error: any) {
      console.error('Errore nel salvataggio:', error);
      alert(error.response?.data?.message || 'Errore nel salvataggio del saldo');
    }
  };

  const handleEdit = (balance: EmployeeLeaveBalance) => {
    setEditingBalance(balance);
    setFormData({
      employeeId: balance.employeeId,
      leaveType: balance.leaveType,
      year: balance.year,
      totalDays: balance.totalDays,
      notes: balance.notes || ''
    });
    setShowModal(true);
  };

  const handleDelete = async (balanceId: number) => {
    if (!confirm('Sei sicuro di voler eliminare questo saldo?')) return;

    try {
      await axios.delete(`/leaverequests/balances/${balanceId}`);
      alert('Saldo eliminato');
      fetchBalances();
    } catch (error: any) {
      console.error('Errore nell\'eliminazione:', error);
      alert(error.response?.data?.message || 'Errore nell\'eliminazione del saldo');
    }
  };

  const resetForm = () => {
    setFormData({
      employeeId: 0,
      leaveType: LeaveType.Ferie,
      year: new Date().getFullYear(),
      totalDays: 0,
      notes: ''
    });
  };

  const getLeaveTypeLabel = (type: LeaveType) => {
    switch (type) {
      case LeaveType.Ferie: return 'Ferie';
      case LeaveType.ROL: return 'ROL';
      case LeaveType.PAR: return 'PAR';
      case LeaveType.Malattia: return 'Malattia';
      case LeaveType.ExFestivita: return 'Ex-festività';
      case LeaveType.Welfare: return 'Welfare';
      case LeaveType.Permesso: return 'Permesso';
      case LeaveType.Altro: return 'Altro';
      default: return 'Sconosciuto';
    }
  };

  // Raggruppa i saldi per dipendente
  const balancesByEmployee = balances.reduce((acc, balance) => {
    if (!acc[balance.employeeId]) {
      acc[balance.employeeId] = {
        employeeName: balance.employeeName,
        balances: []
      };
    }
    acc[balance.employeeId].balances.push(balance);
    return acc;
  }, {} as Record<number, { employeeName: string; balances: EmployeeLeaveBalance[] }>);

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Saldi Ferie Dipendenti</h1>
          <p className="text-gray-600 mt-1">
            Configura i saldi disponibili per i tuoi dipendenti
          </p>
        </div>
        <div className="flex gap-3">
          <select
            value={selectedYear}
            onChange={(e) => setSelectedYear(Number(e.target.value))}
            className="border border-gray-300 rounded-lg px-4 py-2"
          >
            {[2024, 2025, 2026, 2027].map(year => (
              <option key={year} value={year}>{year}</option>
            ))}
          </select>
          <button
            onClick={() => {
              setEditingBalance(null);
              resetForm();
              setShowModal(true);
            }}
            className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 transition-colors"
          >
            + Aggiungi Saldo
          </button>
        </div>
      </div>

      {/* Balances List */}
      {loading ? (
        <div className="text-center py-12">
          <div className="text-gray-600">Caricamento...</div>
        </div>
      ) : Object.keys(balancesByEmployee).length === 0 ? (
        <div className="bg-white rounded-lg shadow p-12 text-center">
          <div className="text-gray-500 mb-4">
            Nessun saldo configurato per l'anno {selectedYear}
          </div>
          <button
            onClick={() => {
              setEditingBalance(null);
              resetForm();
              setShowModal(true);
            }}
            className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 transition-colors"
          >
            Configura Primo Saldo
          </button>
        </div>
      ) : (
        <div className="grid gap-6">
          {Object.entries(balancesByEmployee).map(([employeeId, { employeeName, balances: empBalances }]) => (
            <div key={employeeId} className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
              <h3 className="text-lg font-bold text-gray-900 mb-4">{employeeName}</h3>
              <div className="grid md:grid-cols-4 gap-4">
                {empBalances.map((balance) => (
                  <div
                    key={balance.id}
                    className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                  >
                    <div className="flex items-start justify-between mb-2">
                      <div className="text-sm font-medium text-gray-600">
                        {balance.leaveTypeName}
                      </div>
                      <div className="flex gap-1">
                        <button
                          onClick={() => handleEdit(balance)}
                          className="text-blue-600 hover:text-blue-700 text-sm"
                        >
                          ✎
                        </button>
                        <button
                          onClick={() => handleDelete(balance.id)}
                          className="text-red-600 hover:text-red-700 text-sm"
                        >
                          ✕
                        </button>
                      </div>
                    </div>
                    <div className="text-3xl font-bold text-blue-600 mb-1">
                      {balance.remainingDays}
                    </div>
                    <div className="text-xs text-gray-500">
                      su {balance.totalDays} totali
                    </div>
                    <div className="text-xs text-gray-500">
                      ({balance.usedDays} utilizzati)
                    </div>
                    {balance.notes && (
                      <div className="text-xs text-gray-400 mt-2 italic">
                        {balance.notes}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Add/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-xl max-w-md w-full p-6">
            <h3 className="text-xl font-bold text-gray-900 mb-4">
              {editingBalance ? 'Modifica Saldo' : 'Aggiungi Saldo'}
            </h3>

            <form onSubmit={handleSubmit}>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Dipendente
                  </label>
                  <select
                    value={formData.employeeId}
                    onChange={(e) => setFormData({ ...formData, employeeId: Number(e.target.value) })}
                    className="w-full border border-gray-300 rounded-lg px-4 py-2"
                    required
                    disabled={!!editingBalance}
                  >
                    <option value={0}>Seleziona dipendente...</option>
                    {employees.map((emp) => (
                      <option key={emp.id} value={emp.id}>
                        {emp.firstName} {emp.lastName}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Tipo Permesso
                  </label>
                  <select
                    value={formData.leaveType}
                    onChange={(e) => setFormData({ ...formData, leaveType: Number(e.target.value) as LeaveType })}
                    className="w-full border border-gray-300 rounded-lg px-4 py-2"
                    required
                    disabled={!!editingBalance}
                  >
                    <option value={LeaveType.Ferie}>Ferie</option>
                    <option value={LeaveType.ROL}>ROL</option>
                    <option value={LeaveType.PAR}>PAR</option>
                    <option value={LeaveType.ExFestivita}>Ex-festività</option>
                    <option value={LeaveType.Welfare}>Welfare</option>
                    <option value={LeaveType.Permesso}>Permesso</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Anno
                  </label>
                  <select
                    value={formData.year}
                    onChange={(e) => setFormData({ ...formData, year: Number(e.target.value) })}
                    className="w-full border border-gray-300 rounded-lg px-4 py-2"
                    required
                    disabled={!!editingBalance}
                  >
                    {[2024, 2025, 2026, 2027].map(year => (
                      <option key={year} value={year}>{year}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Giorni Totali
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="0.5"
                    value={formData.totalDays}
                    onChange={(e) => setFormData({ ...formData, totalDays: Number(e.target.value) })}
                    className="w-full border border-gray-300 rounded-lg px-4 py-2"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Note (opzionale)
                  </label>
                  <textarea
                    value={formData.notes}
                    onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-4 py-2"
                    rows={2}
                    placeholder="es: Include 2 giorni dall'anno precedente"
                  />
                </div>
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  type="button"
                  onClick={() => {
                    setShowModal(false);
                    setEditingBalance(null);
                    resetForm();
                  }}
                  className="flex-1 bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300 transition-colors"
                >
                  Annulla
                </button>
                <button
                  type="submit"
                  className="flex-1 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Salva
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
