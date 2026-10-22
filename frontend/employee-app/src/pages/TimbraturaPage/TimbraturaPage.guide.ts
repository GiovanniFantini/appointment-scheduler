import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "clock",
  "title": "Timbratura",
  "version": 1,
  "feature": "Timbratura",
  "tours": [
    {
      "id": "overview",
      "title": "Registra la tua presenza",
      "description": "Entrata, pause e uscita del turno.",
      "steps": [
        {
          "target": "[data-guide=\"clock.today\"]",
          "title": "Trova il turno di oggi",
          "text": "Le azioni compaiono se hai un turno previsto e la timbratura è attiva per la filiale."
        },
        {
          "target": "[data-guide=\"clock.today\"]",
          "title": "Segui lo stato del turno",
          "text": "Usa Entrata all’inizio, i comandi pausa durante il turno e Uscita alla fine. Ogni comando registra una timbratura.",
          "readOnly": true
        },
        {
          "target": "[data-guide=\"clock.history\"]",
          "title": "Consulta lo storico",
          "text": "Apri Le mie timbrature per vedere le giornate registrate e i relativi orari."
        }
      ]
    },
    {
      "id": "anomaly",
      "title": "Giustifica un’anomalia",
      "description": "Apri la segnalazione e indica il motivo.",
      "steps": [
        {
          "target": "[data-guide=\"clock.page\"]",
          "title": "Controlla le segnalazioni",
          "text": "Quando una timbratura richiede una giustificazione compare un avviso sopra i turni."
        },
        {
          "target": "[data-guide=\"clock.anomaly\"]",
          "title": "Apri l’anomalia",
          "text": "Premi l’avviso, se presente, per aprire la giustificazione.",
          "optional": true,
          "nextWhen": "[data-guide=\"clock.justification\"]"
        },
        {
          "target": "[data-guide=\"clock.justification\"]",
          "title": "Spiega il motivo",
          "text": "Scegli la motivazione e aggiungi una nota utile al responsabile.",
          "optional": true
        },
        {
          "target": "[data-guide=\"clock.justificationSave\"]",
          "title": "Invia la giustificazione",
          "text": "Chiudi la guida e invia quando sei pronto. Potrai seguire la revisione nella pagina Timbratura.",
          "optional": true,
          "readOnly": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
