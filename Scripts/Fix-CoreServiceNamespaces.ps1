param([string]$Path = "C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports.Core\Services\DataService")

$files = Get-ChildItem $Path -Filter "*.cs" -Recurse
$modified = 0

foreach ($file in $files) {
    $original = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    $content  = $original

    # 1. Fix namespace
    $content = $content -replace 'namespace BlazorDematReports\.Services\.DataService\b', 'namespace BlazorDematReports.Core.Services.DataService'

    # 2. Rimuovi "using BlazorDematReports.Application;" (namespace progetto Blazor)
    $content = $content -replace '(?m)^using BlazorDematReports\.Application;\r?\n', ''

    # 3. Sostituisci "using BlazorDematReports.Dto;" con namespace Core
    $content = $content -replace 'using BlazorDematReports\.Dto;', 'using BlazorDematReports.Core.Application.Dto;'

    # 4. Sostituisci "using BlazorDematReports.Interfaces.IDataService;" con namespace Core
    $content = $content -replace 'using BlazorDematReports\.Interfaces\.IDataService;', 'using BlazorDematReports.Core.Interfaces.IDataService;'

    if ($content -ne $original) {
        $tmp = $file.FullName + ".tmp"
        [System.IO.File]::WriteAllText($tmp, $content, [System.Text.Encoding]::UTF8)
        Move-Item $tmp $file.FullName -Force
        Write-Host "FIXED $($file.Name)"
        $modified++
    } else {
        Write-Host "SKIP  $($file.Name)"
    }
}
Write-Host "Totale modificati: $modified"
