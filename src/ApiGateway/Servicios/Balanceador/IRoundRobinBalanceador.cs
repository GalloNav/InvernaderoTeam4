namespace ApiGateway.Servicios.Balanceador;

public interface IRoundRobinBalanceador
{
    Uri ObtenerSiguiente(string servicio);
}
