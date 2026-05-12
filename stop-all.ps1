# stop-all.ps1 — Detiene el sistema completo
Write-Host "Deteniendo procesos dotnet..." -ForegroundColor Yellow
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Deteniendo contenedores Docker..." -ForegroundColor Yellow
Set-Location "C:\Users\ernes\Invernadero"
docker compose down

Write-Host "Sistema detenido completamente." -ForegroundColor Green