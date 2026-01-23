import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import {
  LeaveRequest,
  RespondToLeaveRequest,
  LeaveRequestStatus
} from '../types/leave';

interface LeaveRequestsProps {
  user: any;
  onLogout: () => void;
}

export default function LeaveRequests({ }: LeaveRequestsProps) {
  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState<'all' | 'pending' | 'approved' | 'rejected'>('pending');
  const [selectedRequest, setSelectedRequest] = useState<LeaveRequest | null>(null);
  const [responseNotes, setResponseNotes] = useState('');

  useEffect(() => {
    fetchRequests();
  }, []);

  const fetchRequests = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/leaverequests/merchant');
      setRequests(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento richieste:', error);
      alert(error.response?.data?.message || 'Errore nel caricamento delle richieste');
    } finally {
      setLoading(false);
    }
  };

  const handleRespond = async (requestId: number, status: LeaveRequestStatus) => {
    try {
      const response: RespondToLeaveRequest = {
        status,
        responseNotes: responseNotes || undefined
      };

      await axios.post(`/leaverequests/${requestId}/respond`, response);
      alert(status === LeaveRequestStatus.Approved ? 'Richiesta approvata!' : 'Richiesta rifiutata');
      setSelectedRequest(null);
      setResponseNotes('');
      fetchRequests();
    } catch (error: any) {
      console.error('Errore nella risposta:', error);
      alert(error.response?.data?.message || 'Errore nella gestione della richiesta');
    }
  };

  const filteredRequests = requests.filter(req => {
    if (filter === 'all') return true;
    if (filter === 'pending') return req.status === LeaveRequestStatus.Pending;
    if (filter === 'approved') return req.status === LeaveRequestStatus.Approved;
    if (filter === 'rejected') return req.status === LeaveRequestStatus.Rejected;
    return true;
  });

  const pendingCount = requests.filter(r => r.status === LeaveRequestStatus.Pending).length;

  const getStatusBadgeColor = (status: LeaveRequestStatus) => {
    switch (status) {
      case LeaveRequestStatus.Pending:
        return 'bg-yellow-100 text-yellow-800 border-yellow-300';
      case LeaveRequestStatus.Approved:
        return 'bg-green-100 text-green-800 border-green-300';
      case LeaveRequestStatus.Rejected:
        return 'bg-red-100 text-red-800 border-red-300';
      case LeaveRequestStatus.Cancelled:
        return 'bg-gray-100 text-gray-800 border-gray-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
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
    <div className="p-6">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Richieste Ferie e Permessi</h1>
        <p className="text-gray-600 mt-1">
          Gestisci le richieste di ferie e permessi dei tuoi dipendenti
        </p>
      </div>

      {/* Filters */}
      <div className="mb-6 flex gap-2">
        <button
          onClick={() => setFilter('pending')}
          className={`px-4 py-2 rounded-lg font-medium transition-colors ${
            filter === 'pending'
              ? 'bg-blue-600 text-white'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          In Attesa {pendingCount > 0 && `(${pendingCount})`}
        </button>
        <button
          onClick={() => setFilter('approved')}
          className={`px-4 py-2 rounded-lg font-medium transition-colors ${
            filter === 'approved'
              ? 'bg-blue-600 text-white'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          Approvate
        </button>
        <button
          onClick={() => setFilter('rejected')}
          className={`px-4 py-2 rounded-lg font-medium transition-colors ${
            filter === 'rejected'
              ? 'bg-blue-600 text-white'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          Rifiutate
        </button>
        <button
          onClick={() => setFilter('all')}
          className={`px-4 py-2 rounded-lg font-medium transition-colors ${
            filter === 'all'
              ? 'bg-blue-600 text-white'
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          Tutte
        </button>
      </div>

      {/* Requests List */}
      {loading ? (
        <div className="text-center py-12">
          <div className="text-gray-600">Caricamento...</div>
        </div>
      ) : filteredRequests.length === 0 ? (
        <div className="bg-white rounded-lg shadow p-12 text-center">
          <div className="text-gray-500">Nessuna richiesta trovata</div>
        </div>
      ) : (
        <div className="grid gap-4">
          {filteredRequests.map((request) => (
            <div
              key={request.id}
              className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-3">
                    <span className={`px-3 py-1 rounded-full text-sm font-medium border ${getStatusBadgeColor(request.status)}`}>
                      {request.statusName}
                    </span>
                    <span className="font-semibold text-lg text-gray-900">
                      {request.employeeName}
                    </span>
                  </div>

                  <div className="grid md:grid-cols-2 gap-4 mb-4">
                    <div>
                      <div className="text-sm text-gray-500 mb-1">Tipo Permesso</div>
                      <div className="font-medium text-gray-900">{request.leaveTypeName}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-500 mb-1">Periodo</div>
                      <div className="font-medium text-gray-900">
                        {formatDate(request.startDate)} - {formatDate(request.endDate)}
                        <span className="ml-2 text-blue-600">({request.daysRequested} giorni)</span>
                      </div>
                    </div>
                  </div>

                  {request.notes && (
                    <div className="mb-4">
                      <div className="text-sm text-gray-500 mb-1">Note del Dipendente</div>
                      <div className="bg-gray-50 p-3 rounded-lg text-gray-900">
                        {request.notes}
                      </div>
                    </div>
                  )}

                  {request.responseNotes && (
                    <div className="mb-4">
                      <div className="text-sm text-gray-500 mb-1">Risposta</div>
                      <div className="bg-blue-50 p-3 rounded-lg text-gray-900">
                        {request.responseNotes}
                      </div>
                    </div>
                  )}

                  <div className="text-xs text-gray-500">
                    Richiesta il {formatDate(request.createdAt)}
                    {request.responseAt && ` - Risposta il ${formatDate(request.responseAt)}`}
                  </div>
                </div>

                {request.status === LeaveRequestStatus.Pending && (
                  <div className="ml-4 flex gap-2">
                    <button
                      onClick={() => setSelectedRequest(request)}
                      className="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 transition-colors"
                    >
                      Approva
                    </button>
                    <button
                      onClick={() => {
                        setSelectedRequest(request);
                        setResponseNotes('');
                      }}
                      className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 transition-colors"
                    >
                      Rifiuta
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Response Modal */}
      {selectedRequest && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-xl max-w-md w-full p-6">
            <h3 className="text-xl font-bold text-gray-900 mb-4">
              Rispondi alla Richiesta
            </h3>

            <div className="mb-4">
              <div className="text-sm text-gray-600 mb-2">
                <strong>{selectedRequest.employeeName}</strong> - {selectedRequest.leaveTypeName}
              </div>
              <div className="text-sm text-gray-600">
                {formatDate(selectedRequest.startDate)} - {formatDate(selectedRequest.endDate)}
                ({selectedRequest.daysRequested} giorni)
              </div>
            </div>

            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Note di risposta (opzionale)
              </label>
              <textarea
                value={responseNotes}
                onChange={(e) => setResponseNotes(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-4 py-2"
                rows={3}
                placeholder="Aggiungi eventuali note..."
              />
            </div>

            <div className="flex gap-3">
              <button
                onClick={() => {
                  setSelectedRequest(null);
                  setResponseNotes('');
                }}
                className="flex-1 bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300 transition-colors"
              >
                Annulla
              </button>
              <button
                onClick={() => handleRespond(selectedRequest.id, LeaveRequestStatus.Rejected)}
                className="flex-1 bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 transition-colors"
              >
                Rifiuta
              </button>
              <button
                onClick={() => handleRespond(selectedRequest.id, LeaveRequestStatus.Approved)}
                className="flex-1 bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 transition-colors"
              >
                Approva
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
