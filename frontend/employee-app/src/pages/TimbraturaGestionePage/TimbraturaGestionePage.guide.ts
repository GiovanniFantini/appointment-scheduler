import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "clockManagement",
  "title": "Gestione timbrature",
  "version": 1,
  "feature": "Timbratura",
  "minLevel": "Manager",
  "tours": [
    {
      "id": "overview",
      "title": "Orientami nella gestione",
      "description": "Filiale, configurazione, presenze e anomalie.",
      "steps": [
        {
          "target": "[data-guide=\"clockManagement.branch\"]",
          "title": "Scegli la filiale",
          "text": "Le impostazioni e i dati visualizzati si riferiscono alla filiale selezionata."
        },
        {
          "target": "[data-guide=\"clockManagement.tabs\"]",
          "title": "Scegli la sezione",
          "text": "Configurazione regola le timbrature; Presenze mostra gli orari; Anomalie raccoglie le segnalazioni; Report riepiloga le ore."
        },
        {
          "target": "[data-guide=\"clockManagement.page\"]",
          "title": "Controlla prima di confermare",
          "text": "Modifiche alle regole e decisioni sui giustificativi hanno comandi di conferma dedicati."
        }
      ]
    },
    {
      "id": "config",
      "title": "Configura la timbratura",
      "description": "Regole e tolleranze della filiale.",
      "steps": [
        {
          "target": "[data-guide=\"clockManagement.config\"]",
          "title": "Apri Configurazione",
          "text": "Seleziona Configurazione.",
          "nextWhen": "[data-guide=\"clockManagement.settings\"]"
        },
        {
          "target": "[data-guide=\"clockManagement.settings\"]",
          "title": "Imposta le regole",
          "text": "Attiva la rilevazione e definisci tolleranze, pause e controlli geografici secondo le esigenze della filiale."
        },
        {
          "target": "[data-guide=\"clockManagement.save\"]",
          "title": "Salva le impostazioni",
          "text": "Chiudi la guida e premi Salva configurazione per applicare le modifiche.",
          "readOnly": true
        }
      ]
    },
    {
      "id": "presences",
      "title": "Consulta le presenze",
      "description": "Intervallo e dettaglio delle timbrature.",
      "steps": [
        {
          "target": "[data-guide=\"clockManagement.presences\"]",
          "title": "Apri Presenze",
          "text": "Seleziona Presenze.",
          "nextWhen": "[data-guide=\"clockManagement.entries\"]"
        },
        {
          "target": "[data-guide=\"clockManagement.entries\"]",
          "title": "Scegli l’intervallo",
          "text": "Imposta Da e A per leggere entrate, uscite, pause e scostamenti dai turni."
        }
      ]
    },
    {
      "id": "review",
      "title": "Revisiona le anomalie",
      "description": "Stato delle segnalazioni e giustificativi.",
      "steps": [
        {
          "target": "[data-guide=\"clockManagement.anomalies\"]",
          "title": "Apri Anomalie",
          "text": "Seleziona Anomalie.",
          "nextWhen": "[data-guide=\"clockManagement.review\"]"
        },
        {
          "target": "[data-guide=\"clockManagement.review\"]",
          "title": "Controlla lo stato",
          "text": "Filtra le segnalazioni e apri Revisiona sulle anomalie in attesa. Leggi la motivazione prima di decidere."
        }
      ]
    },
    {
      "id": "report",
      "title": "Leggi il report presenze",
      "description": "Ore lavorate e riepilogo per risorsa.",
      "steps": [
        {
          "target": "[data-guide=\"clockManagement.report\"]",
          "title": "Apri Report",
          "text": "Seleziona Report.",
          "nextWhen": "[data-guide=\"clockManagement.totals\"]"
        },
        {
          "target": "[data-guide=\"clockManagement.totals\"]",
          "title": "Controlla le ore",
          "text": "Seleziona il periodo e confronta le ore delle risorse della filiale scelta."
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
