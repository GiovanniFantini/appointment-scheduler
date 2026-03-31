# Gestionale Aziendale — Appointment Scheduler

Piattaforma B2B2E per la gestione di eventi aziendali, turni, ferie e risorse umane.

## Architettura

```text
appointment-scheduler/
├── backend/                    # .NET 8 ASP.NET Core API
│   ├── AppointmentScheduler.API/       # Controllers, Program.cs
│   ├── AppointmentScheduler.Core/      # Business logic, Services
│   ├── AppointmentScheduler.Data/      # EF Core DbContext, Migrations
│   └── AppointmentScheduler.Shared/   # Models, DTOs, Enums
└── frontend/
    ├── admin-app/      # Admin panel (port 5175)
    ├── merchant-app/   # Merchant dashboard (port 5174)
    ├── employee-app/   # Employee portal (port 5176)
    └── consumer-app/   # [Work in Progress] (port 5173)
```

## Stack

| Layer | Tecnologia |
|---|---|
| Backend | .NET 8, ASP.NET Core, EF Core, PostgreSQL |
| Auth | JWT Bearer |
| Frontend | React 18, TypeScript, Vite |
| Calendario | FullCalendar v6 |
| Storage | Azure Blob Storage |
| Email | Azure Communication Services |
| Deploy | Azure App Service |

## Account Types

| Tipo | Descrizione |
|---|---|
| **Admin** | God mode — gestisce merchant, report piattaforma, utenti |
| **Merchant** | Persona giuridica/azienda — gestisce eventi, risorse, ruoli |
| **Employee** | Dipendente/risorsa — vede il proprio calendario, fa richieste |

## Entità Centrale: Evento

| Campo | Tipo | Note |
|---|---|---|
| Titolo | string | Obbligatorio |
| Tipologia | enum | Turno, ChiusuraAziendale, Ferie, Permessi, Malattia |
| Data inizio | DateOnly | Obbligatoria |
| Data fine | DateOnly? | |
| Tutto il giorno | bool | |
| Orario da/a | TimeOnly? | Precisione al minuto |
| Reperibilità | bool | Solo Turno — on-call |
| Titolari | EmployeeParticipant[] | Titolare + co-titolari |
| Ricorrenza | string? | Formato crontab-like |
| Notifica | bool | |
| Note | string? | |

## Sistema Ruoli Merchant

I ruoli sono custom per azienda. Feature disponibili:
**Calendario** · **Richieste** · **Risorse** · **Ruoli** · **Documenti** · **Report**

Il ruolo **"Responsabile App"** viene creato automaticamente con tutte le feature attive.

## Colori Calendario (Employee view)

| Tipo | Colore |
|---|---|
| Turno | Blu #3b82f6 |
| Ferie | Rosa #ec4899 |
| Permessi | Viola #8b5cf6 |
| Malattia | Ambra #f59e0b |
| Chiusura aziendale | Nero #1f2937 |
| Turni scoperti | Rosso #ef4444 |
| Reperibilità | Bordo tratteggiato |

## Multi-Company Employee

Un dipendente può appartenere a più aziende. Al login, se ha più aziende, vede la schermata di selezione. Il JWT company-specific include `MerchantId`, `EmployeeId` e le `Feature[]` abilitate.

## Setup Locale

Vedi [SETUP.md](SETUP.md) per le istruzioni complete.

## API

Swagger disponibile su `http://localhost:5000/swagger`.

| Endpoint auth | Descrizione |
|---|---|
| `POST /api/auth/admin/login` | Login admin |
| `POST /api/auth/merchant/login` | Login merchant |
| `POST /api/auth/merchant/register` | Registrazione merchant |
| `POST /api/auth/employee/login` | Login employee |
| `POST /api/auth/employee/register` | Registrazione employee |
| `POST /api/auth/employee/select-company/:id` | Seleziona azienda |
| `POST /api/auth/forgot-password` | Reset password |
| `POST /api/auth/reset-password` | Conferma reset |

## Azure Integrations

- **Azure Database for PostgreSQL** — database principale
- **Azure Blob Storage** — documenti HR/payroll
- **Azure Communication Services** — email transazionale
- **Azure App Service** — hosting (1 backend + 4 frontend)
