import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "notifications",
  "title": "Notifiche",
  "version": 1,
  "tours": [
    {
      "id": "overview",
      "title": "Leggi gli aggiornamenti",
      "description": "Notifiche non lette e collegamenti.",
      "steps": [
        {
          "target": "[data-guide=\"notifications.page\"]",
          "title": "Controlla gli aggiornamenti",
          "text": "Le notifiche raccolgono le comunicazioni disponibili per il tuo account."
        },
        {
          "target": "[data-guide=\"notifications.item\"]",
          "title": "Apri una notifica",
          "text": "Aprendo una notifica viene segnata come letta. Se riguarda un documento, il collegamento porta alla pagina Documenti.",
          "optional": true,
          "readOnly": true
        },
        {
          "target": "[data-guide=\"notifications.readAll\"]",
          "title": "Segna tutte come lette",
          "text": "Usa questo comando quando hai consultato gli aggiornamenti. Compare solo se ci sono notifiche non lette.",
          "optional": true,
          "readOnly": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
