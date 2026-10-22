import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "calendar",
  "title": "Calendario",
  "version": 2,
  "feature": "Calendario",
  "tours": [
    {
      "id": "overview",
      "title": "Consulta i turni",
      "description": "Viste, colori e dettagli degli eventi.",
      "steps": [
        {
          "target": "[data-guide=\"calendar.grid\"]",
          "title": "Scegli la vista",
          "text": "Usa Giorno, Settimana e Mese. Con Oggi torni alla data attuale."
        },
        {
          "target": "[data-guide=\"calendar.legend\"]",
          "title": "Leggi i colori",
          "text": "La legenda distingue turni, assenze e chiusure. Controlla anche le indicazioni di reperibilità e copertura."
        },
        {
          "target": "[data-guide=\"calendar.grid\"]",
          "title": "Apri un evento",
          "text": "Seleziona un turno per consultarne i dettagli. Le azioni disponibili dipendono dal tuo livello di accesso."
        }
      ]
    },
    {
      "id": "create",
      "title": "Crea un turno",
      "description": "Apri la procedura guidata già presente.",
      "steps": [
        {
          "target": "[data-guide=\"calendar.new\"]",
          "title": "Apri il turno",
          "text": "Premi Nuovo turno per iniziare.",
          "nextWhen": "[data-guide=\"shift.form\"]"
        },
        {
          "target": "[data-guide=\"shift.steps\"]",
          "title": "Segui i passaggi",
          "text": "Compila quando e dove, poi scegli chi partecipa. Se sono configurate mansioni, trovi anche il passaggio Cosa serve. Ogni passaggio ha un aiuto apribile."
        },
        {
          "target": "[data-guide=\"shift.body\"]",
          "title": "Completa i dettagli",
          "text": "Puoi chiudere la guida e continuare nel modulo. Il turno viene creato soltanto quando confermi il salvataggio."
        }
      ],
      "minLevel": "Manager"
    },
    {
      "id": "copy",
      "title": "Copia la settimana precedente",
      "description": "Riporta i turni precedenti nella settimana visualizzata.",
      "steps": [
        {
          "target": "[data-guide=\"calendar.grid\"]",
          "title": "Apri la vista Settimana",
          "text": "Seleziona Settimana nella barra del calendario per mostrare il comando di copia.",
          "nextWhen": "[data-guide=\"calendar.copy\"]"
        },
        {
          "target": "[data-guide=\"calendar.copy\"]",
          "title": "Apri la copia",
          "text": "Nella settimana da compilare premi Copia settimana scorsa: verranno proposti i turni dei sette giorni precedenti.",
          "nextWhen": "[data-guide=\"calendar.copyForm\"]"
        },
        {
          "target": "[data-guide=\"calendar.copyForm\"]",
          "title": "Controlla partenza e destinazione",
          "text": "L’anteprima mostra da quale settimana si copia e in quale settimana verranno aggiunti i turni. Le date dipendono dalla settimana visualizzata nel calendario."
        },
        {
          "target": "[data-guide=\"calendar.copyForm\"]",
          "title": "Conferma quando sei pronto",
          "text": "Chiudi la guida e conferma solo dopo aver controllato i turni. Eventuali conflitti vengono segnalati dopo la copia, quando i turni sono già stati creati."
        }
      ],
      "minLevel": "Manager"
    },
    {
      "id": "assign",
      "title": "Assegna personale",
      "description": "Trova il turno e controlla i partecipanti.",
      "steps": [
        {
          "target": "[data-guide=\"calendar.grid\"]",
          "title": "Scegli un turno esistente",
          "text": "Apri un turno dalla griglia per vedere le assegnazioni e le azioni consentite."
        },
        {
          "target": "[data-guide=\"calendar.legend\"]",
          "title": "Controlla la copertura",
          "text": "Se sono configurate mansioni, verifica personale richiesto e disponibile."
        },
        {
          "target": "[data-guide=\"calendar.grid\"]",
          "title": "Assegna le risorse",
          "text": "Nel modulo del turno scegli i partecipanti. Con livello Operatore puoi assegnare personale ai turni esistenti."
        }
      ],
      "minLevel": "Operator"
    }
  ]
} satisfies PageGuideDefinition
