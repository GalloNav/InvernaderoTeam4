using System.Buffers.Binary;
using Google.Protobuf.WellKnownTypes;
using Invernadero.Contracts.Mediciones;

namespace MS1.Ingesta.Adaptadores;

// Frame: [0xB2][sensorId 2B BE][temp int*100 BE 4B][hum int*100 BE 4B] = 11 bytes
public class SensorAdapterMarcaB : ISensorAdapter
{
    public bool TryParse(byte[] frame, out MedicionEvent? medicion)
    {
        if (frame.Length < 11)
        {
            medicion = null;
            return false;
        }

        var sensorId  = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(1));
        var tempCenti = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(3));
        var humCenti  = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(7));

        medicion = new MedicionEvent
        {
            MedicionId    = Guid.NewGuid().ToString(),
            SensorId      = sensorId.ToString(),
            InvernaderoId = "INV-001",
            Timestamp     = Timestamp.FromDateTime(DateTime.UtcNow),
            Temperatura   = tempCenti / 100.0f,
            Humedad       = humCenti  / 100.0f,
            Marca         = "MarcaB"
        };
        return true;
    }
}
