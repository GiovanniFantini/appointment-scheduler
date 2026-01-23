import { useState, useEffect, useMemo } from 'react';
import axios from '../lib/axios';
import {
  LeaveRequest,
  RespondToLeaveRequest,
  LeaveRequestStatus
} from '../types/leave';
import AppLayout from '../components/layout/AppLayout';
import { useToast } from '../components/Toast';

interface LeaveRequestsProps {
  user: any;
  onLogout: () => void;
}

export default function LeaveRequests({ user, onLogout }: LeaveRequestsProps) {
  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState<'all' | 'pending' | 'approved' | 'rejected'>('pending');
  const [selectedRequest, setSelectedRequest] = useState<LeaveRequest | null>(null);
  const [responseNotes, setResponseNotes] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;
  const { showToast } = useToast();

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
      showToast(error.response?.data?.message || 'Errore nel caricamento delle richieste', 'error');
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
      showToast(
        status === LeaveRequestStatus.Approved ? 'Richiesta approvata!' : 'Richiesta rifiutata',
        status === LeaveRequestStatus.Approved ? 'success' : 'info'
      );
      setSelectedRequest(null);
      setResponseNotes('');
      fetchRequests();
    } catch (error: any) {
      console.error('Errore nella risposta:', error);
      showToast(error.response?.data?.message || 'Errore nella gestione della richiesta', 'error');
    }
  };

  // Filtri e ricerca avanzati
  const filteredRequests = useMemo(() => {
    let filtered = requests;

    // Filtro per stato
    if (filter === 'pending') filtered = filtered.filter(r => r.status === LeaveRequestStatus.Pending);
    else if (filter === 'approved') filtered = filtered.filter(r => r.status === LeaveRequestStatus.Approved);
    else if (filter === 'rejected') filtered = filtered.filter(r => r.status === LeaveRequestStatus.Rejected);

    // Filtro per ricerca dipendente
    if (searchQuery) {
      filtered = filtered.filter(r =>
        r.employeeName.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }

    // Filtro per periodo
    if (startDate) {
      filtered = filtered.filter(r => new Date(r.startDate) >= new Date(startDate));
    }
    if (endDate) {
      filtered = filtered.filter(r => new Date(r.endDate) <= new Date(endDate));
    }

    return filtered;
  }, [requests, filter, searchQuery, startDate, endDate]);

  // Paginazione
  const totalPages = Math.ceil(filteredRequests.length / itemsPerPage);
  const paginatedRequests = filteredRequests.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  // Reset alla pagina 1 quando cambiano i filtri
  useEffect(() => {
    setCurrentPage(1);
  }, [filter, searchQuery, startDate, endDate]);

  // Statistiche
  const stats = useMemo(() => {
    const pending = requests.filter(r => r.status === LeaveRequestStatus.Pending).length;
    const approved = requests.filter(r => r.status === LeaveRequestStatus.Approved).length;
    const rejected = requests.filter(r => r.status === LeaveRequestStatus.Rejected).length;
    const totalDaysRequested = requests
      .filter(r => r.status === LeaveRequestStatus.Pending)
      .reduce((sum, r) => sum + r.daysRequested, 0);

    return { pending, approved, rejected, totalDaysRequested };
  }, [requests]);

  const getStatusBadgeColor = (status: LeaveRequestStatus) => {
    switch (status) {
      case LeaveRequestStatus.Pending:
        return 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30';
      case LeaveRequestStatus.Approved:
        return 'bg-neon-green/20 text-neon-green border-neon-green/30';
      case LeaveRequestStatus.Rejected:
        return 'bg-red-500/20 text-red-400 border-red-500/30';
      case LeaveRequestStatus.Cancelled:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
      default:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('it-IT', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  };

  const clearFilters = () => {
    setSearchQuery('');
    setStartDate('');
    setEndDate('');
    setFilter('pending');
  };

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Richieste Ferie">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold gradient-text mb-2">Richieste Ferie e Permessi</h1>
          <p className="text-gray-400">
            Gestisci le richieste di ferie e permessi dei tuoi dipendenti
          </p>
        </div>

        {/* Dashboard Statistiche */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
          <div className="glass-card rounded-3xl p-6 border border-yellow-500/30 hover:border-yellow-500/50 transition-all hover:shadow-glow-yellow animate-slide-up">
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">In Attesa</div>
              <div className="text-2xl">⏳</div>
            </div>
            <div className="text-4xl font-bold text-yellow-400">{stats.pending}</div>
            <div className="text-xs text-gray-500 mt-1">{stats.totalDaysRequested} giorni totali</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-green/30 hover:border-neon-green/50 transition-all hover:shadow-glow-green animate-slide-up" style={{ animationDelay: '0.1s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Approvate</div>
              <div className="text-2xl">✅</div>
            </div>
            <div className="text-4xl font-bold text-neon-green">{stats.approved}</div>
            <div className="text-xs text-gray-500 mt-1">Richieste accettate</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-red-500/30 hover:border-red-500/50 transition-all hover:shadow-glow-pink animate-slide-up" style={{ animationDelay: '0.2s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Rifiutate</div>
              <div className="text-2xl">❌</div>
            </div>
            <div className="text-4xl font-bold text-red-400">{stats.rejected}</div>
            <div className="text-xs text-gray-500 mt-1">Richieste negate</div>
          </div>

          <div className="glass-card rounded-3xl p-6 border border-neon-cyan/30 hover:border-neon-cyan/50 transition-all hover:shadow-glow-cyan animate-slide-up" style={{ animationDelay: '0.3s' }}>
            <div className="flex items-center justify-between mb-2">
              <div className="text-sm text-gray-400">Totali</div>
              <div className="text-2xl">📋</div>
            </div>
            <div className="text-4xl font-bold gradient-text">{requests.length}</div>
            <div className="text-xs text-gray-500 mt-1">Tutte le richieste</div>
          </div>
        </div>

        {/* Filtri e Ricerca */}
        <div className="glass-card rounded-3xl p-6 mb-6 border border-white/10">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
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

            <div>
              <label className="block text-xs text-gray-400 mb-2 font-semibold">📅 Data Inizio (da)</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full px-4 py-2.5 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all"
              />
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-2 font-semibold">📅 Data Fine (a)</label>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full px-4 py-2.5 bg-white/5 border border-white/10 rounded-xl text-white focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all"
              />
            </div>

            <div className="flex items-end">
              <button
                onClick={clearFilters}
                className="w-full glass-card-dark px-4 py-2.5 rounded-xl hover:border-neon-purple/50 transition-all font-semibold text-gray-300 border border-white/10"
              >
                🔄 Reset Filtri
              </button>
            </div>
          </div>

          {/* Filter Buttons */}
          <div className="flex gap-3 flex-wrap">
            <button
              onClick={() => setFilter('pending')}
              className={`px-5 py-2.5 rounded-xl font-semibold transition-all border ${
                filter === 'pending'
                  ? 'bg-gradient-to-r from-neon-blue/20 to-neon-cyan/20 text-neon-cyan border-neon-cyan/50 shadow-glow-cyan'
                  : 'glass-card-dark text-gray-300 border-white/10 hover:border-neon-cyan/30'
              }`}
            >
              ⏳ In Attesa {stats.pending > 0 && `(${stats.pending})`}
            </button>
            <button
              onClick={() => setFilter('approved')}
              className={`px-5 py-2.5 rounded-xl font-semibold transition-all border ${
                filter === 'approved'
                  ? 'bg-gradient-to-r from-neon-green/20 to-neon-cyan/20 text-neon-green border-neon-green/50 shadow-glow-green'
                  : 'glass-card-dark text-gray-300 border-white/10 hover:border-neon-green/30'
              }`}
            >
              ✅ Approvate
            </button>
            <button
              onClick={() => setFilter('rejected')}
              className={`px-5 py-2.5 rounded-xl font-semibold transition-all border ${
                filter === 'rejected'
                  ? 'bg-gradient-to-r from-red-500/20 to-neon-pink/20 text-red-400 border-red-500/50 shadow-glow-pink'
                  : 'glass-card-dark text-gray-300 border-white/10 hover:border-red-500/30'
              }`}
            >
              ❌ Rifiutate
            </button>
            <button
              onClick={() => setFilter('all')}
              className={`px-5 py-2.5 rounded-xl font-semibold transition-all border ${
                filter === 'all'
                  ? 'bg-gradient-to-r from-neon-purple/20 to-neon-pink/20 text-neon-purple border-neon-purple/50 shadow-glow-purple'
                  : 'glass-card-dark text-gray-300 border-white/10 hover:border-neon-purple/30'
              }`}
            >
              📋 Tutte
            </button>
          </div>
        </div>

        {/* Requests List */}
        {loading ? (
          <div className="space-y-6">
            {[1, 2, 3].map((i) => (
              <div key={i} className="glass-card rounded-3xl p-6 border border-white/10 animate-pulse">
                <div className="h-6 bg-white/10 rounded w-1/4 mb-4"></div>
                <div className="h-4 bg-white/10 rounded w-3/4 mb-2"></div>
                <div className="h-4 bg-white/10 rounded w-1/2"></div>
              </div>
            ))}
          </div>
        ) : filteredRequests.length === 0 ? (
          <div className="glass-card rounded-3xl p-16 text-center border border-white/10 shadow-glow-purple animate-fade-in">
            <svg className="w-24 h-24 text-gray-600 mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <p className="text-2xl text-gray-400 mb-2">Nessuna richiesta trovata</p>
            <p className="text-gray-500">Prova a modificare i filtri di ricerca</p>
          </div>
        ) : (
          <>
            <div className="grid gap-6 mb-6">
              {paginatedRequests.map((request, index) => (
                <div
                  key={request.id}
                  className="glass-card rounded-3xl shadow-lg p-6 border border-white/10 hover:border-neon-purple/50 transition-all hover:shadow-glow-purple animate-slide-up"
                  style={{ animationDelay: `${index * 0.05}s` }}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-4">
                        <span className={`px-3 py-1.5 rounded-xl text-sm font-bold border ${getStatusBadgeColor(request.status)}`}>
                          {request.statusName}
                        </span>
                        <span className="text-xl font-bold gradient-text">
                          {request.employeeName}
                        </span>
                      </div>

                      <div className="grid md:grid-cols-2 gap-4 mb-4">
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-purple/20">
                          <div className="text-xs text-gray-500 mb-1">📋 Tipo Permesso</div>
                          <div className="font-semibold text-neon-purple">{request.leaveTypeName}</div>
                        </div>
                        <div className="glass-card-dark p-3 rounded-xl border border-neon-blue/20">
                          <div className="text-xs text-gray-500 mb-1">📅 Periodo</div>
                          <div className="font-semibold text-neon-blue">
                            {formatDate(request.startDate)} - {formatDate(request.endDate)}
                            <span className="ml-2 text-neon-cyan">({request.daysRequested} giorni)</span>
                          </div>
                        </div>
                      </div>

                      {request.notes && (
                        <div className="mb-4 glass-card-dark p-4 rounded-xl border border-white/5">
                          <div className="text-xs text-gray-500 mb-2">💬 Note del Dipendente</div>
                          <div className="text-gray-300">{request.notes}</div>
                        </div>
                      )}

                      {request.responseNotes && (
                        <div className="mb-4 glass-card-dark p-4 rounded-xl border border-neon-cyan/20">
                          <div className="text-xs text-gray-500 mb-2">✉️ Risposta</div>
                          <div className="text-neon-cyan">{request.responseNotes}</div>
                        </div>
                      )}

                      <div className="text-xs text-gray-500">
                        🕐 Richiesta il {formatDate(request.createdAt)}
                        {request.responseAt && ` - Risposta il ${formatDate(request.responseAt)}`}
                      </div>
                    </div>

                    {request.status === LeaveRequestStatus.Pending && (
                      <div className="ml-4 flex flex-col gap-3">
                        <button
                          onClick={() => setSelectedRequest(request)}
                          className="bg-gradient-to-r from-neon-green to-neon-cyan text-white px-5 py-2.5 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold whitespace-nowrap"
                        >
                          ✅ Approva
                        </button>
                        <button
                          onClick={() => {
                            setSelectedRequest(request);
                            setResponseNotes('');
                          }}
                          className="glass-card-dark px-5 py-2.5 rounded-xl hover:bg-red-500/20 font-semibold text-gray-300 hover:text-red-400 border border-white/10 hover:border-red-500/50 transition-all transform hover:scale-105 whitespace-nowrap"
                        >
                          ❌ Rifiuta
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>

            {/* Paginazione */}
            {totalPages > 1 && (
              <div className="flex justify-center items-center gap-2">
                <button
                  onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                  className="glass-card-dark px-4 py-2 rounded-xl border border-white/10 hover:border-neon-cyan/50 transition-all disabled:opacity-50 disabled:cursor-not-allowed font-semibold"
                >
                  ← Precedente
                </button>

                <div className="flex gap-2">
                  {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                    <button
                      key={page}
                      onClick={() => setCurrentPage(page)}
                      className={`px-4 py-2 rounded-xl font-semibold transition-all border ${
                        currentPage === page
                          ? 'bg-gradient-to-r from-neon-cyan/20 to-neon-blue/20 text-neon-cyan border-neon-cyan/50'
                          : 'glass-card-dark text-gray-300 border-white/10 hover:border-neon-cyan/30'
                      }`}
                    >
                      {page}
                    </button>
                  ))}
                </div>

                <button
                  onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                  className="glass-card-dark px-4 py-2 rounded-xl border border-white/10 hover:border-neon-cyan/50 transition-all disabled:opacity-50 disabled:cursor-not-allowed font-semibold"
                >
                  Successiva →
                </button>
              </div>
            )}
          </>
        )}

        {/* Response Modal */}
        {selectedRequest && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50 backdrop-blur-sm">
            <div className="glass-card rounded-3xl max-w-md w-full p-6 border border-neon-purple/30 animate-scale-in">
              <h3 className="text-2xl font-bold gradient-text mb-4">
                Rispondi alla Richiesta
              </h3>

              <div className="mb-6 glass-card-dark p-4 rounded-xl border border-white/10">
                <div className="text-sm text-gray-400 mb-2">
                  <span className="text-neon-cyan font-bold">{selectedRequest.employeeName}</span> - {selectedRequest.leaveTypeName}
                </div>
                <div className="text-sm text-gray-300">
                  📅 {formatDate(selectedRequest.startDate)} - {formatDate(selectedRequest.endDate)}
                  <span className="ml-2 text-neon-blue">({selectedRequest.daysRequested} giorni)</span>
                </div>
              </div>

              <div className="mb-6">
                <label className="block text-sm font-bold text-white/90 mb-2">
                  Note di risposta (opzionale)
                </label>
                <textarea
                  value={responseNotes}
                  onChange={(e) => setResponseNotes(e.target.value)}
                  className="w-full px-4 py-3 bg-white/5 border border-white/10 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-neon-cyan focus:shadow-glow-cyan transition-all duration-300"
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
                  className="flex-1 glass-card-dark px-4 py-3 rounded-xl hover:border-white/20 transition-all font-semibold text-gray-300 border border-white/10"
                >
                  Annulla
                </button>
                <button
                  onClick={() => handleRespond(selectedRequest.id, LeaveRequestStatus.Rejected)}
                  className="flex-1 bg-gradient-to-r from-red-500 to-neon-pink text-white px-4 py-3 rounded-xl hover:shadow-glow-pink transition-all transform hover:scale-105 font-semibold"
                >
                  ❌ Rifiuta
                </button>
                <button
                  onClick={() => handleRespond(selectedRequest.id, LeaveRequestStatus.Approved)}
                  className="flex-1 bg-gradient-to-r from-neon-green to-neon-cyan text-white px-4 py-3 rounded-xl hover:shadow-glow-cyan transition-all transform hover:scale-105 font-semibold"
                >
                  ✅ Approva
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
