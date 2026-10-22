import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "dashboard",
  "title": "Dashboard",
  "version": 1,
  "tours": [
    {
      "id": "overview",
      "title": "Orientami nella pagina",
      "description": "Indicatori e accessi alle funzioni disponibili.",
      "steps": [
        {
          "target": "[data-guide=\"merchantDashboard.page\"]",
          "title": "La tua area gestione",
          "text": "Qui trovi il riepilogo aziendale e gli accessi a Ruoli e Report, se inclusi nel pacchetto."
        },
        {
          "target": "[data-guide=\"merchantDashboard.stats\"]",
          "title": "Il riepilogo",
          "text": "Controlla dipendenti, eventi e richieste. Per il dettaglio dei dati apri Report."
        },
        {
          "target": "[data-guide=\"merchantDashboard.page\"]",
          "title": "Configurazione e operatività",
          "text": "I ruoli si configurano qui. Le attività quotidiane si svolgono nel portale dipendenti, secondo i permessi assegnati."
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
