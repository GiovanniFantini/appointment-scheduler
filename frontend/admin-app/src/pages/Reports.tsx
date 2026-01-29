import { useState, useEffect } from 'react'
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, LineElement, PointElement, ArcElement, Title, Tooltip, Legend, Filler } from 'chart.js'
import { Bar, Line, Doughnut } from 'react-chartjs-2'
import { saveAs } from 'file-saver'
import AppLayout from '../components/layout/AppLayout'
import apiClient from '../lib/axios'

// Registra i componenti Chart.js
ChartJS.register(CategoryScale, LinearScale, BarElement, LineElement, PointElement, ArcElement, Title, Tooltip, Legend, Filler)

interface User {
  userId: number
  email: string
  firstName: string
  lastName: string
  roles: string[]
  isAdmin: boolean
  isConsumer: boolean
  isMerchant: boolean
  isEmployee: boolean
  merchantId?: number
}

interface ReportsProps {
  user: User
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

interface AdminGlobalReport {
  periodStart: string
  periodEnd: string
  generatedAt: string
  totalMerchants: number
  activeMerchants: number
  totalEmployees: number
  totalConsumers: number
  totalBookings: number
  totalShifts: number
  totalHoursWorked: number
  topMerchants: MerchantActivity[]
  monthlyTrends: MonthlyTrend[]
}

interface MerchantActivity {
  merchantId: number
  businessName: string
  employeeCount: number
  bookingCount: number
  totalHoursWorked: number
  averageAttendanceRate: number
}

interface MonthlyTrend {
  year: number
  month: number
  monthName: string
  newMerchants: number
  newEmployees: number
  totalBookings: number
  totalHoursWorked: number
}

function Reports({ user, onLogout }: ReportsProps) {
  const [dashboardStats, setDashboardStats] = useState<DashboardStats | null>(null)
  const [adminReport, setAdminReport] = useState<AdminGlobalReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [activeTab, setActiveTab] = useState<'dashboard' | 'global'>('dashboard')
  const [dateRange, setDateRange] = useState({
    startDate: new Date(new Date().getFullYear(), 0, 1).toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0]
  })

  useEffect(() => {
    loadDashboardStats()
  }, [])

  useEffect(() => {
    if (activeTab === 'global') {
      loadAdminReport()
    }
  }, [dateRange, activeTab])

