<#
.SYNOPSIS
Builds the level catalog (content/catalog) band by band, as tools/catalog/README.md describes.

.DESCRIPTION
The twin of build-catalog.sh, for Windows PowerShell 5.1 and PowerShell 7: the same bands, seeds, segments, retries
and checks, so both give the same files. It never commits or pushes: it says what to commit.

.EXAMPLE
pwsh -File tools/catalog/build-catalog.ps1
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1 -Jobs 14
powershell -ExecutionPolicy Bypass -File tools\catalog\build-catalog.ps1 -Check 0011-0025
#>
[CmdletBinding()]
param(
    # Threads for generate (default: the logical cores minus 2). The levels never depend on it.
    [int]$Jobs = [Math]::Max(1, [Environment]::ProcessorCount - 2),
    # Stop after the bands that start at or before this level.
    [int]$ToLevel = 5000,
    # Seeds tried after the band's own seed for a level that gets no accepted candidate.
    [int]$Retries = 2,
    # Work folder (gitignored): the pipeline build, the batches and the logs.
    [string]$Work = 'content/work/catalog-build',
    # Reuse the pipeline build in the work folder.
    [switch]$NoBuild,
    # Skip the whole-catalog validate and score at the end.
    [switch]$NoFinal,
    # Regenerate this finished band (e.g. 0011-0025) with only the earlier levels as history and compare its files.
    [string]$Check = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $Root
[Environment]::CurrentDirectory = $Root

$Catalog = 'content/catalog'
$Manifest = "$Catalog/build-manifest.jsonl"
$Pipe = "$Work/pipe/bloomlings-pipeline.dll"
$Utf8 = New-Object System.Text.UTF8Encoding($false)

# Fixed segments per band: the levels depend on them, never on -Jobs.
$Bands = @(
    @{ Band = '0011-0025'; First = 11;   Last = 25;   Profile = 'band-0011-0025'; Seed = 1; Segments = 1 },
    @{ Band = '0026-0050'; First = 26;   Last = 50;   Profile = 'band-0026-0050'; Seed = 1; Segments = 1 },
    @{ Band = '0051-0100'; First = 51;   Last = 100;  Profile = 'band-0051-0100'; Seed = 1; Segments = 1 },
    @{ Band = '0101-0250'; First = 101;  Last = 250;  Profile = 'band-0101-0250'; Seed = 1; Segments = 3 },
    @{ Band = '0251-0500'; First = 251;  Last = 500;  Profile = 'band-0251-0500'; Seed = 1; Segments = 5 },
    @{ Band = '0501-1000'; First = 501;  Last = 1000; Profile = 'band-0501-1000'; Seed = 1; Segments = 10 },
    @{ Band = '1001-2000'; First = 1001; Last = 2000; Profile = 'band-1001-2000'; Seed = 1; Segments = 14 },
    @{ Band = '2001-3000'; First = 2001; Last = 3000; Profile = 'band-2001-5000'; Seed = 1; Segments = 14 },
    @{ Band = '3001-4000'; First = 3001; Last = 4000; Profile = 'band-2001-5000'; Seed = 1; Segments = 14 },
    @{ Band = '4001-5000'; First = 4001; Last = 5000; Profile = 'band-2001-5000'; Seed = 1; Segments = 14 }
)

function Say([string]$Message) { Write-Host ('[{0}] {1}' -f [DateTime]::UtcNow.ToString('HH:mm:ss'), $Message) }

function Fail([string]$Message) { Say "STOP: $Message"; exit 1 }

function Name([int]$Level) { 'level-{0:D5}.json' -f $Level }

# Level numbers of a folder's levels/ (or of the folder itself), ascending.
function LevelsIn([string]$Dir) {
    if (Test-Path "$Dir/levels") { $Dir = "$Dir/levels" }
    if (-not (Test-Path $Dir)) { return @() }
    $found = foreach ($f in Get-ChildItem -Path $Dir -Filter 'level-*.json' -File) {
        if ($f.Name -match '^level-(\d+)\.json$') { [int]$Matches[1] }
    }
    return @($found | Sort-Object)
}

function CopyLevel([string]$From, [string]$To, [int]$Level) {
    foreach ($kind in 'levels', 'validation') {
        $source = "$From/$kind/$(Name $Level)"
        if (-not (Test-Path $source)) { return $false }
        New-Item -ItemType Directory -Force -Path "$To/$kind" | Out-Null
        Copy-Item -Path $source -Destination "$To/$kind/$(Name $Level)" -Force
    }
    return $true
}

function Quote([string]$Arg) { if ($Arg -match '[\s"]') { '"' + ($Arg -replace '"', '\"') + '"' } else { $Arg } }

function ReadShared([string]$Path) {
    try {
        $stream = [System.IO.File]::Open($Path, 'Open', 'Read', 'ReadWrite')
        try { return (New-Object System.IO.StreamReader($stream)).ReadToEnd() } finally { $stream.Dispose() }
    } catch { return '' }
}

# Runs dotnet with its output in files, showing the progress lines (stderr) as they come; returns the exit code.
function Invoke-Dotnet([string[]]$Arguments, [string]$OutFile, [string]$ErrFile, [switch]$Quiet) {
    $argLine = ($Arguments | ForEach-Object { Quote $_ }) -join ' '
    $p = Start-Process -FilePath 'dotnet' -ArgumentList $argLine -NoNewWindow -PassThru -RedirectStandardOutput $OutFile -RedirectStandardError $ErrFile
    $null = $p.Handle
    $shown = 0
    $done = $false
    while (-not $done) {
        $done = $p.HasExited
        if (-not $done) { Start-Sleep -Seconds 5 }
        if (-not $Quiet) {
            $lines = @((ReadShared $ErrFile) -split "`n" | Where-Object { $_ -match '^\s+L\d+:' })
            for ($i = $shown; $i -lt $lines.Count; $i++) { Say ('  ' + $lines[$i].Trim()) }
            $shown = $lines.Count
        }
    }
    $p.WaitForExit()
    return $p.ExitCode
}

function Pipeline([string[]]$Arguments, [string]$OutFile, [string]$ErrFile, [switch]$Quiet) {
    Invoke-Dotnet -Arguments (@($Pipe) + $Arguments) -OutFile $OutFile -ErrFile $ErrFile -Quiet:$Quiet
}

function ReadReport([string]$Path) { Get-Content -Path $Path -Raw | ConvertFrom-Json }

function IssueCounts([string]$Path) {
    $report = ReadReport $Path
    $issues = @($report.issues).Count
    return @{ Errors = [int]$report.errors; Warnings = $issues - [int]$report.errors }
}

function CommitOf {
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $c = (& git rev-parse --short HEAD 2>$null)
        if (-not $c) { $c = 'unknown' }
        $changes = (& git status --porcelain -- core content/profiles content/pictures content/showcase content/curated content/readability 2>$null)
        if ($changes) { $c = "$c+changes" }
        return [string]$c
    } finally { $ErrorActionPreference = $saved }
}

