namespace MS1.Ingesta.Adaptadores;

public static class AdapterFactory
{
    public const byte MarcaAMarker = 0xA1;
    public const byte MarcaBMarker = 0xB2;
    public const byte MarcaCMarker = 0xC3;

    // Returns total frame size in bytes (including the marker byte).
    // -1 means unknown/unsupported marker.
    public static int GetFrameSize(byte marker) => marker switch
    {
        MarcaAMarker => 10,  // 1 + 1 + 4 + 4
        MarcaBMarker => 11,  // 1 + 2 + 4 + 4
        MarcaCMarker => 1,   // solo el marcador antes de lanzar
        _            => -1
    };

    public static ISensorAdapter GetAdapter(byte marker) => marker switch
    {
        MarcaAMarker => new SensorAdapterMarcaA(),
        MarcaBMarker => new SensorAdapterMarcaB(),
        MarcaCMarker => new SensorAdapterMarcaC(),
        _            => throw new ArgumentException($"Marcador desconocido: 0x{marker:X2}")
    };
}
