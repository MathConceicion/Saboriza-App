namespace GastronomiaMarketplace.Infrastructure.Geolocation;

public static class HaversineCalculator
{
    private const double RaioTerraKm = 6371.0088;

    public static double CalcularKm(double lat1, double lon1, double lat2, double lon2)
    {
        double ToRad(double deg) => deg * Math.PI / 180.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var rLat1 = ToRad(lat1);
        var rLat2 = ToRad(lat2);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return RaioTerraKm * c;
    }

    public static double CalcularKm((double Lat, double Lng) origem, (double Lat, double Lng) destino)
        => CalcularKm(origem.Lat, origem.Lng, destino.Lat, destino.Lng);
}
