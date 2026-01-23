import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import {
  LeaveRequest,
  CreateLeaveRequest,
  EmployeeLeaveBalance,
  LeaveType,
  LeaveRequestStatus
} from '../types/leave';
import AppLayout from '../components/layout/AppLayout';

interface LeaveRequestsProps {
  user: any;
  onLogout: () => void;
}

export default function LeaveRequests({ user, onLogout }: LeaveRequestsProps) {
  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [balances, setBalances] = useState<EmployeeLeaveBalance[]>([]);
  const [loading, setLoading] = useState(false);
  const [showNewRequestModal, setShowNewRequestModal] = useState(false);
  const [selectedYear, setSelectedYear] = useState(new Date().getFullYear());

  // Form state
  const [formData, setFormData] = useState<CreateLeaveRequest>({
    leaveType: LeaveType.Ferie,
    startDate: '',
    endDate: '',
    notes: ''
  });

  useEffect(() => {
    fetchRequests();
    fetchBalances();
  }, [selectedYear]);

  const fetchRequests = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/leaverequests/my-requests');
      setRequests(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento richieste:', error);
      alert(error.response?.data?.message || 'Errore nel caricamento delle richieste');
    } finally {
      setLoading(false);
    }
  };

  const fetchBalances = async () => {
    try {
      const response = await axios.get('/leaverequests/my-balances', {
        params: { year: selectedYear }
      });
      setBalances(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento saldi:', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await axios.post('/leaverequests', formData);
      alert('Richiesta inviata con successo!');
      setShowNewRequestModal(false);
      setFormData({
        leaveType: LeaveType.Ferie,
        startDate: '',
        endDate: '',
        notes: ''
      });
      fetchRequests();
      fetchBalances();
    } catch (error: any) {
      console.error('Errore nella creazione richiesta:', error);
      alert(error.response?.data?.message || 'Errore nella creazione della richiesta');
    }
  };

  const handleCancel = async (requestId: number) => {
    if (!confirm('Sei sicuro di voler cancellare questa richiesta?')) return;

    try {
      await axios.delete(`/leaverequests/${requestId}`);
      alert('Richiesta cancellata');
      fetchRequests();
      fetchBalances();
    } catch (error: any) {
      console.error('Errore nella cancellazione:', error);
      alert(error.response?.data?.message || 'Errore nella cancellazione della richiesta');
    }
  };

  const getStatusBadgeColor = (status: LeaveRequestStatus) => {
    switch (status) {
      case LeaveRequestStatus.Pending:
        return 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30';
      case LeaveRequestStatus.Approved:
        return 'bg-green-500/20 text-green-400 border-green-500/30';
      case LeaveRequestStatus.Rejected:
        return 'bg-red-500/20 text-red-400 border-red-500/30';
      case LeaveRequestStatus.Cancelled:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
      default:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
    }
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

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('it-IT', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  };

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Gestione Ferie e Permessi">
      <div className="max-w-7xl mx-auto">
        {/* Saldi Ferie */}
        <div className="mb-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-bold text-white">Saldi Disponibili</h2>
            <select
              value={selectedYear}
              onChange={(e) => setSelectedYear(Number(e.target.value))}
              className="bg-gray-800 text-white border border-cyan-500/30 rounded-lg px-4 py-2"
            >
              {[2024, 2025, 2026, 2027].map(year => (
                <option key={year} value={year}>{year}</option>
              ))}
            </select>
          </div>

          <div className="grid md:grid-cols-4 gap-4 mb-6">
            {balances.length > 0 ? (
              balances.map((balance) => (
                <div
                  key={balance.id}
                  className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30"
                >
                  <div className="text-gray-400 text-sm mb-1">{balance.leaveTypeName}</div>
                  <div className="text-3xl font-bold text-cyan-400">
                    {balance.remainingDays}
                  </div>
                  <div className="text-xs text-gray-500 mt-1">
                    su {balance.totalDays} totali
                  </div>
                  <div className="text-xs text-gray-500">
                    ({balance.usedDays} utilizzati)
                  </div>
                </div>
              ))
            ) : (
              <div className="col-span-4 bg-gray-800/50 backdrop-blur-sm rounded-xl p-8 border border-cyan-500/30 text-center">
                <p className="text-gray-400">
                  Nessun saldo configurato per l'anno {selectedYear}.
                  <br />
                  Contatta il tuo responsabile per la configurazione dei saldi.
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Pulsante Nuova Richiesta */}
        <div className="mb-6">
          <button
            onClick={() => setShowNewRequestModal(true)}
            className="w-full md:w-auto bg-gradient-to-r from-cyan-500 to-blue-500 text-white px-6 py-3 rounded-xl font-semibold hover:shadow-lg hover:shadow-cyan-500/50 transition-all"
          >
            + Nuova Richiesta
          </button>
        </div>

        {/* Lista Richieste */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl border border-cyan-500/30 overflow-hidden">
          <div className="p-4 border-b border-cyan-500/30">
            <h2 className="text-xl font-bold text-white">Le Mie Richieste</h2>
          </div>

          {loading ? (
            <div className="p-8 text-center text-gray-400">
              Caricamento...
            </div>
          ) : requests.length === 0 ? (
            <div className="p-8 text-center text-gray-400">
              Nessuna richiesta presente
            </div>
          ) : (
            <div className="divide-y divide-gray-700">
              {requests.map((request) => (
                <div key={request.id} className="p-4 hover:bg-gray-700/30 transition-colors">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <span className={`px-3 py-1 rounded-full text-sm border ${getStatusBadgeColor(request.status)}`}>
                          {request.statusName}
                        </span>
                        <span className="font-semibold text-white">
                          {request.leaveTypeName}
                        </span>
                      </div>

                      <div className="text-sm text-gray-300 mb-2">
                        <span className="font-medium">Dal:</span> {formatDate(request.startDate)}
                        {' - '}
                        <span className="font-medium">Al:</span> {formatDate(request.endDate)}
                        {' '}
                        <span className="text-cyan-400">({request.daysRequested} giorni)</span>
                      </div>

                      {request.notes && (
                        <div className="text-sm text-gray-400 mb-2">
                          <span className="font-medium">Note:</span> {request.notes}
                        </div>
                      )}

                      {request.responseNotes && (
                        <div className="text-sm text-gray-400 bg-gray-900/50 p-2 rounded">
                          <span className="font-medium text-cyan-400">Risposta:</span> {request.responseNotes}
                        </div>
                      )}

                      <div className="text-xs text-gray-500 mt-2">
                        Richiesta il {formatDate(request.createdAt)}
                        {request.responseAt && ` - Risposta il ${formatDate(request.responseAt)}`}
                      </div>
                    </div>

                    <div className="ml-4">
                      {request.status === LeaveRequestStatus.Pending && (
                        <button
                          onClick={() => handleCancel(request.id)}
                          className="text-red-400 hover:text-red-300 text-sm"
                        >
                          Cancella
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Modal Nuova Richiesta */}
        {showNewRequestModal && (
          <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
            <div className="bg-gray-900 rounded-xl border border-cyan-500/30 max-w-md w-full p-6">
              <h3 className="text-xl font-bold text-white mb-4">Nuova Richiesta</h3>

              <form onSubmit={handleSubmit}>
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-2">
                      Tipo Permesso
                    </label>
                    <select
                      value={formData.leaveType}
                      onChange={(e) => setFormData({ ...formData, leaveType: Number(e.target.value) as LeaveType })}
                      className="w-full bg-gray-800 text-white border border-cyan-500/30 rounded-lg px-4 py-2"
                      required
                    >
                      <option value={LeaveType.Ferie}>Ferie</option>
                      <option value={LeaveType.ROL}>ROL</option>
                      <option value={LeaveType.PAR}>PAR</option>
                      <option value={LeaveType.Malattia}>Malattia</option>
                      <option value={LeaveType.ExFestivita}>Ex-festività</option>
                      <option value={LeaveType.Welfare}>Welfare</option>
                      <option value={LeaveType.Permesso}>Permesso</option>
                      <option value={LeaveType.Altro}>Altro</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-2">
                      Data Inizio
                    </label>
                    <input
                      type="date"
                      value={formData.startDate}
                      onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                      className="w-full bg-gray-800 text-white border border-cyan-500/30 rounded-lg px-4 py-2"
                      required
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-2">
                      Data Fine
                    </label>
                    <input
                      type="date"
                      value={formData.endDate}
                      onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                      className="w-full bg-gray-800 text-white border border-cyan-500/30 rounded-lg px-4 py-2"
                      required
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-2">
                      Note (opzionale)
                    </label>
                    <textarea
                      value={formData.notes}
                      onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                      className="w-full bg-gray-800 text-white border border-cyan-500/30 rounded-lg px-4 py-2"
                      rows={3}
                      placeholder="Eventuali note o motivazioni..."
                    />
                  </div>
                </div>

                <div className="flex gap-3 mt-6">
                  <button
                    type="button"
                    onClick={() => setShowNewRequestModal(false)}
                    className="flex-1 bg-gray-700 text-white px-4 py-2 rounded-lg hover:bg-gray-600 transition-colors"
                  >
                    Annulla
                  </button>
                  <button
                    type="submit"
                    className="flex-1 bg-gradient-to-r from-cyan-500 to-blue-500 text-white px-4 py-2 rounded-lg font-semibold hover:shadow-lg hover:shadow-cyan-500/50 transition-all"
                  >
                    Invia Richiesta
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
