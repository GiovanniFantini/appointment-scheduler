import type { PageGuideDefinition } from './types'

/** Guides for account pages shared by merchant and employee. */
export const accountPageGuides: Record<string, PageGuideDefinition> = {
  "/profile": {
    "id": "profile",
    "title": "Il mio profilo",
    "version": 1,
    "tours": [
      {
        "id": "overview",
        "title": "Consulta il profilo",
        "description": "Dati personali e sicurezza dell’account.",
        "steps": [
          {
            "target": "[data-guide=\"profile.page\"]",
            "title": "I tuoi dati",
            "text": "Controlla le informazioni dell’account. Per correggere i dati personali rivolgiti al responsabile."
          },
          {
            "target": "[data-guide=\"profile.password\"]",
            "title": "Cambia password",
            "text": "Inserisci password attuale, nuova password e conferma. Le indicazioni nel modulo mostrano i requisiti."
          },
          {
            "target": "[data-guide=\"profile.passwordSave\"]",
            "title": "Conferma la modifica",
            "text": "La guida non cambia la password. Completa il modulo e usa il comando di conferma quando sei pronto.",
            "readOnly": true
          }
        ]
      }
    ]
  },
  "/preferences": {
    "id": "preferences",
    "title": "Preferenze",
    "version": 1,
    "tours": [
      {
        "id": "overview",
        "title": "Personalizza l’aspetto",
        "description": "Scegli il tema dell’interfaccia.",
        "steps": [
          {
            "target": "[data-guide=\"preferences.page\"]",
            "title": "Le preferenze del browser",
            "text": "Le impostazioni di questa pagina vengono ricordate nel browser in uso."
          },
          {
            "target": "[data-guide=\"preferences.theme\"]",
            "title": "Scegli il tema",
            "text": "Seleziona Chiaro o Scuro. La modifica si applica subito e si salva automaticamente."
          }
        ]
      }
    ]
  },
  "/activity": {
    "id": "activity",
    "title": "Registro attività",
    "version": 1,
    "tours": [
      {
        "id": "overview",
        "title": "Consulta il registro",
        "description": "Filtri e dettaglio delle operazioni.",
        "steps": [
          {
            "target": "[data-guide=\"activity.filters\"]",
            "title": "Filtra le attività",
            "text": "Scegli categoria ed esito per restringere le operazioni visualizzate."
          },
          {
            "target": "[data-guide=\"activity.advanced\"]",
            "title": "Affina la ricerca",
            "text": "Apri Filtri avanzati per cercare per data, utente o oggetto."
          },
          {
            "target": "[data-guide=\"activity.page\"]",
            "title": "Leggi l’esito",
            "text": "Distingui le interazioni osservate dalle modifiche salvate. L’esportazione riguarda la pagina corrente del registro."
          }
        ]
      }
    ]
  }
}
