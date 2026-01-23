import { useState, useEffect, useMemo } from 'react';
import axios from '../lib/axios';
import {
  EmployeeLeaveBalance,
  UpsertEmployeeLeaveBalanceRequest,
  LeaveType
} from '../types/leave';
import AppLayout from '../components/layout/AppLayout';
import { useToast } from '../components/Toast';

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
  const [searchQuery, setSearchQuery] = useState('');
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null);
  const { showToast } = useToast();

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
      showToast(error.response?.data?.message || 'Errore nel caricamento dei saldi', 'error');
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
      showToast('Saldo salvato con successo!', 'success');
      setShowModal(false);
      setEditingBalance(null);
      resetForm();
      fetchBalances();
    } catch (error: any) {
      console.error('Errore nel salvataggio:', error);
      showToast(error.response?.data?.message || 'Errore nel salvataggio del saldo', 'error');
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
    try {
      await axios.delete(`/leaverequests/balances/${balanceId}`);
      showToast('Saldo eliminato', 'success');
      setDeleteConfirm(null);
      fetchBalances();
    } catch (error: any) {
      console.error('Errore nell\'eliminazione:', error);
      showToast(error.response?.data?.message || 'Errore nell\'eliminazione del saldo', 'error');
    }
  };

  const resetForm = () => {
    setFormData({
      employeeId: 0,
      leaveType: LeaveType.Ferie,
      year: selectedYear,
      totalDays: 0,
      notes: ''
    });
  };

  // Raggruppa i saldi per dipendente
  const balancesByEmployee = useMemo(() => {
    return balances.reduce((acc, balance) => {
      if (!acc[balance.employeeId]) {
        acc[balance.employeeId] = {
          employeeName: balance.employeeName,
          balances: []
        };
      }
      acc[balance.employeeId].balances.push(balance);
      return acc;
    }, {} as Record<number, { employeeName: string; balances: EmployeeLeaveBalance[] }>);
  }, [balances]);

  // Filtro ricerca
  const filteredBalances = useMemo(() => {
    if (!searchQuery) return balancesByEmployee;

    return Object.entries(balancesByEmployee).reduce((acc, [employeeId, data]) => {
      if (data.employeeName.toLowerCase().includes(searchQuery.toLowerCase())) {
        acc[Number(employeeId)] = data;
      }
      return acc;
    }, {} as typeof balancesByEmployee);
  }, [balancesByEmployee, searchQuery]);

  // Statistiche aggregate
  const stats = useMemo(() => {
    const totalEmployees = Object.keys(balancesByEmployee).length;
    const totalBalances = balances.length;
    const totalDaysAvailable = balances.reduce((sum, b) => sum + b.remainingDays, 0);
    const totalDaysUsed = balances.reduce((sum, b) => sum + b.usedDays, 0);
    const totalDaysAllocated = balances.reduce((sum, b) => sum + b.totalDays, 0);

    return {
      totalEmployees,
      totalBalances,
      totalDaysAvailable,
      totalDaysUsed,
      totalDaysAllocated
    };
  }, [balances, balancesByEmployee]);

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Saldi Ferie">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold gradient-text mb-2">Saldi Ferie Dipendenti</h1>
          <p className="text-gray-400">
            Configura i saldi disponibili per i tuoi dipendenti
          </p>
        </div>

        {/* Dashboard Statistiche */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
          <div className="glass-card rounded-3xl p-6 border border-neon-cyan/30 hover:border-neon-cyan/50 transition-all hover:shadow-glow-cyan animate-slide-up">
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Dipendenti</div>
              <div className="text-2xl">👥</div>
            </div>
            <div className="text-4xl font-bold text-neon-cyan">{stats.totalEmployees}</div>
            <div className="text-xs text-gray-500 mt-1">Con saldi configurati</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-green/30 hover:border-neon-green/50 transition-all hover:shadow-glow-green animate-slide-up" style={{ animationDelay: '0.1s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Giorni Disponibili</div>
              <div className="text-2xl">✅</div>
            </div>
            <div className="text-4xl font-bold text-neon-green">{stats.totalDaysAvailable.toFixed(1)}</div>
            <div className="text-xs text-gray-500 mt-1">Totale non utilizzati</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-blue/30 hover:border-neon-blue/50 transition-all hover:shadow-glow-blue animate-slide-up" style={{ animationDelay: '0.2s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Giorni Utilizzati</div>
              <div className="text-2xl">📅</div>
            </div>
            <div className="text-4xl font-bold text-neon-blue">{stats.totalDaysUsed.toFixed(1)}</div>
            <div className="text-xs text-gray-500 mt-1">Di {stats.totalDaysAllocated.toFixed(1)} totali</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-purple/30 hover:border-neon-purple/50 transition-all hover:shadow-glow-purple animate-slide-up" style={{ animationDelay: '0.3s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Tipi Permesso</div>
              <div className="text-2xl">📋</div>
            </div>
            <div className="text-4xl font-bold gradient-text">{stats.totalBalances}</div>
            <div className="text-xs text-gray-500 mt-1">Totale saldi configurati</div>
          </div>
        </div>

        {/* Filtri e Azioni */}
        <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10">
          <div className="flex flex-col md:flex-row gap-4 items-end">
            <div className="flex-1">
              <label className="block text-xs text-gray-400 mb-2 font-semibold">🔍 Cerca Dipendente</label>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Nome dipendente..."
                className="w-full px-4 py-2.5 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all"
              />
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-2 font-semibold">📅 Anno</label>
              <select
                value={selectedYear}
                onChange={(e) => setSelectedYear(Number(e.target.value))}
                className="glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white focus:border-neon-cyan/50 focus:outline-none transition-colors"
              >
                {[2024, 2025, 2026, 2027].map(year => (
                  <option key={year} value={year}>{year}</option>
                ))}
              </select>
            </div>

            <button
              onClick={() => {
                setEditingBalance(null);
                resetForm();
                setShowModal(true);
              }}
              className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-2.5 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold whitespace-nowrap"
            >
              ➕ Aggiungi Saldo
            </button>

            {searchQuery && (
              <button
                onClick={() => setSearchQuery('')}
                className="glass-card-dark px-4 py-2.5 rounded-xl hover:border-neon-purple/50 transition-all font-semibold text-gray-300 border border-white/10 whitespace-nowrap"
              >
                🔄 Reset
              </button>
            )}
          </div>
        </div>

        {/* Balances List */}
        {loading ? (
          <div className="space-y-6">
            {[1, 2, 3].map((i) => (
              <div key={i} className="glass-card rounded-3xl p-6 border border-white/10 animate-pulse">
                <div className="h-8 bg-white/10 rounded w-1/3 mb-6"></div>
                <div className="grid md:grid-cols-4 gap-4">
                  {[1, 2, 3, 4].map((j) => (
                    <div key={j} className="h-32 bg-white/10 rounded-xl"></div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        ) : Object.keys(filteredBalances).length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <p className="text-2xl text-gray-400 mb-4">
              {searchQuery ? 'Nessun dipendente trovato' : `Nessun saldo configurato per l'anno ${selectedYear}`}
            </p>
            {!searchQuery && (
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
            )}
          </div>
        ) : (
          <div className="grid gap-6">
            {Object.entries(filteredBalances).map(([employeeId, { employeeName, balances: empBalances }], index) => (
              <div key={employeeId} className="glass-card rounded-3xl border border-white/10 p-6 hover:border-neon-purple/50 transition-all hover:shadow-glow-purple animate-slide-up" style={{ animationDelay: `${index * 0.05}s` }}>
                <div className="flex items-center justify-between mb-6">
                  <h3 className="text-2xl font-bold gradient-text">{employeeName}</h3>
                  <div className="text-sm text-gray-400">
                    {empBalances.reduce((sum, b) => sum + b.remainingDays, 0).toFixed(1)} giorni disponibili
                  </div>
                </div>
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
                            className="text-neon-blue hover:text-neon-cyan transition-colors text-lg"
                            title="Modifica"
                          >
                            ✎
                          </button>
                          <button
                            onClick={() => setDeleteConfirm(balance.id)}
                            className="text-red-400 hover:text-red-300 transition-colors text-lg"
                            title="Elimina"
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

        {/* Delete Confirmation Modal */}
        {deleteConfirm !== null && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 backdrop-blur-sm">
            <div className="glass-card rounded-3xl max-w-md w-full p-6 border border-red-500/30 animate-scale-in">
              <h3 className="text-2xl font-bold text-red-400 mb-4">
                ⚠️ Conferma Eliminazione
              </h3>
              <p className="text-gray-300 mb-6">
                Sei sicuro di voler eliminare questo saldo? Questa azione non può essere annullata.
              </p>
              <div className="flex gap-3">
                <button
                  onClick={() => setDeleteConfirm(null)}
                  className="flex-1 glass-card-dark px-4 py-3 rounded-xl hover:border-white/20 transition-all font-semibold text-gray-300 border border-white/10"
                >
                  Annulla
                </button>
                <button
                  onClick={() => handleDelete(deleteConfirm)}
                  className="flex-1 bg-gradient-to-r from-red-500 to-neon-pink text-white px-4 py-3 rounded-xl hover:shadow-glow-pink transition-all transform hover:scale-105 font-semibold"
                >
                  Elimina
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
