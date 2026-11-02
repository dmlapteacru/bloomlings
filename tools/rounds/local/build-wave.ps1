# Builds one wave of round levels on your own computer (Windows PowerShell 5.1 and PowerShell 7): the twin of
# build-wave.sh. Hand positions (bridges and other custom mechanics) are skipped and listed: Claude builds those.
# See tools/rounds/local/README.md.
#
#   powershell -ExecutionPolicy Bypass -File tools\rounds\local\build-wave.ps1 -From 51 -To 100 [-Jobs 2] [-Reassign] [-Minutes M]
#
# Resumable; the same inputs give the same levels on any computer. It never commits or pushes.
param([Parameter(Mandatory=$true)][int]$From, [Parameter(Mandatory=$true)][int]$To, [int]$Jobs = 2, [switch]$Reassign, [int]$Minutes = 0)
$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path; Set-Location $Root
if (-not (Get-Command node -ErrorAction SilentlyContinue)) { Write-Host 'Node.js 22 or newer is needed (https://nodejs.org)'; exit 2 }
$major = [int](node -p 'process.versions.node.split(".")[0]')
if ($major -lt 22) { Write-Host "Node.js 22 or newer is needed, found $(node -v)"; exit 2 }
$Wave = 'L{0:D4}-L{1:D4}' -f $From, $To; $Dir = "content/rounds/runs/$Wave"
if (-not (Test-Path "$Dir/mechanics.json")) { Write-Host "No wave plan $Dir/mechanics.json yet: ask Claude to prepare the wave's mechanics plan first."; exit 2 }
Write-Host "== plan $Wave"
$planArgs = @('tools/rounds/rounds.mjs', 'plan', "$From", "$To"); if ($Reassign) { $planArgs += '--reassign' }
& node @planArgs *> "$Dir/plan.log"; if ($LASTEXITCODE -ne 0) { Get-Content "$Dir/plan.log" -Tail 20; exit 1 }
Write-Host "== run $Wave (jobs $Jobs)"
$runArgs = @('tools/rounds/rounds.mjs', 'run', "$From", "$To", '--jobs', "$Jobs"); if ($Minutes -gt 0) { $runArgs += @('--minutes', "$Minutes") }
& node @runArgs 2>&1 | Tee-Object -FilePath "$Dir/run.log"; $code = $LASTEXITCODE
Write-Host ''
Write-Host "== done (exit $code). Read: $Dir/summary.md and $Dir/audit.md; the sample pilot: $Dir/sample/"
Write-Host '   Hand positions and anything still open are listed at the end of summary.md.'
Write-Host "   To hand it over:  git add content/rounds; git commit -m `"Wave $Wave built locally`"; git push"
exit $code
