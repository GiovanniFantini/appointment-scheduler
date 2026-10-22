import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "planning",
  "title": "Pianificazione",
  "version": 1,
  "feature": "Calendario",
  "minLevel": "Operator",
  "tours": [
    {
      "id": "overview",
      "title": "Leggi la settimana",
      "description": "Risorse sulle righe e giorni sulle colonne.",
      "steps": [
        {
          "target": "[data-guide=\"planning.week\"]",
          "title": "Scegli la settimana",
          "text": "Usa Settimana precedente, Oggi e Settimana successiva."
        },
        {
          "target": "[data-guide=\"planning.grid\"]",
          "title": "Trova la risorsa",
          "text": "Ogni riga rappresenta una risorsa. Le celle mostrano i turni dei singoli giorni.",
          "optional": true
        },
        {
          "target": "[data-guide=\"planning.page\"]",
          "title": "Apri il turno",
          "text": "Seleziona un turno per le assegnazioni. La creazione e la modifica completa richiedono il livello Manager."
        }
      ]
    },
    {
      "id": "copy",
      "title": "Copia i turni della settimana",
      "description": "Destinazione e numero di settimane consecutive.",
      "steps": [
        {
          "target": "[data-guide=\"planning.week\"]",
          "title": "Controlla la settimana di partenza",
          "text": "La copia usa i turni della settimana che stai visualizzando."
        },
        {
          "target": "[data-guide=\"planning.destination\"]",
          "title": "Scegli il lunedì di destinazione",
          "text": "Imposta la settimana in cui vuoi riportare i turni."
        },
        {
          "target": "[data-guide=\"planning.count\"]",
          "title": "Scegli quante settimane",
          "text": "Indica per quante settimane consecutive ripetere la pianificazione."
        },
        {
          "target": "[data-guide=\"planning.clone\"]",
          "title": "Conferma la copia",
          "text": "Chiudi la guida e premi Clona solo dopo aver verificato partenza e destinazione.",
          "readOnly": true
        }
      ],
      "minLevel": "Manager"
    }
  ]
} satisfies PageGuideDefinition
