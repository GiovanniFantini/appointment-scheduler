import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "branches",
  "title": "Filiali",
  "version": 1,
  "feature": "Filiali",
  "tours": [
    {
      "id": "overview",
      "title": "Organizza sedi e reparti",
      "description": "Sede principale, filiali e aree operative.",
      "steps": [
        {
          "target": "[data-guide=\"branches.page\"]",
          "title": "Le sedi dell’azienda",
          "text": "Ogni filiale raggruppa risorse, turni e reparti. La sede principale è riconoscibile dall’etichetta dedicata."
        },
        {
          "target": "[data-guide=\"branches.new\"]",
          "title": "Aggiungi una filiale",
          "text": "Usa Nuova filiale per una singola sede. Se stai iniziando, Configura le filiali propone i passaggi guidati."
        },
        {
          "target": "[data-guide=\"branches.department\"]",
          "title": "Aggiungi i reparti",
          "text": "Il comando Reparto nelle schede delle filiali permette di suddividere le aree operative.",
          "optional": true
        }
      ]
    },
    {
      "id": "create",
      "title": "Aggiungi una filiale",
      "description": "Dati della sede e salvataggio.",
      "steps": [
        {
          "target": "[data-guide=\"branches.new\"]",
          "title": "Apri la nuova filiale",
          "text": "Premi Nuova filiale.",
          "nextWhen": "[data-guide=\"branches.form\"]"
        },
        {
          "target": "[data-guide=\"branches.form\"]",
          "title": "Compila i dati della sede",
          "text": "Indica un nome riconoscibile e gli eventuali dati di contatto e indirizzo."
        },
        {
          "target": "[data-guide=\"branches.form\"]",
          "title": "Salva e aggiungi i reparti",
          "text": "Chiudi la guida e salva la filiale. Potrai poi aggiungere i suoi reparti dalla scheda."
        }
      ]
    },
    {
      "id": "department",
      "title": "Aggiungi un reparto",
      "description": "Suddividi una filiale in aree operative.",
      "steps": [
        {
          "target": "[data-guide=\"branches.page\"]",
          "title": "Trova la filiale",
          "text": "Nelle aziende con più sedi, ogni scheda filiale mostra i suoi reparti. Per la prima configurazione puoi usare Configura le filiali."
        },
        {
          "target": "[data-guide=\"branches.department\"]",
          "title": "Apri il reparto",
          "text": "Premi Reparto nella filiale corretta, se disponibile.",
          "optional": true,
          "nextWhen": "[data-guide=\"branches.departmentForm\"]"
        },
        {
          "target": "[data-guide=\"branches.departmentForm\"]",
          "title": "Scegli nome e colore",
          "text": "Usa un nome riconoscibile per l’area, come Vendita o Magazzino.",
          "optional": true
        },
        {
          "target": "[data-guide=\"branches.departmentSave\"]",
          "title": "Salva il reparto",
          "text": "Chiudi la guida e salva quando sei pronto. Il reparto sarà disponibile per risorse e turni.",
          "optional": true,
          "readOnly": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
