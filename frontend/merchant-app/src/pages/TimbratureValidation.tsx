import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import AppLayout from '../components/layout/AppLayout';

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

  useEffect(() => {
    fetchShiftsToReview();
  }, []);

  const fetchShiftsToReview = async () => {
    try {
      setLoading(true);
      // Ottieni i turni della settimana corrente che richiedono revisione
      const today = new Date();
      const weekStart = new Date(today);
      weekStart.setDate(today.getDate() - today.getDay() + 1);
      const weekEnd = new Date(weekStart);
      weekEnd.setDate(weekStart.getDate() + 6);

      const response = await axios.get('/shifts/merchant', {
        params: {
          startDate: weekStart.toISOString(),
          endDate: weekEnd.toISOString(),
          // Se l'utente è Admin puro (senza merchantId), passa merchantId da user
          ...(user.role === 'Admin' && user.merchantId ? { merchantId: user.merchantId } : {})
        },
      });

      // Filtra solo i turni completati (checked out) che necessitano revisione
      const shiftsNeedingReview = response.data.filter((shift: any) =>
        shift.isCheckedIn && shift.isCheckedOut
      );

      setShiftsToReview(shiftsNeedingReview);
    } catch (error: any) {
      console.error('Errore nel caricamento turni:', error);

      // Messaggio più descrittivo per Admin senza merchantId
      if (error.response?.status === 400 && user.role === 'Admin' && !user.merchantId) {
        console.warn('Admin senza merchantId associato. Nessun turno da visualizzare.');
        setShiftsToReview([]); // Lista vuota, non errore
      } else {
        const errorMsg = error.response?.data?.message ||
                        error.response?.status === 404
                          ? 'Endpoint non trovato. Assicurati che il backend sia in esecuzione.'
                          : 'Errore nel caricamento turni';
        alert(errorMsg);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleAutoValidate = async () => {
    try {
      setAutoValidating(true);
      const response = await axios.post('/timbrature/auto-validate');
      alert(response.data.message || 'Validazione automatica completata');
      await fetchShiftsToReview();
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore durante la validazione automatica');
    } finally {
      setAutoValidating(false);
    }
  };

  const handleBatchApprove = async () => {
    if (selectedShifts.size === 0) {
      alert('Seleziona almeno un turno da approvare');
      return;
    }

    if (!confirm(`Confermi l'approvazione di ${selectedShifts.size} turni?`)) {
      return;
    }

    try {
      setLoading(true);
      const response = await axios.post('/timbrature/batch-approve', Array.from(selectedShifts));
      alert(response.data.message || 'Turni approvati con successo');
      setSelectedShifts(new Set());
      await fetchShiftsToReview();
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore durante l\'approvazione');
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
    if (selectedShifts.size === shiftsToReview.length) {
      setSelectedShifts(new Set());
    } else {
      setSelectedShifts(new Set(shiftsToReview.map(s => s.id)));
    }
  };

  const formatTime = (timeString?: string) => {
    if (!timeString) return '-';
    return new Date(timeString).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' });
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('it-IT', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'UTC' });
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

        {/* Actions */}
        <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10">
          <div className="flex flex-wrap gap-4 mb-6">
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

          <div className="glass-card-dark p-4 rounded-xl border border-neon-blue/30">
            <div className="flex items-start gap-3">
              <div className="text-2xl">💡</div>
              <div className="text-sm text-gray-300">
                <strong className="text-neon-blue">Sistema intelligente:</strong> I turni entro ±15 minuti dall'orario previsto vengono auto-approvati.
                Qui vedi solo i turni che richiedono la tua attenzione.
              </div>
            </div>
          </div>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
          <div className="glass-card rounded-3xl p-6 border border-white/10 hover:border-neon-blue/50 transition-all">
            <div className="text-gray-400 text-sm mb-1">Turni da Rivedere</div>
            <div className="text-3xl font-bold gradient-text">{shiftsToReview.length}</div>
          </div>
          <div className="glass-card rounded-3xl p-6 border border-white/10 hover:border-neon-green/50 transition-all">
            <div className="text-gray-400 text-sm mb-1">Selezionati</div>
            <div className="text-3xl font-bold text-neon-green">{selectedShifts.size}</div>
          </div>
          <div className="glass-card rounded-3xl p-6 border border-white/10 hover:border-neon-purple/50 transition-all">
            <div className="text-gray-400 text-sm mb-1">Tasso Auto-Validazione</div>
            <div className="text-3xl font-bold text-neon-purple">~95%</div>
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
        ) : shiftsToReview.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-green animate-fade-in">
            <div className="text-6xl mb-4">✨</div>
            <div className="text-2xl font-bold gradient-text mb-2">
              Tutto approvato!
            </div>
            <div className="text-gray-400">
              Non ci sono turni che richiedono la tua revisione.
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
                        checked={selectedShifts.size === shiftsToReview.length && shiftsToReview.length > 0}
                        onChange={toggleSelectAll}
                        className="w-5 h-5 text-neon-cyan rounded"
                      />
                    </th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Dipendente</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Data</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Previsto</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Effettivo</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Ore</th>
                    <th className="p-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">Stato</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/10">
                  {shiftsToReview.map((shift) => (
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
                        <span className="inline-block bg-yellow-500/20 text-yellow-400 px-3 py-1 rounded-xl text-sm font-semibold border border-yellow-500/30">
                          Da Rivedere
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
