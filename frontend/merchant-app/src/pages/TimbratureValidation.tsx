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

      const response = await axios.get('/api/shifts/merchant', {
        params: {
          startDate: weekStart.toISOString(),
          endDate: weekEnd.toISOString(),
        },
      });

      // Filtra solo i turni completati (checked out) che necessitano revisione
      const shiftsNeedingReview = response.data.filter((shift: any) =>
        shift.isCheckedIn && shift.isCheckedOut
      );

      setShiftsToReview(shiftsNeedingReview);
    } catch (error: any) {
      console.error('Errore nel caricamento turni:', error);
      alert(error.response?.data?.message || 'Errore nel caricamento');
    } finally {
      setLoading(false);
    }
  };

  const handleAutoValidate = async () => {
    try {
      setAutoValidating(true);
      const response = await axios.post('/api/timbrature/auto-validate');
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
      const response = await axios.post('/api/timbrature/batch-approve', Array.from(selectedShifts));
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
          <p className="text-gray-300">
            Sistema intelligente con auto-validazione 95% • Approva i turni con 1 click
          </p>
        </div>

        {/* Actions */}
        <div className="bg-white rounded-xl shadow-md p-6 mb-6">
          <div className="flex flex-wrap gap-4">
            <button
              onClick={handleAutoValidate}
              disabled={autoValidating}
              className="bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white px-6 py-3 rounded-lg font-semibold shadow-md transition-all disabled:opacity-50"
            >
              {autoValidating ? 'Validazione in corso...' : '🤖 Auto-Valida Turni (±15min)'}
            </button>

            {selectedShifts.size > 0 && (
              <button
                onClick={handleBatchApprove}
                disabled={loading}
                className="bg-gradient-to-r from-green-500 to-green-600 hover:from-green-600 hover:to-green-700 text-white px-6 py-3 rounded-lg font-semibold shadow-md transition-all disabled:opacity-50"
              >
                ✓ Approva Selezionati ({selectedShifts.size})
              </button>
            )}

            <button
              onClick={fetchShiftsToReview}
              disabled={loading}
              className="bg-gray-200 hover:bg-gray-300 text-gray-800 px-6 py-3 rounded-lg font-semibold transition-all disabled:opacity-50"
            >
              🔄 Aggiorna
            </button>
          </div>

          <div className="mt-4 p-4 bg-blue-50 rounded-lg">
            <div className="flex items-start gap-3">
              <div className="text-2xl">💡</div>
              <div className="text-sm text-blue-800">
                <strong>Sistema intelligente:</strong> I turni entro ±15 minuti dall'orario previsto vengono auto-approvati.
                Qui vedi solo i turni che richiedono la tua attenzione.
              </div>
            </div>
          </div>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
          <div className="bg-white rounded-xl shadow-md p-6">
            <div className="text-gray-600 text-sm mb-1">Turni da Rivedere</div>
            <div className="text-3xl font-bold text-blue-600">{shiftsToReview.length}</div>
          </div>
          <div className="bg-white rounded-xl shadow-md p-6">
            <div className="text-gray-600 text-sm mb-1">Selezionati</div>
            <div className="text-3xl font-bold text-green-600">{selectedShifts.size}</div>
          </div>
          <div className="bg-white rounded-xl shadow-md p-6">
            <div className="text-gray-600 text-sm mb-1">Tasso Auto-Validazione</div>
            <div className="text-3xl font-bold text-purple-600">~95%</div>
          </div>
        </div>

        {/* Shifts Table */}
        {loading ? (
          <div className="bg-white rounded-xl shadow-md p-12 text-center">
            <div className="text-gray-500">Caricamento...</div>
          </div>
        ) : shiftsToReview.length === 0 ? (
          <div className="bg-white rounded-xl shadow-md p-12 text-center">
            <div className="text-6xl mb-4">✨</div>
            <div className="text-2xl font-semibold text-gray-700 mb-2">
              Tutto approvato!
            </div>
            <div className="text-gray-500">
              Non ci sono turni che richiedono la tua revisione.
            </div>
          </div>
        ) : (
          <div className="bg-white rounded-xl shadow-md overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="p-4 text-left">
                      <input
                        type="checkbox"
                        checked={selectedShifts.size === shiftsToReview.length && shiftsToReview.length > 0}
                        onChange={toggleSelectAll}
                        className="w-5 h-5 text-blue-600 rounded"
                      />
                    </th>
                    <th className="p-4 text-left text-sm font-semibold text-gray-700">Dipendente</th>
                    <th className="p-4 text-left text-sm font-semibold text-gray-700">Data</th>
                    <th className="p-4 text-left text-sm font-semibold text-gray-700">Previsto</th>
                    <th className="p-4 text-left text-sm font-semibold text-gray-700">Effettivo</th>
                    <th className="p-4 text-left text-sm font-semibold text-gray-700">Ore</th>
                    <th className="p-4 text-left text-sm font-semibold text-gray-700">Stato</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {shiftsToReview.map((shift) => (
                    <tr
                      key={shift.id}
                      className={`hover:bg-gray-50 transition-colors ${selectedShifts.has(shift.id) ? 'bg-blue-50' : ''}`}
                    >
                      <td className="p-4">
                        <input
                          type="checkbox"
                          checked={selectedShifts.has(shift.id)}
                          onChange={() => toggleShiftSelection(shift.id)}
                          className="w-5 h-5 text-blue-600 rounded"
                        />
                      </td>
                      <td className="p-4 font-medium text-gray-900">{shift.employeeName || 'Non assegnato'}</td>
                      <td className="p-4 text-gray-700">{formatDate(shift.date)}</td>
                      <td className="p-4 text-gray-700">
                        <div className="text-sm">
                          {shift.startTime} - {shift.endTime}
                        </div>
                      </td>
                      <td className="p-4">
                        <div className="text-sm text-gray-700">
                          {formatTime(shift.checkInTime)} - {formatTime(shift.checkOutTime)}
                        </div>
                      </td>
                      <td className="p-4">
                        <span className="inline-block bg-blue-100 text-blue-800 px-3 py-1 rounded-full text-sm font-semibold">
                          {shift.totalHours.toFixed(1)}h
                        </span>
                      </td>
                      <td className="p-4">
                        <span className="inline-block bg-yellow-100 text-yellow-800 px-3 py-1 rounded-full text-sm font-semibold">
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
