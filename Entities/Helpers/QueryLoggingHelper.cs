using Microsoft.Extensions.Logging;
using NLog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Entities.Helpers
{
    /// <summary>
    /// Helper unificato per il logging strutturato delle query e operazioni di database.
    /// Supporta sia Microsoft.Extensions.Logging che NLog per massima compatibilità.
    /// Gestisce correttamente i metodi asincroni, performance tracking e contesto operativo.
    /// </summary>
    public static class QueryLoggingHelper
    {
        private static Microsoft.Extensions.Logging.ILogger? _microsoftSqlQueryLogger;
        private static NLog.Logger? _nlogSqlQueryLogger;
        private static readonly Dictionary<string, string> _methodDescriptionCache = new();
        private static readonly Dictionary<string, MethodMetadata> _methodMetadataCache = new();
        private static readonly object _lockObject = new();

        /// <summary>
        /// Se false (default), LogQueryExecution è no-op — nessuna scrittura su disco.
        /// Impostato da Initialize leggendo Logging:EnableQueryExecutionLog da appsettings.
        /// </summary>
        private static bool _isEnabled;

        /// <summary>
        /// Metadati del metodo per performance e caching.
        /// </summary>
        private class MethodMetadata
        {
            public string ClassName { get; set; } = string.Empty;
            public string MethodName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string OperationType { get; set; } = string.Empty;
            public DateTime LastAccessed { get; set; } = DateTime.Now;
            public string ProjectContext { get; set; } = string.Empty;
        }

        /// <summary>
        /// Inizializza il logger unificato per le query SQL.
        /// Supporta sia Microsoft.Extensions.Logging che NLog.
        /// </summary>
        /// <param name="loggerFactory">Factory per la creazione dei logger Microsoft (opzionale).</param>
        /// <param name="nlogLoggerName">Nome del logger NLog da utilizzare (opzionale).</param>
        /// <param name="projectContext">Contesto del progetto (es: "BlazorDematReports", "DataReading", "LibraryLavorazioni").</param>
        /// <param name="enableQueryExecutionLog">
        /// Abilita il log verboso delle query. Leggere da <c>Logging:EnableQueryExecutionLog</c> in appsettings.
        /// Default <c>false</c>: nessuna scrittura su disco, adatto a collaudo e produzione.
        /// Impostare <c>true</c> solo in Development tramite appsettings.Development.json.
        /// </param>
        public static void Initialize(
            ILoggerFactory? loggerFactory = null,
            string? nlogLoggerName = null,
            string projectContext = "",
            bool enableQueryExecutionLog = false)
        {
            lock (_lockObject)
            {
                _isEnabled = enableQueryExecutionLog;

                // Inizializza Microsoft Logger se fornito
                if (loggerFactory != null)
                {
                    var loggerName = string.IsNullOrEmpty(projectContext)
                        ? "SqlQueries"
                        : $"{projectContext}.SqlQueries";
                    _microsoftSqlQueryLogger = loggerFactory.CreateLogger(loggerName);
                }

                // Inizializza NLog se specificato
                if (!string.IsNullOrEmpty(nlogLoggerName))
                {
                    _nlogSqlQueryLogger = LogManager.GetLogger(nlogLoggerName);
                }
                else if (!string.IsNullOrEmpty(projectContext))
                {
                    _nlogSqlQueryLogger = LogManager.GetLogger($"{projectContext}.SqlQueries");
                }
            }
        }

        /// <summary>
        /// Logga l'esecuzione di una query o operazione di database in formato strutturato.
        /// Supporta sia NLog che Microsoft.Extensions.Logging.
        /// </summary>
        /// <param name="logger">Logger specifico da utilizzare (Microsoft.Extensions.Logging - opzionale).</param>
        /// <param name="nlogLogger">Logger NLog specifico da utilizzare (opzionale).</param>
        /// <param name="queryType">Tipo di operazione (SELECT, INSERT, UPDATE, DELETE).</param>
        /// <param name="entityName">Nome dell'entità coinvolta (opzionale).</param>
        /// <param name="additionalInfo">Informazioni aggiuntive sulla query (opzionale).</param>
        /// <param name="logLevel">Livello di log Microsoft (default: Information).</param>
        /// <param name="nlogLevel">Livello di log NLog (default: Info).</param>
        /// <param name="callerMemberName">Nome del metodo chiamante (compilato automaticamente).</param>
        /// <param name="callerFilePath">Percorso file sorgente (compilato automaticamente).</param>
        public static void LogQueryExecution(
            Microsoft.Extensions.Logging.ILogger? logger = null,
            NLog.Logger? nlogLogger = null,
            string? queryType = null,
            string? entityName = null,
            string? additionalInfo = null,
            Microsoft.Extensions.Logging.LogLevel logLevel = Microsoft.Extensions.Logging.LogLevel.Information,
            NLog.LogLevel? nlogLevel = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "")
        {
            // Se il log è disabilitato (produzione/collaudo) esce immediatamente senza alcun I/O.
            // Abilitare tramite Logging:EnableQueryExecutionLog=true in appsettings.Development.json.
            if (!_isEnabled && logger == null && nlogLogger == null)
                return;

            try
            {
                var methodInfo = GetOrCacheMethodMetadata(callerMemberName, callerFilePath);

                // Determina i logger da utilizzare
                var microsoftLogger = logger ?? _microsoftSqlQueryLogger;
                var nLogger = nlogLogger ?? _nlogSqlQueryLogger;

                // Se nessun logger è disponibile, esci silenziosamente
                if (microsoftLogger == null && nLogger == null)
                    return;

                // Usa il queryType fornito o inferisce dall'operationType dei metadati
                var operationType = queryType ?? methodInfo.OperationType;

                // Usa entityName fornito o estrae dal nome della classe
                var entity = entityName ?? ExtractEntityFromClassName(methodInfo.ClassName);

                // Usa additionalInfo fornito o usa la descrizione del metodo
                var info = additionalInfo ?? methodInfo.Description ?? $"Operazione database: {methodInfo.MethodName}";

                // Costruisci il messaggio di log
                string message = BuildLogMessage(operationType, entity, methodInfo, info, queryType, entityName);

                // Log con Microsoft Logger
                if (microsoftLogger != null && microsoftLogger.IsEnabled(logLevel))
                {
                    microsoftLogger.Log(logLevel, message);
                }

                // Log con NLog
                if (nLogger != null)
                {
                    var nlogLevelToUse = nlogLevel ?? NLog.LogLevel.Info;
                    nLogger.Log(nlogLevelToUse, message);
                }
            }
            catch (Exception ex)
            {
                // Fallback logging per evitare che errori nel logger compromettano l'applicazione
                try
                {
                    var fallbackLogger = logger ?? _microsoftSqlQueryLogger;
                    fallbackLogger?.LogWarning("Errore nel QueryLoggingHelper: {Error}", ex.Message);
                }
                catch
                {
                    // Ultimo tentativo con Debug
                    Debug.WriteLine($"QueryLoggingHelper Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Costruisce il messaggio di log strutturato.
        /// </summary>
        private static string BuildLogMessage(
            string operationType,
            string entity,
            MethodMetadata methodInfo,
            string info,
            string? queryType,
            string? entityName)
        {
            // Se sono forniti queryType ed entityName espliciti, usa il formato avanzato
            if (!string.IsNullOrEmpty(queryType) && !string.IsNullOrEmpty(entityName))
            {
                return $"QUERY EXECUTION - ADVANCED\n" +
                       $"   Operation: {operationType}\n" +
                       $"   Entity: {entity}\n" +
                       $"   Service: {methodInfo.ClassName}\n" +
                       $"   Method: {methodInfo.MethodName}\n" +
                       $"   Project: {methodInfo.ProjectContext}\n" +
                       $"   Info: {info}\n" +
                       $"   Timestamp: {DateTime.Now:HH:mm:ss.fff}";
            }
            else
            {
                // Formato classico per compatibilità
                return $"QUERY EXECUTION\n" +
                       $"   Service: {methodInfo.ClassName}\n" +
                       $"   Method: {methodInfo.MethodName}\n" +
                       $"   Operation: {operationType}\n" +
                       $"   Project: {methodInfo.ProjectContext}\n" +
                       $"   Description: {info}\n" +
                       $"   Timestamp: {DateTime.Now:HH:mm:ss.fff}";
            }
        }

        /// <summary>
        /// Estrae il nome dell'entità dal nome della classe del servizio.
        /// </summary>
        private static string ExtractEntityFromClassName(string className)
        {
            // Rimuove prefissi e suffissi comuni
            var patterns = new[] { "Service", "Repository", "Manager", "Handler", "Factory", "Provider" };

            foreach (var pattern in patterns)
            {
                if (className.StartsWith(pattern) && className.Length > pattern.Length)
                {
                    return className.Substring(pattern.Length);
                }
                if (className.EndsWith(pattern) && className.Length > pattern.Length)
                {
                    return className.Substring(0, className.Length - pattern.Length);
                }
            }

            return className;
        }

        /// <summary>
        /// Ottiene o crea metadati del metodo con caching per performance.
        /// </summary>
        private static MethodMetadata GetOrCacheMethodMetadata(string callerMemberName, string callerFilePath)
        {
            var projectContext = DetermineProjectContext(callerFilePath);
            var cacheKey = $"{projectContext}.{Path.GetFileNameWithoutExtension(callerFilePath)}.{callerMemberName}";

            lock (_lockObject)
            {
                if (_methodMetadataCache.TryGetValue(cacheKey, out var cachedMetadata))
                {
                    cachedMetadata.LastAccessed = DateTime.Now;
                    return cachedMetadata;
                }

                var methodInfo = GetEnhancedCallerInfo(callerMemberName, callerFilePath);
                var description = GetCachedMethodDescription(methodInfo.className, methodInfo.methodName, callerFilePath);
                var operationType = InferOperationType(methodInfo.methodName);

                var metadata = new MethodMetadata
                {
                    ClassName = methodInfo.className,
                    MethodName = methodInfo.methodName,
                    Description = description,
                    OperationType = operationType,
                    ProjectContext = projectContext,
                    LastAccessed = DateTime.Now
                };

                _methodMetadataCache[cacheKey] = metadata;

                // Cleanup periodico della cache
                if (_methodMetadataCache.Count > 1000)
                {
                    CleanupMetadataCache();
                }

                return metadata;
            }
        }

        /// <summary>
        /// Determina il contesto del progetto dal percorso del file.
        /// </summary>
        private static string DetermineProjectContext(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "Unknown";

            if (filePath.Contains("BlazorDematReports"))
                return "BlazorDematReports";
            if (filePath.Contains("DataReading"))
                return "DataReading";
            if (filePath.Contains("LibraryLavorazioni") || filePath.Contains("ClassLibraryLavorazioni"))
                return "LibraryLavorazioni";
            if (filePath.Contains("Entities"))
                return "Entities";

            return "Unknown";
        }

        /// <summary>
        /// Inferisce il tipo di operazione dal nome del metodo.
        /// </summary>
        private static string InferOperationType(string methodName)
        {
            var lowerMethodName = methodName.ToLowerInvariant();

            return lowerMethodName switch
            {
                var name when name.Contains("get") || name.Contains("find") || name.Contains("retrieve") ||
                              (name.Contains("set") && name.Contains("dati")) => "SELECT",
                var name when name.Contains("add") || name.Contains("create") || name.Contains("insert") => "INSERT",
                var name when name.Contains("update") || name.Contains("modify") || name.Contains("edit") => "UPDATE",
                var name when name.Contains("delete") || name.Contains("remove") => "DELETE",
                var name when name.Contains("count") => "COUNT",
                var name when name.Contains("exists") => "EXISTS",
                var name when name.Contains("validate") => "VALIDATE",
                var name when name.Contains("execute") || name.Contains("esegui") => "EXECUTE",
                _ => "QUERY"
            };
        }

        /// <summary>
        /// Pulisce la cache dei metadati rimuovendo le entry più vecchie.
        /// </summary>
        private static void CleanupMetadataCache()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-30);
            var keysToRemove = _methodMetadataCache
                .Where(kvp => kvp.Value.LastAccessed < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _methodMetadataCache.Remove(key);
            }
        }

        /// <summary>
        /// Ottiene informazioni migliorate sul metodo chiamante, gestendo metodi async.
        /// </summary>
        private static (string className, string methodName) GetEnhancedCallerInfo(string callerMemberName, string callerFilePath)
        {
            try
            {
                // Estrai il nome della classe dal percorso del file
                var fileName = Path.GetFileNameWithoutExtension(callerFilePath);
                var className = fileName;

                // Pulisci il nome del metodo dai suffissi async generati dal compilatore
                var methodName = CleanAsyncMethodName(callerMemberName);

                return (className, methodName);
            }
            catch
            {
                return ("Unknown", "Unknown");
            }
        }

        /// <summary>
        /// Pulisce i nomi dei metodi async dai suffissi generati dal compilatore.
        /// </summary>
        private static string CleanAsyncMethodName(string methodName)
        {
            // Rimuovi pattern come <MethodNameAsync>d__X generati dal compilatore C#
            if (methodName.StartsWith("<") && methodName.Contains(">"))
            {
                var match = Regex.Match(methodName, @"<(.+?)>");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return methodName;
        }

        /// <summary>
        /// Ottiene la descrizione del metodo dalla cache o la estrae dal file sorgente.
        /// </summary>
        private static string? GetCachedMethodDescription(string className, string methodName, string filePath)
        {
            var cacheKey = $"{className}.{methodName}";

            lock (_lockObject)
            {
                if (_methodDescriptionCache.TryGetValue(cacheKey, out string? cachedDescription))
                {
                    return cachedDescription;
                }

                var description = ExtractMethodDescription(methodName, filePath);
                if (!string.IsNullOrEmpty(description))
                {
                    _methodDescriptionCache[cacheKey] = description;
                }

                return description;
            }
        }

        /// <summary>
        /// Estrae la descrizione del metodo dai commenti XML del file sorgente.
        /// Attiva solo in Development (quando _isEnabled=true): in produzione i file .cs non esistono.
        /// </summary>
        private static string? ExtractMethodDescription(string methodName, string filePath)
        {
            // Legge il file sorgente solo se il log è abilitato e il file esiste (Development).
            if (!_isEnabled)
                return null;
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var sourceCode = File.ReadAllText(filePath);

                // Pattern migliorato per catturare i commenti XML prima del metodo
                var patterns = new[]
                {
                    $@"/// <summary>\s*\n?\s*/// (.*?)\s*\n?\s*/// </summary>.*?{Regex.Escape(methodName)}\s*\(",
                    $@"/// <summary>(.*?)</summary>.*?{Regex.Escape(methodName)}\s*\(",
                    $@"/// <inheritdoc/>\s*.*?{Regex.Escape(methodName)}\s*\("
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(sourceCode, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (pattern.Contains("inheritdoc"))
                        {
                            return "Implementazione interfaccia - vedi documentazione interfaccia";
                        }
                        return CleanSummaryText(match.Groups[1].Value);
                    }
                }
            }
            catch
            {
                // Ignora errori di parsing del file
            }

            return null;
        }

        /// <summary>
        /// Pulisce il testo del commento XML rimuovendo markup e formattazione.
        /// </summary>
        private static string CleanSummaryText(string summaryText)
        {
            if (string.IsNullOrEmpty(summaryText))
                return string.Empty;

            // Rimuovi markup XML
            var cleaned = Regex.Replace(summaryText, @"<[^>]*>", " ");
            // Rimuovi prefissi ///
            cleaned = Regex.Replace(cleaned, @"///\s*", " ");
            // Normalizza spazi
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            // Rimuovi caratteri di controllo
            cleaned = Regex.Replace(cleaned, @"[\r\n\t]", " ");

            return cleaned.Trim();
        }

        /// <summary>
        /// Ottiene le statistiche della cache per diagnostica.
        /// </summary>
        public static (int MethodCount, int DescriptionCount, DateTime? OldestAccess) GetCacheStats()
        {
            lock (_lockObject)
            {
                var oldestAccess = _methodMetadataCache.Values.Any()
                    ? _methodMetadataCache.Values.Min(v => v.LastAccessed)
                    : (DateTime?)null;

                return (_methodMetadataCache.Count, _methodDescriptionCache.Count, oldestAccess);
            }
        }

        /// <summary>
        /// Pulisce manualmente tutte le cache per liberare memoria.
        /// </summary>
        public static void ClearAllCaches()
        {
            lock (_lockObject)
            {
                _methodMetadataCache.Clear();
                _methodDescriptionCache.Clear();
            }
        }
    }
}