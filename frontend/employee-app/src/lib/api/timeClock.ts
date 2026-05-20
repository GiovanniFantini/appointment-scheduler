import apiClient from '../axios'
import type {
  CurrentClockStatusDto,
  ClockActionResultDto,
  ClockActionRequest,
  TimeEntryDto,
  TimeClockAnomalyDto,
  JustifyAnomalyRequest,
  WellbeingStatsDto,
} from '../../types/timbratura'
import { TimeClockAnomalyStatus } from '../../types/timbratura'

/** Client API per la timbratura lato dipendente. */
export const timeClockApi = {
  async getStatus(): Promise<CurrentClockStatusDto> {
    const res = await apiClient.get<CurrentClockStatusDto>('/time-clock/status')
    return res.data
  },

  async clockIn(payload: ClockActionRequest): Promise<ClockActionResultDto> {
    const res = await apiClient.post<ClockActionResultDto>('/time-clock/clock-in', payload)
    return res.data
  },

  async clockOut(payload: ClockActionRequest): Promise<ClockActionResultDto> {
    const res = await apiClient.post<ClockActionResultDto>('/time-clock/clock-out', payload)
    return res.data
  },

  async startBreak(payload: ClockActionRequest): Promise<ClockActionResultDto> {
    const res = await apiClient.post<ClockActionResultDto>('/time-clock/break/start', payload)
    return res.data
  },

  async endBreak(payload: ClockActionRequest): Promise<ClockActionResultDto> {
    const res = await apiClient.post<ClockActionResultDto>('/time-clock/break/end', payload)
    return res.data
  },

  async getMyEntries(from?: string, to?: string): Promise<TimeEntryDto[]> {
    const res = await apiClient.get<TimeEntryDto[]>('/time-clock/my-entries', {
      params: { from, to },
    })
    return res.data
  },

  async getMyAnomalies(status?: TimeClockAnomalyStatus): Promise<TimeClockAnomalyDto[]> {
    const res = await apiClient.get<TimeClockAnomalyDto[]>('/time-clock/my-anomalies', {
      params: { status },
    })
    return res.data
  },

  async justifyAnomaly(id: number, payload: JustifyAnomalyRequest): Promise<TimeClockAnomalyDto> {
    const res = await apiClient.post<TimeClockAnomalyDto>(
      `/time-clock/anomalies/${id}/justify`,
      payload,
    )
    return res.data
  },

  async getWellbeing(): Promise<WellbeingStatsDto> {
    const res = await apiClient.get<WellbeingStatsDto>('/time-clock/wellbeing')
    return res.data
  },
}

/**
 * Legge la posizione GPS dal browser. Risolve sempre (anche se il permesso è
 * negato o la geolocalizzazione non è disponibile): in tal caso restituisce un
 * oggetto vuoto, e il backend registra comunque la timbratura.
 */
export function getBrowserLocation(): Promise<{
  latitude?: number
  longitude?: number
  gpsAccuracyMeters?: number
}> {
  return new Promise(resolve => {
    if (!('geolocation' in navigator)) {
      resolve({})
      return
    }
    navigator.geolocation.getCurrentPosition(
      pos => resolve({
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
        gpsAccuracyMeters: pos.coords.accuracy,
      }),
      () => resolve({}),
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 },
    )
  })
}
