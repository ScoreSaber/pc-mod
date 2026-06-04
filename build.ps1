param(
    [ValidateSet("Debug", "Dev", "Release")]
    [string] $Configuration = "Dev",

    [string] $BeatSaberDir = "",
    [string] $ReferencesDir = "",

    [switch] $Deploy,
    [switch] $NoRestore,
    [switch] $NoZip
)

$ErrorActionPreference = "Stop"

function Resolve-OptionalPath([string] $Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    if (!(Test-Path -LiteralPath $Path)) {
        throw "Path does not exist: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Assert-ReferencesDir([string] $Path) {
    $managedDir = Join-Path $Path "Beat Saber_Data/Managed"
    $pluginsDir = Join-Path $Path "Plugins"

    if (!(Test-Path -LiteralPath $managedDir) -or !(Test-Path -LiteralPath $pluginsDir)) {
        throw "Reference directory must contain Beat Saber_Data/Managed and Plugins: $Path"
    }
}

function Assert-GameDir([string] $Path) {
    $gameExe = Join-Path $Path "Beat Saber.exe"

    if (!(Test-Path -LiteralPath $gameExe)) {
        throw "Beat Saber game directory must contain Beat Saber.exe: $Path"
    }
}

function Assert-PluginReference([string] $PluginName, [string[]] $SearchDirs) {
    foreach ($dir in $SearchDirs) {
        if ([string]::IsNullOrWhiteSpace($dir)) {
            continue
        }

        $pluginPath = Join-Path $dir "Plugins/$PluginName.dll"
        if (Test-Path -LiteralPath $pluginPath) {
            return
        }
    }

    $searched = ($SearchDirs | Where-Object { ![string]::IsNullOrWhiteSpace($_) }) -join ", "
    throw "Missing Plugins/$PluginName.dll. Add it to Refs/Plugins, set SCORESABER_BEAT_SABER_DIR, or pass -BeatSaberDir. Searched: $searched"
}

function ConvertTo-MSBuildBool([bool] $Value) {
    if ($Value) {
        return "True"
    }

    return "False"
}

$repoRoot = $PSScriptRoot
$solution = Join-Path $repoRoot "ScoreSaber.sln"
$repoRefsDir = Join-Path $repoRoot "Refs"

if ([string]::IsNullOrWhiteSpace($BeatSaberDir)) {
    $BeatSaberDir = $env:SCORESABER_BEAT_SABER_DIR
}

if ([string]::IsNullOrWhiteSpace($BeatSaberDir)) {
    $BeatSaberDir = $env:BEAT_SABER_DIR
}

if ([string]::IsNullOrWhiteSpace($BeatSaberDir)) {
    $BeatSaberDir = $env:BeatSaberDir
}

if ([string]::IsNullOrWhiteSpace($BeatSaberDir)) {
    $BeatSaberDir = $env:GameDirectory
}

if ([string]::IsNullOrWhiteSpace($ReferencesDir)) {
    $ReferencesDir = $env:SCORESABER_REFS_DIR
}

$gameDir = Resolve-OptionalPath $BeatSaberDir
$referenceDir = Resolve-OptionalPath $ReferencesDir

if ([string]::IsNullOrWhiteSpace($referenceDir) -and ![string]::IsNullOrWhiteSpace($gameDir)) {
    $referenceDir = $gameDir
}

if ([string]::IsNullOrWhiteSpace($referenceDir) -and (Test-Path -LiteralPath $repoRefsDir)) {
    $referenceDir = (Resolve-Path -LiteralPath $repoRefsDir).Path
}

if ([string]::IsNullOrWhiteSpace($referenceDir)) {
    throw "No Beat Saber references found. Create ./Refs, set SCORESABER_REFS_DIR, or pass -ReferencesDir/-BeatSaberDir."
}

Assert-ReferencesDir $referenceDir
Assert-PluginReference "LeaderboardCore" @($referenceDir, $gameDir)

if ($Deploy) {
    if ([string]::IsNullOrWhiteSpace($gameDir)) {
        throw "Deploy requires -BeatSaberDir or SCORESABER_BEAT_SABER_DIR."
    }

    Assert-GameDir $gameDir
}

$disableCopyToGame = ConvertTo-MSBuildBool (!$Deploy)
$disableZipRelease = ConvertTo-MSBuildBool ($NoZip -or $Configuration -ne "Release")
$gameDirectory = $referenceDir

if (![string]::IsNullOrWhiteSpace($gameDir)) {
    $gameDirectory = $gameDir
}

$buildArgs = @(
    "build",
    $solution,
    "-c",
    $Configuration,
    "-p:BeatSaberDir=$referenceDir",
    "-p:GameReferences=$referenceDir",
    "-p:GameDirectory=$gameDirectory",
    "-p:DisableCopyToGame=$disableCopyToGame",
    "-p:DisableZipRelease=$disableZipRelease"
)

if ($NoRestore) {
    $buildArgs += "--no-restore"
}

& dotnet @buildArgs
