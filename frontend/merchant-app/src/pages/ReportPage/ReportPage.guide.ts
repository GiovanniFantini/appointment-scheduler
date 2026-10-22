import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "reports",
  "title": "Report",
  "version": 1,
  "feature": "Report",
  "tours": [
    {
      "id": "overview",
      "title": "Leggi il riepilogo",
      "description": "Periodo, filiale e significato delle ore.",
      "steps": [
        {
          "target": "[data-guide=\"reports.filters\"]",
          "title": "Scegli il periodo",
          "text": "Imposta Dal e Al oppure usa Questo mese e Mese scorso."
        },
        {
          "target": "[data-guide=\"reports.branch\"]",
          "title": "Limita alla filiale",
          "text": "Seleziona una filiale o lascia Tutte le filiali per il riepilogo aziendale."
        },
        {
          "target": "[data-guide=\"reports.stats\"]",
          "title": "Leggi gli indicatori",
          "text": "Le ore lavorate derivano dalle timbrature e sono al netto delle pause. I turni pianificati sono un conteggio distinto.",
          "optional": true
        },
        {
          "target": "[data-guide=\"reports.detail\"]",
          "title": "Vai al dettaglio",
          "text": "Qui trovi una riga per dipendente, turno e giornata. Una sessione ancora aperta può avere ore parziali.",
          "optional": true
        }
      ]
    },
    {
      "id": "export",
      "title": "Esporta i dati",
      "description": "Riepilogo e dettaglio del periodo selezionato.",
      "steps": [
        {
          "target": "[data-guide=\"reports.filters\"]",
          "title": "Controlla il perimetro",
          "text": "Le esportazioni si riferiscono al periodo e alla filiale selezionati."
        },
        {
          "target": "[data-guide=\"reports.export\"]",
          "title": "Esporta il riepilogo",
          "text": "Scarica gli indicatori e la distribuzione giornaliera in CSV."
        },
        {
          "target": "[data-guide=\"reports.detailExport\"]",
          "title": "Esporta le timbrature",
          "text": "Per le singole righe usa Esporta dettaglio CSV. Il pulsante è disponibile quando ci sono timbrature.",
          "optional": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
