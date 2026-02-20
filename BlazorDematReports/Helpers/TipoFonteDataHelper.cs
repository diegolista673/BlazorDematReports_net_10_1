using Entities.Converters;
using Entities.Enums;
using System.ComponentModel;

namespace BlazorDematReports.Helpers;

/// <summary>
/// Helper UI per la gestione dell'enum TipoFonteData.
/// Per la conversione enum → string database, usare TipoFonteDataConverter direttamente.
/// </summary>
public static class TipoFonteDataHelper
{
    /// <summary>
    /// Ottiene la descrizione user-friendly dell'enum TipoFonteData.
    /// </summary>
    public static string GetDescription(this TipoFonteData tipoFonte)
    {
        var fieldInfo = tipoFonte.GetType().GetField(tipoFonte.ToString());
        if (fieldInfo == null)
            return tipoFonte.ToString();

        var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : tipoFonte.ToString();
    }

    /// <summary>
    /// Ottiene tutti i tipi fonte disponibili per l'UI (esclusi obsoleti).
    /// </summary>
    public static IEnumerable<TipoFonteData> GetAvailableTypes()
    {
        return new[]
        {
            TipoFonteData.SQL,
            TipoFonteData.HandlerIntegrato
        };
    }

    /// <summary>
    /// Ottiene il colore MudBlazor per il tipo fonte.
    /// </summary>
    public static MudBlazor.Color GetColor(TipoFonteData tipoFonte)
    {
        return tipoFonte switch
        {
            TipoFonteData.SQL => MudBlazor.Color.Primary,
            TipoFonteData.HandlerIntegrato => MudBlazor.Color.Secondary,
            _ => MudBlazor.Color.Default
        };
    }

    /// <summary>
    /// Ottiene l'icona MudBlazor per il tipo fonte.
    /// </summary>
    public static string GetIcon(TipoFonteData tipoFonte)
    {
        return tipoFonte switch
        {
            TipoFonteData.SQL => MudBlazor.Icons.Material.Filled.Storage,
            TipoFonteData.HandlerIntegrato => MudBlazor.Icons.Material.Filled.Code,
            _ => MudBlazor.Icons.Material.Filled.QuestionMark
        };
    }

    /// <summary>
    /// Converte TipoFonteData enum a stringa per TipoTask (database).
    /// Extension method che delega al converter EF Core (single source of truth).
    /// </summary>
    public static string ToTaskString(this TipoFonteData tipoFonte)
    {
        return TipoFonteDataConverter.ConvertToDatabase(tipoFonte);
    }
}
