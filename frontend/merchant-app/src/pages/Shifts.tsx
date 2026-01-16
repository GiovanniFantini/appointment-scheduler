import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import { Shift, ShiftTemplate, CreateShiftRequest, CreateShiftsFromTemplateRequest } from '../types/shift';

interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  role?: string;
  isActive: boolean;
}

type ViewMode = 'week' | 'month';

export default function Shifts() {
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [templates, setTemplates] = useState<ShiftTemplate[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [viewMode, setViewMode] = useState<ViewMode>('week');
  const [currentDate, setCurrentDate] = useState(new Date());
  const [loading, setLoading] = useState(false);
  const [selectedShift, setSelectedShift] = useState<Shift | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showTemplateModal, setShowTemplateModal] = useState(false);

  useEffect(() => {
    fetchShifts();
    fetchTemplates();
    fetchEmployees();
  }, [currentDate, viewMode]);

  const fetchShifts = async () => {
    try {
      setLoading(true);
      const { startDate, endDate } = getDateRange();
      const response = await axios.get('/api/shifts/merchant', {
        params: {
          startDate: startDate.toISOString(),
          endDate: endDate.toISOString(),
        },
      });
      setShifts(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento turni:', error);
      alert(error.response?.data?.message || 'Errore nel caricamento dei turni');
    } finally {
      setLoading(false);
    }
  };

  const fetchTemplates = async () => {
    try {
      const response = await axios.get('/api/shifttemplates');
      setTemplates(response.data);
    } catch (error) {
      console.error('Errore nel caricamento template:', error);
    }
  };

  const fetchEmployees = async () => {
    try {
      const response = await axios.get('/api/employees/my-employees');
      setEmployees(response.data);
    } catch (error) {
      console.error('Errore nel caricamento dipendenti:', error);
    }
  };

  const getDateRange = () => {
    const start = new Date(currentDate);
    const end = new Date(currentDate);

    if (viewMode === 'week') {
      // Inizio settimana (Lunedì)
      const day = start.getDay();
      const diff = start.getDate() - day + (day === 0 ? -6 : 1);
      start.setDate(diff);
      start.setHours(0, 0, 0, 0);

      // Fine settimana (Domenica)
      end.setDate(start.getDate() + 6);
      end.setHours(23, 59, 59, 999);
    } else {
      // Inizio mese
      start.setDate(1);
      start.setHours(0, 0, 0, 0);

      // Fine mese
      end.setMonth(end.getMonth() + 1);
      end.setDate(0);
      end.setHours(23, 59, 59, 999);
    }

    return { startDate: start, endDate: end };
  };

  const getDaysInView = () => {
    const { startDate, endDate } = getDateRange();
    const days: Date[] = [];
    const current = new Date(startDate);

    while (current <= endDate) {
      days.push(new Date(current));
      current.setDate(current.getDate() + 1);
    }

    return days;
  };

  const getShiftsForDay = (date: Date) => {
    const dateStr = date.toISOString().split('T')[0];
    return shifts.filter(shift => shift.date.split('T')[0] === dateStr);
  };

  const handlePrevious = () => {
    const newDate = new Date(currentDate);
    if (viewMode === 'week') {
      newDate.setDate(newDate.getDate() - 7);
    } else {
      newDate.setMonth(newDate.getMonth() - 1);
    }
    setCurrentDate(newDate);
  };

  const handleNext = () => {
    const newDate = new Date(currentDate);
    if (viewMode === 'week') {
      newDate.setDate(newDate.getDate() + 7);
    } else {
      newDate.setMonth(newDate.getMonth() + 1);
    }
    setCurrentDate(newDate);
  };

  const handleToday = () => {
    setCurrentDate(new Date());
  };

  const getTimeDisplay = (timeSpan: string) => {
    // TimeSpan format: "HH:mm:ss"
    const parts = timeSpan.split(':');
    return `${parts[0]}:${parts[1]}`;
  };

  const formatDate = (date: Date) => {
    if (viewMode === 'week') {
      return date.toLocaleDateString('it-IT', { weekday: 'short', day: 'numeric', month: 'short' });
    }
    return date.toLocaleDateString('it-IT', { day: 'numeric' });
  };

  const getCurrentPeriodLabel = () => {
    if (viewMode === 'week') {
      const { startDate, endDate } = getDateRange();
      return `${startDate.toLocaleDateString('it-IT', { day: 'numeric', month: 'short' })} - ${endDate.toLocaleDateString('it-IT', { day: 'numeric', month: 'short', year: 'numeric' })}`;
    }
    return currentDate.toLocaleDateString('it-IT', { month: 'long', year: 'numeric' });
  };

  const handleDeleteShift = async (shiftId: number) => {
    if (!confirm('Sei sicuro di voler eliminare questo turno?')) return;

    try {
      await axios.delete(`/api/shifts/${shiftId}`);
      fetchShifts();
      alert('Turno eliminato con successo');
    } catch (error: any) {
      console.error('Errore eliminazione turno:', error);
      alert(error.response?.data?.message || 'Errore nell\'eliminazione del turno');
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-purple-900 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-6 mb-6 border border-cyan-500/30">
          <div className="flex justify-between items-center mb-4">
            <h1 className="text-3xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 to-blue-500">
              Gestione Turni
            </h1>
            <button
              onClick={() => setShowTemplateModal(true)}
              className="px-4 py-2 bg-gradient-to-r from-purple-500 to-pink-500 text-white rounded-lg hover:from-purple-600 hover:to-pink-600 transition-all"
            >
              Template Turni
            </button>
          </div>

          {/* Navigation Controls */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <button
                onClick={handlePrevious}
                className="px-4 py-2 bg-gray-700/50 text-cyan-400 rounded-lg hover:bg-gray-700 transition-all"
              >
                ← Precedente
              </button>
              <button
                onClick={handleToday}
                className="px-4 py-2 bg-gray-700/50 text-cyan-400 rounded-lg hover:bg-gray-700 transition-all"
              >
                Oggi
              </button>
              <button
                onClick={handleNext}
                className="px-4 py-2 bg-gray-700/50 text-cyan-400 rounded-lg hover:bg-gray-700 transition-all"
              >
                Successivo →
              </button>
              <span className="text-xl font-semibold text-cyan-400">
                {getCurrentPeriodLabel()}
              </span>
            </div>

            <div className="flex gap-2">
              <button
                onClick={() => setViewMode('week')}
                className={`px-4 py-2 rounded-lg transition-all ${
                  viewMode === 'week'
                    ? 'bg-gradient-to-r from-cyan-500 to-blue-500 text-white'
                    : 'bg-gray-700/50 text-gray-300 hover:bg-gray-700'
                }`}
              >
                Settimana
              </button>
              <button
                onClick={() => setViewMode('month')}
                className={`px-4 py-2 rounded-lg transition-all ${
                  viewMode === 'month'
                    ? 'bg-gradient-to-r from-cyan-500 to-blue-500 text-white'
                    : 'bg-gray-700/50 text-gray-300 hover:bg-gray-700'
                }`}
              >
                Mese
              </button>
            </div>
          </div>
        </div>

        {/* Calendar Grid */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl border border-cyan-500/30 overflow-hidden">
          {loading ? (
            <div className="p-12 text-center text-cyan-400">
              Caricamento turni...
            </div>
          ) : (
            <div className={`grid ${viewMode === 'week' ? 'grid-cols-7' : 'grid-cols-7'} gap-px bg-gray-700/50`}>
              {getDaysInView().map((day, index) => {
                const dayShifts = getShiftsForDay(day);
                const isToday = day.toDateString() === new Date().toDateString();

                return (
                  <div
                    key={index}
                    className={`bg-gray-800/80 min-h-32 p-3 ${
                      isToday ? 'ring-2 ring-cyan-500' : ''
                    }`}
                  >
                    <div className={`font-semibold mb-2 ${
                      isToday ? 'text-cyan-400' : 'text-gray-300'
                    }`}>
                      {formatDate(day)}
                    </div>

                    <div className="space-y-1">
                      {dayShifts.map(shift => (
                        <div
                          key={shift.id}
                          className="text-xs p-2 rounded cursor-pointer hover:opacity-80 transition-opacity"
                          style={{ backgroundColor: shift.color + '40', borderLeft: `3px solid ${shift.color}` }}
                          onClick={() => setSelectedShift(shift)}
                        >
                          <div className="font-semibold text-white">
                            {getTimeDisplay(shift.startTime)} - {getTimeDisplay(shift.endTime)}
                          </div>
                          {shift.employeeName && (
                            <div className="text-gray-300 truncate">{shift.employeeName}</div>
                          )}
                          <div className="text-gray-400">{shift.totalHours}h</div>
                        </div>
                      ))}
                    </div>

                    {viewMode === 'week' && (
                      <button
                        onClick={() => {
                          // TODO: Open create shift modal for this date
                          setShowCreateModal(true);
                        }}
                        className="mt-2 w-full text-xs py-1 text-cyan-400 hover:bg-cyan-500/20 rounded transition-all"
                      >
                        + Aggiungi
                      </button>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Shift Detail Modal */}
        {selectedShift && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50">
            <div className="bg-gray-800 rounded-xl p-6 max-w-md w-full border border-cyan-500/30">
              <h2 className="text-2xl font-bold text-cyan-400 mb-4">Dettagli Turno</h2>

              <div className="space-y-3 text-gray-300">
                <div>
                  <span className="font-semibold">Data:</span>{' '}
                  {new Date(selectedShift.date).toLocaleDateString('it-IT')}
                </div>
                <div>
                  <span className="font-semibold">Orario:</span>{' '}
                  {getTimeDisplay(selectedShift.startTime)} - {getTimeDisplay(selectedShift.endTime)}
                </div>
                <div>
                  <span className="font-semibold">Durata:</span> {selectedShift.totalHours} ore
                </div>
                {selectedShift.breakDurationMinutes > 0 && (
                  <div>
                    <span className="font-semibold">Pausa:</span> {selectedShift.breakDurationMinutes} minuti
                  </div>
                )}
                <div>
                  <span className="font-semibold">Dipendente:</span>{' '}
                  {selectedShift.employeeName || 'Non assegnato'}
                </div>
                <div>
                  <span className="font-semibold">Tipo:</span> {selectedShift.shiftTypeName}
                </div>
                {selectedShift.notes && (
                  <div>
                    <span className="font-semibold">Note:</span> {selectedShift.notes}
                  </div>
                )}
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  onClick={() => handleDeleteShift(selectedShift.id)}
                  className="flex-1 px-4 py-2 bg-red-500/20 text-red-400 rounded-lg hover:bg-red-500/30 transition-all"
                >
                  Elimina
                </button>
                <button
                  onClick={() => setSelectedShift(null)}
                  className="flex-1 px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Chiudi
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
