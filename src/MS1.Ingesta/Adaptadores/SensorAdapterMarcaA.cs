using Google.Protobuf.WellKnownTypes;
using Invernadero.Contracts.Mediciones;

namespace MS1.Ingesta.Adaptadores;

// Frame: [0xA1][sensorId 1B][temp float LE 4B][hum float LE 4B] = 10 bytes
public class SensorAdapterMarcaA : ISensorAdapter
{
    public bool TryParse(byte[] frame, out MedicionEvent? medicion)
    {
        if (frame.Length < 10)
        {
            medicion = null;
            return false;
        }

        medicion = new MedicionEvent
        {
            MedicionId    = Guid.NewGuid().ToString(),
            SensorId      = frame[1].ToString(),
            InvernaderoId = "INV-001",
            Timestamp     = Timestamp.FromDateTime(DateTime.UtcNow),
            Temperatura   = BitConverter.ToSingle(frame, 2),
            Humedad       = BitConverter.ToSingle(frame, 6),
            Marca         = "MarcaA"
        };
        return true;
    }
}
