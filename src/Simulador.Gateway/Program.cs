using System.Buffers.Binary;
using System.Net.Sockets;
using Serilog;

var host       = "localhost";
var port       = 6000;
var intervalMs = 2000;

for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--host":     host = args[i + 1]; break;
        case "--port":     int.TryParse(args[i + 1], out port); break;
        case "--interval": int.TryParse(args[i + 1], out intervalMs); break;
    }
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("Simulador arrancando → {Host}:{Port} (intervalo {Interval}ms)", host, port, intervalMs);

var rng = new Random();

while (true)
{
    var sensorId  = rng.Next(1, 6);                                      // 1..5
    var temp      = (float)(rng.NextDouble() * (35.0 - 18.0) + 18.0);   // 18–35 °C
    var hum       = (float)(rng.NextDouble() * (90.0 - 40.0) + 40.0);   // 40–90 %
    var useMarcaA = rng.Next(2) == 0;

    byte[] frame;
    string label;

    if (useMarcaA)
    {
        // [0xA1][sensorId 1B][temp float LE 4B][hum float LE 4B]
        label    = "MarcaA";
        frame    = new byte[10];
        frame[0] = 0xA1;
        frame[1] = (byte)sensorId;
        BitConverter.TryWriteBytes(frame.AsSpan(2), temp);
        BitConverter.TryWriteBytes(frame.AsSpan(6), hum);
    }
    else
    {
        // [0xB2][sensorId 2B BE][temp int*100 BE 4B][hum int*100 BE 4B]
        label    = "MarcaB";
        frame    = new byte[11];
        frame[0] = 0xB2;
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(1), (ushort)sensorId);
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(3),  (int)(temp * 100));
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(7),  (int)(hum  * 100));
    }

    try
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port);
        var stream = tcp.GetStream();
        await stream.WriteAsync(frame);
        await stream.FlushAsync();
        Log.Information("→ Enviado [{Label}] sensor={SensorId} T={Temp:F1} H={Hum:F1}",
            label, sensorId, temp, hum);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "No se pudo enviar frame — ¿MS1 está arrancado?");
    }

    await Task.Delay(intervalMs);
}
