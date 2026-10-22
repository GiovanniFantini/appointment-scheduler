import type { PageGuideDefinition } from '@scheduler/ui'

/** Contextual paths for this page. Keep targets aligned with data-guide anchors. */
export default {
  "id": "inventory",
  "title": "Magazzino",
  "version": 2,
  "feature": "Magazzino",
  "tours": [
    {
      "id": "overview",
      "title": "Orientami nel magazzino",
      "description": "Filiale, sezioni e disponibilità.",
      "steps": [
        {
          "target": "[data-guide=\"inventory.branch\"]",
          "title": "Scegli la filiale",
          "text": "Stock, movimenti e ordini dipendono dalla filiale operativa selezionata."
        },
        {
          "target": "[data-guide=\"inventory.tabs\"]",
          "title": "Scegli la sezione",
          "text": "Panoramica riassume la situazione. Articoli, Movimenti, Fornitori e Ordini acquisto mostrano i rispettivi dettagli."
        },
        {
          "target": "[data-guide=\"inventory.page\"]",
          "title": "Consulta il tuo perimetro",
          "text": "Le azioni disponibili dipendono dal tuo livello: consultazione, operatività o gestione completa."
        }
      ]
    },
    {
      "id": "items",
      "title": "Consulta gli articoli",
      "description": "Cerca nel catalogo e leggi le giacenze.",
      "steps": [
        {
          "target": "[data-guide=\"inventory.tab.items\"]",
          "title": "Apri Articoli",
          "text": "Seleziona Articoli per aprire il catalogo.",
          "nextWhen": "[data-guide=\"inventory.search\"]"
        },
        {
          "target": "[data-guide=\"inventory.search\"]",
          "title": "Cerca un articolo",
          "text": "Usa la ricerca per restringere il catalogo. Le schede mostrano unità di misura, giacenze e stato dell’articolo."
        },
        {
          "target": "[data-guide=\"inventory.branch\"]",
          "title": "Controlla la filiale",
          "text": "Verifica la filiale operativa per interpretare correttamente le disponibilità."
        }
      ]
    },
    {
      "id": "adjust",
      "title": "Rettifica una giacenza",
      "description": "Articolo, variazione e motivo.",
      "steps": [
        {
          "target": "[data-guide=\"inventory.tab.movements\"]",
          "title": "Apri Movimenti",
          "text": "Seleziona Movimenti per trovare la rettifica stock.",
          "nextWhen": "[data-guide=\"inventory.adjustment\"]"
        },
        {
          "target": "[data-guide=\"inventory.adjustmentItem\"]",
          "title": "Scegli articolo e filiale",
          "text": "Controlla articolo e filiale prima di modificare la giacenza."
        },
        {
          "target": "[data-guide=\"inventory.delta\"]",
          "title": "Indica la variazione",
          "text": "Inserisci la differenza, per esempio +12 o -3, non il saldo finale. Aggiungi costo e motivo richiesti."
        },
        {
          "target": "[data-guide=\"inventory.adjustmentSave\"]",
          "title": "Registra quando sei pronto",
          "text": "Chiudi la guida e premi Registra rettifica per aggiornare la giacenza e lo storico.",
          "readOnly": true
        }
      ],
      "minLevel": "Operator",
      "unavailableWhen": "[data-guide=\"inventory.noBranches\"]",
      "unavailableMessage": "Non hai filiali assegnate: la rettifica stock non è disponibile. Chiedi al responsabile l’assegnazione di una filiale e riprova."
    },
    {
      "id": "receive",
      "title": "Ricevi un ordine",
      "description": "Ordine, quantità effettive e conferma.",
      "steps": [
        {
          "target": "[data-guide=\"inventory.tab.orders\"]",
          "title": "Apri Ordini acquisto",
          "text": "Seleziona Ordini acquisto.",
          "nextWhen": "[data-guide=\"inventory.orders\"]"
        },
        {
          "target": "[data-guide=\"inventory.orders\"]",
          "title": "Scegli l’ordine",
          "text": "Su un ordine confermato con quantità ancora da ricevere trovi Registra ricezione. Se non ci sono ordini adatti, il comando non compare."
        },
        {
          "target": "[data-guide=\"inventory.receive\"]",
          "title": "Apri la ricezione",
          "text": "Premi Registra ricezione su un ordine con merce ancora da ricevere.",
          "optional": true,
          "nextWhen": "[data-guide=\"inventory.receiptForm\"]"
        },
        {
          "target": "[data-guide=\"inventory.receivedQuantity\"]",
          "title": "Indica le quantità arrivate",
          "text": "Inserisci solo le quantità effettivamente ricevute. Puoi registrare anche una ricezione parziale.",
          "optional": true
        },
        {
          "target": "[data-guide=\"inventory.receiptSave\"]",
          "title": "Registra quando sei pronto",
          "text": "Chiudi la guida e premi Registra ricezione. La conferma aggiorna giacenze e stato dell’ordine.",
          "optional": true,
          "readOnly": true
        }
      ],
      "minLevel": "Operator"
    },
    {
      "id": "createItem",
      "title": "Crea un articolo",
      "description": "Identificazione, unità e soglia di riordino.",
      "steps": [
        {
          "target": "[data-guide=\"inventory.tab.items\"]",
          "title": "Apri Articoli",
          "text": "Seleziona Articoli.",
          "nextWhen": "[data-guide=\"inventory.newItem\"]"
        },
        {
          "target": "[data-guide=\"inventory.newItem\"]",
          "title": "Apri il nuovo articolo",
          "text": "Premi Nuovo articolo.",
          "nextWhen": "[data-guide=\"inventory.itemForm\"]"
        },
        {
          "target": "[data-guide=\"inventory.itemForm\"]",
          "title": "Compila l’anagrafica",
          "text": "Indica SKU univoco, nome e unità di misura. La soglia di riordino aiuta a individuare gli articoli sotto scorta."
        },
        {
          "target": "[data-guide=\"inventory.itemSave\"]",
          "title": "Salva quando sei pronto",
          "text": "Chiudi la guida e premi Salva articolo. Le giacenze si registrano attraverso i movimenti.",
          "readOnly": true
        }
      ],
      "minLevel": "Manager"
    },
    {
      "id": "supplier",
      "title": "Crea un fornitore",
      "description": "Anagrafica e contatti per gli acquisti.",
      "steps": [
        {
          "target": "[data-guide=\"inventory.tab.suppliers\"]",
          "title": "Apri Fornitori",
          "text": "Seleziona Fornitori.",
          "nextWhen": "[data-guide=\"inventory.newSupplier\"]"
        },
        {
          "target": "[data-guide=\"inventory.newSupplier\"]",
          "title": "Apri il nuovo fornitore",
          "text": "Premi Nuovo fornitore.",
          "nextWhen": "[data-guide=\"inventory.supplierForm\"]"
        },
        {
          "target": "[data-guide=\"inventory.supplierForm\"]",
          "title": "Compila i contatti",
          "text": "Inserisci il nome e i contatti utili per gli acquisti."
        },
        {
          "target": "[data-guide=\"inventory.supplierSave\"]",
          "title": "Salva il fornitore",
          "text": "Chiudi la guida e salva per renderlo selezionabile negli ordini.",
          "readOnly": true
        }
      ],
      "minLevel": "Manager"
    },
    {
      "id": "order",
      "title": "Crea un ordine di acquisto",
      "description": "Fornitore, filiale e righe dell’ordine.",
      "steps": [
        {
          "target": "[data-guide=\"inventory.tab.orders\"]",
          "title": "Apri Ordini acquisto",
          "text": "Seleziona Ordini acquisto.",
          "nextWhen": "[data-guide=\"inventory.newOrder\"]"
        },
        {
          "target": "[data-guide=\"inventory.newOrder\"]",
          "title": "Apri il nuovo ordine",
          "text": "Premi Nuovo ordine.",
          "nextWhen": "[data-guide=\"inventory.orderForm\"]"
        },
        {
          "target": "[data-guide=\"inventory.orderForm\"]",
          "title": "Compila l’ordine",
          "text": "Scegli filiale e fornitore. Aggiungi articoli, quantità e costi unitari. Servono articoli e fornitori già configurati."
        },
        {
          "target": "[data-guide=\"inventory.orderSave\"]",
          "title": "Crea la bozza",
          "text": "Chiudi la guida e premi Crea ordine. Controlla la bozza prima di usare Conferma ordine.",
          "readOnly": true
        }
      ],
      "minLevel": "Manager"
    }
  ]
} satisfies PageGuideDefinition
