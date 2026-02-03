import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import AppLayout from '../components/layout/AppLayout';

interface BoardMessage {
  id: number;
  merchantId: number;
  authorUserId: number;
  authorName: string;
  title: string;
  content: string;
  priority: number;
  priorityName: string;
  category: string;
  isPinned: boolean;
  expiresAt?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  readCount: number;
}

interface BachecaProps {
  user: any;
  onLogout: () => void;
}

const CATEGORIES = ['Generale', 'Turni', 'HR', 'Evento', 'Sicurezza', 'Formazione'];

export default function Bacheca({ user, onLogout }: BachecaProps) {
  const [messages, setMessages] = useState<BoardMessage[]>([]);
  const [loading, setLoading] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [selectedMessage, setSelectedMessage] = useState<BoardMessage | null>(null);
  const [filterCategory, setFilterCategory] = useState<string>('');
  const [filterPriority, setFilterPriority] = useState<number | null>(null);

  // Form state
  const [formTitle, setFormTitle] = useState('');
  const [formContent, setFormContent] = useState('');
  const [formPriority, setFormPriority] = useState(0);
  const [formCategory, setFormCategory] = useState('Generale');
  const [formIsPinned, setFormIsPinned] = useState(false);
  const [formExpiresAt, setFormExpiresAt] = useState('');

  useEffect(() => {
    fetchMessages();
  }, []);

  const fetchMessages = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/boardmessages/merchant');
      setMessages(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento messaggi:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    try {
      await axios.post('/boardmessages', {
        title: formTitle,
        content: formContent,
        priority: formPriority,
        category: formCategory,
        isPinned: formIsPinned,
        expiresAt: formExpiresAt || null,
      });
      setShowCreateModal(false);
      resetForm();
      fetchMessages();
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore nella creazione del messaggio');
    }
  };

  const handleUpdate = async () => {
    if (!selectedMessage) return;
    try {
      await axios.put(`/boardmessages/${selectedMessage.id}`, {
        title: formTitle,
        content: formContent,
        priority: formPriority,
        category: formCategory,
        isPinned: formIsPinned,
        expiresAt: formExpiresAt || null,
      });
      setShowEditModal(false);
      setSelectedMessage(null);
      resetForm();
      fetchMessages();
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore nell\'aggiornamento del messaggio');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Sei sicuro di voler eliminare questo messaggio?')) return;
    try {
      await axios.delete(`/boardmessages/${id}`);
      fetchMessages();
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore nell\'eliminazione del messaggio');
    }
  };

  const openEditModal = (message: BoardMessage) => {
    setSelectedMessage(message);
    setFormTitle(message.title);
    setFormContent(message.content);
    setFormPriority(message.priority);
    setFormCategory(message.category);
    setFormIsPinned(message.isPinned);
    setFormExpiresAt(message.expiresAt ? message.expiresAt.split('T')[0] : '');
    setShowEditModal(true);
  };

  const resetForm = () => {
    setFormTitle('');
    setFormContent('');
    setFormPriority(0);
    setFormCategory('Generale');
    setFormIsPinned(false);
    setFormExpiresAt('');
  };

  const getPriorityBadge = (priority: number) => {
    switch (priority) {
      case 2:
        return <span className="px-2 py-1 text-xs font-bold rounded-full bg-red-500/20 text-red-400 border border-red-500/30">Urgente</span>;
      case 1:
        return <span className="px-2 py-1 text-xs font-bold rounded-full bg-yellow-500/20 text-yellow-400 border border-yellow-500/30">Importante</span>;
      default:
        return <span className="px-2 py-1 text-xs font-bold rounded-full bg-gray-500/20 text-gray-400 border border-gray-500/30">Normale</span>;
    }
  };

  const getCategoryBadge = (category: string) => {
    const colors: Record<string, string> = {
      'Generale': 'bg-blue-500/20 text-blue-400 border-blue-500/30',
      'Turni': 'bg-cyan-500/20 text-cyan-400 border-cyan-500/30',
      'HR': 'bg-purple-500/20 text-purple-400 border-purple-500/30',
      'Evento': 'bg-green-500/20 text-green-400 border-green-500/30',
      'Sicurezza': 'bg-orange-500/20 text-orange-400 border-orange-500/30',
      'Formazione': 'bg-pink-500/20 text-pink-400 border-pink-500/30',
    };
    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-full border ${colors[category] || colors['Generale']}`}>
        {category}
      </span>
    );
  };

  const filteredMessages = messages.filter(m => {
    if (filterCategory && m.category !== filterCategory) return false;
    if (filterPriority !== null && m.priority !== filterPriority) return false;
    return true;
  });

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Bacheca Aziendale">
      <div className="max-w-5xl mx-auto">
        {/* Header */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-6 mb-6 border border-cyan-500/30">
          <div className="flex items-center justify-between flex-wrap gap-4">
            <div>
              <h1 className="text-3xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 to-blue-500">
                Bacheca Aziendale
              </h1>
              <p className="text-gray-400 mt-1">Gestisci le comunicazioni per i tuoi dipendenti</p>
            </div>
            <button
              onClick={() => { resetForm(); setShowCreateModal(true); }}
              className="bg-gradient-to-r from-cyan-500 to-blue-500 text-white px-6 py-3 rounded-xl hover:from-cyan-600 hover:to-blue-600 transition-all font-semibold flex items-center gap-2"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              Nuovo Messaggio
            </button>
          </div>
        </div>

        {/* Stats */}
        <div className="grid md:grid-cols-4 gap-4 mb-6">
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30">
            <div className="text-gray-400 text-sm mb-1">Totale Messaggi</div>
            <div className="text-3xl font-bold text-cyan-400">{messages.length}</div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30">
            <div className="text-gray-400 text-sm mb-1">Urgenti</div>
            <div className="text-3xl font-bold text-red-400">{messages.filter(m => m.priority === 2).length}</div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30">
            <div className="text-gray-400 text-sm mb-1">Fissati</div>
            <div className="text-3xl font-bold text-yellow-400">{messages.filter(m => m.isPinned).length}</div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30">
            <div className="text-gray-400 text-sm mb-1">Questo Mese</div>
            <div className="text-3xl font-bold text-purple-400">
              {messages.filter(m => {
                const d = new Date(m.createdAt);
                const now = new Date();
                return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
              }).length}
            </div>
          </div>
        </div>

        {/* Filters */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 mb-6 border border-cyan-500/30">
          <div className="flex items-center gap-4 flex-wrap">
            <span className="text-gray-400 text-sm font-medium">Filtra:</span>
            <select
              value={filterCategory}
              onChange={(e) => setFilterCategory(e.target.value)}
              className="bg-gray-700/50 text-white px-3 py-2 rounded-lg border border-gray-600 text-sm"
            >
              <option value="">Tutte le categorie</option>
              {CATEGORIES.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
            <select
              value={filterPriority === null ? '' : filterPriority}
              onChange={(e) => setFilterPriority(e.target.value === '' ? null : parseInt(e.target.value))}
              className="bg-gray-700/50 text-white px-3 py-2 rounded-lg border border-gray-600 text-sm"
            >
              <option value="">Tutte le priorità</option>
              <option value="0">Normale</option>
              <option value="1">Importante</option>
              <option value="2">Urgente</option>
            </select>
            {(filterCategory || filterPriority !== null) && (
              <button
                onClick={() => { setFilterCategory(''); setFilterPriority(null); }}
                className="text-cyan-400 text-sm hover:text-cyan-300 transition-colors"
              >
                Resetta filtri
              </button>
            )}
          </div>
        </div>

        {/* Messages List */}
        {loading ? (
          <div className="text-center text-cyan-400 py-12">Caricamento messaggi...</div>
        ) : filteredMessages.length === 0 ? (
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-12 border border-cyan-500/30 text-center">
            <svg className="w-16 h-16 mx-auto text-gray-600 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M19 20H5a2 2 0 01-2-2V6a2 2 0 012-2h10a2 2 0 012 2v1m2 13a2 2 0 01-2-2V7m2 13a2 2 0 002-2V9a2 2 0 00-2-2h-2m-4-3H9M7 16h6M7 8h6v4H7V8z" />
            </svg>
            <p className="text-gray-400 text-lg">Nessun messaggio in bacheca</p>
            <p className="text-gray-500 text-sm mt-1">Crea il primo messaggio per comunicare con i tuoi dipendenti</p>
          </div>
        ) : (
          <div className="space-y-4">
            {filteredMessages.map(message => (
              <div
                key={message.id}
                className={`bg-gray-800/50 backdrop-blur-sm rounded-xl p-6 border transition-all hover:border-cyan-500/50 ${
                  message.isPinned ? 'border-yellow-500/30' : 'border-cyan-500/30'
                }`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-3 mb-2 flex-wrap">
                      {message.isPinned && (
                        <span className="text-yellow-400" title="Fissato in alto">
                          <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                            <path d="M5 5a2 2 0 012-2h6a2 2 0 012 2v2H5V5zm0 4h10l-1.5 7.5a1 1 0 01-1 .5h-5a1 1 0 01-1-.5L5 9z" />
                          </svg>
                        </span>
                      )}
                      <h3 className="text-xl font-bold text-white truncate">{message.title}</h3>
                      {getPriorityBadge(message.priority)}
                      {getCategoryBadge(message.category)}
                    </div>
                    <p className="text-gray-300 whitespace-pre-wrap mb-3">{message.content}</p>
                    <div className="flex items-center gap-4 text-sm text-gray-500">
                      <span>Di: {message.authorName}</span>
                      <span>
                        {new Date(message.createdAt).toLocaleDateString('it-IT', {
                          day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
                        })}
                      </span>
                      {message.updatedAt && <span>(modificato)</span>}
                      <span className="text-cyan-400">
                        <svg className="w-4 h-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                        {message.readCount} letture
                      </span>
                      {message.expiresAt && (
                        <span className="text-orange-400">
                          Scade: {new Date(message.expiresAt).toLocaleDateString('it-IT')}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2 flex-shrink-0">
                    <button
                      onClick={() => openEditModal(message)}
                      className="p-2 text-gray-400 hover:text-cyan-400 hover:bg-gray-700/50 rounded-lg transition-all"
                      title="Modifica"
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button
                      onClick={() => handleDelete(message.id)}
                      className="p-2 text-gray-400 hover:text-red-400 hover:bg-gray-700/50 rounded-lg transition-all"
                      title="Elimina"
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Create Modal */}
        {showCreateModal && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50">
            <div className="bg-gray-800 rounded-xl p-6 max-w-lg w-full border border-cyan-500/30 max-h-[90vh] overflow-y-auto">
              <h2 className="text-2xl font-bold text-cyan-400 mb-6">Nuovo Messaggio</h2>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-1">Titolo *</label>
                  <input
                    type="text"
                    value={formTitle}
                    onChange={(e) => setFormTitle(e.target.value)}
                    className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    placeholder="Titolo del messaggio"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-1">Contenuto *</label>
                  <textarea
                    value={formContent}
                    onChange={(e) => setFormContent(e.target.value)}
                    rows={5}
                    className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none resize-none"
                    placeholder="Scrivi il contenuto del messaggio..."
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-1">Priorità</label>
                    <select
                      value={formPriority}
                      onChange={(e) => setFormPriority(parseInt(e.target.value))}
                      className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    >
                      <option value={0}>Normale</option>
                      <option value={1}>Importante</option>
                      <option value={2}>Urgente</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-1">Categoria</label>
                    <select
                      value={formCategory}
                      onChange={(e) => setFormCategory(e.target.value)}
                      className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    >
                      {CATEGORIES.map(cat => (
                        <option key={cat} value={cat}>{cat}</option>
                      ))}
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-1">Data Scadenza (opzionale)</label>
                  <input
                    type="date"
                    value={formExpiresAt}
                    onChange={(e) => setFormExpiresAt(e.target.value)}
                    className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                  />
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isPinned"
                    checked={formIsPinned}
                    onChange={(e) => setFormIsPinned(e.target.checked)}
                    className="w-4 h-4 rounded border-gray-600 bg-gray-700 text-cyan-500 focus:ring-cyan-500"
                  />
                  <label htmlFor="isPinned" className="text-sm text-gray-300">Fissa in alto nella bacheca</label>
                </div>
              </div>
              <div className="flex gap-3 mt-6">
                <button
                  onClick={handleCreate}
                  disabled={!formTitle.trim() || !formContent.trim()}
                  className="flex-1 bg-gradient-to-r from-cyan-500 to-blue-500 text-white px-4 py-2 rounded-lg font-semibold disabled:opacity-50 disabled:cursor-not-allowed hover:from-cyan-600 hover:to-blue-600 transition-all"
                >
                  Pubblica
                </button>
                <button
                  onClick={() => setShowCreateModal(false)}
                  className="px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Annulla
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Edit Modal */}
        {showEditModal && selectedMessage && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50">
            <div className="bg-gray-800 rounded-xl p-6 max-w-lg w-full border border-cyan-500/30 max-h-[90vh] overflow-y-auto">
              <h2 className="text-2xl font-bold text-cyan-400 mb-6">Modifica Messaggio</h2>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-1">Titolo *</label>
                  <input
                    type="text"
                    value={formTitle}
                    onChange={(e) => setFormTitle(e.target.value)}
                    className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-1">Contenuto *</label>
                  <textarea
                    value={formContent}
                    onChange={(e) => setFormContent(e.target.value)}
                    rows={5}
                    className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none resize-none"
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-1">Priorità</label>
                    <select
                      value={formPriority}
                      onChange={(e) => setFormPriority(parseInt(e.target.value))}
                      className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    >
                      <option value={0}>Normale</option>
                      <option value={1}>Importante</option>
                      <option value={2}>Urgente</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-300 mb-1">Categoria</label>
                    <select
                      value={formCategory}
                      onChange={(e) => setFormCategory(e.target.value)}
                      className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                    >
                      {CATEGORIES.map(cat => (
                        <option key={cat} value={cat}>{cat}</option>
                      ))}
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-1">Data Scadenza (opzionale)</label>
                  <input
                    type="date"
                    value={formExpiresAt}
                    onChange={(e) => setFormExpiresAt(e.target.value)}
                    className="w-full bg-gray-700/50 text-white px-4 py-2 rounded-lg border border-gray-600 focus:border-cyan-500 focus:outline-none"
                  />
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isPinnedEdit"
                    checked={formIsPinned}
                    onChange={(e) => setFormIsPinned(e.target.checked)}
                    className="w-4 h-4 rounded border-gray-600 bg-gray-700 text-cyan-500 focus:ring-cyan-500"
                  />
                  <label htmlFor="isPinnedEdit" className="text-sm text-gray-300">Fissa in alto nella bacheca</label>
                </div>
              </div>
              <div className="flex gap-3 mt-6">
                <button
                  onClick={handleUpdate}
                  disabled={!formTitle.trim() || !formContent.trim()}
                  className="flex-1 bg-gradient-to-r from-cyan-500 to-blue-500 text-white px-4 py-2 rounded-lg font-semibold disabled:opacity-50 disabled:cursor-not-allowed hover:from-cyan-600 hover:to-blue-600 transition-all"
                >
                  Salva Modifiche
                </button>
                <button
                  onClick={() => { setShowEditModal(false); setSelectedMessage(null); }}
                  className="px-4 py-2 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all"
                >
                  Annulla
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AppLayout>
  );
}