  const loadDashboardStats = async () => {
    try {
      const response = await apiClient.get('/reports/dashboard/admin')
      setDashboardStats(response.data)
    } catch (error) {
      console.error('Errore caricamento dashboard stats:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadAdminReport = async () => {
    try {
      setLoading(true)
      const response = await apiClient.get('/reports/admin-global', {
        params: { startDate: dateRange.startDate, endDate: dateRange.endDate }
      })
      setAdminReport(response.data)
    } catch (error) {
      console.error('Errore caricamento report admin:', error)
    } finally {
      setLoading(false)
    }
  }

  const exportReport = async (format: 'pdf' | 'excel' | 'csv') => {
    setExporting(true)
    try {
      const response = await apiClient.post(`/reports/export/${format}`, {
        reportType: 'admin-global',
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
      saveAs(blob, `report_admin_globale_${dateRange.startDate}_${dateRange.endDate}.${extension}`)
    } catch (error) {
      console.error('Errore export:', error)
      alert('Errore durante l\'export del report')
    } finally {
      setExporting(false)
    }
  }

  const _formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('it-IT', { day: '2-digit', month: '2-digit', year: 'numeric' })
  }
  // Keep formatDate for potential future use
  void _formatDate

  // Chart configurations
  const monthlyTrendsChartData = adminReport ? {
    labels: adminReport.monthlyTrends.map(t => t.monthName),
    datasets: [
      {
        label: 'Prenotazioni',
        data: adminReport.monthlyTrends.map(t => t.totalBookings),
        backgroundColor: 'rgba(59, 130, 246, 0.5)',
        borderColor: 'rgb(59, 130, 246)',
        borderWidth: 2
      },
      {
        label: 'Nuovi Merchant',
        data: adminReport.monthlyTrends.map(t => t.newMerchants),
        backgroundColor: 'rgba(147, 51, 234, 0.5)',
        borderColor: 'rgb(147, 51, 234)',
        borderWidth: 2
      }
    ]
  } : null

  const hoursWorkedChartData = adminReport ? {
    labels: adminReport.monthlyTrends.map(t => t.monthName),
    datasets: [{
      label: 'Ore Lavorate',
      data: adminReport.monthlyTrends.map(t => t.totalHoursWorked),
      fill: true,
      backgroundColor: 'rgba(16, 185, 129, 0.2)',
      borderColor: 'rgb(16, 185, 129)',
      tension: 0.4
    }]
  } : null

  const topMerchantsChartData = adminReport ? {
    labels: adminReport.topMerchants.slice(0, 5).map(m => m.businessName.substring(0, 15)),
    datasets: [{
      label: 'Prenotazioni',
      data: adminReport.topMerchants.slice(0, 5).map(m => m.bookingCount),
      backgroundColor: [
        'rgba(59, 130, 246, 0.7)',
        'rgba(147, 51, 234, 0.7)',
        'rgba(16, 185, 129, 0.7)',
        'rgba(245, 158, 11, 0.7)',
        'rgba(239, 68, 68, 0.7)'
      ]
    }]
  } : null

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { labels: { color: '#374151' } }
    },
    scales: {
      x: { ticks: { color: '#374151' }, grid: { color: 'rgba(0,0,0,0.1)' } },
      y: { ticks: { color: '#374151' }, grid: { color: 'rgba(0,0,0,0.1)' } }
    }
  }

  const lineChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { labels: { color: '#374151' } }
    },
    scales: {
      x: { ticks: { color: '#374151' }, grid: { color: 'rgba(0,0,0,0.1)' } },
      y: { ticks: { color: '#374151' }, grid: { color: 'rgba(0,0,0,0.1)' } }
    }
  }

  const doughnutOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' as const, labels: { color: '#374151' } } }
  }

  return (
    <AppLayout user={user} onLogout={onLogout} pageTitle="Report e Statistiche">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-800 mb-2">Report Globale Piattaforma</h1>
            <p className="text-gray-600">Analisi completa di merchant, dipendenti e attivita</p>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => exportReport('pdf')}
              disabled={exporting}
              className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg flex items-center gap-2 disabled:opacity-50 transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              PDF
            </button>
            <button
              onClick={() => exportReport('excel')}
              disabled={exporting}
              className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg flex items-center gap-2 disabled:opacity-50 transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              Excel
            </button>
            <button
              onClick={() => exportReport('csv')}
              disabled={exporting}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 disabled:opacity-50 transition-colors"
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
            className={`px-6 py-3 rounded-lg font-semibold transition-all ${
              activeTab === 'dashboard'
                ? 'bg-purple-600 text-white'
                : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
            }`}
          >
            Dashboard Real-time
          </button>
          <button
            onClick={() => setActiveTab('global')}
            className={`px-6 py-3 rounded-lg font-semibold transition-all ${
              activeTab === 'global'
                ? 'bg-purple-600 text-white'
                : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
            }`}
          >
            Report Globale
          </button>
        </div>

        {/* Date Range Picker */}
        {activeTab === 'global' && (
          <div className="bg-white rounded-lg shadow p-4 mb-6 flex flex-wrap gap-4 items-center">
            <div>
              <label className="text-gray-800 text-sm font-medium">Data Inizio</label>
              <input
                type="date"
                value={dateRange.startDate}
                onChange={(e) => setDateRange(prev => ({ ...prev, startDate: e.target.value }))}
                className="ml-2 px-3 py-2 border border-gray-300 rounded-lg text-gray-800 bg-white"
              />
            </div>
            <div>
              <label className="text-gray-800 text-sm font-medium">Data Fine</label>
              <input
                type="date"
                value={dateRange.endDate}
                onChange={(e) => setDateRange(prev => ({ ...prev, endDate: e.target.value }))}
                className="ml-2 px-3 py-2 border border-gray-300 rounded-lg text-gray-800 bg-white"
              />
            </div>
            <button
              onClick={loadAdminReport}
              className="px-4 py-2 bg-purple-600 hover:bg-purple-700 text-white rounded-lg transition-colors"
            >
              Aggiorna
            </button>
          </div>
        )}

        {loading ? (
          <div className="flex justify-center py-20">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
          </div>
        ) : (
          <>
            {/* Dashboard Tab */}
            {activeTab === 'dashboard' && dashboardStats && (
              <div className="space-y-6">
                {/* KPI Cards */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-600">Turni Oggi</span>
                      <div className="bg-blue-100 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-gray-800">{dashboardStats.todayShifts}</p>
                    <p className="text-sm text-green-600 mt-1">
                      {dashboardStats.todayEmployeesPresent} presenti
                    </p>
                  </div>

                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-600">Prenotazioni Oggi</span>
                      <div className="bg-purple-100 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-gray-800">{dashboardStats.todayBookings}</p>
                    <div className={`text-sm mt-1 ${dashboardStats.bookingsTrend >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {dashboardStats.bookingsTrend >= 0 ? '+' : ''}{dashboardStats.bookingsTrend}% vs mese scorso
                    </div>
                  </div>

                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-600">Ore Settimana</span>
                      <div className="bg-green-100 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-gray-800">{dashboardStats.weekHoursWorked}h</p>
                    <p className="text-sm text-gray-500 mt-1">su {dashboardStats.weekHoursScheduled}h programmate</p>
                  </div>

                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <div className="flex items-center justify-between mb-4">
                      <span className="text-gray-600">Anomalie Pendenti</span>
                      <div className="bg-red-100 p-2 rounded-lg">
                        <svg className="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                      </div>
                    </div>
                    <p className="text-3xl font-bold text-gray-800">{dashboardStats.todayPendingAnomalies}</p>
                    <p className="text-sm text-gray-500 mt-1">Da validare</p>
                  </div>
                </div>

                {/* Trend Cards */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <h3 className="text-gray-600 mb-2">Trend Ore Lavorate</h3>
                    <div className={`text-2xl font-bold ${dashboardStats.hoursWorkedTrend >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {dashboardStats.hoursWorkedTrend >= 0 ? '+' : ''}{dashboardStats.hoursWorkedTrend}%
                    </div>
                    <p className="text-sm text-gray-500">rispetto al mese precedente</p>
                  </div>
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <h3 className="text-gray-600 mb-2">Tasso Presenza Globale</h3>
                    <p className="text-2xl font-bold text-gray-800">{dashboardStats.monthAttendanceRate}%</p>
                    <div className={`text-sm ${dashboardStats.attendanceRateTrend >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                      {dashboardStats.attendanceRateTrend >= 0 ? '+' : ''}{dashboardStats.attendanceRateTrend}% vs mese scorso
                    </div>
                  </div>
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <h3 className="text-gray-600 mb-2">Richieste Ferie</h3>
                    <p className="text-2xl font-bold text-gray-800">{dashboardStats.weekLeaveRequests}</p>
                    <p className="text-sm text-gray-500">questa settimana</p>
                  </div>
                </div>
              </div>
            )}

            {/* Global Report Tab */}
            {activeTab === 'global' && adminReport && (
              <div className="space-y-6">
                {/* Summary Stats */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <p className="text-gray-600 text-sm">Merchant Totali</p>
                    <p className="text-3xl font-bold text-gray-800">{adminReport.totalMerchants}</p>
                    <p className="text-sm text-green-600">{adminReport.activeMerchants} attivi</p>
                  </div>
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <p className="text-gray-600 text-sm">Dipendenti Totali</p>
                    <p className="text-3xl font-bold text-gray-800">{adminReport.totalEmployees}</p>
                  </div>
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <p className="text-gray-600 text-sm">Consumatori</p>
                    <p className="text-3xl font-bold text-gray-800">{adminReport.totalConsumers}</p>
                  </div>
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <p className="text-gray-600 text-sm">Prenotazioni Totali</p>
                    <p className="text-3xl font-bold text-purple-600">{adminReport.totalBookings}</p>
                  </div>
                </div>

                {/* Charts */}
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <h3 className="text-xl font-bold text-gray-800 mb-4">Trend Mensili</h3>
                    <div style={{ height: '300px' }}>
                      {monthlyTrendsChartData && <Bar data={monthlyTrendsChartData} options={chartOptions} />}
                    </div>
                  </div>
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <h3 className="text-xl font-bold text-gray-800 mb-4">Ore Lavorate nel Tempo</h3>
                    <div style={{ height: '300px' }}>
                      {hoursWorkedChartData && <Line data={hoursWorkedChartData} options={lineChartOptions} />}
                    </div>
                  </div>
                </div>

                {/* Top Merchants */}
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <h3 className="text-xl font-bold text-gray-800 mb-4">Top 5 Merchant</h3>
                    <div style={{ height: '250px' }}>
                      {topMerchantsChartData && <Doughnut data={topMerchantsChartData} options={doughnutOptions} />}
                    </div>
                  </div>
                  <div className="bg-white rounded-lg shadow-lg p-6">
                    <h3 className="text-xl font-bold text-gray-800 mb-4">Classifica Merchant</h3>
                    <div className="space-y-3">
                      {adminReport.topMerchants.slice(0, 5).map((merchant, idx) => (
                        <div key={merchant.merchantId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                          <div className="flex items-center gap-3">
                            <span className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-white ${
                              idx === 0 ? 'bg-yellow-500' :
                              idx === 1 ? 'bg-gray-400' :
                              idx === 2 ? 'bg-orange-400' : 'bg-gray-300'
                            }`}>
                              {idx + 1}
                            </span>
                            <div>
                              <p className="font-semibold text-gray-800">{merchant.businessName}</p>
                              <p className="text-sm text-gray-500">{merchant.employeeCount} dipendenti</p>
                            </div>
                          </div>
                          <div className="text-right">
                            <p className="font-bold text-purple-600">{merchant.bookingCount}</p>
                            <p className="text-xs text-gray-500">prenotazioni</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Full Merchant Table */}
                <div className="bg-white rounded-lg shadow-lg overflow-hidden">
                  <div className="p-6 border-b">
                    <h3 className="text-xl font-bold text-gray-800">Tutti i Merchant</h3>
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead className="bg-gray-50">
                        <tr>
                          <th className="px-6 py-4 text-left text-sm font-semibold text-gray-600">Merchant</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-600">Dipendenti</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-600">Prenotazioni</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-600">Ore Lavorate</th>
                          <th className="px-6 py-4 text-center text-sm font-semibold text-gray-600">Tasso Presenza</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-100">
                        {adminReport.topMerchants.map((merchant) => (
                          <tr key={merchant.merchantId} className="hover:bg-gray-50 transition-colors">
                            <td className="px-6 py-4 text-gray-800 font-medium">{merchant.businessName}</td>
                            <td className="px-6 py-4 text-center text-gray-600">{merchant.employeeCount}</td>
                            <td className="px-6 py-4 text-center text-gray-600">{merchant.bookingCount}</td>
                            <td className="px-6 py-4 text-center text-gray-600">{merchant.totalHoursWorked}h</td>
                            <td className="px-6 py-4 text-center">
                              <span className={`px-3 py-1 rounded-full text-sm ${
                                merchant.averageAttendanceRate >= 90 ? 'bg-green-100 text-green-700' :
                                merchant.averageAttendanceRate >= 70 ? 'bg-yellow-100 text-yellow-700' :
                                'bg-red-100 text-red-700'
                              }`}>
                                {merchant.averageAttendanceRate}%
                              </span>
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
