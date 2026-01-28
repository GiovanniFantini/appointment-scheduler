import { useState, useEffect } from 'react'
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, LineElement, PointElement, ArcElement, Title, Tooltip, Legend, Filler } from 'chart.js'
import { Bar, Line, Doughnut } from 'react-chartjs-2'
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
  employeeId?: number
  employeeName?: string
}

interface MerchantSummary {
  merchantId: number
  businessName: string
  periodStart: string
  periodEnd: string
  totalEmployees: number
  activeEmployees: number
  totalShifts: number
  totalHoursScheduled: number
  totalHoursWorked: number
  averageAttendanceRate: number
  totalAnomalies: number
  pendingAnomalies: number
  resolvedAnomalies: number
  totalLeaveRequests: number
  pendingLeaveRequests: number
  approvedLeaveRequests: number
  rejectedLeaveRequests: number
  totalBookings: number
  completedBookings: number
  cancelledBookings: number
  employeeSummaries: EmployeeSummary[]
  dailyStats: DailyStat[]
  weeklyStats: WeeklyStat[]
}

interface EmployeeSummary {
  employeeId: number
  employeeName: string
  shiftsAssigned: number
  shiftsCompleted: number
  hoursWorked: number
  overtimeHours: number
  attendanceRate: number
  anomalies: number
  leaveDaysTaken: number
}

interface DailyStat {
  date: string
  dayOfWeek: string
  employeesPresent: number
  employeesAbsent: number
  employeesOnLeave: number
  totalHoursWorked: number
  shiftsCompleted: number
  anomalies: number
  bookings: number
}

interface WeeklyStat {
  weekNumber: number
  weekStart: string
  weekEnd: string
  totalHoursWorked: number
  totalHoursScheduled: number
  shiftsCompleted: number
  totalAnomalies: number
  averageAttendanceRate: number
}

