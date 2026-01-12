# Sistema di Versioning Automatico

Questo progetto utilizza un sistema di versioning dinamico basato su **Git tags e commit SHA**, che si aggiorna automaticamente senza intervento manuale.

## 🎯 Formato Versione

```
{semantic-version}+{commit-sha}

Esempi:
- 0.0.1+abc1234  (sviluppo locale)
- 1.2.3+def5678  (produzione)
```

## 📦 Componenti del Sistema

### 1. Backend API (ASP.NET Core)

**File:** `backend/AppointmentScheduler.API/Controllers/VersionController.cs`

```csharp
// Endpoint: GET /api/version
{
  "version": "0.0.1+abc1234",
  "environment": "Development",
  "buildDate": "2026-01-12T10:30:00Z",
  "apiName": "Appointment Scheduler API"
}
```

**Funzionamento:**
1. Legge `VERSION`, `GIT_COMMIT_SHA`, `BUILD_NUMBER`, `BUILD_TIME` da environment variables
2. Se env vars non disponibili (sviluppo locale), esegue `git` commands
3. Fallback a valori di default se Git non disponibile

### 2. Frontend (React + Vite)

**File critici (per ogni frontend: consumer-app, merchant-app, admin-app):**

```
vite-version-plugin.ts         - Plugin Vite per injection versione
vite.config.ts                  - Configurazione Vite (include plugin)
src/vite-env.d.ts              - Type definitions per costanti
src/components/VersionInfo.tsx - Componente UI per display versione
```

**Funzionamento:**
1. `vite-version-plugin.ts` esegue git commands durante il build:
   ```bash
   git describe --tags --abbrev=0  # → v0.0.1
   git rev-parse --short HEAD      # → abc1234
   ```
2. Inietta costanti globali:
   ```typescript
   __APP_VERSION__ = "0.0.1"
   __GIT_COMMIT__ = "abc1234"
   __GIT_BRANCH__ = "main"
   __BUILD_TIME__ = "2026-01-12T10:30:00Z"
   ```
3. `VersionInfo.tsx` mostra le versioni nell'UI

### 3. GitHub Actions CI/CD

**File:** `.github/workflows/deploy-*.yml` (4 workflow)

Ogni workflow include lo step **"Extract version info"**:

```yaml
- name: Extract version info
  id: version
  run: |
    GIT_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.1")
    VERSION=${GIT_TAG#v}
    GIT_COMMIT=$(git rev-parse --short HEAD)
    BUILD_NUMBER=${{ github.run_number }}
    BUILD_TIME=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    echo "VERSION=$VERSION" >> $GITHUB_OUTPUT
    echo "GIT_COMMIT=$GIT_COMMIT" >> $GITHUB_OUTPUT
    # ...

- name: Build application
  env:
    VERSION: ${{ steps.version.outputs.VERSION }}
    GIT_COMMIT_SHA: ${{ steps.version.outputs.GIT_COMMIT }}
    # ...
```

## 🚀 Workflow di Rilascio

### Sviluppo Locale

```bash
# Modifiche al codice
git add .
git commit -m "feat: nuova feature"

# Build locale - versione si aggiorna automaticamente
cd frontend/consumer-app
npm run build
# → Mostra: "0.0.1+abc1234" (commit SHA aggiornato)
```

### Rilascio in Produzione

```bash
# 1. Completa tutte le modifiche e commit
git add .
git commit -m "feat: sistema di prenotazioni completo"

# 2. Crea tag semantico
git tag -a v1.0.0 -m "Release 1.0.0: Prima release produzione"

# 3. Push commit e tag
git push origin main
git push origin v1.0.0

# 4. GitHub Actions si attiva automaticamente
#    - Estrae versione "1.0.0" dal tag v1.0.0
#    - Estrae commit SHA (es. "abc1234")
#    - Build number incrementale (es. "42")
#    - Deploy in produzione con versione "1.0.0+abc1234"
```

### Incremento Versioni (Semantic Versioning)

```bash
# PATCH (bugfix): 0.0.1 → 0.0.2
git tag v0.0.2 -m "Fix: corregge bug prenotazioni"
git push origin v0.0.2

# MINOR (nuova feature): 0.0.2 → 0.1.0
git tag v0.1.0 -m "Feature: aggiunge notifiche email"
git push origin v0.1.0

# MAJOR (breaking change): 0.1.0 → 1.0.0
git tag v1.0.0 -m "Release: prima versione produzione"
git push origin v1.0.0
```

## ⚙️ Configurazione Dettagliata

### Aggiungere Versioning a un Nuovo Frontend

Se aggiungi un nuovo frontend (es. `partner-app`):

**1. Crea `vite-version-plugin.ts`:**
```bash
cp frontend/consumer-app/vite-version-plugin.ts frontend/partner-app/
```

**2. Aggiorna `vite.config.ts`:**
```typescript
import { viteVersionPlugin } from './vite-version-plugin'

export default defineConfig({
  plugins: [react(), viteVersionPlugin()],
  // ...
})
```

**3. Aggiorna `src/vite-env.d.ts`:**
```typescript
// Version info injected at build time
declare const __APP_VERSION__: string;
declare const __GIT_COMMIT__: string;
declare const __GIT_BRANCH__: string;
declare const __BUILD_TIME__: string;
```

