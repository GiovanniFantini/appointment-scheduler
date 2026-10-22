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
      "description": "Il riepilogo della giornata e i collegamenti utili.",
      "steps": [
        {
          "target": "[data-guide=\"dashboard.page\"]",
          "title": "La tua giornata",
          "text": "Questa pagina raccoglie le informazioni consentite dal tuo ruolo nell’azienda selezionata."
        },
        {
          "target": "[data-guide=\"dashboard.stats\"]",
          "title": "Controlla il riepilogo",
          "text": "Qui trovi gli indicatori delle funzioni attive, come turni e richieste.",
          "optional": true
        },
        {
          "target": "[data-guide=\"dashboard.content\"]",
          "title": "Apri una funzione",
          "text": "Usa Accesso rapido o il menu. La guida di ogni pagina spiega le azioni che puoi svolgere."
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
