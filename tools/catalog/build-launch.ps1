<# Builds the replacement 2500-level launch on the owner's computer. No APK, commits or network uploads. #>
param(
    [int]$Jobs = 2,
    [int]$Retries = 2,
    [string]$Levels = '1-2500',
    [string]$Out = 'content/work/launch-catalog',
    [switch]$Status,
    [switch]$PlanOnly,
    [switch]$Install
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$previousLocation = Get-Location
try {
    Set-Location $repo
    & dotnet build core/src/Bloomlings.Pipeline -c Release -m:2 --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Pipeline build failed.' }
    function Invoke-Launch {
        param([string[]]$PipelineArgs)
        & dotnet run --project core/src/Bloomlings.Pipeline -c Release --no-build -- @PipelineArgs
        if ($LASTEXITCODE -ne 0) { throw "Launch command failed: $($PipelineArgs -join ' ')" }
    }
    Invoke-Launch -PipelineArgs @('launch','plan')
    if ($PlanOnly) { return }
    Invoke-Launch -PipelineArgs @('launch','status')
    if ($Status) { return }
    Invoke-Launch -PipelineArgs @('launch','generate','--levels',$Levels,'--out',$Out,'--jobs',"$Jobs",'--retries',"$Retries")
    if ($Install) { Invoke-Launch -PipelineArgs @('launch','install','--from',$Out) }
    Write-Host "Generated catalog: $Out. -Install connects a complete validated build to the game source."
}
finally { Set-Location $previousLocation }
