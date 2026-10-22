import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "documents",
  "title": "Documenti",
  "version": 2,
  "feature": "Documenti",
  "tours": [
    {
      "id": "overview",
      "title": "Trova i tuoi documenti",
      "description": "Filtri, dettaglio e download.",
      "steps": [
        {
          "target": "[data-guide=\"documents.personal\"]",
          "title": "I tuoi documenti",
          "text": "Questa sezione contiene i documenti destinati a te. Se è vuota, attendi la pubblicazione da parte del responsabile."
        },
        {
          "target": "[data-guide=\"documents.filter\"]",
          "title": "Filtra la ricerca",
          "text": "Scegli il tipo di documento e l’anno di riferimento."
        },
        {
          "target": "[data-guide=\"documents.download\"]",
          "title": "Scarica il documento",
          "text": "Usa Scarica sulla riga del documento disponibile.",
          "optional": true
        },
        {
          "target": "[data-guide=\"documents.detail\"]",
          "title": "Controlla le versioni",
          "text": "Apri il dettaglio per consultare le versioni e le informazioni del documento.",
          "optional": true
        }
      ]
    },
    {
      "id": "upload",
      "title": "Carica per una risorsa",
      "description": "Destinatario, tipo e periodo di riferimento.",
      "steps": [
        {
          "target": "[data-guide=\"documents.upload\"]",
          "title": "Gestione documenti",
          "text": "Questo modulo carica un documento per un’altra risorsa. I tuoi documenti restano nella sezione personale."
        },
        {
          "target": "[data-guide=\"documents.recipient\"]",
          "title": "Scegli il destinatario",
          "text": "Controlla la risorsa destinataria prima di scegliere il file."
        },
        {
          "target": "[data-guide=\"documents.type\"]",
          "title": "Classifica il documento",
          "text": "Imposta tipo e titolo. Anno e mese indicano il periodo di riferimento e sono facoltativi."
        },
        {
          "target": "[data-guide=\"documents.submit\"]",
          "title": "Conferma il caricamento",
          "text": "Dopo aver selezionato il file, chiudi la guida e premi Carica documento. Il file viene caricato soltanto con questa azione.",
          "readOnly": true
        }
      ],
      "minLevel": "Operator"
    },
    {
      "id": "versions",
      "title": "Carica una nuova versione",
      "description": "Aggiorna il file mantenendo lo storico.",
      "steps": [
        {
          "target": "[data-guide=\"documents.personal\"]",
          "title": "Trova il documento",
          "text": "Usa i filtri per trovare il documento da aggiornare."
        },
        {
          "target": "[data-guide=\"documents.detail\"]",
          "title": "Apri il dettaglio",
          "text": "Premi Dettaglio sul documento, se disponibile.",
          "optional": true,
          "nextWhen": "[data-guide=\"documents.detailPanel\"]"
        },
        {
          "target": "[data-guide=\"documents.versionForm\"]",
          "title": "Scegli il nuovo file",
          "text": "Nella sezione Nuova versione seleziona il file aggiornato e aggiungi eventuali note.",
          "optional": true
        },
        {
          "target": "[data-guide=\"documents.versionSave\"]",
          "title": "Carica quando sei pronto",
          "text": "Chiudi la guida e premi Carica nuova versione. Le versioni precedenti rimangono nello storico.",
          "optional": true,
          "readOnly": true
        }
      ],
      "minLevel": "Operator",
      "requiresTarget": "[data-guide=\"documents.detail\"]",
      "unavailableMessage": "Non ci sono documenti personali visibili con questi filtri. Modifica la ricerca o riprova quando sarà disponibile un documento destinato a te. I documenti caricati per altre risorse non compaiono nella tua sezione personale."
    },
    {
      "id": "acknowledge",
      "title": "Conferma la presa visione",
      "description": "Distingui il download dalla conferma di lettura.",
      "steps": [
        {
          "target": "[data-guide=\"documents.personal\"]",
          "title": "Trova il documento",
          "text": "Il download e la presa visione sono due azioni distinte."
        },
        {
          "target": "[data-guide=\"documents.detail\"]",
          "title": "Apri il dettaglio",
          "text": "Premi Dettaglio sul documento, se disponibile.",
          "optional": true,
          "nextWhen": "[data-guide=\"documents.detailPanel\"]"
        },
        {
          "target": "[data-guide=\"documents.acknowledge\"]",
          "title": "Conferma dopo aver letto",
          "text": "Dopo aver letto la versione, chiudi la guida e usa Confermo presa visione per registrarne la lettura.",
          "optional": true,
          "readOnly": true
        }
      ],
      "requiresTarget": "[data-guide=\"documents.detail\"]",
      "unavailableMessage": "Non ci sono documenti personali visibili con questi filtri. Modifica la ricerca o riprova quando sarà disponibile un documento destinato a te. I documenti caricati per altre risorse non compaiono nella tua sezione personale."
    },
    {
      "id": "delete",
      "title": "Elimina un documento",
      "description": "Verifica il documento prima della conferma.",
      "minLevel": "Manager",
      "steps": [
        {
          "target": "[data-guide=\"documents.personal\"]",
          "title": "Trova il documento",
          "text": "Controlla titolo e periodo del documento da eliminare."
        },
        {
          "target": "[data-guide=\"documents.detail\"]",
          "title": "Apri il dettaglio",
          "text": "Premi Dettaglio sul documento, se disponibile.",
          "optional": true,
          "nextWhen": "[data-guide=\"documents.detailPanel\"]"
        },
        {
          "target": "[data-guide=\"documents.delete\"]",
          "title": "Controlla prima di eliminare",
          "text": "Chiudi la guida e usa Elimina documento solo se vuoi rimuoverlo. Il comando richiede una conferma dedicata.",
          "optional": true,
          "readOnly": true
        }
      ],
      "requiresTarget": "[data-guide=\"documents.detail\"]",
      "unavailableMessage": "Non ci sono documenti personali visibili con questi filtri. Modifica la ricerca o riprova quando sarà disponibile un documento destinato a te. I documenti caricati per altre risorse non compaiono nella tua sezione personale."
    }
  ]
} satisfies PageGuideDefinition
