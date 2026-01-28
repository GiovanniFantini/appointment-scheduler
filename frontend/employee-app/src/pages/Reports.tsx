import { useState, useEffect } from 'react'
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, LineElement, PointElement, ArcElement, Title, Tooltip, Legend, Filler } from 'chart.js'
import { Bar, Doughnut } from 'react-chartjs-2'
import { saveAs } from 'file-saver'
import AppLayout from '../components/layout/AppLayout'
import apiClient from '../lib/axios'

// Registra i componenti Chart.js
ChartJS.register(CategoryScale, LinearScale, BarElement, LineElement, PointElement, ArcElement, Title, Tooltip, Legend, Filler)

interface ReportsProps {
  user: any
  onLogout: () => void
}

interface DashboardStats {
  generatedAt: string
  todayShifts: number
  todayEmployeesPresent: number
  todayEmployeesAbsent: number
  todayPendingAnomalies: number
  todayBookings: number
  weekHoursWorked: number
  weekHoursScheduled: number
  weekAnomalies: number
  weekLeaveRequests: number
  monthHoursWorked: number
  monthAttendanceRate: number
  monthTotalShifts: number
  hoursWorkedTrend: number
  attendanceRateTrend: number
  bookingsTrend: number
  upcomingEvents: UpcomingEvent[]
}

interface UpcomingEvent {
  eventType: string
  date: string
  title: string
  description: string
}

interface AttendanceReport {
  employeeId: number
  employeeName: string
  email: string
  periodStart: string
  periodEnd: string
  totalShiftsAssigned: number
  shiftsCompleted: number
  shiftsMissed: number
  attendanceRate: number
  totalHoursScheduled: number
  totalHoursWorked: number
  totalOvertimeHours: number
  totalAnomalies: number
  lateArrivals: number
  earlyDepartures: number
  unauthorizedAbsences: number
  leaveRequestsApproved: number
  leaveDaysTaken: number
  leaveDaysRemaining: number
  dailyDetails: DailyDetail[]
}

interface DailyDetail {
  date: string
  status: string
  scheduledStart?: string
  scheduledEnd?: string
  actualCheckIn?: string
  actualCheckOut?: string
  hoursScheduled: number
  hoursWorked: number
  overtimeHours: number
  hasAnomaly: boolean
  anomalyType?: string
  notes?: string
}

