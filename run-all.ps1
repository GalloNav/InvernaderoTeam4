# run-all.ps1 — Levanta el sistema completo de Invernadero IoT
$ErrorActionPreference = "Continue"
$root = "C:\Users\ernes\Invernadero"

Write-Host "=== Sistema de Sensores en Invernadero ===" -ForegroundColor Green

Write-Host "`n[0/4] Limpiando procesos dotnet previos..." -ForegroundColor Yellow
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "`n[1/4] Levantando infraestructura Docker..." -ForegroundColor Cyan
Set-Location $root
docker compose up -d

Write-Host "`n[2/4] Esperando 20 segundos a que Keycloak/SQL/RabbitMQ esten listos..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

Write-Host "`n[3/4] Verificando scripts auxiliares..." -ForegroundColor Cyan
$scriptsRequeridos = @("start-ms2-5012.ps1", "start-ms3-5013.ps1", "start-ms5-5015.ps1")
foreach ($s in $scriptsRequeridos) {
    $ruta = Join-Path $root $s
    if (-not (Test-Path $ruta)) {
        Write-Host "ERROR: Falta el script $s en la raiz del proyecto." -ForegroundColor Red
        exit 1
    }
}
Write-Host "Scripts auxiliares OK." -ForegroundColor Green

Write-Host "`n[4/4] Lanzando microservicios en tabs de Windows Terminal..." -ForegroundColor Cyan

function Start-Tab {
    param([string]$Title, [string]$Path)
    Start-Process wt -ArgumentList "-w", "0", "new-tab", "--title", "`"$Title`"", "-d", "`"$Path`"", "powershell", "-NoExit", "-Command", "dotnet run"
    Start-Sleep -Milliseconds 500
}

function Start-TabScript {
    param([string]$Title, [string]$ScriptPath)
    Start-Process wt -ArgumentList "-w", "0", "new-tab", "--title", "`"$Title`"", "powershell", "-NoExit", "-File", "`"$ScriptPath`""
    Start-Sleep -Milliseconds 500
}

# MS5 — primera y segunda instancia
Start-Tab       -Title "MS5 (5005)"   -Path "$root\src\MS5.SensoresUsuarios"
Start-TabScript -Title "MS5 (5015)"   -ScriptPath "$root\start-ms5-5015.ps1"

# MS2 — primera y segunda instancia
Start-Tab       -Title "MS2 (5002)"   -Path "$root\src\MS2.Mediciones"
Start-TabScript -Title "MS2 (5012)"   -ScriptPath "$root\start-ms2-5012.ps1"

# MS3 — primera y segunda instancia
Start-Tab       -Title "MS3 (5003)"   -Path "$root\src\MS3.Alarmas"
Start-TabScript -Title "MS3 (5013)"   -ScriptPath "$root\start-ms3-5013.ps1"

# Instancias unicas
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
Write-Host "`nInstancias balanceadas activas:" -ForegroundColor Cyan
Write-Host "  MS2 -> 5002 + 5012" -ForegroundColor White
Write-Host "  MS3 -> 5003 + 5013" -ForegroundColor White
Write-Host "  MS5 -> 5005 + 5015" -ForegroundColor White
Write-Host "`nUsuarios demo:" -ForegroundColor Cyan
Write-Host "  admin / admin123       (rol Admin)" -ForegroundColor White
Write-Host "  operador / operador123 (rol Operador)" -ForegroundColor White
Write-Host "  tecnico / tecnico123   (rol Tecnico)" -ForegroundColor White
