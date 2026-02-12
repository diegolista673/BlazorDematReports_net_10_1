using Entities.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Entities.Converters;

/// <summary>
/// Converter EF Core per mappare TipoFonteData enum a stringa nel database.
/// Mantiene compatibilità con il database esistente (SQL, HandlerIntegrato).
/// </summary>
public class TipoFonteDataConverter : ValueConverter<TipoFonteData, string>
{
    public TipoFonteDataConverter()
        : base(
            enumValue => ConvertToDatabase(enumValue),
            dbValue => ConvertFromDatabase(dbValue))
    {
    }

    /// <summary>
    /// Converte TipoFonteData enum a stringa per il database.
    /// Questo metodo è pubblico per permettere il riuso della logica di conversione.
    /// </summary>
    public static string ConvertToDatabase(TipoFonteData enumValue)
    {
        return enumValue switch
        {
            TipoFonteData.SQL => "SQL",
            TipoFonteData.HandlerIntegrato => "HandlerIntegrato",
            _ => throw new ArgumentOutOfRangeException(nameof(enumValue), enumValue, "TipoFonteData non valido")
        };
    }

    /// <summary>
    /// Converte stringa dal database a TipoFonteData enum.
    /// Include backward compatibility per EmailCSV legacy.
    /// </summary>
    public static TipoFonteData ConvertFromDatabase(string dbValue)
    {
        return dbValue switch
        {
            "SQL" => TipoFonteData.SQL,
            "HandlerIntegrato" => TipoFonteData.HandlerIntegrato,
            _ => throw new ArgumentException($"Valore TipoFonte non valido: {dbValue}", nameof(dbValue))
        };
    }
}
