# Sistema di Versioning Automatico

Sistema di versioning Git-based con formato `{version}+{commit-sha}` (es. `1.0.0+abc1234`).

## Formato Versione

- **Version**: Git tag (es. `v1.0.0` → `1.0.0`)
- **Commit SHA**: Hash corto (es. `abc1234`)
- **Build time**: Timestamp UTC

## Come Funziona

### Backend API
- Legge `VERSION`, `GIT_COMMIT_SHA` da environment variables (CI/CD)
- Fallback a git commands in sviluppo locale
- Endpoint: `GET /api/version`

### Frontend (React + Vite)
- `vite-version-plugin.ts` esegue git commands durante build
- Inietta costanti globali: `__APP_VERSION__`, `__GIT_COMMIT__`
- `VersionInfo.tsx` mostra versioni in UI

### GitHub Actions
- Step "Extract version info" in tutti i workflow
- Inietta env vars durante build

## Incrementare Versione

```bash
# Crea Git tag
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0

# GitHub Actions deploy automaticamente
```

**Semantic Versioning:**
- PATCH: `v0.0.2` (bugfix)
- MINOR: `v0.1.0` (nuova feature)
- MAJOR: `v1.0.0` (breaking change)

## File Critici

**NON modificare senza consultare documentazione:**

### Backend
- `backend/AppointmentScheduler.API/Controllers/VersionController.cs`

### Frontend (tutti e 3)
- `vite-version-plugin.ts`
- `vite.config.ts` (deve includere `viteVersionPlugin()`)
- `src/vite-env.d.ts`
- `src/components/VersionInfo.tsx`

### GitHub Actions
- `.github/workflows/deploy-*.yml` (step "Extract version info" obbligatorio)

## Verifica

**Sviluppo locale:**
```bash
# Frontend
cd frontend/consumer-app
npm run build

# Backend
curl http://localhost:5000/api/version
```

**Produzione:**
Controlla footer UI: `[App v1.0.0+abc1234] [API v1.0.0+abc1234]`

## Regole Importanti

### ❌ NON FARE MAI
- Modificare versioni in `package.json` o `.csproj`
- Rimuovere `vite-version-plugin.ts`
- Hardcodare versioni nel codice

### ✅ FARE SEMPRE
- Usare Git tags per incrementare versioni
- Verificare che `vite.config.ts` includa `viteVersionPlugin()`
- Mantenere step "Extract version info" nei workflow GitHub Actions

## Troubleshooting

**Frontend mostra "0.0.1+dev":**
- Verifica che `vite-version-plugin.ts` esista
- Rebuild: `npm run build`

**Backend mostra "0.0.0+dev":**
- Crea Git tag: `git tag v0.0.1 && git push origin v0.0.1`

**Vedi [TROUBLESHOOTING.md](TROUBLESHOOTING.md) per problemi comuni**
