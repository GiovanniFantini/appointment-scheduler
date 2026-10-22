import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "roles",
  "title": "Ruoli",
  "version": 2,
  "feature": "Ruoli",
  "tours": [
    {
      "id": "overview",
      "title": "Orientami nella pagina",
      "description": "Funzionalità, livelli e ruolo predefinito.",
      "steps": [
        {
          "target": "[data-guide=\"roles.page\"]",
          "title": "Un ruolo per ogni responsabilità",
          "text": "Ogni ruolo stabilisce le funzioni disponibili alle risorse a cui viene assegnato."
        },
        {
          "target": "[data-guide=\"roles.feature\"]",
          "title": "Attiva le funzioni",
          "text": "Le funzioni visibili dipendono dal pacchetto aziendale. Il ruolo Responsabile App è predefinito e non modificabile."
        },
        {
          "target": "[data-guide=\"roles.level\"]:not(:disabled)",
          "title": "Scegli il livello",
          "text": "Il testo sotto al selettore spiega cosa consente il livello scelto. I livelli cambiano le azioni disponibili nel portale dipendenti.",
          "optional": true
        },
        {
          "target": "[data-guide=\"roles.save\"]:not(:disabled)",
          "title": "Conferma le modifiche",
          "text": "Dopo aver modificato un ruolo personalizzato, premi Salva nella sua scheda.",
          "readOnly": true,
          "optional": true
        }
      ]
    },
    {
      "id": "create",
      "title": "Crea un ruolo",
      "description": "Apri il modulo e scegli un nome riconoscibile.",
      "steps": [
        {
          "target": "[data-guide=\"roles.new\"]",
          "title": "Apri un nuovo ruolo",
          "text": "Premi Nuovo Ruolo per aprire il modulo.",
          "nextWhen": "[data-guide=\"roles.form\"]"
        },
        {
          "target": "[data-guide=\"roles.name\"]",
          "title": "Scegli il nome",
          "text": "Usa un nome che descriva la responsabilità, per esempio Responsabile turni."
        },
        {
          "target": "[data-guide=\"roles.create\"]",
          "title": "Crea e configura",
          "text": "Quando sei pronto, chiudi la guida e premi Crea Ruolo. Poi imposta funzioni e livelli nella nuova scheda.",
          "readOnly": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
