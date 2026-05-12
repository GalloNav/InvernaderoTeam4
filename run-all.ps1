# run-all.ps1 — Levanta el sistema completo de Invernadero IoT
$ErrorActionPreference = "Continue"
$root = "C:\Users\ernes\Invernadero"

Write-Host "=== Sistema de Sensores en Invernadero ===" -ForegroundColor Green

Write-Host "`n[0/3] Limpiando procesos dotnet previos..." -ForegroundColor Yellow
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "`n[1/3] Levantando infraestructura Docker..." -ForegroundColor Cyan
Set-Location $root
docker compose up -d

Write-Host "`n[2/3] Esperando 20 segundos a que Keycloak/SQL/RabbitMQ esten listos..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

Write-Host "`n[3/3] Lanzando microservicios en tabs de Windows Terminal..." -ForegroundColor Cyan

# Tabs normales (dotnet run en su carpeta)
function Start-Tab {
    param([string]$Title, [string]$Path)
    Start-Process wt -ArgumentList "-w", "0", "new-tab", "--title", "`"$Title`"", "-d", "`"$Path`"", "powershell", "-NoExit", "-Command", "dotnet run"
    Start-Sleep -Milliseconds 500
}

# Tab especial que invoca un script auxiliar (para MS2-5012 que necesita env var)
function Start-TabScript {
    param([string]$Title, [string]$ScriptPath)
    Start-Process wt -ArgumentList "-w", "0", "new-tab", "--title", "`"$Title`"", "powershell", "-NoExit", "-File", "`"$ScriptPath`""
    Start-Sleep -Milliseconds 500
}

Start-Tab       -Title "MS5"          -Path "$root\src\MS5.SensoresUsuarios"
Start-Tab       -Title "MS2 (5002)"   -Path "$root\src\MS2.Mediciones"
Start-TabScript -Title "MS2 (5012)"   -ScriptPath "$root\start-ms2-5012.ps1"
Start-Tab       -Title "MS3"          -Path "$root\src\MS3.Alarmas"
Start-Tab       -Title "MS4"          -Path "$root\src\MS4.Notificaciones"
Start-Tab       -Title "Gateway"      -Path "$root\src\ApiGateway"
Start-Tab       -Title "MS1"          -Path "$root\src\MS1.Ingesta"
Start-Tab       -Title "Simulador"    -Path "$root\src\Simulador.Gateway"
Start-Tab       -Title "BlazorClient" -Path "$root\src\BlazorClient"

Write-Host "`n=== Servicios lanzados ===" -ForegroundColor Green
Write-Host "BlazorClient:        http://localhost:5006" -ForegroundColor Cyan
Write-Host "Keycloak admin:      http://localhost:8080  (admin/admin)" -ForegroundColor Gray
Write-Host "RabbitMQ mgmt:       http://localhost:15672 (guest/guest)" -ForegroundColor Gray
Write-Host "Gateway Swagger:     http://localhost:5000/swagger" -ForegroundColor Gray
Write-Host "`nUsuarios demo:" -ForegroundColor Cyan
Write-Host "  admin / admin123       (rol Admin)" -ForegroundColor White
Write-Host "  operador / operador123 (rol Operador)" -ForegroundColor White
Write-Host "  tecnico / tecnico123   (rol Tecnico)" -ForegroundColor White