using Invernadero.Contracts.Mediciones;

namespace MS1.Ingesta.Adaptadores;

public interface ISensorAdapter
{
    bool TryParse(byte[] frame, out MedicionEvent? medicion);
}
