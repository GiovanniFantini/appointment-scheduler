@echo off
echo ====================================================
echo Avvio backend e frontend...
echo ====================================================

REM Salvo la cartella del batch (assoluta)
set "SCRIPT_DIR=%~dp0"

REM Avvio backend
echo Avvio backend...
if exist "%SCRIPT_DIR%backend\AppointmentScheduler.API" (
    start "" cmd /k "cd /d "%SCRIPT_DIR%backend\AppointmentScheduler.API" && dotnet run"
) else (
    echo ERRORE: percorso backend non trovato: "%SCRIPT_DIR%backend\AppointmentScheduler.API"
)
timeout /t 5 /nobreak >nul

REM Lista delle cartelle frontend
set "FRONTEND_FOLDERS=admin-app merchant-app employee-app consumer-app"

REM Ciclo per frontend
for %%F in (%FRONTEND_FOLDERS%) do (
    if exist "%SCRIPT_DIR%frontend\%%F" (
		echo Avvio frontend/%%F...
        start "" cmd /k "cd /d "%SCRIPT_DIR%frontend\%%F" && npm run dev"
    ) else (
        echo ERRORE: percorso frontend/%%F non trovato: "%SCRIPT_DIR%frontend\%%F"
    )
    timeout /t 1 /nobreak >nul
)

echo ====================================================
echo Tutti i server sono stati avviati!
echo Verifica eventuali errori sopra.
pause