# start-ms5-5015.ps1 — Segunda instancia de MS5 en puerto 5015
$env:ASPNETCORE_URLS = "http://localhost:5015"
Set-Location "C:\Users\ernes\Invernadero\src\MS5.SensoresUsuarios"
dotnet run --no-launch-profile
