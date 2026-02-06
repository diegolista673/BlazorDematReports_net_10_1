using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Oracle.ManagedDataAccess.Client;

namespace BlazorDematReports.Services.Validation;

/// <summary>
/// Servizio unificato per validazione query SQL e test connessioni database.
/// Consolidato da QueryService e SqlValidationService originali.
/// Implementa protezioni contro SQL injection, verifica parametri obbligatori,
/// validazione sintassi T-SQL e colonne obbligatorie.
/// </summary>
public class SqlValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlValidationService>? _logger;
    
    
    // Pattern pericolosi che indicano possibili SQL injection
    private static readonly string[] DangerousPatterns = new[]
    {
        @";\s*DROP\s+",           // DROP TABLE/DATABASE
        @";\s*DELETE\s+",         // DELETE FROM
        @";\s*TRUNCATE\s+",       // TRUNCATE TABLE
        @";\s*ALTER\s+",          // ALTER TABLE
        @";\s*CREATE\s+",         // CREATE TABLE
        @";\s*UPDATE\s+",         // UPDATE (aggiunto)
        @";\s*INSERT\s+",         // INSERT (aggiunto)
        @";\s*EXEC\s*\(",         // EXEC(
        @";\s*EXECUTE\s*\(",      // EXECUTE(
        @"xp_cmdshell",           // SQL Server command execution
        @"sp_executesql",         // Dynamic SQL execution
        @"UNION\s+.*SELECT",      // UNION-based injection
        @"--\s*$",                // SQL comments inline
        @"--.*",                  // SQL comments (qualsiasi)
        @"/\*.*\*/"               // Multi-line comments
    };
    
    // Keyword vietate (stored procedure sistema)
    private static readonly string[] RestrictedKeywords = new[]
    {
        "xp_", "sp_OA", "sp_make", "sp_configure"
    };

    // Colonne obbligatorie per query produzione
    private static readonly string[] RequiredColumns = new[]
    {
        "Operatore", "DataLavorazione", "Documenti", "Fogli", "Pagine"
    };

    public SqlValidationService(IConfiguration configuration, ILogger<SqlValidationService>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Valida query SQL contro pattern di SQL injection e requisiti obbligatori.
    /// Supporta sia @startDate/@endDate che @startData/@endData.
    /// </summary>
    public ValidationResult ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ValidationResult.Error("Query SQL obbligatoria");

        // 1. Check lunghezza massima
        if (query.Length > 1024)
            return ValidationResult.Error("Query troppo lunga (massimo 1024 caratteri)");

        // 2. Check pattern pericolosi (SQL injection)
        foreach (var pattern in DangerousPatterns)
        {
            if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                var cleanPattern = pattern.Replace(@"\s+", " ").Replace(@"\(", "(");
                _logger?.LogWarning("SQL INJECTION rilevato: Pattern '{Pattern}' trovato nella query", cleanPattern);
                return ValidationResult.Error(
                    $"SQL INJECTION RILEVATO: Pattern '{cleanPattern}' non consentito. " +
                    "Query bloccata per motivi di sicurezza.");
            }
        }

        // 3. Check keyword vietate
        foreach (var keyword in RestrictedKeywords)
        {
            if (query.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogWarning("Keyword vietata '{Keyword}' trovata nella query", keyword);
                return ValidationResult.Error(
                    $"Keyword vietata: '{keyword}'. " +
                    "Stored procedure di sistema non consentite.");
            }
        }

        // 4. Check SELECT obbligatorio (solo query di lettura)
        var trimmedQuery = query.Trim();
        if (!trimmedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !trimmedQuery.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)) // CTE (Common Table Expression)
        {
            return ValidationResult.Warning(
                "Query non inizia con SELECT o WITH. " +
                "Solo query di lettura accettate per motivi di sicurezza.");
        }

        // 5. Check parametri obbligatori (supporta entrambi i formati)
        bool hasDateParams = 
            (query.Contains("@startDate", StringComparison.OrdinalIgnoreCase) && 
             query.Contains("@endDate", StringComparison.OrdinalIgnoreCase)) ||
            (query.Contains("@startData", StringComparison.OrdinalIgnoreCase) && 
             query.Contains("@endData", StringComparison.OrdinalIgnoreCase));

        if (!hasDateParams)
        {
            return ValidationResult.Error(
                "Parametri di data mancanti. " +
                "Utilizzare @startDate e @endDate (o @startData e @endData) per il filtraggio delle date.");
        }

        return ValidationResult.Success("Query validata con successo.");
    }

    /// <summary>
    /// Valida la sintassi SQL usando il parser T-SQL di Microsoft.
    /// Include anche controlli manuali per clausole obbligatorie (FROM).
    /// </summary>
    /// <param name="query">Query SQL da validare sintatticamente.</param>
    /// <returns>Risultato validazione con eventuali errori di sintassi.</returns>
    public ValidationResult ValidateSqlSyntax(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ValidationResult.Error("Query SQL obbligatoria");

        try
        {
            // 1. Controlli manuali pre-parsing per clausole obbligatorie
            var preCheckResult = ValidateQueryStructure(query);
            if (!preCheckResult.IsValid)
                return preCheckResult;
            
            // 2. Parser T-SQL Microsoft
            var parser = new TSql160Parser(false);

            // Analizza la query
            using var reader = new StringReader(query);
            var fragment = parser.Parse(reader, out IList<ParseError> errors);

            if (errors != null && errors.Count > 0)
            {
                var errorMessages = string.Join("; ", errors.Select(e => 
                    $"Linea {e.Line}, Colonna {e.Column}: {e.Message}"));
                
                _logger?.LogWarning("Errori sintassi SQL: {Errors}", errorMessages);
                
                return ValidationResult.Error(
                    $"Errori di sintassi SQL rilevati:\n{errorMessages}");
            }

            return ValidationResult.Success("Sintassi SQL valida");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante validazione sintassi SQL");
            return ValidationResult.Error($"Errore durante validazione sintassi: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Controlli manuali sulla struttura della query SQL.
    /// Verifica presenza clausole obbligatorie come FROM.
    /// </summary>
    private ValidationResult ValidateQueryStructure(string query)
    {
        var normalizedQuery = query.Trim();
        
        // Rimuovi commenti SQL per analisi pulita
        normalizedQuery = Regex.Replace(normalizedQuery, @"--.*$", "", RegexOptions.Multiline);
        normalizedQuery = Regex.Replace(normalizedQuery, @"/\*.*?\*/", "", RegexOptions.Singleline);
        
        // Controlla se inizia con SELECT
        if (normalizedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            // SELECT deve avere FROM (a meno che non sia una query di sistema come SELECT @@VERSION)
            bool hasFrom = Regex.IsMatch(normalizedQuery, @"\bFROM\b", RegexOptions.IgnoreCase);
            bool isSystemQuery = Regex.IsMatch(normalizedQuery, @"SELECT\s+@@", RegexOptions.IgnoreCase);
            
            if (!hasFrom && !isSystemQuery)
            {
                _logger?.LogWarning("Query SELECT senza clausola FROM rilevata");
                return ValidationResult.Error(
                    "Sintassi errata: SELECT richiede la clausola FROM.\n" +
                    "Esempio: SELECT col1, col2 FROM tabella WHERE ...");
            }
            
            // Verifica ordine clausole: SELECT ... FROM ... WHERE ... GROUP BY ... ORDER BY
            if (hasFrom)
            {
                var selectPos = normalizedQuery.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
                var fromPos = normalizedQuery.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
                var wherePos = normalizedQuery.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
                
                if (fromPos < selectPos)
                {
                    return ValidationResult.Error("Sintassi errata: FROM non può precedere SELECT");
                }
                
                if (wherePos > 0 && wherePos < fromPos)
                {
                    return ValidationResult.Error("Sintassi errata: WHERE non può precedere FROM");
                }
            }
        }
        else if (normalizedQuery.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            // CTE deve contenere SELECT e FROM
            bool hasSelect = Regex.IsMatch(normalizedQuery, @"\bSELECT\b", RegexOptions.IgnoreCase);
            bool hasFrom = Regex.IsMatch(normalizedQuery, @"\bFROM\b", RegexOptions.IgnoreCase);
            
            if (!hasSelect)
            {
                return ValidationResult.Error("CTE (WITH) richiede una clausola SELECT");
            }
            
            if (!hasFrom)
            {
                return ValidationResult.Error("CTE (WITH) richiede una clausola FROM");
            }
        }
        
        return ValidationResult.Success("Struttura query valida");
    }

    /// <summary>
    /// Valida che la query contenga le colonne obbligatorie per la produzione.
    /// Migrato da QueryService.
    /// Colonne richieste: Operatore, DataLavorazione, Documenti, Fogli, Pagine
    /// </summary>
    /// <param name="query">Query SQL da validare.</param>
    /// <returns>Risultato validazione con colonne mancanti se presenti.</returns>
    public ValidationResult ValidateColumnNames(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ValidationResult.Error("Query SQL obbligatoria");

        // Estrai la sezione SELECT
        var selectMatch = Regex.Match(query, @"select\s+(.*?)\s+from", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!selectMatch.Success)
        {
            return ValidationResult.Error("Impossibile trovare la sezione SELECT nella query");
        }

        string columnsSection = selectMatch.Groups[1].Value;

        // Verifica che non contenga SELECT * (ma consenti funzioni aggregate come COUNT(*), SUM(*))
        // Pattern più preciso: asterisco NON preceduto da una funzione aggregata
        var selectStarPattern = @"(?<!COUNT\s*\()\*(?!\s*\))";  // * non in COUNT(*)
        
        // Controllo più semplice: verifica se c'è una virgola prima del primo asterisco
        // Se SELECT *, l'asterisco sarà il primo elemento dopo SELECT
        var trimmedColumns = columnsSection.Trim();
        if (trimmedColumns.StartsWith("*") || Regex.IsMatch(trimmedColumns, @"^\s*\*\s*$"))
        {
            _logger?.LogWarning("SELECT * rilevato nella query");
            return ValidationResult.Error(
                "SELECT * non consentito. " +
                "Specificare esplicitamente le colonne: Operatore, DataLavorazione, Documenti, Fogli, Pagine");
        }

        // Verifica presenza colonne obbligatorie
        var missingColumns = new List<string>();
        foreach (var column in RequiredColumns)
        {
            if (!Regex.IsMatch(columnsSection, $@"\b{column}\b", RegexOptions.IgnoreCase))
            {
                missingColumns.Add(column);
            }
        }

        if (missingColumns.Any())
        {
            var missing = string.Join(", ", missingColumns);
            _logger?.LogWarning("Colonne obbligatorie mancanti: {Columns}", missing);
            return ValidationResult.Error(
                $"Colonne obbligatorie mancanti: {missing}. " +
                $"La query deve includere: {string.Join(", ", RequiredColumns)}");
        }

        return ValidationResult.Success(
            $"Tutte le colonne obbligatorie presenti: {string.Join(", ", RequiredColumns)}");
    }

    /// <summary>
    /// Testa connessione al database.
    /// </summary>
    public async Task<ValidationResult> TestConnectionAsync(
        string connectionStringName, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionStringName))
            return ValidationResult.Error("Nome connection string obbligatorio");

        var connectionString = _configuration.GetConnectionString(connectionStringName);
        
        if (string.IsNullOrWhiteSpace(connectionString))
            return ValidationResult.Error(
                $"Connection string '{connectionStringName}' non trovata in configurazione");

        try
        {
            // Determina tipo database dalla connection string
            if (connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            {
                // SQL Server
                await using var sqlConn = new SqlConnection(connectionString);
                await sqlConn.OpenAsync(ct);
                var serverVersion = sqlConn.ServerVersion;
                
                return ValidationResult.Success(
                    $"Connessione SQL Server '{connectionStringName}' riuscita. Versione: {serverVersion}");
            }
            else if (connectionString.Contains("User Id", StringComparison.OrdinalIgnoreCase))
            {
                // Oracle
                await using var oraConn = new OracleConnection(connectionString);
                await oraConn.OpenAsync(ct);
                var serverVersion = oraConn.ServerVersion;
                
                return ValidationResult.Success(
                    $"Connessione Oracle '{connectionStringName}' riuscita. Versione: {serverVersion}");
            }
            else
            {
                return ValidationResult.Error("Tipo database non riconosciuto dalla connection string");
            }
        }
        catch (SqlException sqlEx)
        {
            return ValidationResult.Error(
                $"Errore connessione SQL Server: {sqlEx.Message}");
        }
        catch (OracleException oraEx)
        {
            return ValidationResult.Error(
                $"Errore connessione Oracle: {oraEx.Message}");
        }
        catch (TimeoutException)
        {
            return ValidationResult.Error(
                $" Timeout connessione a '{connectionStringName}'. Verificare rete e firewall.");
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(
                $" Errore connessione: {ex.Message}");
        }
    }

    /// <summary>
    /// Testa esecuzione query (solo schema, nessun dato).
    /// </summary>
    public async Task<ValidationResult> TestQueryExecutionAsync(
        string connectionStringName,
        string query,
        CancellationToken ct = default)
    {
        // Prima valida la query
        var validationResult = ValidateQuery(query);
        if (!validationResult.IsValid)
            return validationResult;

        var connectionString = _configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            return ValidationResult.Error(
                $" Connection string '{connectionStringName}' non trovata");

        try
        {
            // Parametri di test
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;

            if (connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            {
                // SQL Server
                await using var conn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(query, conn)
                {
                    CommandTimeout = 30 // 30 secondi timeout
                };
                
                cmd.Parameters.AddWithValue("@startData", startDate);
                cmd.Parameters.AddWithValue("@endData", endDate);

                await conn.OpenAsync(ct);
                
                // ExecuteReader con SchemaOnly = legge solo schema, non dati
                await using var reader = await cmd.ExecuteReaderAsync(
                    System.Data.CommandBehavior.SchemaOnly, ct);

                var foundColumns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    foundColumns.Add(reader.GetName(i));
                }

                // Verifica colonne obbligatorie
                var expectedColumns = new[] { "operatore", "DataLavorazione", "Documenti", "Fogli", "Pagine" };
                var missingColumns = expectedColumns
                    .Except(foundColumns, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (missingColumns.Any())
                {
                    return ValidationResult.Warning(
                        $" Query eseguita ma mancano colonne obbligatorie: {string.Join(", ", missingColumns)}.\n" +
                        $"Colonne trovate: {string.Join(", ", foundColumns)}\n" +
                        "Colonne attese: operatore, DataLavorazione, Documenti, Fogli, Pagine");
                }

                return ValidationResult.Success(
                    $"Query testata con successo! Colonne trovate: {string.Join(", ", foundColumns)}");
            }
            else
            {
                // Oracle
                await using var conn = new OracleConnection(connectionString);
                await using var cmd = new OracleCommand(query, conn)
                {
                    CommandTimeout = 30
                };
                
                cmd.Parameters.Add("startData", OracleDbType.Date).Value = startDate;
                cmd.Parameters.Add("endData", OracleDbType.Date).Value = endDate;

                await conn.OpenAsync(ct);
                await using var reader = await cmd.ExecuteReaderAsync(
                    System.Data.CommandBehavior.SchemaOnly, ct);

                var foundColumns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    foundColumns.Add(reader.GetName(i));
                }

                return ValidationResult.Success(
                    $"Query Oracle testata con successo! Colonne: {string.Join(", ", foundColumns)}");
            }
        }
        catch (SqlException sqlEx)
        {
            return ValidationResult.Error(
                $" Errore SQL: {sqlEx.Message}");
        }
        catch (OracleException oraEx)
        {
            return ValidationResult.Error(
                $" Errore Oracle: {oraEx.Message}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(
                $" Errore test query: {ex.Message}");
        }
    }
}

/// <summary>
/// Risultato di una validazione.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public ValidationSeverity Severity { get; init; }
    public string Message { get; init; } = "";

    public static ValidationResult Success(string message) => new()
    {
        IsValid = true,
        Severity = ValidationSeverity.Success,
        Message = message
    };

    public static ValidationResult Warning(string message) => new()
    {
        IsValid = true, // Warning non blocca il salvataggio
        Severity = ValidationSeverity.Warning,
        Message = message
    };

    public static ValidationResult Error(string message) => new()
    {
        IsValid = false, // Error blocca il salvataggio
        Severity = ValidationSeverity.Error,
        Message = message
    };
}

/// <summary>
/// Severità del risultato di validazione.
/// </summary>
public enum ValidationSeverity
{
    Success,
    Warning,
    Error
}
