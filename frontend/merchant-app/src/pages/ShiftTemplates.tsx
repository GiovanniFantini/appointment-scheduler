import { useState, useEffect } from 'react';

import axios from '../lib/axios';
import { ShiftTemplate, ShiftType, RecurrencePattern, CreateShiftTemplateRequest } from '../types/shift';
import AppLayout from '../components/layout/AppLayout';

interface ShiftTemplatesProps {
  user: any;
  onLogout: () => void;
}

const SHIFT_COLORS = [
  '#00d4ff', '#b24bf3', '#ff2e97', '#00fff9', '#39ff14',
  '#fffc00', '#ff6b35', '#ff1744', '#7c4dff', '#00e5ff'
];

const SHIFT_TYPE_LABELS: Record<ShiftType, string> = {
  [ShiftType.Morning]: 'Mattina',
  [ShiftType.Afternoon]: 'Pomeriggio',
  [ShiftType.Evening]: 'Sera',
  [ShiftType.Night]: 'Notte',
  [ShiftType.FullDay]: 'Giornata Intera',
  [ShiftType.Custom]: 'Personalizzato',
};

const DAYS_OF_WEEK = [
  'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'
];

const DAYS_OF_WEEK_IT = [
  'Lunedì', 'Martedì', 'Mercoledì', 'Giovedì', 'Venerdì', 'Sabato', 'Domenica'
];

