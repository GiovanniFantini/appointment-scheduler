import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import { Shift, ShiftEmployee } from '../types/shift';
import AppLayout from '../components/layout/AppLayout';

type ViewMode = 'week' | 'month';

interface TeamCalendarProps {
  user: any;
  onLogout: () => void;
}

/** Format a local Date as YYYY-MM-DD without UTC conversion */
const toLocalDateString = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

export default function TeamCalendar({ user, onLogout }: TeamCalendarProps) {
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [viewMode, setViewMode] = useState<ViewMode>('week');
  const [currentDate, setCurrentDate] = useState(new Date());
  const [loading, setLoading] = useState(false);
  const [selectedShift, setSelectedShift] = useState<Shift | null>(null);
  const [filterEmployee, setFilterEmployee] = useState<string>('');

  useEffect(() => {
    fetchTeamShifts();
  }, [currentDate, viewMode]);

  const fetchTeamShifts = async () => {
    try {
      setLoading(true);
      const { startDate, endDate } = getDateRange();
      const response = await axios.get('/shifts/team', {
        params: {
          startDate: toLocalDateString(startDate),
          endDate: toLocalDateString(endDate),
        },
      });
      setShifts(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento turni del team:', error);
    } finally {
      setLoading(false);
    }
  };

  const getDateRange = () => {
    const start = new Date(currentDate);
    const end = new Date(currentDate);

    if (viewMode === 'week') {
      const day = start.getDay();
      const diff = start.getDate() - day + (day === 0 ? -6 : 1);
      start.setDate(diff);
      start.setHours(0, 0, 0, 0);
      end.setDate(start.getDate() + 6);
      end.setHours(23, 59, 59, 999);
    } else {
      start.setDate(1);
      start.setHours(0, 0, 0, 0);
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
    const dateStr = toLocalDateString(date);
    return shifts.filter(shift => shift.date.split('T')[0] === dateStr);
  };

  // Get unique employees from all shifts
  const allEmployees = (() => {
    const employeeMap = new Map<number, string>();
    shifts.forEach(shift => {
      if (shift.employees) {
        shift.employees.forEach((emp: ShiftEmployee) => {
          if (!employeeMap.has(emp.employeeId)) {
            employeeMap.set(emp.employeeId, emp.employeeName);
          }
        });
      }
    });
    return Array.from(employeeMap.entries()).map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name));
  })();

  const getEmployeeNames = (shift: Shift): string => {
    if (shift.employees && shift.employees.length > 0) {
      return shift.employees.map((e: ShiftEmployee) => e.employeeName).join(', ');
    }
    return shift.employeeName || 'Non assegnato';
  };

  const filteredShiftsForDay = (date: Date) => {
    const dayShifts = getShiftsForDay(date);
    if (!filterEmployee) return dayShifts;
    return dayShifts.filter(shift =>
      shift.employees?.some((e: ShiftEmployee) => e.employeeName === filterEmployee)
    );
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

  const getTimeDisplay = (timeSpan: string) => {
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

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Calendario Team">
      <div className="max-w-7xl mx-auto">
        {/* Print Header */}
        <div className="print-header">
          <h1>Calendario Turni Team</h1>
          <p>{getCurrentPeriodLabel()} — Vista {viewMode === 'week' ? 'Settimanale' : 'Mensile'}</p>
        </div>

        {/* Print Button */}
        <div className="mb-4 flex justify-end no-print">
          <button
            onClick={() => window.print()}
            className="bg-gradient-to-r from-gray-600 to-gray-700 text-white px-6 py-3 rounded-xl hover:bg-gray-600 transition-all font-semibold"
          >
            Stampa Calendario
          </button>
        </div>

        {/* Header */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-6 mb-6 border border-cyan-500/30 no-print">
          <h1 className="text-3xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 to-blue-500 mb-4">
            Calendario Turni Team
          </h1>

          <div className="flex items-center justify-between flex-wrap gap-4">
            <div className="flex items-center gap-4 flex-wrap">
              <button
                onClick={handlePrevious}
                className="px-4 py-2 bg-gray-700/50 text-cyan-400 rounded-lg hover:bg-gray-700 transition-all"
              >
                ← Precedente
              </button>
              <button
                onClick={() => setCurrentDate(new Date())}
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

            <div className="flex gap-2 items-center flex-wrap">
              {/* Employee filter */}
              <select
                value={filterEmployee}
                onChange={(e) => setFilterEmployee(e.target.value)}
                className="bg-gray-700/50 text-white px-3 py-2 rounded-lg border border-gray-600 text-sm"
              >
                <option value="">Tutti i colleghi</option>
                {allEmployees.map(emp => (
                  <option key={emp.id} value={emp.name}>{emp.name}</option>
                ))}
              </select>

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

        {/* Legend */}
        {allEmployees.length > 0 && (
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 mb-6 border border-cyan-500/30 no-print">
            <div className="text-sm text-gray-400 mb-2 font-medium">Dipendenti nel team:</div>
            <div className="flex flex-wrap gap-2">
              {allEmployees.map(emp => (
                <button
                  key={emp.id}
                  onClick={() => setFilterEmployee(filterEmployee === emp.name ? '' : emp.name)}
                  className={`px-3 py-1 rounded-full text-sm transition-all ${
                    filterEmployee === emp.name
                      ? 'bg-cyan-500/30 text-cyan-300 border border-cyan-500/50'
                      : 'bg-gray-700/50 text-gray-300 border border-gray-600 hover:border-cyan-500/30'
                  }`}
                >
                  {emp.name}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Calendar */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl border border-cyan-500/30 overflow-hidden">
          {loading ? (
            <div className="p-12 text-center text-cyan-400">Caricamento turni del team...</div>
          ) : (
            <div className={`grid grid-cols-7 gap-px bg-gray-700/50`}>
              {getDaysInView().map((day, index) => {
                const dayShifts = filteredShiftsForDay(day);
                const isToday = day.toDateString() === new Date().toDateString();

                return (
                  <div
                    key={index}
                    className={`bg-gray-800/80 min-h-32 p-3 ${isToday ? 'ring-2 ring-cyan-500' : ''}`}
                  >
                    <div className={`font-semibold mb-2 ${isToday ? 'text-cyan-400' : 'text-gray-300'}`}>
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
                          <div className="text-gray-300 text-xs truncate">
                            {getEmployeeNames(shift)}
                          </div>
                          {shift.shiftTemplateName && (
                            <div className="text-gray-500 text-xs truncate">{shift.shiftTemplateName}</div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Print Footer */}
        <div className="print-footer">
          Stampato il {new Date().toLocaleDateString('it-IT', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
        </div>

        {/* Shift Detail Modal */}
        {selectedShift && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50">
            <div className="bg-gray-800 rounded-xl p-6 max-w-md w-full border border-cyan-500/30">
              <h2 className="text-2xl font-bold text-cyan-400 mb-4">Dettagli Turno</h2>

              <div className="space-y-3 text-gray-300">
                <div>
                  <span className="font-semibold">Data:</span>{' '}
                  {new Date(selectedShift.date).toLocaleDateString('it-IT', { timeZone: 'UTC' })}
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
                  <span className="font-semibold">Tipo:</span> {selectedShift.shiftTypeName}
                </div>
                <div>
                  <span className="font-semibold">Dipendenti:</span>
                  {selectedShift.employees && selectedShift.employees.length > 0 ? (
                    <div className="mt-1 space-y-1">
                      {selectedShift.employees.map((emp: ShiftEmployee) => (
                        <div key={emp.id} className="flex items-center gap-2 text-sm">
                          <div className="w-2 h-2 bg-cyan-400 rounded-full"></div>
                          <span>{emp.employeeName}</span>
                          {emp.isCheckedIn && (
                            <span className="text-xs text-green-400">Check-in</span>
                          )}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <span className="text-gray-500 ml-2">Non assegnato</span>
                  )}
                </div>
                {selectedShift.notes && (
                  <div>
                    <span className="font-semibold">Note:</span> {selectedShift.notes}
                  </div>
                )}
              </div>

              <button
                onClick={() => setSelectedShift(null)}
                className="w-full mt-6 px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
              >
                Chiudi
              </button>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
