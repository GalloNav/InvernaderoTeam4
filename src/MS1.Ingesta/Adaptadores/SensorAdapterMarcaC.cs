using Invernadero.Contracts.Mediciones;

namespace MS1.Ingesta.Adaptadores;

public class SensorAdapterMarcaC : ISensorAdapter
{
    public bool TryParse(byte[] frame, out MedicionEvent? medicion)
    {
        medicion = null;
        throw new NotImplementedException("Sensor Marca C no está soportado aún");
    }
}