function ShiftTemplates({ user, onLogout }: ShiftTemplatesProps) {
  const [templates, setTemplates] = useState<ShiftTemplate[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);

  const [formData, setFormData] = useState<CreateShiftTemplateRequest>({
    name: '',
    description: '',
    shiftType: ShiftType.Custom,
    startTime: '09:00:00',
    endTime: '17:00:00',
    breakDurationMinutes: 30,
    recurrencePattern: RecurrencePattern.None,
    recurrenceDays: '',
    color: SHIFT_COLORS[0],
  });

  const [selectedDays, setSelectedDays] = useState<string[]>([]);

  useEffect(() => {
    fetchTemplates();
  }, []);

  const fetchTemplates = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/shifttemplates');
      setTemplates(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento template:', error);
      alert(error.response?.data?.message || 'Errore nel caricamento dei template');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const recurrenceDays = formData.recurrencePattern === RecurrencePattern.Weekly || formData.recurrencePattern === RecurrencePattern.BiWeekly
      ? selectedDays.join(',')
      : '';

    const payload = {
      ...formData,
      recurrenceDays,
    };

    try {
      if (editingId) {
        await axios.put(`/shifttemplates/${editingId}`, { ...payload, isActive: true });
        alert('Template aggiornato con successo');
      } else {
        await axios.post('/shifttemplates', payload);
        alert('Template creato con successo');
      }

      resetForm();
      fetchTemplates();
    } catch (error: any) {
      console.error('Errore nel salvataggio template:', error);
      alert(error.response?.data?.message || 'Errore nel salvataggio del template');
    }
  };

  const handleEdit = (template: ShiftTemplate) => {
    setEditingId(template.id);
    setFormData({
      name: template.name,
      description: template.description || '',
      shiftType: template.shiftType,
      startTime: template.startTime,
      endTime: template.endTime,
      breakDurationMinutes: template.breakDurationMinutes,
      recurrencePattern: template.recurrencePattern,
      recurrenceDays: template.recurrenceDays || '',
      color: template.color,
    });
    setSelectedDays(template.recurrenceDays ? template.recurrenceDays.split(',') : []);
    setShowForm(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questo template?')) return;

    try {
      await axios.delete(`/shifttemplates/${id}`);
      fetchTemplates();
      alert('Template eliminato con successo');
    } catch (error: any) {
      console.error('Errore eliminazione template:', error);
      alert(error.response?.data?.message || 'Errore nell\'eliminazione del template');
    }
  };

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      shiftType: ShiftType.Custom,
      startTime: '09:00:00',
      endTime: '17:00:00',
      breakDurationMinutes: 30,
      recurrencePattern: RecurrencePattern.None,
      recurrenceDays: '',
      color: SHIFT_COLORS[0],
    });
    setSelectedDays([]);
    setEditingId(null);
    setShowForm(false);
  };

  const toggleDay = (day: string) => {
    setSelectedDays(prev =>
      prev.includes(day) ? prev.filter(d => d !== day) : [...prev, day]
    );
  };

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Template Turni">
      <div className="container mx-auto px-4 py-8">
        {/* Action Button */}
        <div className="mb-6">
          <button
            onClick={() => setShowForm(true)}
            className="bg-gradient-to-r from-neon-cyan to-neon-blue text-white px-6 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
          >
            + Nuovo Template
          </button>
        </div>

        {/* Templates Grid */}
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {loading ? (
            <div className="col-span-full text-center text-neon-cyan py-12">
              Caricamento...
            </div>
          ) : templates.length === 0 ? (
            <div className="col-span-full text-center text-gray-400 py-12">
              Nessun template trovato. Crea il primo template!
            </div>
          ) : (
            templates.map(template => (
              <div
                key={template.id}
                className="glass-card p-6 rounded-3xl border border-white/10 hover:border-neon-cyan/50 transition-all transform hover:scale-105 hover:-translate-y-2"
              >
                <div className="flex items-start justify-between mb-3">
                  <div
                    className="w-4 h-4 rounded-full"
                    style={{ backgroundColor: template.color }}
                  />
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleEdit(template)}
                      className="text-cyan-400 hover:text-cyan-300 text-sm"
                    >
                      Modifica
                    </button>
                    <button
                      onClick={() => handleDelete(template.id)}
                      className="text-red-400 hover:text-red-300 text-sm"
                    >
                      Elimina
                    </button>
                  </div>
                </div>

                <h3 className="text-lg font-semibold text-white mb-2">{template.name}</h3>
                {template.description && (
                  <p className="text-gray-400 text-sm mb-3">{template.description}</p>
                )}

                <div className="space-y-1 text-sm text-gray-300">
                  <div>
                    <span className="text-gray-400">Tipo:</span> {template.shiftTypeName}
                  </div>
                  <div>
                    <span className="text-gray-400">Orario:</span>{' '}
                    {template.startTime.substring(0, 5)} - {template.endTime.substring(0, 5)}
                  </div>
                  <div>
                    <span className="text-gray-400">Ore totali:</span> {template.totalHours}h
                  </div>
                  {template.breakDurationMinutes > 0 && (
                    <div>
                      <span className="text-gray-400">Pausa:</span> {template.breakDurationMinutes} min
                    </div>
                  )}
                  {template.recurrenceDays && (
                    <div>
                      <span className="text-gray-400">Giorni:</span>{' '}
                      {template.recurrenceDays.split(',').map(day => {
                        const index = DAYS_OF_WEEK.indexOf(day);
                        return index >= 0 ? DAYS_OF_WEEK_IT[index] : day;
                      }).join(', ')}
                    </div>
                  )}
                </div>
              </div>
            ))
          )}
        </div>

        {/* Form Modal */}
        {showForm && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 overflow-y-auto">
            <div className="bg-gray-800 rounded-xl p-6 max-w-2xl w-full border border-cyan-500/30 my-8">
              <h2 className="text-2xl font-bold text-cyan-400 mb-6">
                {editingId ? 'Modifica Template' : 'Nuovo Template'}
              </h2>

              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-gray-300 mb-2">Nome Template *</label>
                  <input
                    type="text"
                    required
                    value={formData.name}
                    onChange={e => setFormData({ ...formData, name: e.target.value })}
                    className="w-full px-4 py-2 bg-gray-700/50 border border-gray-600 rounded-lg text-white focus:border-cyan-500 focus:outline-none"
                    placeholder="es: Turno Mattina Weekday"
                  />
                </div>

                <div>
                  <label className="block text-gray-300 mb-2">Descrizione</label>
                  <textarea
                    value={formData.description}
                    onChange={e => setFormData({ ...formData, description: e.target.value })}
                    className="w-full px-4 py-2 bg-gray-700/50 border border-gray-600 rounded-lg text-white focus:border-cyan-500 focus:outline-none"
                    rows={2}
                    placeholder="Descrizione opzionale"
                  />
                </div>

                <div className="grid md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-gray-300 mb-2">Tipo Turno *</label>
                    <select
                      value={formData.shiftType}
                      onChange={e => setFormData({ ...formData, shiftType: parseInt(e.target.value) as ShiftType })}
                      className="w-full px-4 py-2 bg-gray-700/50 border border-gray-600 rounded-lg text-white focus:border-cyan-500 focus:outline-none"
                    >
                      {Object.entries(SHIFT_TYPE_LABELS).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-gray-300 mb-2">Colore *</label>
                    <div className="flex gap-2">
                      {SHIFT_COLORS.map(color => (
                        <button
                          key={color}
                          type="button"
                          onClick={() => setFormData({ ...formData, color })}
                          className={`w-8 h-8 rounded-full transition-all ${
                            formData.color === color ? 'ring-2 ring-cyan-400 ring-offset-2 ring-offset-gray-800' : ''
                          }`}
                          style={{ backgroundColor: color }}
                        />
                      ))}
                    </div>
                  </div>
                </div>

                <div className="grid md:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-gray-300 mb-2">Ora Inizio *</label>
                    <input
                      type="time"
                      required
                      value={formData.startTime.substring(0, 5)}
                      onChange={e => setFormData({ ...formData, startTime: e.target.value + ':00' })}
                      className="w-full px-4 py-2 bg-gray-700/50 border border-gray-600 rounded-lg text-white focus:border-cyan-500 focus:outline-none"
                    />
                  </div>

                  <div>
                    <label className="block text-gray-300 mb-2">Ora Fine *</label>
                    <input
                      type="time"
                      required
                      value={formData.endTime.substring(0, 5)}
                      onChange={e => setFormData({ ...formData, endTime: e.target.value + ':00' })}
                      className="w-full px-4 py-2 bg-gray-700/50 border border-gray-600 rounded-lg text-white focus:border-cyan-500 focus:outline-none"
                    />
                  </div>

                  <div>
                    <label className="block text-gray-300 mb-2">Pausa (minuti)</label>
                    <input
                      type="number"
                      min="0"
                      value={formData.breakDurationMinutes}
                      onChange={e => setFormData({ ...formData, breakDurationMinutes: parseInt(e.target.value) || 0 })}
                      className="w-full px-4 py-2 bg-gray-700/50 border border-gray-600 rounded-lg text-white focus:border-cyan-500 focus:outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-gray-300 mb-2">Pattern Ricorrenza</label>
                  <select
                    value={formData.recurrencePattern}
                    onChange={e => setFormData({ ...formData, recurrencePattern: parseInt(e.target.value) as RecurrencePattern })}
                    className="w-full px-4 py-2 bg-gray-700/50 border border-gray-600 rounded-lg text-white focus:border-cyan-500 focus:outline-none"
                  >
                    <option value={RecurrencePattern.None}>Nessuna ricorrenza</option>
                    <option value={RecurrencePattern.Daily}>Giornaliera</option>
                    <option value={RecurrencePattern.Weekly}>Settimanale</option>
                    <option value={RecurrencePattern.BiWeekly}>Bi-settimanale</option>
                    <option value={RecurrencePattern.Monthly}>Mensile</option>
                  </select>
                </div>

                {(formData.recurrencePattern === RecurrencePattern.Weekly || formData.recurrencePattern === RecurrencePattern.BiWeekly) && (
                  <div>
                    <label className="block text-gray-300 mb-2">Giorni della Settimana</label>
                    <div className="grid grid-cols-7 gap-2">
                      {DAYS_OF_WEEK.map((day, index) => (
                        <button
                          key={day}
                          type="button"
                          onClick={() => toggleDay(day)}
                          className={`px-2 py-2 text-sm rounded-lg transition-all ${
                            selectedDays.includes(day)
                              ? 'bg-cyan-500 text-white'
                              : 'bg-gray-700/50 text-gray-300 hover:bg-gray-700'
                          }`}
                        >
                          {DAYS_OF_WEEK_IT[index].substring(0, 3)}
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                <div className="flex gap-3 pt-4">
                  <button
                    type="submit"
                    className="flex-1 px-6 py-2 bg-gradient-to-r from-cyan-500 to-blue-500 text-white rounded-lg hover:from-cyan-600 hover:to-blue-600 transition-all"
                  >
                    {editingId ? 'Aggiorna' : 'Crea'} Template
                  </button>
                  <button
                    type="button"
                    onClick={resetForm}
                    className="px-6 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                  >
                    Annulla
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

export default ShiftTemplates;