function Reports({ user, onLogout }: ReportsProps) {
  const [dashboardStats, setDashboardStats] = useState<DashboardStats | null>(null)
  const [attendanceReport, setAttendanceReport] = useState<AttendanceReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [activeTab, setActiveTab] = useState<'dashboard' | 'attendance'>('dashboard')
  const [dateRange, setDateRange] = useState({
    startDate: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0]
  })

  useEffect(() => {
    loadDashboardStats()
  }, [])

  useEffect(() => {
    if (activeTab === 'attendance') {
      loadAttendanceReport()
    }
  }, [dateRange, activeTab])

  const loadDashboardStats = async () => {
    try {
      const response = await apiClient.get('/reports/dashboard/employee')
      setDashboardStats(response.data)
    } catch (error) {
      console.error('Errore caricamento dashboard stats:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadAttendanceReport = async () => {
    try {
      setLoading(true)
      const response = await apiClient.get('/reports/attendance/my', {
        params: { startDate: dateRange.startDate, endDate: dateRange.endDate, includeDetails: true }
      })
      setAttendanceReport(response.data)
    } catch (error) {
      console.error('Errore caricamento report presenze:', error)
    } finally {
      setLoading(false)
    }
  }

  const exportReport = async (format: 'pdf' | 'excel' | 'csv') => {
    setExporting(true)
    try {
      const response = await apiClient.post(`/reports/export/${format}`, {
        reportType: 'attendance',
        format,
        startDate: dateRange.startDate,
        endDate: dateRange.endDate,
        includeDetails: true
      }, { responseType: 'blob' })

      const extension = format === 'excel' ? 'xlsx' : format
      const mimeType = format === 'pdf' ? 'application/pdf' :
                       format === 'excel' ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' :
                       'text/csv'
      const blob = new Blob([response.data], { type: mimeType })
      saveAs(blob, `mio_report_presenze_${dateRange.startDate}_${dateRange.endDate}.${extension}`)
    } catch (error) {
      console.error('Errore export:', error)
      alert('Errore durante l\'export del report')
    } finally {
      setExporting(false)
    }
  }

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('it-IT', { day: '2-digit', month: '2-digit', year: 'numeric' })
  }

  const formatTime = (timeStr?: string) => {
    if (!timeStr) return '-'
    if (timeStr.includes('T')) {
      return new Date(timeStr).toLocaleTimeString('it-IT', { hour: '2-digit', minute: '2-digit' })
    }
    return timeStr.substring(0, 5)
  }

  // Chart data
  const dailyHoursChartData = attendanceReport ? {
    labels: attendanceReport.dailyDetails.slice(-14).map(d => {
      const date = new Date(d.date)
      return `${date.getDate()}/${date.getMonth() + 1}`
    }),
    datasets: [
      {
        label: 'Ore Lavorate',
        data: attendanceReport.dailyDetails.slice(-14).map(d => d.hoursWorked),
        backgroundColor: 'rgba(59, 130, 246, 0.5)',
        borderColor: 'rgb(59, 130, 246)',
        borderWidth: 2
      },
      {
        label: 'Ore Programmate',
        data: attendanceReport.dailyDetails.slice(-14).map(d => d.hoursScheduled),
        backgroundColor: 'rgba(147, 51, 234, 0.3)',
        borderColor: 'rgb(147, 51, 234)',
        borderWidth: 2
      }
    ]
  } : null

  const statusDistributionData = attendanceReport ? {
    labels: ['Presenze', 'Assenze', 'Ferie'],
    datasets: [{
      data: [
        attendanceReport.dailyDetails.filter(d => d.status === 'Present').length,
        attendanceReport.dailyDetails.filter(d => d.status === 'Absent' || d.status === 'Scheduled').length,
        attendanceReport.dailyDetails.filter(d => d.status === 'Leave').length
      ],
      backgroundColor: ['rgba(16, 185, 129, 0.7)', 'rgba(239, 68, 68, 0.7)', 'rgba(147, 51, 234, 0.7)'],
      borderWidth: 0
    }]
  } : null

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { labels: { color: '#9CA3AF' } }
    },
    scales: {
      x: { ticks: { color: '#9CA3AF' }, grid: { color: 'rgba(255,255,255,0.1)' } },
      y: { ticks: { color: '#9CA3AF' }, grid: { color: 'rgba(255,255,255,0.1)' } }
    }
  }

  const doughnutOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' as const, labels: { color: '#9CA3AF' } } }
  }

  return (
    <AppLayout user={user} onLogout={onLogout}>
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
          <div>
            <h1 className="text-4xl font-bold gradient-text mb-2">I Miei Report</h1>
            <p className="text-gray-400">Visualizza le tue presenze e statistiche personali</p>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => exportReport('pdf')}
              disabled={exporting}
              className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg flex items-center gap-2 disabled:opacity-50"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              PDF
            </button>
            <button
              onClick={() => exportReport('excel')}
              disabled={exporting}
              className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg flex items-center gap-2 disabled:opacity-50"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              Excel
            </button>
            <button
              onClick={() => exportReport('csv')}
              disabled={exporting}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 disabled:opacity-50"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
              CSV
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex gap-2 mb-6">
          <button
            onClick={() => setActiveTab('dashboard')}
            className={`px-6 py-3 rounded-xl font-semibold transition-all ${
              activeTab === 'dashboard'
                ? 'bg-gradient-to-r from-neon-blue to-neon-purple text-white'
                : 'glass-card text-gray-400 hover:text-white'
            }`}
          >
            Dashboard
          </button>
          <button
            onClick={() => setActiveTab('attendance')}
            className={`px-6 py-3 rounded-xl font-semibold transition-all ${
              activeTab === 'attendance'
                ? 'bg-gradient-to-r from-neon-blue to-neon-purple text-white'
                : 'glass-card text-gray-400 hover:text-white'
            }`}
          >
            Dettaglio Presenze
          </button>
        </div>

        {/* Date Range Picker */}
        {activeTab === 'attendance' && (
          <div className="glass-card p-4 rounded-xl mb-6 flex flex-wrap gap-4 items-center">
            <div>
              <label className="text-gray-400 text-sm">Data Inizio</label>
              <input
                type="date"
                value={dateRange.startDate}
                onChange={(e) => setDateRange(prev => ({ ...prev, startDate: e.target.value }))}
                className="ml-2 px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
              />
            </div>
            <div>
              <label className="text-gray-400 text-sm">Data Fine</label>
              <input
                type="date"
                value={dateRange.endDate}
                onChange={(e) => setDateRange(prev => ({ ...prev, endDate: e.target.value }))}
                className="ml-2 px-3 py-2 bg-white/10 border border-white/20 rounded-lg text-white"
              />
            </div>
            <button
              onClick={loadAttendanceReport}
              className="px-4 py-2 bg-neon-blue hover:bg-neon-blue/80 text-white rounded-lg"
            >
              Aggiorna
            </button>
          </div>
        )}

        {loading ? (
          <div className="flex justify-center py-20">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-neon-blue"></div>
          </div>
        ) : (
          <>
            {/* Dashboard Tab */}
            {activeTab === 'dashboard' && dashboardStats && (
              <div className="space-y-6">
                {/* KPI Cards */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-400">I Miei Turni Oggi</span>
                      <div className="bg-neon-blue/20 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-white">{dashboardStats.todayShifts}</p>
                  </div>

                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-400">Ore Settimana</span>
                      <div className="bg-neon-purple/20 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-neon-purple" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-white">{dashboardStats.weekHoursWorked}h</p>
                    <p className="text-sm text-gray-400">su {dashboardStats.weekHoursScheduled}h programmate</p>
                  </div>

                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-400">Ore Mese</span>
                      <div className="bg-neon-cyan/20 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-white">{dashboardStats.monthHoursWorked}h</p>
                    <p className="text-sm text-gray-400">{dashboardStats.monthTotalShifts} turni completati</p>
                  </div>

                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-400">Tasso Presenza</span>
                      <div className="bg-neon-green/20 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-neon-green">{dashboardStats.monthAttendanceRate}%</p>
                  </div>
                </div>

                {/* Alert Anomalie */}
                {dashboardStats.todayPendingAnomalies > 0 && (
                  <div className="glass-card p-4 rounded-xl border border-red-500/30 bg-red-500/10">
                    <div className="flex items-center gap-3">
                      <svg className="w-6 h-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                      </svg>
                      <p className="text-red-400">
                        Hai <strong>{dashboardStats.todayPendingAnomalies}</strong> anomalie pendenti da verificare
                      </p>
                    </div>
                  </div>
                )}

                {/* Upcoming Events */}
                {dashboardStats.upcomingEvents.length > 0 && (
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-xl font-bold text-white mb-4">Prossimi Turni</h3>
                    <div className="space-y-3">
                      {dashboardStats.upcomingEvents.map((event, idx) => (
                        <div key={idx} className="flex items-center gap-4 p-3 bg-white/5 rounded-xl">
                          <div className="bg-neon-blue/20 p-2 rounded-lg">
                            <svg className="w-5 h-5 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                          </div>
                          <div className="flex-1">
                            <p className="text-white font-medium">{event.title}</p>
                            <p className="text-sm text-gray-400">{formatDate(event.date)}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Attendance Tab */}
            {activeTab === 'attendance' && attendanceReport && (
              <div className="space-y-6">
                {/* Summary Stats */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Turni Assegnati</p>
                    <p className="text-3xl font-bold text-white">{attendanceReport.totalShiftsAssigned}</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Turni Completati</p>
                    <p className="text-3xl font-bold text-neon-green">{attendanceReport.shiftsCompleted}</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Ore Lavorate</p>
                    <p className="text-3xl font-bold text-white">{attendanceReport.totalHoursWorked}h</p>
                    <p className="text-sm text-gray-500">su {attendanceReport.totalHoursScheduled}h</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Straordinari</p>
                    <p className="text-3xl font-bold text-neon-purple">{attendanceReport.totalOvertimeHours}h</p>
                  </div>
                </div>

                {/* Charts */}
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                  <div className="glass-card p-6 rounded-2xl border border-white/10 lg:col-span-2">
                    <h3 className="text-xl font-bold text-white mb-4">Ore Giornaliere (ultimi 14 giorni)</h3>
                    <div style={{ height: '300px' }}>
                      {dailyHoursChartData && <Bar data={dailyHoursChartData} options={chartOptions} />}
                    </div>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-xl font-bold text-white mb-4">Distribuzione Presenze</h3>
                    <div style={{ height: '250px' }}>
                      {statusDistributionData && <Doughnut data={statusDistributionData} options={doughnutOptions} />}
                    </div>
                  </div>
                </div>

                {/* Anomalies & Leave */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-lg font-bold text-white mb-4">Anomalie Rilevate</h3>
                    <div className="space-y-3">
                      <div className="flex justify-between items-center p-3 bg-white/5 rounded-lg">
                        <span className="text-gray-400">Ritardi</span>
                        <span className="text-yellow-400 font-bold">{attendanceReport.lateArrivals}</span>
                      </div>
                      <div className="flex justify-between items-center p-3 bg-white/5 rounded-lg">
                        <span className="text-gray-400">Uscite anticipate</span>
                        <span className="text-yellow-400 font-bold">{attendanceReport.earlyDepartures}</span>
                      </div>
                      <div className="flex justify-between items-center p-3 bg-white/5 rounded-lg">
                        <span className="text-gray-400">Assenze non autorizzate</span>
                        <span className="text-red-400 font-bold">{attendanceReport.unauthorizedAbsences}</span>
                      </div>
                    </div>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-lg font-bold text-white mb-4">Ferie e Permessi</h3>
                    <div className="space-y-3">
                      <div className="flex justify-between items-center p-3 bg-white/5 rounded-lg">
                        <span className="text-gray-400">Richieste approvate</span>
                        <span className="text-neon-green font-bold">{attendanceReport.leaveRequestsApproved}</span>
                      </div>
                      <div className="flex justify-between items-center p-3 bg-white/5 rounded-lg">
                        <span className="text-gray-400">Giorni utilizzati</span>
                        <span className="text-white font-bold">{attendanceReport.leaveDaysTaken}</span>
                      </div>
                      <div className="flex justify-between items-center p-3 bg-neon-green/10 rounded-lg border border-neon-green/30">
                        <span className="text-gray-400">Giorni rimanenti</span>
                        <span className="text-neon-green font-bold">{attendanceReport.leaveDaysRemaining}</span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Daily Details Table */}
                <div className="glass-card rounded-2xl border border-white/10 overflow-hidden">
                  <div className="p-6 border-b border-white/10">
                    <h3 className="text-xl font-bold text-white">Dettaglio Giornaliero</h3>
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead className="bg-white/5">
                        <tr>
                          <th className="px-6 py-4 text-left text-sm font-semibold text-gray-400">Data</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Stato</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Ingresso</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Uscita</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Ore</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Anomalia</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-white/5">
                        {attendanceReport.dailyDetails.slice().reverse().slice(0, 30).map((day, idx) => (
                          <tr key={idx} className="hover:bg-white/5 transition-colors">
                            <td className="px-6 py-4 text-white">{formatDate(day.date)}</td>
                            <td className="px-6 py-4 text-center">
                              <span className={`px-3 py-1 rounded-full text-sm ${
                                day.status === 'Present' ? 'bg-green-500/20 text-green-400' :
                                day.status === 'Leave' ? 'bg-purple-500/20 text-purple-400' :
                                day.status === 'Scheduled' ? 'bg-yellow-500/20 text-yellow-400' :
                                'bg-red-500/20 text-red-400'
                              }`}>
                                {day.status === 'Present' ? 'Presente' :
                                 day.status === 'Leave' ? 'Ferie' :
                                 day.status === 'Scheduled' ? 'Programmato' : 'Assente'}
                              </span>
                            </td>
                            <td className="px-6 py-4 text-center text-gray-300">{formatTime(day.actualCheckIn)}</td>
                            <td className="px-6 py-4 text-center text-gray-300">{formatTime(day.actualCheckOut)}</td>
                            <td className="px-6 py-4 text-center text-white font-medium">{day.hoursWorked.toFixed(1)}h</td>
                            <td className="px-6 py-4 text-center">
                              {day.hasAnomaly ? (
                                <span className="px-3 py-1 rounded-full text-sm bg-red-500/20 text-red-400">
                                  {day.anomalyType || 'Si'}
                                </span>
                              ) : (
                                <span className="text-gray-500">-</span>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </AppLayout>
  )
}

export default Reports
