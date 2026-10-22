import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "people",
  "title": "Risorse",
  "version": 2,
  "feature": "Risorse",
  "tours": [
    {
      "id": "overview",
      "title": "Trova le risorse",
      "description": "Tipi, filiali e mansioni.",
      "steps": [
        {
          "target": "[data-guide=\"people.filters\"]",
          "title": "Restringi l’elenco",
          "text": "Scegli se mostrare tutte le risorse, solo quelle interne o solo quelle esterne."
        },
        {
          "target": "[data-guide=\"people.page\"]",
          "title": "Controlla le schede",
          "text": "Le schede raccolgono ruolo e informazioni della risorsa. Apri Modifica per aggiornare i dati."
        },
        {
          "target": "[data-guide=\"people.new\"]",
          "title": "Aggiungi una risorsa",
          "text": "Puoi inserire persone interne o esterne, anche prima che abbiano creato un account."
        }
      ]
    },
    {
      "id": "create",
      "title": "Aggiungi una risorsa",
      "description": "Anagrafica, email e ruolo operativo.",
      "steps": [
        {
          "target": "[data-guide=\"people.new\"]",
          "title": "Apri la nuova risorsa",
          "text": "Premi il comando di aggiunta.",
          "nextWhen": "[data-guide=\"people.form\"]"
        },
        {
          "target": "[data-guide=\"people.email\"]",
          "title": "Controlla l’email",
          "text": "L’email collega la risorsa al suo account. Verifica che sia corretta."
        },
        {
          "target": "[data-guide=\"people.role\"]",
          "title": "Scegli il ruolo",
          "text": "Il ruolo determina le funzioni consentite. Completa filiale, reparti e mansioni secondo necessità."
        },
        {
          "target": "[data-guide=\"people.save\"]",
          "title": "Salva la risorsa",
          "text": "Chiudi la guida e salva per aggiungere la risorsa all’azienda.",
          "readOnly": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
