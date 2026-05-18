# start-ms2-5012.ps1 — Segunda instancia de MS2 en puerto 5012
$env:ASPNETCORE_URLS = "http://localhost:5012"
Set-Location "C:\Users\ernes\Invernadero\src\MS2.Mediciones"
dotnet run --no-launch-profile
