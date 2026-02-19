# Fix-Service-Mapper-ConfigUser.ps1
# Rimuove mapper e configUser ridondanti nei Service figli di ServiceBase<T>
# e aggiorna la chiamata base() al costruttore full (contextFactory, logger, mapper, configUser).
# Esclusi: ServiceBase, ServiceWrapper, ServiceTaskManagement, ServiceRepartiProduzione, ServiceMail
# (questi non ereditano ServiceBase<T> o hanno pattern diverso).

param(
    [string]$ServicesPath = "C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports\Services\DataService"
)

$excluded = @("ServiceBase.cs","ServiceWrapper.cs","ServiceTaskManagement.cs",
              "ServiceRepartiProduzione.cs","ServiceMail.cs")

$files = Get-ChildItem -Path $ServicesPath -Filter "Service*.cs" |
         Where-Object { $excluded -notcontains $_.Name }

$modified = 0
$skipped  = 0

foreach ($file in $files) {
    $original = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    $content  = $original

    $hasMapper     = $content -match 'private readonly IMapper mapper;'
    $hasConfigUser = $content -match 'private readonly ConfigUser configUser;'

    if (-not ($hasMapper -or $hasConfigUser)) {
        Write-Host "SKIP  $($file.Name)" -ForegroundColor DarkGray
        $skipped++
        continue
    }

    # 1. Rimuove "private readonly IMapper mapper;" (riga singola)
    $content = $content -replace '(?m)^        private readonly IMapper mapper;\r?\n', ''

    # 2. Rimuove "private readonly ConfigUser configUser;" (riga singola)
    $content = $content -replace '(?m)^        private readonly ConfigUser configUser;\r?\n', ''

    # 3. Aggiorna base() → base(contextFactory, logger, mapper, configUser)
    #    Solo se ancora nella forma precedente
    $content = $content -replace ': base\(contextFactory, logger\)', ': base(contextFactory, logger, mapper, configUser)'

    # 4. Rimuove "this.mapper = mapper;" dal corpo costruttore
    $content = $content -replace '(?m)^\s+this\.mapper = mapper;\r?\n', ''

    # 5. Rimuove "this.configUser = configUser;" dal corpo costruttore
    $content = $content -replace '(?m)^\s+this\.configUser = configUser;\r?\n', ''

    if ($content -eq $original) {
        Write-Host "NOCHG $($file.Name)" -ForegroundColor Yellow
        $skipped++
        continue
    }

    # Scrittura atomica: file temp + move
    $tmpPath = $file.FullName + ".tmp"
    [System.IO.File]::WriteAllText($tmpPath, $content, [System.Text.Encoding]::UTF8)
    Move-Item -Path $tmpPath -Destination $file.FullName -Force
    Write-Host "FIXED $($file.Name)" -ForegroundColor Green
    $modified++
}

Write-Host ""
Write-Host "Risultato: $modified modificati, $skipped saltati" -ForegroundColor Cyan
