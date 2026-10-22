import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "skills",
  "title": "Mansioni",
  "version": 1,
  "feature": "Mansioni",
  "tours": [
    {
      "id": "overview",
      "title": "Organizza le mansioni",
      "description": "Qualifiche operative e copertura dei turni.",
      "steps": [
        {
          "target": "[data-guide=\"skills.page\"]",
          "title": "Definisci le qualifiche",
          "text": "Le mansioni descrivono le capacità operative delle risorse. Usale se ti serve controllare la copertura dei turni."
        },
        {
          "target": "[data-guide=\"skills.new\"]",
          "title": "Aggiungi una mansione",
          "text": "Crea una voce con un nome concreto, come Cassiere o Repartista."
        },
        {
          "target": "[data-guide=\"skills.page\"]",
          "title": "Collega persone e turni",
          "text": "Assegna le mansioni alle risorse, poi indica quali servono nei turni per valutarne la copertura."
        }
      ]
    },
    {
      "id": "create",
      "title": "Crea una mansione",
      "description": "Nome e colore della qualifica.",
      "steps": [
        {
          "target": "[data-guide=\"skills.new\"]",
          "title": "Apri la mansione",
          "text": "Premi Nuova mansione.",
          "nextWhen": "[data-guide=\"skills.form\"]"
        },
        {
          "target": "[data-guide=\"skills.form\"]",
          "title": "Scegli nome e colore",
          "text": "Il nome identifica la qualifica. Il colore la rende riconoscibile nei turni."
        },
        {
          "target": "[data-guide=\"skills.save\"]",
          "title": "Conferma la creazione",
          "text": "Chiudi la guida e salva. La mansione sarà disponibile per risorse e turni.",
          "readOnly": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
