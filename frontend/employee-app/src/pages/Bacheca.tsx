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
  isReadByCurrentUser: boolean;
}

interface BachecaProps {
  user: any;
  onLogout: () => void;
}

export default function Bacheca({ user, onLogout }: BachecaProps) {
  const [messages, setMessages] = useState<BoardMessage[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedMessage, setSelectedMessage] = useState<BoardMessage | null>(null);
  const [filterCategory, setFilterCategory] = useState<string>('');
  const [showOnlyUnread, setShowOnlyUnread] = useState(false);

  useEffect(() => {
    fetchMessages();
  }, []);

  const fetchMessages = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/boardmessages/employee');
      setMessages(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento messaggi:', error);
    } finally {
      setLoading(false);
    }
  };

  const markAsRead = async (messageId: number) => {
    try {
      await axios.post(`/boardmessages/${messageId}/read`);
      setMessages(prev =>
        prev.map(m => m.id === messageId ? { ...m, isReadByCurrentUser: true } : m)
      );
    } catch (error) {
      console.error('Errore nel segnare come letto:', error);
    }
  };

  const openMessage = (message: BoardMessage) => {
    setSelectedMessage(message);
    if (!message.isReadByCurrentUser) {
      markAsRead(message.id);
    }
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

  const unreadCount = messages.filter(m => !m.isReadByCurrentUser).length;
  const categories = [...new Set(messages.map(m => m.category))];

  const filteredMessages = messages.filter(m => {
    if (filterCategory && m.category !== filterCategory) return false;
    if (showOnlyUnread && m.isReadByCurrentUser) return false;
    return true;
  });

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Bacheca">
      <div className="max-w-5xl mx-auto">
        {/* Header */}
        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-6 mb-6 border border-cyan-500/30">
          <h1 className="text-3xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 to-blue-500 mb-2">
            Bacheca Aziendale
          </h1>
          <p className="text-gray-400">Comunicazioni e avvisi dall'azienda</p>
        </div>

        {/* Stats */}
        <div className="grid md:grid-cols-3 gap-4 mb-6">
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30">
            <div className="text-gray-400 text-sm mb-1">Messaggi Totali</div>
            <div className="text-3xl font-bold text-cyan-400">{messages.length}</div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30">
            <div className="text-gray-400 text-sm mb-1">Da Leggere</div>
            <div className="text-3xl font-bold text-yellow-400">{unreadCount}</div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-cyan-500/30">
            <div className="text-gray-400 text-sm mb-1">Urgenti</div>
            <div className="text-3xl font-bold text-red-400">
              {messages.filter(m => m.priority === 2 && !m.isReadByCurrentUser).length}
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
              {categories.map(cat => (
                <option key={cat} value={cat}>{cat}</option>
              ))}
            </select>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={showOnlyUnread}
                onChange={(e) => setShowOnlyUnread(e.target.checked)}
                className="w-4 h-4 rounded border-gray-600 bg-gray-700 text-cyan-500 focus:ring-cyan-500"
              />
              <span className="text-sm text-gray-300">Solo non letti</span>
            </label>
          </div>
        </div>

        {/* Messages List */}
        {loading ? (
          <div className="text-center text-cyan-400 py-12">Caricamento messaggi...</div>
        ) : filteredMessages.length === 0 ? (
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-12 border border-cyan-500/30 text-center">
            <svg className="w-16 h-16 mx-auto text-gray-600 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
            </svg>
            <p className="text-gray-400 text-lg">
              {showOnlyUnread ? 'Nessun messaggio da leggere' : 'Nessun messaggio in bacheca'}
            </p>
          </div>
        ) : (
          <div className="space-y-3">
            {filteredMessages.map(message => (
              <div
                key={message.id}
                onClick={() => openMessage(message)}
                className={`bg-gray-800/50 backdrop-blur-sm rounded-xl p-5 border cursor-pointer transition-all hover:border-cyan-500/50 ${
                  message.isPinned
                    ? 'border-yellow-500/30'
                    : !message.isReadByCurrentUser
                    ? 'border-cyan-500/50 bg-cyan-500/5'
                    : 'border-cyan-500/20'
                }`}
              >
                <div className="flex items-start gap-4">
                  {/* Unread indicator */}
                  <div className="pt-1 flex-shrink-0">
                    {!message.isReadByCurrentUser ? (
                      <div className="w-3 h-3 bg-cyan-400 rounded-full animate-pulse" />
                    ) : (
                      <div className="w-3 h-3 bg-gray-600 rounded-full" />
                    )}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-3 mb-2 flex-wrap">
                      {message.isPinned && (
                        <span className="text-yellow-400" title="Fissato">
                          <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                            <path d="M5 5a2 2 0 012-2h6a2 2 0 012 2v2H5V5zm0 4h10l-1.5 7.5a1 1 0 01-1 .5h-5a1 1 0 01-1-.5L5 9z" />
                          </svg>
                        </span>
                      )}
                      <h3 className={`text-lg font-bold truncate ${!message.isReadByCurrentUser ? 'text-white' : 'text-gray-300'}`}>
                        {message.title}
                      </h3>
                      {getPriorityBadge(message.priority)}
                      {getCategoryBadge(message.category)}
                    </div>
                    <p className="text-gray-400 line-clamp-2 mb-2">{message.content}</p>
                    <div className="flex items-center gap-4 text-xs text-gray-500">
                      <span>{message.authorName}</span>
                      <span>
                        {new Date(message.createdAt).toLocaleDateString('it-IT', {
                          day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
                        })}
                      </span>
                    </div>
                  </div>

                  <div className="flex-shrink-0 text-gray-500">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Message Detail Modal */}
        {selectedMessage && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50">
            <div className="bg-gray-800 rounded-xl p-6 max-w-lg w-full border border-cyan-500/30 max-h-[90vh] overflow-y-auto">
              <div className="flex items-center gap-3 mb-4 flex-wrap">
                {selectedMessage.isPinned && (
                  <span className="text-yellow-400">
                    <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                      <path d="M5 5a2 2 0 012-2h6a2 2 0 012 2v2H5V5zm0 4h10l-1.5 7.5a1 1 0 01-1 .5h-5a1 1 0 01-1-.5L5 9z" />
                    </svg>
                  </span>
                )}
                {getPriorityBadge(selectedMessage.priority)}
                {getCategoryBadge(selectedMessage.category)}
              </div>

              <h2 className="text-2xl font-bold text-cyan-400 mb-4">{selectedMessage.title}</h2>

              <div className="text-gray-300 whitespace-pre-wrap mb-6 leading-relaxed">
                {selectedMessage.content}
              </div>

              <div className="border-t border-gray-700 pt-4 space-y-2 text-sm text-gray-500">
                <div className="flex items-center gap-2">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                  <span>Pubblicato da: <strong className="text-gray-300">{selectedMessage.authorName}</strong></span>
                </div>
                <div className="flex items-center gap-2">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  <span>
                    {new Date(selectedMessage.createdAt).toLocaleDateString('it-IT', {
                      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
                      hour: '2-digit', minute: '2-digit'
                    })}
                  </span>
                </div>
                {selectedMessage.expiresAt && (
                  <div className="flex items-center gap-2 text-orange-400">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <span>Scade il {new Date(selectedMessage.expiresAt).toLocaleDateString('it-IT')}</span>
                  </div>
                )}
              </div>

              <button
                onClick={() => setSelectedMessage(null)}
                className="w-full mt-6 px-4 py-3 bg-gray-700/50 text-gray-300 rounded-lg hover:bg-gray-700 transition-all font-medium"
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
