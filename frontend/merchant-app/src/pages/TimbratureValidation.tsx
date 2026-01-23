import { useState, useEffect, useMemo } from 'react';
import axios from '../lib/axios';
import AppLayout from '../components/layout/AppLayout';
import { useToast } from '../components/Toast';

interface ShiftToValidate {
  id: number;
  employeeName: string;
  date: string;
  startTime: string;
  endTime: string;
  checkInTime?: string;
  checkOutTime?: string;
  totalHours: number;
  isCheckedIn: boolean;
  isCheckedOut: boolean;
}

interface TimbratureValidationProps {
  user: any;
  onLogout: () => void;
}

export default function TimbratureValidation({ user, onLogout }: TimbratureValidationProps) {
  const [shiftsToReview, setShiftsToReview] = useState<ShiftToValidate[]>([]);
  const [selectedShifts, setSelectedShifts] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(false);
  const [autoValidating, setAutoValidating] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [selectedShiftDetails, setSelectedShiftDetails] = useState<ShiftToValidate | null>(null);
  const { showToast } = useToast();

  // Imposta date default (settimana corrente)
  useEffect(() => {
    const today = new Date();
    const weekStart = new Date(today);
    weekStart.setDate(today.getDate() - today.getDay() + 1);
    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekStart.getDate() + 6);

    setStartDate(weekStart.toISOString().split('T')[0]);
    setEndDate(weekEnd.toISOString().split('T')[0]);
  }, []);

  useEffect(() => {
    if (startDate && endDate) {
      fetchShiftsToReview();
    }
  }, [startDate, endDate]);

  const fetchShiftsToReview = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/shifts/merchant', {
        params: {
          startDate: new Date(startDate).toISOString(),
          endDate: new Date(endDate + 'T23:59:59').toISOString(),
          ...(user.role === 'Admin' && user.merchantId ? { merchantId: user.merchantId } : {})
        },
      });

      const shiftsNeedingReview = response.data.filter((shift: any) =>
        shift.isCheckedIn && shift.isCheckedOut
      );

      setShiftsToReview(shiftsNeedingReview);
    } catch (error: any) {
      console.error('Errore nel caricamento turni:', error);

      if (error.response?.status === 400 && user.role === 'Admin' && !user.merchantId) {
        console.warn('Admin senza merchantId associato. Nessun turno da visualizzare.');
        setShiftsToReview([]);
      } else {
        const errorMsg = error.response?.data?.message ||
                        error.response?.status === 404
                          ? 'Endpoint non trovato. Assicurati che il backend sia in esecuzione.'
                          : 'Errore nel caricamento turni';
        showToast(errorMsg, 'error');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleAutoValidate = async () => {
    try {
      setAutoValidating(true);
      const response = await axios.post('/timbrature/auto-validate');
      showToast(response.data.message || 'Validazione automatica completata', 'success');
      await fetchShiftsToReview();
    } catch (error: any) {
      showToast(error.response?.data?.message || 'Errore durante la validazione automatica', 'error');
    } finally {
      setAutoValidating(false);
    }
  };

  const handleBatchApprove = async () => {
    if (selectedShifts.size === 0) {
      showToast('Seleziona almeno un turno da approvare', 'warning');
      return;
    }

    try {
      setLoading(true);
      const response = await axios.post('/timbrature/batch-approve', Array.from(selectedShifts));
      showToast(response.data.message || 'Turni approvati con successo', 'success');
      setSelectedShifts(new Set());
      await fetchShiftsToReview();
    } catch (error: any) {
      showToast(error.response?.data?.message || 'Errore durante l\'approvazione', 'error');
    } finally {
      setLoading(false);
    }
  };

  const toggleShiftSelection = (shiftId: number) => {
    const newSelected = new Set(selectedShifts);
    if (newSelected.has(shiftId)) {
      newSelected.delete(shiftId);
    } else {
      newSelected.add(shiftId);
    }
    setSelectedShifts(newSelected);
  };

  const toggleSelectAll = () => {
    if (selectedShifts.size === filteredShifts.length) {
      setSelectedShifts(new Set());
    } else {
      setSelectedShifts(new Set(filteredShifts.map(s => s.id)));
    }
  };

  // Filtro ricerca
  const filteredShifts = useMemo(() => {
    if (!searchQuery) return shiftsToReview;

    return shiftsToReview.filter(shift =>
      shift.employeeName?.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [shiftsToReview, searchQuery]);

  // Statistiche
  const stats = useMemo(() => {
    const totalHours = filteredShifts.reduce((sum, s) => sum + s.totalHours, 0);
    const avgHours = filteredShifts.length > 0 ? totalHours / filteredShifts.length : 0;

    return {
      totalShifts: filteredShifts.length,
      selectedCount: selectedShifts.size,
      totalHours: totalHours.toFixed(1),
      avgHours: avgHours.toFixed(1)
    };
  }, [filteredShifts, selectedShifts]);

  const formatTime = (timeString?: string) => {
    if (!timeString) return '-';
    return new Date(timeString).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('it-IT', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });
  };

  const setCurrentWeek = () => {
    const today = new Date();
    const weekStart = new Date(today);
    weekStart.setDate(today.getDate() - today.getDay() + 1);
    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekStart.getDate() + 6);

    setStartDate(weekStart.toISOString().split('T')[0]);
    setEndDate(weekEnd.toISOString().split('T')[0]);
  };

  const setLastWeek = () => {
    const today = new Date();
    const weekStart = new Date(today);
    weekStart.setDate(today.getDate() - today.getDay() + 1 - 7);
    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekStart.getDate() + 6);

    setStartDate(weekStart.toISOString().split('T')[0]);
    setEndDate(weekEnd.toISOString().split('T')[0]);
  };

  const setCurrentMonth = () => {
    const today = new Date();
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 0);

    setStartDate(monthStart.toISOString().split('T')[0]);
    setEndDate(monthEnd.toISOString().split('T')[0]);
  };

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Validazione Timbrature">
      <div className="container mx-auto px-4 py-8">
        {/* Header Info */}
        <div className="mb-8">
          <h2 className="text-3xl font-bold gradient-text mb-2">Validazione Intelligente</h2>
          <p className="text-gray-400">
            Sistema intelligente con auto-validazione 95% • Approva i turni con 1 click
          </p>
        </div>

        {/* Dashboard Statistiche */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
          <div className="glass-card rounded-3xl p-6 border border-yellow-500/30 hover:border-yellow-500/50 transition-all hover:shadow-glow-yellow animate-slide-up">
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Turni da Rivedere</div>
              <div className="text-2xl">⏳</div>
            </div>
            <div className="text-4xl font-bold text-yellow-400">{stats.totalShifts}</div>
            <div className="text-xs text-gray-500 mt-1">Nel periodo selezionato</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-green/30 hover:border-neon-green/50 transition-all hover:shadow-glow-green animate-slide-up" style={{ animationDelay: '0.1s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Selezionati</div>
              <div className="text-2xl">✓</div>
            </div>
            <div className="text-4xl font-bold text-neon-green">{stats.selectedCount}</div>
            <div className="text-xs text-gray-500 mt-1">Pronti per approvazione</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-blue/30 hover:border-neon-blue/50 transition-all hover:shadow-glow-blue animate-slide-up" style={{ animationDelay: '0.2s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Ore Totali</div>
              <div className="text-2xl">⏱️</div>
            </div>
            <div className="text-4xl font-bold text-neon-blue">{stats.totalHours}h</div>
            <div className="text-xs text-gray-500 mt-1">Nel periodo</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-purple/30 hover:border-neon-purple/50 transition-all hover:shadow-glow-purple animate-slide-up" style={{ animationDelay: '0.3s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Media Ore/Turno</div>
              <div className="text-2xl">📊</div>
            </div>
            <div className="text-4xl font-bold gradient-text">{stats.avgHours}h</div>
            <div className="text-xs text-gray-500 mt-1">Media periodo</div>
          </div>
        </div>

        {/* Filtri e Azioni */}
        <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10">
          {/* Periodo di ricerca */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
            <div>
              <label className="block text-xs text-gray-400 mb-2 font-semibold">📅 Data Inizio</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full px-4 py-2.5 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all"
              />
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-2 font-semibold">📅 Data Fine</label>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full px-4 py-2.5 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all"
              />
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-2 font-semibold">🔍 Cerca Dipendente</label>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Nome dipendente..."
                className="w-full px-4 py-2.5 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all"
              />
            </div>

            <div className="flex items-end">
              <button
                onClick={() => setSearchQuery('')}
                className="w-full glass-card-dark px-4 py-2.5 rounded-xl hover:border-neon-purple/50 transition-all font-semibold text-gray-300 border border-white/10"
              >
                🔄 Reset Ricerca
              </button>
            </div>
          </div>

          {/* Quick filters */}
          <div className="flex flex-wrap gap-2 mb-4">
            <button
              onClick={setCurrentWeek}
              className="glass-card-dark px-4 py-2 rounded-xl hover:border-neon-cyan/50 transition-all font-semibold text-gray-300 border border-white/10 text-sm"
            >
              📅 Settimana Corrente
            </button>
            <button
              onClick={setLastWeek}
              className="glass-card-dark px-4 py-2 rounded-xl hover:border-neon-cyan/50 transition-all font-semibold text-gray-300 border border-white/10 text-sm"
            >
              📅 Settimana Scorsa
            </button>
            <button
              onClick={setCurrentMonth}
              className="glass-card-dark px-4 py-2 rounded-xl hover:border-neon-cyan/50 transition-all font-semibold text-gray-300 border border-white/10 text-sm"
            >
              📅 Mese Corrente
            </button>
          </div>

          {/* Actions */}
          <div className="flex flex-wrap gap-4">
            <button
              onClick={handleAutoValidate}
              disabled={autoValidating}
              className="bg-gradient-to-r from-neon-blue to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold disabled:opacity-50"
            >
              {autoValidating ? '⏳ Validazione in corso...' : '🤖 Auto-Valida Turni (±15min)'}
            </button>

            {selectedShifts.size > 0 && (
              <button
                onClick={handleBatchApprove}
                disabled={loading}
                className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-6 py-3 rounded-xl hover:shadow-glow-green transition-all transform hover:scale-105 font-semibold disabled:opacity-50"
              >
                ✓ Approva Selezionati ({selectedShifts.size})
              </button>
            )}

            <button
              onClick={fetchShiftsToReview}
              disabled={loading}
              className="glass-card-dark px-6 py-3 rounded-xl hover:border-neon-cyan/50 transition-all font-semibold text-gray-300 border border-white/10 disabled:opacity-50"
            >
              🔄 Aggiorna
            </button>
          </div>

          <div className="glass-card-dark p-4 rounded-xl border border-neon-blue/30 mt-4">
            <div className="flex items-start gap-3">
              <div className="text-2xl">💡</div>
              <div className="text-sm text-gray-300">
                <strong className="text-neon-blue">Sistema intelligente:</strong> I turni entro ±15 minuti dall'orario previsto vengono auto-approvati.
                Qui vedi solo i turni che richiedono la tua attenzione.
              </div>
            </div>
          </div>
        </div>

        {/* Shifts Table */}
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
        ) : filteredShifts.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-green animate-fade-in">
            <div className="text-6xl mb-4">✨</div>
            <div className="text-2xl font-bold gradient-text mb-2">
              {searchQuery ? 'Nessun dipendente trovato' : 'Tutto approvato!'}
            </div>
            <div className="text-gray-400">
              {searchQuery ? 'Prova con un altro nome' : 'Non ci sono turni che richiedono la tua revisione.'}
            </div>
          </div>
        ) : (
          <div className="glass-card rounded-3xl overflow-hidden border border-white/10">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="glass-card-dark border-b border-white/10">
                  <tr>
                    <th className="p-4 text-left">
                      <input
                        type="checkbox"
                        checked={selectedShifts.size === filteredShifts.length && filteredShifts.length > 0}
                        onChange={toggleSelectAll}
                        className="w-5 h-5 text-neon-cyan rounded"
                      />
                    </th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Dipendente</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Data</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Previsto</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Effettivo</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Ore</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Azioni</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/10">
                  {filteredShifts.map((shift) => (
                    <tr
                      key={shift.id}
                      className={`hover:bg-white/5 transition-colors ${selectedShifts.has(shift.id) ? 'bg-neon-cyan/10' : ''}`}
                    >
                      <td className="p-4">
                        <input
                          type="checkbox"
                          checked={selectedShifts.has(shift.id)}
                          onChange={() => toggleShiftSelection(shift.id)}
                          className="w-5 h-5 text-neon-cyan rounded"
                        />
                      </td>
                      <td className="p-4 font-medium text-white">{shift.employeeName || 'Non assegnato'}</td>
                      <td className="p-4 text-gray-300">{formatDate(shift.date)}</td>
                      <td className="p-4 text-gray-300">
                        <div className="text-sm">
                          {shift.startTime} - {shift.endTime}
                        </div>
                      </td>
                      <td className="p-4">
                        <div className="text-sm text-gray-300">
                          {formatTime(shift.checkInTime)} - {formatTime(shift.checkOutTime)}
                        </div>
                      </td>
                      <td className="p-4">
                        <span className="inline-block bg-neon-blue/20 text-neon-blue px-3 py-1 rounded-xl text-sm font-semibold border border-neon-blue/30">
                          {shift.totalHours.toFixed(1)}h
                        </span>
                      </td>
                      <td className="p-4">
                        <button
                          onClick={() => setSelectedShiftDetails(shift)}
                          className="text-neon-cyan hover:text-neon-blue transition-colors font-semibold text-sm"
                        >
                          👁️ Dettagli
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Shift Details Modal */}
        {selectedShiftDetails && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 backdrop-blur-sm">
            <div className="glass-card rounded-3xl max-w-lg w-full p-6 border border-neon-cyan/30 animate-scale-in">
              <h3 className="text-2xl font-bold gradient-text mb-6">
                📋 Dettagli Turno
              </h3>

              <div className="space-y-4">
                <div className="glass-card-dark p-4 rounded-xl border border-white/10">
                  <div className="text-xs text-gray-400 mb-1">👤 Dipendente</div>
                  <div className="text-lg font-bold text-neon-cyan">{selectedShiftDetails.employeeName}</div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="glass-card-dark p-4 rounded-xl border border-white/10">
                    <div className="text-xs text-gray-400 mb-1">📅 Data</div>
                    <div className="font-semibold text-white">{formatDate(selectedShiftDetails.date)}</div>
                  </div>

                  <div className="glass-card-dark p-4 rounded-xl border border-white/10">
                    <div className="text-xs text-gray-400 mb-1">⏱️ Ore Totali</div>
                    <div className="font-semibold text-neon-blue text-xl">{selectedShiftDetails.totalHours.toFixed(1)}h</div>
                  </div>
                </div>

                <div className="glass-card-dark p-4 rounded-xl border border-neon-purple/20">
                  <div className="text-xs text-gray-400 mb-2">🕐 Orario Previsto</div>
                  <div className="font-semibold text-neon-purple">
                    {selectedShiftDetails.startTime} - {selectedShiftDetails.endTime}
                  </div>
                </div>

                <div className="glass-card-dark p-4 rounded-xl border border-neon-green/20">
                  <div className="text-xs text-gray-400 mb-2">✓ Orario Effettivo</div>
                  <div className="font-semibold text-neon-green">
                    {formatTime(selectedShiftDetails.checkInTime)} - {formatTime(selectedShiftDetails.checkOutTime)}
                  </div>
                </div>
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  onClick={() => setSelectedShiftDetails(null)}
                  className="flex-1 glass-card-dark px-4 py-3 rounded-xl hover:border-white/20 transition-all font-semibold text-gray-300 border border-white/10"
                >
                  Chiudi
                </button>
                <button
                  onClick={() => {
                    toggleShiftSelection(selectedShiftDetails.id);
                    setSelectedShiftDetails(null);
                  }}
                  className={`flex-1 px-4 py-3 rounded-xl transition-all transform hover:scale-105 font-semibold ${
                    selectedShifts.has(selectedShiftDetails.id)
                      ? 'bg-gradient-to-r from-red-500 to-neon-pink text-white hover:shadow-glow-pink'
                      : 'bg-gradient-to-r from-neon-green to-neon-cyan text-white hover:shadow-glow-cyan'
                  }`}
                >
                  {selectedShifts.has(selectedShiftDetails.id) ? '✕ Deseleziona' : '✓ Seleziona'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
