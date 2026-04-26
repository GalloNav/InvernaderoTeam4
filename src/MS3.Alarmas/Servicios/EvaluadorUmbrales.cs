using Invernadero.Contracts.Alarmas;

namespace MS3.Alarmas.Servicios;

public sealed class EvaluadorUmbrales
{
    public IReadOnlyList<(TipoAlarma Tipo, float ValorMedido, float Umbral)> Evaluar(
        float temperatura,
        float humedad,
        UmbralEfectivo umbral)
    {
        var alarmas = new List<(TipoAlarma, float, float)>();

        if (temperatura > 0f)
        {
            if (temperatura > umbral.TempMax)
                alarmas.Add((TipoAlarma.TemperaturaAlta, temperatura, umbral.TempMax));
            else if (temperatura < umbral.TempMin)
                alarmas.Add((TipoAlarma.TemperaturaBaja, temperatura, umbral.TempMin));
        }

        if (humedad > 0f)
        {
            if (humedad > umbral.HumMax)
                alarmas.Add((TipoAlarma.HumedadAlta, humedad, umbral.HumMax));
            else if (humedad < umbral.HumMin)
                alarmas.Add((TipoAlarma.HumedadBaja, humedad, umbral.HumMin));
        }

        return alarmas;
    }
}
