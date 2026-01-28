using System.ComponentModel;
using System.Reflection;
using LibraryLavorazioni.Lavorazioni.Interfaces;

namespace LibraryLavorazioni.Shared.Discovery;

/// <summary>
/// Servizio per discovery automatica handler C# tramite reflection.
/// Esegue scan UNA VOLTA all'avvio e cacha risultati.
/// </summary>
public sealed class HandlerDiscoveryService
{
    private static readonly Lazy<IReadOnlyList<HandlerInfo>> _cachedHandlers = new(DiscoverHandlers);

    /// <summary>
    /// Lista handler disponibili (cached, thread-safe).
    /// </summary>
    public static IReadOnlyList<HandlerInfo> AvailableHandlers => _cachedHandlers.Value;

    private static IReadOnlyList<HandlerInfo> DiscoverHandlers()
    {
        var handlers = new List<HandlerInfo>();

        // Assembly da scansionare
        var assembly = typeof(ILavorazioneHandler).Assembly;

        // Trova tutte le classi che implementano ILavorazioneHandler
        var handlerTypes = assembly.GetTypes()
            .Where(t => typeof(ILavorazioneHandler).IsAssignableFrom(t)
                     && t.IsClass
                     && !t.IsAbstract
                     && t.IsPublic
                     && t.Name != "UnifiedDataSourceHandler"); // Escludi handler unificato

        foreach (var type in handlerTypes)
        {
            var code = GetHandlerCode(type);
            var description = GetHandlerDescription(type);

            handlers.Add(new HandlerInfo
            {
                ClassName = type.Name,
                FullTypeName = type.FullName!,
                Code = code,
                Description = description,
                HandlerType = type
            });
        }

        return handlers.OrderBy(h => h.ClassName).ToList().AsReadOnly();
    }

    private static string GetHandlerCode(Type type)
    {
        try
        {
            // Prova a leggere da attributo
            var attr = type.GetCustomAttribute<HandlerCodeAttribute>();
            if (attr != null) return attr.Code;

            // Fallback: nome classe senza "Handler"
            return type.Name.Replace("Handler", "");
        }
        catch
        {
            return type.Name;
        }
    }

    private static string GetHandlerDescription(Type type)
    {
        // Leggi da attributo Description
        var attr = type.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? type.Name.Replace("Handler", "").Replace("_", " ");
    }

    /// <summary>
    /// Verifica se un handler esiste.
    /// </summary>
    public static bool HandlerExists(string className)
    {
        return AvailableHandlers.Any(h =>
            h.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ottiene info di un handler specifico.
    /// </summary>
    public static HandlerInfo? GetHandler(string className)
    {
        return AvailableHandlers.FirstOrDefault(h =>
            h.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Info su un handler C# disponibile.
/// </summary>
public class HandlerInfo
{
    public string ClassName { get; init; } = null!;
    public string FullTypeName { get; init; } = null!;
    public string? Code { get; init; }
    public string? Description { get; init; }
    public Type HandlerType { get; init; } = null!;
}

/// <summary>
/// Attributo per specificare codice handler esplicitamente.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class HandlerCodeAttribute : Attribute
{
    public string Code { get; }
    public HandlerCodeAttribute(string code) => Code = code;
}
