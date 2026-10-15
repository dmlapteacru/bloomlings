<# Builds three isolated, solved story mosaics on your computer. Never writes to the production catalog. #>
param(
    [string]$Out = 'content/work/art-pilot-local',
    [switch]$Render
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$previousLocation = Get-Location
try {
    Set-Location $repo
    $output = [IO.Path]::GetFullPath((Join-Path $repo $Out))
    $workRoot = [IO.Path]::GetFullPath((Join-Path $repo 'content/work')) + [IO.Path]::DirectorySeparatorChar
    if (-not $output.StartsWith($workRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The pilot output must be inside content/work/. Production folders are never pilot outputs.'
    }
    $pilot = Join-Path $repo 'content/pictures/pilot'
    $registry = Get-Content -Raw "$pilot/compositions.json" | ConvertFrom-Json
    $pipeline = Join-Path $repo 'core/src/Bloomlings.Pipeline'
    & dotnet build $pipeline -c Release -m:2 --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Pipeline build failed.' }
    function Invoke-PilotPipeline {
        param([string[]]$PipelineArgs)
        & dotnet run --project $pipeline -c Release --no-build -- @PipelineArgs
        if ($LASTEXITCODE -ne 0) { throw "Pipeline failed: $($PipelineArgs -join ' ')" }
    }
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    $library = Join-Path $output 'library'
    Invoke-PilotPipeline -PipelineArgs @('pictures', 'import', '--src', "$pilot/src", '--out', $library)
    Invoke-PilotPipeline -PipelineArgs @('pictures', 'validate', '--lib', $library)
    $empty = Join-Path $output 'empty-history'
    if ((Test-Path $empty) -and @(Get-ChildItem $empty -Recurse -File).Count -gt 0) {
        throw 'The isolated pilot history directory must be empty.'
    }
    foreach ($composition in $registry.compositions) {
        $id = $composition.pictureId
        $number = [int]$composition.sampleLevel
        $seed = [long]$composition.sampleSeed
        $single = Join-Path $output "input/$id"
        New-Item -ItemType Directory -Force -Path $single | Out-Null
        Copy-Item "$library/$id.json" "$single/$id.json" -Force
        $profile = Get-Content -Raw 'content/profiles/band-0501-1000.json' | ConvertFrom-Json
        $profile.variantCount = @(6, 6)
        $profile.mappingConstraints.allowRoleMerge = $false
        $profile.allowedVariants = @($composition.previewMapping.PSObject.Properties | ForEach-Object { $_.Value })
        $profile.solver.maxCandidatesPerLevel = 12
        $profilePath = Join-Path $output "input/$id.profile.json"
        # Windows PowerShell's UTF8 writer emits a BOM, which the existing JSON reader accepts.
        $profile | ConvertTo-Json -Depth 30 | Set-Content -Encoding UTF8 $profilePath
        $batch = Join-Path $output "batches/$id"
        Invoke-PilotPipeline -PipelineArgs @('generate', '--profile', $profilePath, '--levels', "$number-$number",
            '--seed', "$seed", '--lib', $single, '--catalog', $empty, '--curated', $empty,
            '--keep', $empty, '--class', 'normal', '--out', $batch)
        $filename = 'level-{0:00000}.json' -f $number
        $level = Get-Content -Raw "$batch/levels/$filename" | ConvertFrom-Json
        if ($level.picture.id -ne $id -or ($level.picture.mirror -and $level.picture.mirror -ne 'none')) {
            throw "Unexpected picture/mirror in $id."
        }
        foreach ($role in $composition.previewMapping.PSObject.Properties) {
            if ($level.mapping.($role.Name) -ne $role.Value) {
                throw "The solved palette of $id differs from the selected pilot palette."
            }
        }
        foreach ($kind in @('levels', 'validation')) {
            New-Item -ItemType Directory -Force -Path "$output/catalog/$kind" | Out-Null
            Copy-Item "$batch/$kind/$filename" "$output/catalog/$kind/$filename" -Force
        }
    }
    Invoke-PilotPipeline -PipelineArgs @('validate', '--defs', "$output/catalog", '--lib', $library)
    Invoke-PilotPipeline -PipelineArgs @('score', '--defs', "$output/catalog", '--curated', $empty, '--lib', $library)
    Invoke-PilotPipeline -PipelineArgs @('review', '--defs', "$output/catalog", '--lib', $library, '--out', "$output/review")
    if ($Render) {
        # The game painter renders the checked-in samples; the generated JSON must match them first.
        foreach ($composition in $registry.compositions) {
            $filename = 'level-{0:00000}.json' -f [int]$composition.sampleLevel
            $expected = Get-Content -Raw "$pilot/samples/levels/$filename" | ConvertFrom-Json | ConvertTo-Json -Depth 100 -Compress
            $actual = Get-Content -Raw "$output/catalog/levels/$filename" | ConvertFrom-Json | ConvertTo-Json -Depth 100 -Compress
            if ($actual -ne $expected) { throw "$filename differs from the checked-in sample; review the generator change before rendering." }
        }
        & dotnet run --project playtest/preview -c Release -- --mosaic-pilot --out "$output/game-render"
        if ($LASTEXITCODE -ne 0) { throw 'Game mosaic rendering failed.' }
    }
    Write-Host "Pilot ready: $output/review/index.html"
}
finally { Set-Location $previousLocation }
