# start-ms3-5013.ps1 — Segunda instancia de MS3 en puerto 5013
$env:ASPNETCORE_URLS = "http://localhost:5013"
Set-Location "C:\Users\ernes\Invernadero\src\MS3.Alarmas"
dotnet run --no-launch-profile
