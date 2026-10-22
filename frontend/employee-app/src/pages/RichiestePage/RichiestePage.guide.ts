import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "requests",
  "title": "Richieste",
  "version": 2,
  "feature": "Richieste",
  "tours": [
    {
      "id": "overview",
      "title": "Segui le tue richieste",
      "description": "Crea una richiesta e controllane lo stato.",
      "steps": [
        {
          "target": "[data-guide=\"requests.page\"]",
          "title": "Le tue richieste",
          "text": "Qui trovi ferie, permessi e malattia. Quando presenti, i filtri separano gli stati delle richieste."
        },
        {
          "target": "[data-guide=\"requests.new\"]",
          "title": "Inizia una richiesta",
          "text": "Usa Nuova richiesta. L’invio registra la richiesta, che può richiedere l’approvazione del responsabile."
        },
        {
          "target": "[data-guide=\"requests.page\"]",
          "title": "Controlla l’esito",
          "text": "Torna in questa pagina per verificare se la richiesta è in attesa, approvata o respinta."
        }
      ]
    },
    {
      "id": "create",
      "title": "Richiedi un’assenza",
      "description": "Tipo, date e invio della richiesta.",
      "steps": [
        {
          "target": "[data-guide=\"requests.new\"]",
          "title": "Apri il modulo",
          "text": "Premi Nuova richiesta per iniziare.",
          "nextWhen": "[data-guide=\"requests.form\"]"
        },
        {
          "target": "[data-guide=\"requests.type\"]",
          "title": "Scegli il tipo",
          "text": "Seleziona Ferie, Permesso o Malattia. Per le assenze orarie puoi indicare anche gli orari."
        },
        {
          "target": "[data-guide=\"requests.dates\"]",
          "title": "Indica il periodo",
          "text": "Scegli la data di inizio e, se necessario, quella di fine. Aggiungi eventuali note."
        },
        {
          "target": "[data-guide=\"requests.submit\"]",
          "title": "Invia quando sei pronto",
          "text": "Chiudi la guida e premi Invia richiesta per registrarla. Potrai seguirne lo stato nella pagina.",
          "readOnly": true
        }
      ]
    },
    {
      "id": "manage",
      "title": "Gestisci le assenze del team",
      "description": "Controlla le richieste di assenza da approvare.",
      "steps": [
        {
          "target": "[data-guide=\"requests.manage\"]",
          "title": "Apri la gestione",
          "text": "Premi il comando di gestione per vedere le richieste degli altri dipendenti.",
          "nextWhen": "[data-guide=\"requests.team\"]"
        },
        {
          "target": "[data-guide=\"requests.team\"]",
          "title": "Controlla le assenze",
          "text": "Leggi periodo e motivazione prima di approvare o rifiutare. I comandi compaiono sulle richieste in attesa."
        }
      ],
      "minLevel": "Operator"
    },
    {
      "id": "justifications",
      "title": "Revisiona i giustificativi",
      "description": "Controlla le giustificazioni delle timbrature.",
      "feature": "Timbratura",
      "minLevel": "Manager",
      "steps": [
        {
          "target": "[data-guide=\"requests.manage\"]",
          "title": "Apri la gestione",
          "text": "Premi Gestione richieste per aprire le sezioni disponibili al tuo ruolo.",
          "nextWhen": "[data-guide=\"requests.justifications\"]"
        },
        {
          "target": "[data-guide=\"requests.justifications\"]",
          "title": "Controlla i giustificativi",
          "text": "Qui compaiono le motivazioni inviate dai dipendenti per le anomalie di timbratura."
        },
        {
          "target": "[data-guide=\"requests.review\"]",
          "title": "Revisiona la motivazione",
          "text": "Quando è presente un giustificativo, apri Revisiona e leggi la motivazione prima di decidere.",
          "optional": true,
          "readOnly": true
        }
      ]
    }
  ]
} satisfies PageGuideDefinition