**4. Crea `src/components/VersionInfo.tsx`:**
```bash
cp frontend/consumer-app/src/components/VersionInfo.tsx frontend/partner-app/src/components/
```

**5. Crea GitHub Action workflow:**
```bash
cp .github/workflows/deploy-consumer-app.yml .github/workflows/deploy-partner-app.yml
# Modifica le occorrenze di "consumer" in "partner"
```

### Aggiungere Versioning a un Nuovo Backend

Se aggiungi un nuovo backend:

**1. Crea `VersionController.cs`:**
```bash
cp backend/AppointmentScheduler.API/Controllers/VersionController.cs backend/NewAPI/Controllers/
```

**2. Crea `VersionResponse.cs`:**
```bash
cp backend/AppointmentScheduler.Shared/DTOs/VersionResponse.cs backend/NewShared/DTOs/
```

**3. Aggiorna GitHub Action workflow** per iniettare env vars dopo il deploy

## 🔍 Testing e Verifica

### Test Build Locale

```bash
# Frontend
cd frontend/consumer-app
npm run build

# Verifica output - cerca nel bundle le costanti iniettate
grep -r "0.0.1" dist/

# Backend (se .NET disponibile)
cd backend/AppointmentScheduler.API
dotnet build
dotnet run
# Testa: curl http://localhost:5000/api/version
```

### Test in CI/CD

```bash
# Trigger manualmente un workflow
gh workflow run deploy-consumer-app.yml

# Monitora i log
gh run watch

# Verifica environment variables nel log dello step "Extract version info"
```

### Verifica in Produzione

Dopo il deploy, visita la pagina di login dell'app e controlla il footer:

```
[App v1.0.0+abc1234] [API v1.0.0+abc1234] [Production]
```

## 🐛 Troubleshooting

### Frontend mostra "0.0.1+dev"

**Causa:** Git commands falliscono durante il build

**Soluzione:**
```bash
# Verifica Git disponibile
which git

# Verifica repository Git
git status

# Verifica che vite-version-plugin.ts esista
ls frontend/consumer-app/vite-version-plugin.ts

# Rebuild con verbose
npm run build -- --debug
```

### Backend mostra "0.0.0+dev"

**Causa:** Git non disponibile e nessuna env var configurata

**Soluzione:**
```bash
# Verifica Git tags
git tag -l

# Crea un tag se mancante
git tag v0.0.1
git push origin v0.0.1

# Verifica che VersionController.cs non sia stato modificato
git diff backend/AppointmentScheduler.API/Controllers/VersionController.cs
```

### GitHub Actions non inietta versioni

**Causa:** Step "Extract version info" mancante o env vars non passate

**Soluzione:**
```bash
# Verifica workflow
cat .github/workflows/deploy-consumer-app.yml | grep "Extract version"

# Verifica che le env vars siano passate al build
cat .github/workflows/deploy-consumer-app.yml | grep "VERSION:"
cat .github/workflows/deploy-consumer-app.yml | grep "GIT_COMMIT_SHA:"
```

### Build fallisce con "Cannot find module vite-version-plugin"

**Causa:** Plugin non trovato

**Soluzione:**
```bash
# Verifica che il file esista
ls frontend/consumer-app/vite-version-plugin.ts

# Verifica import in vite.config.ts
grep "viteVersionPlugin" frontend/consumer-app/vite.config.ts

# Se mancante, copia da altro frontend
cp frontend/merchant-app/vite-version-plugin.ts frontend/consumer-app/
```

## 📋 Checklist Pre-Deploy

Prima di ogni deploy in produzione, verifica:

- [ ] Tutti i commit sono pushati
- [ ] Tag semantico creato e pushato
- [ ] GitHub Actions workflow include "Extract version info"
- [ ] Nessuna modifica manuale a package.json version
- [ ] Nessuna modifica manuale a .csproj version
- [ ] vite-version-plugin.ts presente in tutti i frontend
- [ ] VersionInfo.tsx mostra versioni in UI
- [ ] Backend VersionController.cs non modificato

## 🔗 Riferimenti

- **Git Tagging:** https://git-scm.com/book/en/v2/Git-Basics-Tagging
- **Semantic Versioning:** https://semver.org/
- **GitHub Actions Outputs:** https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#setting-an-output-parameter
- **Vite Plugin API:** https://vitejs.dev/guide/api-plugin.html

## ❓ FAQ

**Q: Posso modificare manualmente la versione in package.json?**
A: ❌ No. Le versioni sono gestite tramite Git tags. Modificare package.json non ha effetto.

**Q: Come faccio a incrementare la versione?**
A: ✅ Crea un nuovo Git tag: `git tag v1.2.3 && git push origin v1.2.3`

**Q: Cosa succede se non ho creato tag Git?**
A: Il sistema usa il fallback "v0.0.1" automaticamente.

**Q: Posso vedere la versione nell'UI?**
A: ✅ Sì, il componente VersionInfo mostra versione frontend e backend nella pagina di login.

**Q: Il commit SHA è sicuro da mostrare pubblicamente?**
A: ✅ Sì, il commit SHA è pubblico su GitHub ed è utile per tracciabilità.

**Q: Funziona anche in sviluppo locale?**
A: ✅ Sì, il vite-version-plugin esegue git commands automaticamente durante npm build.

**Q: Cosa succede se Git non è installato nell'ambiente di build?**
A: Il sistema usa env vars iniettate da GitHub Actions. In locale, usa fallback values.