function Reports({ user, onLogout }: ReportsProps) {
  const [dashboardStats, setDashboardStats] = useState<DashboardStats | null>(null)
  const [merchantSummary, setMerchantSummary] = useState<MerchantSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [activeTab, setActiveTab] = useState<'dashboard' | 'summary' | 'employees'>('dashboard')
  const [dateRange, setDateRange] = useState({
    startDate: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0]
  })

  useEffect(() => {
    loadDashboardStats()
  }, [])

  useEffect(() => {
    if (activeTab === 'summary' || activeTab === 'employees') {
      loadMerchantSummary()
    }
  }, [dateRange, activeTab])

  const loadDashboardStats = async () => {
    try {
      const response = await apiClient.get('/reports/dashboard')
      setDashboardStats(response.data)
    } catch (error) {
      console.error('Errore caricamento dashboard stats:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadMerchantSummary = async () => {
    try {
      setLoading(true)
      const response = await apiClient.get('/reports/merchant-summary', {
        params: { startDate: dateRange.startDate, endDate: dateRange.endDate }
      })
      setMerchantSummary(response.data)
    } catch (error) {
      console.error('Errore caricamento merchant summary:', error)
    } finally {
      setLoading(false)
    }
  }

  const exportReport = async (format: 'pdf' | 'excel' | 'csv') => {
    setExporting(true)
    try {
      const response = await apiClient.post(`/reports/export/${format}`, {
        reportType: 'merchant-summary',
        format,
        startDate: dateRange.startDate,
        endDate: dateRange.endDate,
        includeCharts: true,
        includeDetails: true
      }, { responseType: 'blob' })

      const extension = format === 'excel' ? 'xlsx' : format
      const mimeType = format === 'pdf' ? 'application/pdf' :
                       format === 'excel' ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' :
                       'text/csv'
      const blob = new Blob([response.data], { type: mimeType })
      saveAs(blob, `report_${dateRange.startDate}_${dateRange.endDate}.${extension}`)
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

  const _formatDateTime = (dateStr: string) => {
    return new Date(dateStr).toLocaleString('it-IT', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    })
  }
  // Keep formatDateTime for potential future use
  void _formatDateTime

  // Chart configurations
  const weeklyHoursChartData = merchantSummary ? {
    labels: merchantSummary.weeklyStats.map(w => `Sett. ${w.weekNumber}`),
    datasets: [
      {
        label: 'Ore Lavorate',
        data: merchantSummary.weeklyStats.map(w => w.totalHoursWorked),
        backgroundColor: 'rgba(59, 130, 246, 0.5)',
        borderColor: 'rgb(59, 130, 246)',
        borderWidth: 2
      },
      {
        label: 'Ore Programmate',
        data: merchantSummary.weeklyStats.map(w => w.totalHoursScheduled),
        backgroundColor: 'rgba(147, 51, 234, 0.5)',
        borderColor: 'rgb(147, 51, 234)',
        borderWidth: 2
      }
    ]
  } : null

  const attendanceRateChartData = merchantSummary ? {
    labels: merchantSummary.weeklyStats.map(w => `Sett. ${w.weekNumber}`),
    datasets: [{
      label: 'Tasso Presenza %',
      data: merchantSummary.weeklyStats.map(w => w.averageAttendanceRate),
      fill: true,
      backgroundColor: 'rgba(16, 185, 129, 0.2)',
      borderColor: 'rgb(16, 185, 129)',
      tension: 0.4
    }]
  } : null

  const employeePerformanceData = merchantSummary ? {
    labels: merchantSummary.employeeSummaries.slice(0, 10).map(e => e.employeeName.split(' ')[0]),
    datasets: [{
      label: 'Ore Lavorate',
      data: merchantSummary.employeeSummaries.slice(0, 10).map(e => e.hoursWorked),
      backgroundColor: [
        'rgba(59, 130, 246, 0.7)',
        'rgba(147, 51, 234, 0.7)',
        'rgba(16, 185, 129, 0.7)',
        'rgba(245, 158, 11, 0.7)',
        'rgba(239, 68, 68, 0.7)',
        'rgba(6, 182, 212, 0.7)',
        'rgba(236, 72, 153, 0.7)',
        'rgba(132, 204, 22, 0.7)',
        'rgba(249, 115, 22, 0.7)',
        'rgba(99, 102, 241, 0.7)'
      ]
    }]
  } : null

  const anomalyDistributionData = merchantSummary ? {
    labels: ['Risolte', 'Pendenti'],
    datasets: [{
      data: [merchantSummary.resolvedAnomalies, merchantSummary.pendingAnomalies],
      backgroundColor: ['rgba(16, 185, 129, 0.7)', 'rgba(239, 68, 68, 0.7)'],
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
            <h1 className="text-4xl font-bold gradient-text mb-2">Report e Statistiche</h1>
            <p className="text-gray-400">Analisi presenze, assenze e performance</p>
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
            onClick={() => setActiveTab('summary')}
            className={`px-6 py-3 rounded-xl font-semibold transition-all ${
              activeTab === 'summary'
                ? 'bg-gradient-to-r from-neon-blue to-neon-purple text-white'
                : 'glass-card text-gray-400 hover:text-white'
            }`}
          >
            Riepilogo Periodo
          </button>
          <button
            onClick={() => setActiveTab('employees')}
            className={`px-6 py-3 rounded-xl font-semibold transition-all ${
              activeTab === 'employees'
                ? 'bg-gradient-to-r from-neon-blue to-neon-purple text-white'
                : 'glass-card text-gray-400 hover:text-white'
            }`}
          >
            Per Dipendente
          </button>
        </div>

        {/* Date Range Picker (for summary/employees tabs) */}
        {(activeTab === 'summary' || activeTab === 'employees') && (
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
              onClick={loadMerchantSummary}
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
                      <span className="text-gray-400">Turni Oggi</span>
                      <div className="bg-neon-blue/20 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-white">{dashboardStats.todayShifts}</p>
                    <p className="text-sm text-neon-green">
                      {dashboardStats.todayEmployeesPresent} presenti, {dashboardStats.todayEmployeesAbsent} assenti
                    </p>
                  </div>

                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-400">Anomalie Pendenti</span>
                      <div className="bg-red-500/20 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-white">{dashboardStats.todayPendingAnomalies}</p>
                    <p className="text-sm text-gray-400">Da validare oggi</p>
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
                      <span className="text-gray-400">Tasso Presenza</span>
                      <div className="bg-neon-green/20 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-neon-green" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-white">{dashboardStats.monthAttendanceRate}%</p>
                    <div className={`text-sm ${dashboardStats.attendanceRateTrend >= 0 ? 'text-neon-green' : 'text-red-400'}`}>
                      {dashboardStats.attendanceRateTrend >= 0 ? '+' : ''}{dashboardStats.attendanceRateTrend}% vs mese scorso
                    </div>
                  </div>
                </div>

                {/* Trend Cards */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-gray-400 mb-2">Trend Ore Lavorate</h3>
                    <div className={`text-2xl font-bold ${dashboardStats.hoursWorkedTrend >= 0 ? 'text-neon-green' : 'text-red-400'}`}>
                      {dashboardStats.hoursWorkedTrend >= 0 ? '+' : ''}{dashboardStats.hoursWorkedTrend}%
                    </div>
                    <p className="text-sm text-gray-500">rispetto al mese precedente</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-gray-400 mb-2">Richieste Ferie</h3>
                    <p className="text-2xl font-bold text-white">{dashboardStats.weekLeaveRequests}</p>
                    <p className="text-sm text-gray-500">questa settimana</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-gray-400 mb-2">Prenotazioni Oggi</h3>
                    <p className="text-2xl font-bold text-white">{dashboardStats.todayBookings}</p>
                    <div className={`text-sm ${dashboardStats.bookingsTrend >= 0 ? 'text-neon-green' : 'text-red-400'}`}>
                      {dashboardStats.bookingsTrend >= 0 ? '+' : ''}{dashboardStats.bookingsTrend}% vs mese scorso
                    </div>
                  </div>
                </div>

                {/* Upcoming Events */}
                {dashboardStats.upcomingEvents.length > 0 && (
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-xl font-bold text-white mb-4">Prossimi Eventi</h3>
                    <div className="space-y-3">
                      {dashboardStats.upcomingEvents.map((event, idx) => (
                        <div key={idx} className="flex items-center gap-4 p-3 bg-white/5 rounded-xl">
                          <div className={`p-2 rounded-lg ${
                            event.eventType === 'shift' ? 'bg-neon-blue/20' :
                            event.eventType === 'leave' ? 'bg-neon-purple/20' : 'bg-yellow-500/20'
                          }`}>
                            {event.eventType === 'shift' ? (
                              <svg className="w-5 h-5 text-neon-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                              </svg>
                            ) : (
                              <svg className="w-5 h-5 text-neon-purple" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                              </svg>
                            )}
                          </div>
                          <div className="flex-1">
                            <p className="text-white font-medium">{event.title}</p>
                            <p className="text-sm text-gray-400">
                              {formatDate(event.date)} {event.employeeName && `- ${event.employeeName}`}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Summary Tab */}
            {activeTab === 'summary' && merchantSummary && (
              <div className="space-y-6">
                {/* Summary Stats */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Dipendenti Attivi</p>
                    <p className="text-3xl font-bold text-white">{merchantSummary.activeEmployees}</p>
                    <p className="text-sm text-gray-500">su {merchantSummary.totalEmployees} totali</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Turni Periodo</p>
                    <p className="text-3xl font-bold text-white">{merchantSummary.totalShifts}</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Ore Lavorate</p>
                    <p className="text-3xl font-bold text-white">{merchantSummary.totalHoursWorked}h</p>
                    <p className="text-sm text-gray-500">su {merchantSummary.totalHoursScheduled}h programmate</p>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <p className="text-gray-400 text-sm">Tasso Presenza Medio</p>
                    <p className="text-3xl font-bold text-neon-green">{merchantSummary.averageAttendanceRate}%</p>
                  </div>
                </div>

                {/* Charts */}
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-xl font-bold text-white mb-4">Ore Settimanali</h3>
                    <div style={{ height: '300px' }}>
                      {weeklyHoursChartData && <Bar data={weeklyHoursChartData} options={chartOptions} />}
                    </div>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-xl font-bold text-white mb-4">Andamento Presenza</h3>
                    <div style={{ height: '300px' }}>
                      {attendanceRateChartData && <Line data={attendanceRateChartData} options={chartOptions} />}
                    </div>
                  </div>
                </div>

                {/* Anomalies and Leave */}
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                  <div className="glass-card p-6 rounded-2xl border border-white/10">
                    <h3 className="text-xl font-bold text-white mb-4">Anomalie</h3>
                    <div style={{ height: '200px' }}>
                      {anomalyDistributionData && <Doughnut data={anomalyDistributionData} options={doughnutOptions} />}
                    </div>
                    <div className="mt-4 text-center">
                      <p className="text-gray-400">Totale: {merchantSummary.totalAnomalies}</p>
                    </div>
                  </div>
                  <div className="glass-card p-6 rounded-2xl border border-white/10 lg:col-span-2">
                    <h3 className="text-xl font-bold text-white mb-4">Richieste Ferie</h3>
                    <div className="grid grid-cols-3 gap-4">
                      <div className="text-center p-4 bg-yellow-500/10 rounded-xl">
                        <p className="text-2xl font-bold text-yellow-400">{merchantSummary.pendingLeaveRequests}</p>
                        <p className="text-sm text-gray-400">Pendenti</p>
                      </div>
                      <div className="text-center p-4 bg-green-500/10 rounded-xl">
                        <p className="text-2xl font-bold text-green-400">{merchantSummary.approvedLeaveRequests}</p>
                        <p className="text-sm text-gray-400">Approvate</p>
                      </div>
                      <div className="text-center p-4 bg-red-500/10 rounded-xl">
                        <p className="text-2xl font-bold text-red-400">{merchantSummary.rejectedLeaveRequests}</p>
                        <p className="text-sm text-gray-400">Rifiutate</p>
                      </div>
                    </div>
                    <div className="mt-4 pt-4 border-t border-white/10">
                      <div className="flex justify-between">
                        <span className="text-gray-400">Prenotazioni completate</span>
                        <span className="text-white font-bold">{merchantSummary.completedBookings}/{merchantSummary.totalBookings}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Employees Tab */}
            {activeTab === 'employees' && merchantSummary && (
              <div className="space-y-6">
                {/* Employee Performance Chart */}
                <div className="glass-card p-6 rounded-2xl border border-white/10">
                  <h3 className="text-xl font-bold text-white mb-4">Ore Lavorate per Dipendente</h3>
                  <div style={{ height: '300px' }}>
                    {employeePerformanceData && <Bar data={employeePerformanceData} options={chartOptions} />}
                  </div>
                </div>

                {/* Employee Table */}
                <div className="glass-card rounded-2xl border border-white/10 overflow-hidden">
                  <div className="p-6 border-b border-white/10">
                    <h3 className="text-xl font-bold text-white">Dettaglio Dipendenti</h3>
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead className="bg-white/5">
                        <tr>
                          <th className="px-6 py-4 text-left text-sm font-semibold text-gray-400">Dipendente</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Turni</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Completati</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Ore</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Straordinari</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Presenza</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Anomalie</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-400">Ferie</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-white/5">
                        {merchantSummary.employeeSummaries.map((emp) => (
                          <tr key={emp.employeeId} className="hover:bg-white/5 transition-colors">
                            <td className="px-6 py-4 text-white font-medium">{emp.employeeName}</td>
                            <td className="px-6 py-4 text-center text-gray-300">{emp.shiftsAssigned}</td>
                            <td className="px-6 py-4 text-center text-gray-300">{emp.shiftsCompleted}</td>
                            <td className="px-6 py-4 text-center text-gray-300">{emp.hoursWorked}h</td>
                            <td className="px-6 py-4 text-center text-neon-purple">{emp.overtimeHours}h</td>
                            <td className="px-6 py-4 text-center">
                              <span className={`px-3 py-1 rounded-full text-sm ${
                                emp.attendanceRate >= 90 ? 'bg-green-500/20 text-green-400' :
                                emp.attendanceRate >= 70 ? 'bg-yellow-500/20 text-yellow-400' :
                                'bg-red-500/20 text-red-400'
                              }`}>
                                {emp.attendanceRate}%
                              </span>
                            </td>
                            <td className="px-6 py-4 text-center">
                              <span className={`px-3 py-1 rounded-full text-sm ${
                                emp.anomalies === 0 ? 'bg-green-500/20 text-green-400' :
                                emp.anomalies <= 2 ? 'bg-yellow-500/20 text-yellow-400' :
                                'bg-red-500/20 text-red-400'
                              }`}>
                                {emp.anomalies}
                              </span>
                            </td>
                            <td className="px-6 py-4 text-center text-gray-300">{emp.leaveDaysTaken}g</td>
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