function AppendLine([string]$Path, [string]$Line) { [System.IO.File]::AppendAllText((Join-Path $Root $Path), $Line + "`n", $Utf8) }

function JsonList($Items) { '[' + (@($Items) -join ',') + ']' }

# Generates one band into $Out (gen/, fills, assembled/) with $CatalogDir as the earlier levels.
function BuildBand($B, [string]$CatalogDir, [string]$Out, [string]$Previous) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
    New-Item -ItemType Directory -Force -Path $Out | Out-Null
    $history = @()
    if ($Previous -and (Test-Path $Previous)) { $history = @('--history', $Previous) }
    Say "L$($B.First)-$($B.Last) ($($B.Band)): generate --seed $($B.Seed) --segments $($B.Segments) --jobs $Jobs"
    $arguments = @('generate', '--profile', "content/profiles/$($B.Profile).json", '--levels', "$($B.First)-$($B.Last)",
        '--seed', "$($B.Seed)", '--segments', "$($B.Segments)", '--jobs', "$Jobs", '--catalog', $CatalogDir,
        '--keep', 'content/showcase') + $history + @('--out', "$Out/gen", '--json')
    $code = Pipeline $arguments "$Out/gen.json" "$Out/gen.log"
    if ($code -eq 2) { Fail "generate failed (exit 2), see $Out/gen.log" }
    $seams = 0
    try { $seams = [int](ReadReport "$Out/gen.json").seamRepairs } catch { $seams = 0 }

    $kept = @(LevelsIn 'content/showcase')
    $produced = @(LevelsIn "$Out/gen")
    $result = @{ Generated = 0; Kept = 0; Fills = @(); Gaps = @(); Seams = $seams; Seconds = 0; Assembled = "$Out/assembled" }
    for ($n = $B.First; $n -le $B.Last; $n++) {
        if ($kept -contains $n) { $result.Kept++; continue }
        if ($produced -contains $n) { $result.Generated++; continue }
        # A level without an accepted candidate: the next seeds, with the band's other levels as its neighbours.
        $ok = $false
        for ($s = $B.Seed + 1; $s -le $B.Seed + $Retries; $s++) {
            $fill = "$Out/fill-$n-s$s"
            $fillsHistory = @(Get-ChildItem -Path $Out -Directory -Filter 'fill-*' | Where-Object { Test-Path "$($_.FullName)/levels" } | ForEach-Object { "$Out/$($_.Name)" })
            Say "  L$n failed every candidate; seed $s"
            $fillArgs = @('generate', '--profile', "content/profiles/$($B.Profile).json", '--levels', "$n-$n", '--seed', "$s",
                '--catalog', $CatalogDir, '--keep', 'content/showcase', '--history', "$Out/gen") + $fillsHistory + @('--out', $fill)
            $code = Pipeline $fillArgs "$fill.out.log" "$fill.log"
            if ($code -eq 2) { Fail "generate failed (exit 2), see $fill.log" }
            if (Test-Path "$fill/levels/$(Name $n)") {
                $result.Fills += "`"${n}:$s`""
                $result.Generated++
                $ok = $true
                break
            }
            if (Test-Path $fill) { Remove-Item -Recurse -Force $fill }
        }
        if (-not $ok) {
            $result.Gaps += $n
            Say "  L$n stays a gap after seeds $($B.Seed)-$($B.Seed + $Retries)"
        }
    }

    # The band: its generated levels, the fills and its fixed showcase levels.
    $asm = "$Out/assembled"
    foreach ($n in (LevelsIn "$Out/gen")) { if (-not (CopyLevel "$Out/gen" $asm $n)) { Fail "L$n has no validation record" } }
    foreach ($f in @(Get-ChildItem -Path $Out -Directory -Filter 'fill-*')) {
        foreach ($n in (LevelsIn "$Out/$($f.Name)")) { if (-not (CopyLevel "$Out/$($f.Name)" $asm $n)) { Fail "L$n has no validation record" } }
    }
    foreach ($n in $kept) {
        if ($n -ge $B.First -and $n -le $B.Last) { if (-not (CopyLevel 'content/showcase' $asm $n)) { Fail "showcase L$n has no validation record" } }
    }
    $result.Seconds = [int]$watch.Elapsed.TotalSeconds
    return $result
}

# The curated Levels 1-10 with their validation records.
function SeedCurated {
    $have = @(LevelsIn $Catalog | Where-Object { $_ -le 10 }).Count
    if ($have -eq 10) { return }
    Say 'L1-10: the curated levels with their validation records'
    $tmp = "$Work/curated"
    if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
    New-Item -ItemType Directory -Force -Path "$tmp/levels" | Out-Null
    for ($n = 1; $n -le 10; $n++) {
        $source = 'content/curated/level-{0:D4}.json' -f $n
        if (-not (Test-Path $source)) { Fail "content/curated lacks L$n" }
        Copy-Item -Path $source -Destination "$tmp/levels/$(Name $n)"
    }
    $null = Pipeline @('validate', '--defs', $tmp, '--write-records', '--json') "$tmp/validate.json" "$tmp/validate.log" -Quiet
    $counts = IssueCounts "$tmp/validate.json"
    if ($counts.Errors -ne 0) { Fail "the curated levels have $($counts.Errors) validation errors, see $tmp/validate.json" }
    for ($n = 1; $n -le 10; $n++) { $null = CopyLevel $tmp $Catalog $n }
    AppendLine $Manifest ('{"band":"0001-0010","levels":"1-10","source":"content/curated","validation":"0 errors, ' + $counts.Warnings + ' warnings","pipeline":"' + (CommitOf) + '"}')
}

function BandDone([string]$Band) { (Test-Path $Manifest) -and (Select-String -Path $Manifest -SimpleMatch -Quiet -Pattern "`"band`":`"$Band`"") }

New-Item -ItemType Directory -Force -Path $Work, "$Catalog/levels", "$Catalog/validation" | Out-Null
if (-not $NoBuild -or -not (Test-Path $Pipe)) {
    Say "building the pipeline into $Work/pipe"
    $code = Invoke-Dotnet @('build', 'core/src/Bloomlings.Pipeline', '-c', 'Release', '-o', "$Work/pipe") "$Work/build.log" "$Work/build.err.log" -Quiet
    if ($code -ne 0) { Fail "the build failed, see $Work/build.log" }
}

if ($Check) {
    $B = $Bands | Where-Object { $_.Band -eq $Check } | Select-Object -First 1
    if (-not $B) { Fail "no band $Check" }
    $dir = "$Work/check-$($B.Band)"
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
    $view = "$dir/catalog"
    foreach ($n in (LevelsIn $Catalog)) { if ($n -lt $B.First) { $null = CopyLevel $Catalog $view $n } }
    New-Item -ItemType Directory -Force -Path "$view/levels", "$view/validation" | Out-Null
    $r = BuildBand $B $view "$dir/band" ''
    $diffs = 0
    $compared = 0
    foreach ($n in (LevelsIn $r.Assembled)) {
        foreach ($kind in 'levels', 'validation') {
            $compared++
            $mine = "$($r.Assembled)/$kind/$(Name $n)"
            $theirs = "$Catalog/$kind/$(Name $n)"
            if (-not (Test-Path $theirs) -or (Get-FileHash $mine).Hash -ne (Get-FileHash $theirs).Hash) {
                $diffs++
                Say "  differs: $kind/$(Name $n)"
            }
        }
    }
    Say "check $($B.Band): $compared files compared, $diffs differ ($($r.Seconds) s)"
    if ($diffs -eq 0) { exit 0 } else { exit 1 }
}

SeedCurated
$Summary = New-Object System.Collections.Generic.List[string]
$previous = ''
foreach ($B in $Bands) {
    if ($B.First -gt $ToLevel) { break }
    if (BandDone $B.Band) {
        if (@(LevelsIn $Catalog | Where-Object { $_ -ge $B.First -and $_ -le $B.Last }).Count -eq 0) {
            Fail "$Manifest lists band $($B.Band), but content/catalog has none of its levels"
        }
        Say "L$($B.First)-$($B.Last) ($($B.Band)): done earlier, skipped"
        $Summary.Add("$($B.Band) skipped (in $Manifest)")
        $previous = "$Work/$($B.Band)/assembled"
        continue
    }

    # Only finished bands may stand in the catalog: a stopped run's partial copy of this band is removed.
    foreach ($n in (LevelsIn $Catalog)) {
        if ($n -gt $B.Last) { Fail "content/catalog holds L$n, past the unfinished band $($B.Band); restore the catalog first" }
        if ($n -ge $B.First) {
            Remove-Item -Force -ErrorAction SilentlyContinue "$Catalog/levels/$(Name $n)", "$Catalog/validation/$(Name $n)"
        }
    }

    $r = BuildBand $B $Catalog "$Work/$($B.Band)" $previous
    Say "L$($B.First)-$($B.Last): validate ($($r.Generated) generated, $($r.Kept) kept, $(@($r.Gaps).Count) gaps)"
    $report = "$Work/$($B.Band)/validate.json"
    $null = Pipeline @('validate', '--defs', $r.Assembled, '--context', $Catalog, '--json') $report "$Work/$($B.Band)/validate.log" -Quiet
    if (-not (Test-Path $report) -or (Get-Item $report).Length -eq 0) { Fail "validate gave no report, see $Work/$($B.Band)/validate.log" }
    $counts = IssueCounts $report
    if ($counts.Errors -ne 0) {
        foreach ($issue in @((ReadReport $report).issues | Where-Object { $_.error } | Select-Object -First 20)) { Say "  L$($issue.level) $($issue.check): $($issue.message)" }
        Fail "band $($B.Band) has $($counts.Errors) validation errors (report: $report); nothing was copied into $Catalog"
    }
    foreach ($n in (LevelsIn $r.Assembled)) { $null = CopyLevel $r.Assembled $Catalog $n }
    $line = '{"band":"' + $B.Band + '","levels":"' + $B.First + '-' + $B.Last + '","profile":"' + $B.Profile + '","seed":' + $B.Seed +
        ',"segments":' + $B.Segments + ',"retrySeeds":' + $Retries + ',"jobs":' + $Jobs + ',"generated":' + $r.Generated +
        ',"kept":' + $r.Kept + ',"seamRepairs":' + $r.Seams + ',"fills":' + (JsonList $r.Fills) + ',"gaps":' + (JsonList $r.Gaps) +
        ',"seconds":' + $r.Seconds + ',"validation":"0 errors, ' + $counts.Warnings + ' warnings","pipeline":"' + (CommitOf) + '"}'
    AppendLine $Manifest $line
    $fills = if (@($r.Fills).Count -gt 0) { @($r.Fills) -join ' ' } else { 'none' }
    $gaps = if (@($r.Gaps).Count -gt 0) { @($r.Gaps) -join ' ' } else { 'none' }
    $text = "$($B.Band): $($r.Generated) generated, $($r.Kept) kept, fills $fills, gaps $gaps, $($r.Seams) seam repairs, $($r.Seconds) s, validate 0 errors and $($counts.Warnings) warnings"
    Say $text
    $Summary.Add($text)
    $previous = $r.Assembled
}

if (-not $NoFinal) {
    Say 'the whole catalog: validate and score'
    $null = Pipeline @('validate', '--catalog', $Catalog, '--context', 'content/curated', '--json') "$Work/validate-catalog.json" "$Work/validate-catalog.log" -Quiet
    $counts = IssueCounts "$Work/validate-catalog.json"
    $scoreCode = Pipeline @('score', '--defs', $Catalog, '--curated', 'content/curated') "$Work/score.txt" "$Work/score.err.log" -Quiet
    $Summary.Add("catalog: $(@(LevelsIn $Catalog).Count) levels, validate $($counts.Errors) errors, score exit $scoreCode (see $Work/score.txt)")
    if ($counts.Errors -ne 0) {
        $Summary | ForEach-Object { Write-Host $_ }
        Fail "the whole catalog has $($counts.Errors) validation errors, see $Work/validate-catalog.json"
    }
}

[System.IO.File]::WriteAllText((Join-Path $Root "$Work/summary.txt"), (($Summary -join "`n") + "`n"), $Utf8)
Write-Host ''
Write-Host "Summary ($Work/summary.txt):"
$Summary | ForEach-Object { Write-Host $_ }
Write-Host ''
Write-Host 'Nothing was committed. To commit the catalog:'
Write-Host '  git add content/catalog; git commit'
