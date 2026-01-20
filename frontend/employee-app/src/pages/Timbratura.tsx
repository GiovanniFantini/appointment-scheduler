import { useState, useEffect } from 'react';
import axios from '../lib/axios';
import { CurrentStatusResponse, TimbratureResponse, CheckInRequest, CheckOutRequest, AnomalyReason, WellbeingStats } from '../types/timbratura';
import AppLayout from '../components/layout/AppLayout';

interface TimbraturaProps {
  user: any;
  onLogout: () => void;
}

export default function Timbratura({ user, onLogout }: TimbraturaProps) {
  const [status, setStatus] = useState<CurrentStatusResponse | null>(null);
  const [wellbeing, setWellbeing] = useState<WellbeingStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [lastResponse, setLastResponse] = useState<TimbratureResponse | null>(null);
  const [showAnomalyModal, setShowAnomalyModal] = useState(false);
  const [currentTime, setCurrentTime] = useState(new Date());

  useEffect(() => {
    fetchStatus();
    fetchWellbeingStats();

    // Aggiorna l'orologio ogni secondo
    const timer = setInterval(() => {
      setCurrentTime(new Date());
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const fetchStatus = async () => {
    try {
      const response = await axios.get('/api/timbrature/status');
      setStatus(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento stato:', error);
    }
  };

  const fetchWellbeingStats = async () => {
    try {
      const response = await axios.get('/api/timbrature/wellbeing');
      setWellbeing(response.data);
    } catch (error: any) {
      console.error('Errore nel caricamento statistiche benessere:', error);
    }
  };

  const handleCheckIn = async () => {
    if (!status?.currentShift) {
      alert('Nessun turno programmato per oggi');
      return;
    }

    try {
      setLoading(true);
      const request: CheckInRequest = {
        shiftId: status.currentShift.id,
      };

      const response = await axios.post('/api/timbrature/check-in', request);
      setLastResponse(response.data);

      if (response.data.hasAnomaly) {
        setShowAnomalyModal(true);
      }

      await fetchStatus();
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore durante il check-in');
    } finally {
      setLoading(false);
    }
  };

  const handleCheckOut = async () => {
    if (!status?.currentShift) {
      return;
    }

    try {
      setLoading(true);
      const request: CheckOutRequest = {
        shiftId: status.currentShift.id,
      };

      const response = await axios.post('/api/timbrature/check-out', request);
      setLastResponse(response.data);

      if (response.data.hasAnomaly || response.data.hasOvertime) {
        // Mostra messaggio
        setTimeout(() => setLastResponse(null), 5000);
      }

      await fetchStatus();
      await fetchWellbeingStats();
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore durante il check-out');
    } finally {
      setLoading(false);
    }
  };

  const handleResolveAnomaly = async (reason: AnomalyReason) => {
    if (!lastResponse?.anomalyId) return;

    try {
      await axios.post('/api/timbrature/anomaly/resolve', {
        anomalyId: lastResponse.anomalyId,
        reason,
      });

      setShowAnomalyModal(false);
      setLastResponse(null);
    } catch (error: any) {
      alert(error.response?.data?.message || 'Errore durante la risoluzione');
    }
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  };

  const formatDuration = (startTime?: string) => {
    if (!startTime) return '00:00:00';
    const start = new Date(startTime);
    const now = new Date();
    const diff = now.getTime() - start.getTime();
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  };

  const getButtonText = () => {
    if (!status) return 'Caricamento...';
    if (status.isOnBreak) return 'IN PAUSA';
    if (status.isCheckedIn) return 'ESCI';
    return 'ENTRA';
  };

  const getButtonAction = () => {
    if (!status) return;
    if (status.isOnBreak) return; // Non fare nulla se in pausa
    if (status.isCheckedIn) {
      handleCheckOut();
    } else {
      handleCheckIn();
    }
  };

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Timbratura">
      <div className="max-w-2xl mx-auto space-y-6">

        {/* Header con orologio */}
        <div className="text-center py-8">
          <h1 className="text-6xl font-bold text-gray-800 mb-2">
            {formatTime(currentTime)}
          </h1>
          <p className="text-xl text-gray-600">
            {currentTime.toLocaleDateString('it-IT', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
          </p>
        </div>

        {/* Pulsante principale auto-rilevato */}
        <div className="flex justify-center">
          <button
            onClick={getButtonAction}
            disabled={loading || status?.isOnBreak}
            className={`
              w-64 h-64 rounded-full text-4xl font-bold shadow-2xl transform transition-all duration-300
              ${status?.isCheckedIn
                ? 'bg-gradient-to-br from-red-500 to-red-600 hover:from-red-600 hover:to-red-700 text-white'
                : 'bg-gradient-to-br from-green-500 to-green-600 hover:from-green-600 hover:to-green-700 text-white'
              }
              ${status?.isOnBreak ? 'opacity-50 cursor-not-allowed' : 'hover:scale-105 active:scale-95'}
              disabled:opacity-50 disabled:cursor-not-allowed
            `}
          >
            {getButtonText()}
          </button>
        </div>

        {/* Timer visivo + stato corrente */}
        {status && (
          <div className="bg-white rounded-2xl shadow-lg p-8">
            <div className="text-center mb-6">
              <div className="text-3xl font-bold text-gray-800 mb-2">
                {status.statusMessage}
              </div>
              {status.isCheckedIn && (
                <div className="text-5xl font-mono text-blue-600 mb-4">
                  {formatDuration(status.checkInTime)}
                </div>
              )}
              {status.suggestedAction && (
                <div className="inline-block bg-blue-100 text-blue-800 px-6 py-3 rounded-full text-lg">
                  {status.suggestedAction}
                </div>
              )}
            </div>

            {/* Ore lavorate oggi/settimana */}
            <div className="grid grid-cols-2 gap-4 mt-6">
              <div className="bg-gradient-to-br from-blue-50 to-blue-100 rounded-xl p-4 text-center">
                <div className="text-gray-600 text-sm mb-1">Oggi</div>
                <div className="text-3xl font-bold text-blue-600">
                  {status.totalWorkedHoursToday.toFixed(1)}h
                </div>
              </div>
              <div className="bg-gradient-to-br from-indigo-50 to-indigo-100 rounded-xl p-4 text-center">
                <div className="text-gray-600 text-sm mb-1">Questa settimana</div>
                <div className="text-3xl font-bold text-indigo-600">
                  {status.totalWorkedHoursThisWeek.toFixed(1)}h
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Messaggio di risposta (conferma, contesto, prossimi passi) */}
        {lastResponse && (
          <div className={`rounded-2xl shadow-lg p-6 ${lastResponse.success ? 'bg-green-50 border-2 border-green-200' : 'bg-red-50 border-2 border-red-200'}`}>
            <div className="space-y-3">
              <div className="text-2xl font-bold text-gray-800">
                {lastResponse.message}
              </div>
              {lastResponse.context && (
                <div className="text-lg text-gray-700">
                  📊 {lastResponse.context}
                </div>
              )}
              {lastResponse.nextSteps && (
                <div className="text-lg text-gray-700">
                  💡 {lastResponse.nextSteps}
                </div>
              )}
              {lastResponse.empatheticMessage && (
                <div className="text-lg text-blue-700 bg-blue-100 rounded-lg p-4 mt-4">
                  {lastResponse.empatheticMessage}
                </div>
              )}
            </div>
          </div>
        )}

        {/* Alert benessere */}
        {wellbeing?.hasWellbeingAlert && (
          <div className="bg-amber-50 border-2 border-amber-300 rounded-2xl p-6">
            <div className="flex items-start gap-4">
              <div className="text-4xl">💚</div>
              <div>
                <div className="text-lg font-semibold text-amber-800 mb-2">
                  Alert Benessere
                </div>
                <div className="text-amber-700">
                  {wellbeing.wellbeingMessage}
                </div>
                <div className="mt-3 text-sm text-amber-600">
                  Ore questa settimana: <strong>{wellbeing.hoursThisWeek.toFixed(1)}h</strong>
                  {wellbeing.overtimeThisWeek > 0 && (
                    <span className="ml-2">
                      (Straordinari: <strong>{wellbeing.overtimeThisWeek.toFixed(1)}h</strong>)
                    </span>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Turno di oggi */}
        {status?.currentShift && (
          <div className="bg-white rounded-2xl shadow-lg p-6">
            <h3 className="text-xl font-semibold text-gray-800 mb-4">Turno di oggi</h3>
            <div className="space-y-2 text-gray-700">
              <div className="flex justify-between">
                <span>Orario:</span>
                <span className="font-semibold">{status.currentShift.startTime} - {status.currentShift.endTime}</span>
              </div>
              <div className="flex justify-between">
                <span>Durata:</span>
                <span className="font-semibold">{status.currentShift.totalHours}h</span>
              </div>
              {status.currentShift.breakDurationMinutes > 0 && (
                <div className="flex justify-between">
                  <span>Pausa prevista:</span>
                  <span className="font-semibold">{status.currentShift.breakDurationMinutes} min</span>
                </div>
              )}
            </div>
          </div>
        )}

      </div>

      {/* Modal anomalia - Opzioni empatiche */}
      {showAnomalyModal && lastResponse?.hasAnomaly && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-8">
            <h3 className="text-2xl font-bold text-gray-800 mb-4">
              C'è stato qualche imprevisto?
            </h3>
            <p className="text-gray-600 mb-6">
              {lastResponse.empatheticMessage}
            </p>
            <div className="space-y-3">
              {lastResponse.quickResolutionOptions?.map((option) => (
                <button
                  key={option}
                  onClick={() => handleResolveAnomaly(getReasonFromOption(option))}
                  className="w-full bg-blue-50 hover:bg-blue-100 text-blue-800 font-semibold py-3 px-6 rounded-xl transition-colors"
                >
                  {option}
                </button>
              ))}
              <button
                onClick={() => setShowAnomalyModal(false)}
                className="w-full bg-gray-100 hover:bg-gray-200 text-gray-800 font-semibold py-3 px-6 rounded-xl transition-colors"
              >
                Chiudi
              </button>
            </div>
          </div>
        </div>
      )}
    </AppLayout>
  );
}

function getReasonFromOption(option: string): AnomalyReason {
  switch (option) {
    case 'Traffico':
      return AnomalyReason.Traffic;
    case 'Permesso':
      return AnomalyReason.AuthorizedLeave;
    case 'Recupero':
      return AnomalyReason.TimeRecovery;
    default:
      return AnomalyReason.Other;
  }
}
