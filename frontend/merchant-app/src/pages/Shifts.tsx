import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import { Shift, ShiftTemplate, ShiftType, CreateShiftRequest, CreateShiftsFromTemplateRequest, UpdateShiftRequest, AssignShiftRequest } from '../types/shift';

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

const SHIFT_TYPE_NAMES: Record<ShiftType, string> = {
  [ShiftType.Morning]: 'Mattina',
  [ShiftType.Afternoon]: 'Pomeriggio',
  [ShiftType.Evening]: 'Sera',
  [ShiftType.Night]: 'Notte',
  [ShiftType.FullDay]: 'Giornata Intera',
  [ShiftType.Custom]: 'Personalizzato',
};

const COLORS = [
  '#2196F3', '#4CAF50', '#FF9800', '#9C27B0', '#F44336',
  '#00BCD4', '#8BC34A', '#FFC107', '#E91E63', '#3F51B5'
];

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
  const [showEditModal, setShowEditModal] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(false);

  // Form states for Create Shift
  const [createForm, setCreateForm] = useState<CreateShiftRequest>({
    date: '',
    startTime: '09:00:00',
    endTime: '17:00:00',
    breakDurationMinutes: 30,
    shiftType: ShiftType.Custom,
    color: COLORS[0],
  });

  // Form states for Create from Template
  const [templateForm, setTemplateForm] = useState({
    shiftTemplateId: 0,
    startDate: '',
    endDate: '',
    employeeId: undefined as number | undefined,
    daysOfWeek: [] as number[],
  });

  // Form states for Edit Shift
  const [editForm, setEditForm] = useState<UpdateShiftRequest>({
    date: '',
    startTime: '09:00:00',
    endTime: '17:00:00',
    breakDurationMinutes: 30,
    shiftType: ShiftType.Custom,
    color: COLORS[0],
    isActive: true,
  });

  // Form states for Assign Shift
  const [assignForm, setAssignForm] = useState<AssignShiftRequest>({
    employeeId: 0,
  });

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
      setSelectedShift(null);
      fetchShifts();
      alert('Turno eliminato con successo');
    } catch (error: any) {
      console.error('Errore eliminazione turno:', error);
      alert(error.response?.data?.message || 'Errore nell\'eliminazione del turno');
    }
  };

  const openCreateModal = (date?: Date) => {
    const targetDate = date || new Date();
    setCreateForm({
      ...createForm,
      date: targetDate.toISOString().split('T')[0],
    });
    setShowCreateModal(true);
  };

  const handleCreateShift = async () => {
    try {
      await axios.post('/api/shifts', createForm);
      setShowCreateModal(false);
      fetchShifts();
      alert('Turno creato con successo');
      // Reset form
      setCreateForm({
        date: '',
        startTime: '09:00:00',
        endTime: '17:00:00',
        breakDurationMinutes: 30,
        shiftType: ShiftType.Custom,
        color: COLORS[0],
      });
    } catch (error: any) {
      console.error('Errore creazione turno:', error);
      alert(error.response?.data?.message || 'Errore nella creazione del turno');
    }
  };

  const handleCreateFromTemplate = async () => {
    if (!templateForm.shiftTemplateId) {
      alert('Seleziona un template');
      return;
    }

    try {
      const request: CreateShiftsFromTemplateRequest = {
        shiftTemplateId: templateForm.shiftTemplateId,
        startDate: templateForm.startDate,
        endDate: templateForm.endDate,
        employeeId: templateForm.employeeId,
        daysOfWeek: templateForm.daysOfWeek.length > 0 ? templateForm.daysOfWeek : undefined,
      };

      await axios.post('/api/shifts/from-template', request);
      setShowTemplateModal(false);
      fetchShifts();
      alert('Turni creati con successo dal template');
      // Reset form
      setTemplateForm({
        shiftTemplateId: 0,
        startDate: '',
        endDate: '',
        employeeId: undefined,
        daysOfWeek: [],
      });
    } catch (error: any) {
      console.error('Errore creazione turni da template:', error);
      alert(error.response?.data?.message || 'Errore nella creazione dei turni');
    }
  };

  const openEditModal = (shift: Shift) => {
    setSelectedShift(shift);
    setEditForm({
      employeeId: shift.employeeId,
      date: shift.date.split('T')[0],
      startTime: shift.startTime,
      endTime: shift.endTime,
      breakDurationMinutes: shift.breakDurationMinutes,
      shiftType: shift.shiftType,
      color: shift.color,
      notes: shift.notes,
      isActive: shift.isActive,
    });
    setShowEditModal(true);
  };

  const handleEditShift = async () => {
    if (!selectedShift) return;

    try {
      await axios.put(`/api/shifts/${selectedShift.id}`, editForm);
      setShowEditModal(false);
      setSelectedShift(null);
      fetchShifts();
      alert('Turno aggiornato con successo');
    } catch (error: any) {
      console.error('Errore aggiornamento turno:', error);
      alert(error.response?.data?.message || 'Errore nell\'aggiornamento del turno');
    }
  };

  const openAssignModal = (shift: Shift) => {
    setSelectedShift(shift);
    setAssignForm({
      employeeId: shift.employeeId || 0,
    });
    setShowAssignModal(true);
  };

  const handleAssignShift = async () => {
    if (!selectedShift || !assignForm.employeeId) {
      alert('Seleziona un dipendente');
      return;
    }

    try {
      await axios.post(`/api/shifts/${selectedShift.id}/assign`, assignForm);
      setShowAssignModal(false);
      setSelectedShift(null);
      fetchShifts();
      alert('Turno assegnato con successo');
    } catch (error: any) {
      console.error('Errore assegnazione turno:', error);
      alert(error.response?.data?.message || 'Errore nell\'assegnazione del turno');
    }
  };

  const handleTemplateChange = (templateId: number) => {
    const template = templates.find(t => t.id === templateId);
    if (template) {
      setCreateForm({
        ...createForm,
        shiftTemplateId: templateId,
        startTime: template.startTime,
        endTime: template.endTime,
        breakDurationMinutes: template.breakDurationMinutes,
        shiftType: template.shiftType,
        color: template.color,
      });
    }
  };

  const toggleDayOfWeek = (day: number) => {
    setTemplateForm({
      ...templateForm,
      daysOfWeek: templateForm.daysOfWeek.includes(day)
        ? templateForm.daysOfWeek.filter(d => d !== day)
        : [...templateForm.daysOfWeek, day],
    });
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
            <div className="flex gap-3">
              <button
                onClick={() => openCreateModal()}
                className="px-4 py-2 bg-gradient-to-r from-cyan-500 to-blue-500 text-white rounded-lg hover:from-cyan-600 hover:to-blue-600 transition-all"
              >
                + Nuovo Turno
              </button>
              <button
                onClick={() => setShowTemplateModal(true)}
                className="px-4 py-2 bg-gradient-to-r from-purple-500 to-pink-500 text-white rounded-lg hover:from-purple-600 hover:to-pink-600 transition-all"
              >
                Applica Template
              </button>
            </div>
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
                        onClick={() => openCreateModal(day)}
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
        {selectedShift && !showEditModal && !showAssignModal && (
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

              <div className="grid grid-cols-2 gap-3 mt-6">
                <button
                  onClick={() => openEditModal(selectedShift)}
                  className="px-4 py-2 bg-blue-500/20 text-blue-400 rounded-lg hover:bg-blue-500/30 transition-all"
                >
                  Modifica
                </button>
                <button
                  onClick={() => openAssignModal(selectedShift)}
                  className="px-4 py-2 bg-purple-500/20 text-purple-400 rounded-lg hover:bg-purple-500/30 transition-all"
                >
                  Assegna
                </button>
                <button
                  onClick={() => handleDeleteShift(selectedShift.id)}
                  className="px-4 py-2 bg-red-500/20 text-red-400 rounded-lg hover:bg-red-500/30 transition-all"
                >
                  Elimina
                </button>
                <button
                  onClick={() => setSelectedShift(null)}
                  className="px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Chiudi
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Create Shift Modal */}
        {showCreateModal && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 overflow-y-auto">
            <div className="bg-gray-800 rounded-xl p-6 max-w-2xl w-full border border-cyan-500/30 my-8">
              <h2 className="text-2xl font-bold text-cyan-400 mb-6">Crea Nuovo Turno</h2>

              <div className="space-y-4">
                {/* Template Selection (optional) */}
                <div>
                  <label className="block text-gray-300 mb-2">Template (opzionale)</label>
                  <select
                    value={createForm.shiftTemplateId || ''}
                    onChange={(e) => handleTemplateChange(Number(e.target.value))}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                  >
                    <option value="">Nessun template</option>
                    {templates.map(template => (
                      <option key={template.id} value={template.id}>
                        {template.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  {/* Date */}
                  <div>
                    <label className="block text-gray-300 mb-2">Data *</label>
                    <input
                      type="date"
                      value={createForm.date}
                      onChange={(e) => setCreateForm({ ...createForm, date: e.target.value })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>

                  {/* Employee */}
                  <div>
                    <label className="block text-gray-300 mb-2">Dipendente</label>
                    <select
                      value={createForm.employeeId || ''}
                      onChange={(e) => setCreateForm({ ...createForm, employeeId: e.target.value ? Number(e.target.value) : undefined })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    >
                      <option value="">Non assegnato</option>
                      {employees.filter(e => e.isActive).map(emp => (
                        <option key={emp.id} value={emp.id}>
                          {emp.fullName}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="grid grid-cols-3 gap-4">
                  {/* Start Time */}
                  <div>
                    <label className="block text-gray-300 mb-2">Ora Inizio *</label>
                    <input
                      type="time"
                      value={createForm.startTime.substring(0, 5)}
                      onChange={(e) => setCreateForm({ ...createForm, startTime: e.target.value + ':00' })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>

                  {/* End Time */}
                  <div>
                    <label className="block text-gray-300 mb-2">Ora Fine *</label>
                    <input
                      type="time"
                      value={createForm.endTime.substring(0, 5)}
                      onChange={(e) => setCreateForm({ ...createForm, endTime: e.target.value + ':00' })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>

                  {/* Break Duration */}
                  <div>
                    <label className="block text-gray-300 mb-2">Pausa (min)</label>
                    <input
                      type="number"
                      value={createForm.breakDurationMinutes}
                      onChange={(e) => setCreateForm({ ...createForm, breakDurationMinutes: Number(e.target.value) })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      min="0"
                    />
                  </div>
                </div>

                {/* Shift Type */}
                <div>
                  <label className="block text-gray-300 mb-2">Tipo Turno *</label>
                  <select
                    value={createForm.shiftType}
                    onChange={(e) => setCreateForm({ ...createForm, shiftType: Number(e.target.value) as ShiftType })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    required
                  >
                    {Object.entries(SHIFT_TYPE_NAMES).map(([value, name]) => (
                      <option key={value} value={value}>
                        {name}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Color */}
                <div>
                  <label className="block text-gray-300 mb-2">Colore</label>
                  <div className="flex gap-2 flex-wrap">
                    {COLORS.map(color => (
                      <button
                        key={color}
                        type="button"
                        onClick={() => setCreateForm({ ...createForm, color })}
                        className={`w-10 h-10 rounded-lg transition-all ${
                          createForm.color === color ? 'ring-2 ring-white ring-offset-2 ring-offset-gray-800' : ''
                        }`}
                        style={{ backgroundColor: color }}
                      />
                    ))}
                  </div>
                </div>

                {/* Notes */}
                <div>
                  <label className="block text-gray-300 mb-2">Note</label>
                  <textarea
                    value={createForm.notes || ''}
                    onChange={(e) => setCreateForm({ ...createForm, notes: e.target.value })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    rows={3}
                  />
                </div>
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  onClick={handleCreateShift}
                  className="flex-1 px-4 py-2 bg-gradient-to-r from-cyan-500 to-blue-500 text-white rounded-lg hover:from-cyan-600 hover:to-blue-600 transition-all"
                >
                  Crea Turno
                </button>
                <button
                  onClick={() => setShowCreateModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Annulla
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Create from Template Modal */}
        {showTemplateModal && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 overflow-y-auto">
            <div className="bg-gray-800 rounded-xl p-6 max-w-2xl w-full border border-cyan-500/30 my-8">
              <h2 className="text-2xl font-bold text-cyan-400 mb-6">Crea Turni da Template</h2>

              <div className="space-y-4">
                {/* Template Selection */}
                <div>
                  <label className="block text-gray-300 mb-2">Template *</label>
                  <select
                    value={templateForm.shiftTemplateId}
                    onChange={(e) => setTemplateForm({ ...templateForm, shiftTemplateId: Number(e.target.value) })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    required
                  >
                    <option value={0}>Seleziona un template</option>
                    {templates.map(template => (
                      <option key={template.id} value={template.id}>
                        {template.name} ({template.shiftTypeName})
                      </option>
                    ))}
                  </select>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  {/* Start Date */}
                  <div>
                    <label className="block text-gray-300 mb-2">Data Inizio *</label>
                    <input
                      type="date"
                      value={templateForm.startDate}
                      onChange={(e) => setTemplateForm({ ...templateForm, startDate: e.target.value })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>

                  {/* End Date */}
                  <div>
                    <label className="block text-gray-300 mb-2">Data Fine *</label>
                    <input
                      type="date"
                      value={templateForm.endDate}
                      onChange={(e) => setTemplateForm({ ...templateForm, endDate: e.target.value })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>
                </div>

                {/* Employee */}
                <div>
                  <label className="block text-gray-300 mb-2">Dipendente (opzionale)</label>
                  <select
                    value={templateForm.employeeId || ''}
                    onChange={(e) => setTemplateForm({ ...templateForm, employeeId: e.target.value ? Number(e.target.value) : undefined })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                  >
                    <option value="">Tutti i giorni nel range</option>
                    {employees.filter(e => e.isActive).map(emp => (
                      <option key={emp.id} value={emp.id}>
                        {emp.fullName}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Days of Week */}
                <div>
                  <label className="block text-gray-300 mb-2">Giorni della Settimana (opzionale)</label>
                  <div className="flex gap-2 flex-wrap">
                    {[
                      { value: 1, label: 'Lun' },
                      { value: 2, label: 'Mar' },
                      { value: 3, label: 'Mer' },
                      { value: 4, label: 'Gio' },
                      { value: 5, label: 'Ven' },
                      { value: 6, label: 'Sab' },
                      { value: 0, label: 'Dom' },
                    ].map(day => (
                      <button
                        key={day.value}
                        type="button"
                        onClick={() => toggleDayOfWeek(day.value)}
                        className={`px-4 py-2 rounded-lg transition-all ${
                          templateForm.daysOfWeek.includes(day.value)
                            ? 'bg-gradient-to-r from-cyan-500 to-blue-500 text-white'
                            : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                        }`}
                      >
                        {day.label}
                      </button>
                    ))}
                  </div>
                  <p className="text-sm text-gray-400 mt-2">
                    Lascia vuoto per creare turni tutti i giorni nel range
                  </p>
                </div>
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  onClick={handleCreateFromTemplate}
                  className="flex-1 px-4 py-2 bg-gradient-to-r from-purple-500 to-pink-500 text-white rounded-lg hover:from-purple-600 hover:to-pink-600 transition-all"
                >
                  Crea Turni
                </button>
                <button
                  onClick={() => setShowTemplateModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Annulla
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Edit Shift Modal */}
        {showEditModal && selectedShift && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 overflow-y-auto">
            <div className="bg-gray-800 rounded-xl p-6 max-w-2xl w-full border border-cyan-500/30 my-8">
              <h2 className="text-2xl font-bold text-cyan-400 mb-6">Modifica Turno</h2>

              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  {/* Date */}
                  <div>
                    <label className="block text-gray-300 mb-2">Data *</label>
                    <input
                      type="date"
                      value={editForm.date}
                      onChange={(e) => setEditForm({ ...editForm, date: e.target.value })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>

                  {/* Employee */}
                  <div>
                    <label className="block text-gray-300 mb-2">Dipendente</label>
                    <select
                      value={editForm.employeeId || ''}
                      onChange={(e) => setEditForm({ ...editForm, employeeId: e.target.value ? Number(e.target.value) : undefined })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    >
                      <option value="">Non assegnato</option>
                      {employees.filter(e => e.isActive).map(emp => (
                        <option key={emp.id} value={emp.id}>
                          {emp.fullName}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="grid grid-cols-3 gap-4">
                  {/* Start Time */}
                  <div>
                    <label className="block text-gray-300 mb-2">Ora Inizio *</label>
                    <input
                      type="time"
                      value={editForm.startTime.substring(0, 5)}
                      onChange={(e) => setEditForm({ ...editForm, startTime: e.target.value + ':00' })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>

                  {/* End Time */}
                  <div>
                    <label className="block text-gray-300 mb-2">Ora Fine *</label>
                    <input
                      type="time"
                      value={editForm.endTime.substring(0, 5)}
                      onChange={(e) => setEditForm({ ...editForm, endTime: e.target.value + ':00' })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      required
                    />
                  </div>

                  {/* Break Duration */}
                  <div>
                    <label className="block text-gray-300 mb-2">Pausa (min)</label>
                    <input
                      type="number"
                      value={editForm.breakDurationMinutes}
                      onChange={(e) => setEditForm({ ...editForm, breakDurationMinutes: Number(e.target.value) })}
                      className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                      min="0"
                    />
                  </div>
                </div>

                {/* Shift Type */}
                <div>
                  <label className="block text-gray-300 mb-2">Tipo Turno *</label>
                  <select
                    value={editForm.shiftType}
                    onChange={(e) => setEditForm({ ...editForm, shiftType: Number(e.target.value) as ShiftType })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    required
                  >
                    {Object.entries(SHIFT_TYPE_NAMES).map(([value, name]) => (
                      <option key={value} value={value}>
                        {name}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Color */}
                <div>
                  <label className="block text-gray-300 mb-2">Colore</label>
                  <div className="flex gap-2 flex-wrap">
                    {COLORS.map(color => (
                      <button
                        key={color}
                        type="button"
                        onClick={() => setEditForm({ ...editForm, color })}
                        className={`w-10 h-10 rounded-lg transition-all ${
                          editForm.color === color ? 'ring-2 ring-white ring-offset-2 ring-offset-gray-800' : ''
                        }`}
                        style={{ backgroundColor: color }}
                      />
                    ))}
                  </div>
                </div>

                {/* Notes */}
                <div>
                  <label className="block text-gray-300 mb-2">Note</label>
                  <textarea
                    value={editForm.notes || ''}
                    onChange={(e) => setEditForm({ ...editForm, notes: e.target.value })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    rows={3}
                  />
                </div>

                {/* Active Status */}
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={editForm.isActive}
                    onChange={(e) => setEditForm({ ...editForm, isActive: e.target.checked })}
                    className="w-5 h-5 text-cyan-500 bg-gray-700 border-gray-600 rounded focus:ring-cyan-500"
                  />
                  <label htmlFor="isActive" className="text-gray-300">
                    Turno Attivo
                  </label>
                </div>
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  onClick={handleEditShift}
                  className="flex-1 px-4 py-2 bg-gradient-to-r from-blue-500 to-purple-500 text-white rounded-lg hover:from-blue-600 hover:to-purple-600 transition-all"
                >
                  Salva Modifiche
                </button>
                <button
                  onClick={() => setShowEditModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Annulla
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Assign Shift Modal */}
        {showAssignModal && selectedShift && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50">
            <div className="bg-gray-800 rounded-xl p-6 max-w-md w-full border border-cyan-500/30">
              <h2 className="text-2xl font-bold text-cyan-400 mb-6">Assegna Turno</h2>

              <div className="space-y-4">
                {/* Employee */}
                <div>
                  <label className="block text-gray-300 mb-2">Dipendente *</label>
                  <select
                    value={assignForm.employeeId}
                    onChange={(e) => setAssignForm({ ...assignForm, employeeId: Number(e.target.value) })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    required
                  >
                    <option value={0}>Seleziona un dipendente</option>
                    {employees.filter(e => e.isActive).map(emp => (
                      <option key={emp.id} value={emp.id}>
                        {emp.fullName}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Notes */}
                <div>
                  <label className="block text-gray-300 mb-2">Note</label>
                  <textarea
                    value={assignForm.notes || ''}
                    onChange={(e) => setAssignForm({ ...assignForm, notes: e.target.value })}
                    className="w-full px-4 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    rows={3}
                  />
                </div>
              </div>

              <div className="flex gap-3 mt-6">
                <button
                  onClick={handleAssignShift}
                  className="flex-1 px-4 py-2 bg-gradient-to-r from-purple-500 to-pink-500 text-white rounded-lg hover:from-purple-600 hover:to-pink-600 transition-all"
                >
                  Assegna
                </button>
                <button
                  onClick={() => setShowAssignModal(false)}
                  className="flex-1 px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Annulla
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
