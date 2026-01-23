import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import {
  EmployeeLeaveBalance,
  UpsertEmployeeLeaveBalanceRequest,
  LeaveType
} from '../types/leave';
import AppLayout from '../components/layout/AppLayout';

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
    <AppLayout user={user} onLogout={onLogout} pageTitle="Saldi Ferie">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold gradient-text mb-2">Saldi Ferie Dipendenti</h1>
            <p className="text-gray-400">
              Configura i saldi disponibili per i tuoi dipendenti
            </p>
          </div>
          <div className="flex gap-3">
            <select
              value={selectedYear}
              onChange={(e) => setSelectedYear(Number(e.target.value))}
              className="glass-card-dark border border-white/10 rounded-xl px-4 py-2 text-white focus:border-neon-cyan/50 focus:outline-none transition-colors"
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
              className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
            >
              ➕ Aggiungi Saldo
            </button>
          </div>
        </div>

        {/* Balances List */}
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
        ) : Object.keys(balancesByEmployee).length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <p className="text-2xl text-gray-400 mb-4">
              Nessun saldo configurato per l'anno {selectedYear}
            </p>
            <button
              onClick={() => {
                setEditingBalance(null);
                resetForm();
                setShowModal(true);
              }}
              className="bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
            >
              ✨ Configura Primo Saldo
            </button>
          </div>
        ) : (
          <div className="grid gap-6">
            {Object.entries(balancesByEmployee).map(([employeeId, { employeeName, balances: empBalances }], index) => (
              <div key={employeeId} className="glass-card rounded-3xl border border-white/10 p-6 hover:border-neon-purple/50 transition-all hover:shadow-glow-purple animate-slide-up" style={{ animationDelay: `${index * 0.1}s` }}>
                <h3 className="text-2xl font-bold gradient-text mb-6">{employeeName}</h3>
                <div className="grid md:grid-cols-4 gap-4">
                  {empBalances.map((balance) => (
                    <div
                      key={balance.id}
                      className="glass-card-dark rounded-xl p-4 border border-white/10 hover:border-neon-cyan/50 transition-all"
                    >
                      <div className="flex items-start justify-between mb-3">
                        <div className="text-sm font-bold text-neon-cyan">
                          {balance.leaveTypeName}
                        </div>
                        <div className="flex gap-2">
                          <button
                            onClick={() => handleEdit(balance)}
                            className="text-neon-blue hover:text-neon-cyan transition-colors"
                          >
                            ✎
                          </button>
                          <button
                            onClick={() => handleDelete(balance.id)}
                            className="text-red-400 hover:text-red-300 transition-colors"
                          >
                            ✕
                          </button>
                        </div>
                      </div>
                      <div className="text-4xl font-bold text-neon-green mb-2">
                        {balance.remainingDays}
                      </div>
                      <div className="text-xs text-gray-400 mb-1">
                        su {balance.totalDays} totali
                      </div>
                      <div className="text-xs text-gray-500">
                        ({balance.usedDays} utilizzati)
                      </div>
                      {balance.notes && (
                        <div className="text-xs text-gray-400 mt-3 italic border-t border-white/5 pt-2">
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
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 backdrop-blur-sm">
            <div className="glass-card rounded-3xl max-w-md w-full p-6 border border-neon-purple/30 animate-scale-in">
              <h3 className="text-2xl font-bold gradient-text mb-6">
                {editingBalance ? '✏️ Modifica Saldo' : '✨ Aggiungi Saldo'}
              </h3>

              <form onSubmit={handleSubmit}>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-bold text-white/90 mb-2">
                      Dipendente *
                    </label>
                    <select
                      value={formData.employeeId}
                      onChange={(e) => setFormData({ ...formData, employeeId: Number(e.target.value) })}
                      className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
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
                    <label className="block text-sm font-bold text-white/90 mb-2">
                      Tipo Permesso *
                    </label>
                    <select
                      value={formData.leaveType}
                      onChange={(e) => setFormData({ ...formData, leaveType: Number(e.target.value) as LeaveType })}
                      className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
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
                    <label className="block text-sm font-bold text-white/90 mb-2">
                      Anno *
                    </label>
                    <select
                      value={formData.year}
                      onChange={(e) => setFormData({ ...formData, year: Number(e.target.value) })}
                      className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                      required
                      disabled={!!editingBalance}
                    >
                      {[2024, 2025, 2026, 2027].map(year => (
                        <option key={year} value={year}>{year}</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-bold text-white/90 mb-2">
                      Giorni Totali *
                    </label>
                    <input
                      type="number"
                      min="0"
                      step="0.5"
                      value={formData.totalDays}
                      onChange={(e) => setFormData({ ...formData, totalDays: Number(e.target.value) })}
                      className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
                      required
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-bold text-white/90 mb-2">
                      Note (opzionale)
                    </label>
                    <textarea
                      value={formData.notes}
                      onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                      className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
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
                    className="flex-1 glass-card-dark px-4 py-3 rounded-xl hover:border-white/20 transition-all font-semibold text-gray-300 border border-white/10"
                  >
                    Annulla
                  </button>
                  <button
                    type="submit"
                    className="flex-1 bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-4 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
                  >
                    💾 Salva
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
