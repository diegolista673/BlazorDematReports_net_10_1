# Script per aggiornare namespace negli handler ripristinati
$handlersPath = "C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports.Core\Handlers"

# Mappings namespace
$replacements = @{
    "using LibraryLavorazioni.Lavorazioni.Constants;" = "using BlazorDematReports.Core.Constants;"
    "using LibraryLavorazioni.Lavorazioni.Interfaces;" = "using BlazorDematReports.Core.Lavorazioni.Interfaces;"
    "using LibraryLavorazioni.Lavorazioni.Models;" = "using BlazorDematReports.Core.Lavorazioni.Models;"
    "using LibraryLavorazioni.Utility;" = "using BlazorDematReports.Core.Utility;"
    "using LibraryLavorazioni.Utility.Interfaces;" = "using BlazorDematReports.Core.Utility.Interfaces;"
    "using LibraryLavorazioni.Utility.Models;" = "using BlazorDematReports.Core.Utility.Models;"
    "using LibraryLavorazioni.Shared.Services.Email;" = "using BlazorDematReports.Core.Services.Email;"
    
    # Namespace declaration
    "namespace LibraryLavorazioni.Lavorazioni.Handlers.MailHandlers.Ader4" = "namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4"
    "namespace LibraryLavorazioni.Lavorazioni.Handlers.MailHandlers.Hera16" = "namespace BlazorDematReports.Core.Handlers.MailHandlers.Hera16"
    "namespace LibraryLavorazioni.Lavorazioni.Handlers" = "namespace BlazorDematReports.Core.Handlers.LavorazioniHandlers"
    "namespace ClassLibraryLavorazioni.Lavorazioni.Handlers.MailHandlers.Ader4" = "namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4"
}

$files = Get-ChildItem -Path $handlersPath -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

$modifiedCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    foreach ($old in $replacements.Keys) {
        $new = $replacements[$old]
        $content = $content -replace [regex]::Escape($old), $new
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "✅ $($file.Name)" -ForegroundColor Green
        $modifiedCount++
    }
}

Write-Host ""
Write-Host "Totale file modificati: $modifiedCount" -ForegroundColor Cyan
