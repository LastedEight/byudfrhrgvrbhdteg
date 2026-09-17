<#
.SYNOPSIS
    Builds the FPV Drone mod and installs it into the VTOL VR Mod Loader.

.DESCRIPTION
    Finds your VTOL VR install (Steam registry, then every Steam library, then the
    usual paths), builds FpvDroneMod.dll against the game's own Unity assemblies,
    and copies it with Info.json into the mod loader's mods folder.

    The mod cannot be built without the game installed: it compiles against
    UnityEngine.dll from VTOLVR_Data\Managed and ModLoader.dll from the mod
    loader, and neither is redistributable.

.PARAMETER VtolVrDir
    Your VTOL VR folder, if the automatic search does not find it. For example:
    "D:\SteamLibrary\steamapps\common\VTOL VR"

.PARAMETER Configuration
    Release (default) or Debug.

.PARAMETER NoInstall
    Build only. Leaves the result in Builds\FpvDrone instead of copying it into
    the mods folder.

.EXAMPLE
    .\build.ps1

.EXAMPLE
    .\build.ps1 -VtolVrDir "D:\SteamLibrary\steamapps\common\VTOL VR"
#>

[CmdletBinding()]
param(
    [string] $VtolVrDir,
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',
    [switch] $NoInstall
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Definition

function Write-Step { param([string] $Message) Write-Host "==> $Message" -ForegroundColor Cyan }
function Write-Ok   { param([string] $Message) Write-Host "    $Message" -ForegroundColor Green }
function Write-Note { param([string] $Message) Write-Host "    $Message" -ForegroundColor DarkGray }

function Get-SteamLibraries {
    $libraries = @()

    $steamPath = $null
    foreach ($key in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $value = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
            if ($value -and $value.SteamPath)     { $steamPath = $value.SteamPath;     break }
            if ($value -and $value.InstallPath)   { $steamPath = $value.InstallPath;   break }
        } catch { }
    }

    if ($steamPath) { $libraries += $steamPath }

    # Every additional library Steam knows about is listed in libraryfolders.vdf.
    foreach ($base in $libraries.Clone()) {
        $vdf = Join-Path $base 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            foreach ($match in [regex]::Matches((Get-Content -Raw $vdf), '"path"\s*"([^"]+)"')) {
                $libraries += $match.Groups[1].Value -replace '\\\\', '\'
            }
        }
    }

    $libraries += @(
        "${env:ProgramFiles(x86)}\Steam",
        "$env:ProgramFiles\Steam",
        'C:\Steam',
        'D:\Steam',
        'D:\SteamLibrary'
    )

    return $libraries | Where-Object { $_ } | Select-Object -Unique
}

function Find-VtolVr {
    foreach ($library in Get-SteamLibraries) {
        $candidate = Join-Path $library 'steamapps\common\VTOL VR'
        if (Test-Path (Join-Path $candidate 'VTOLVR_Data\Managed')) { return $candidate }
    }

    return $null
}

# ---------------------------------------------------------------- locate the game

Write-Step 'Locating VTOL VR'

if (-not $VtolVrDir) {
    if ($env:VTOLVR_DIR) { $VtolVrDir = $env:VTOLVR_DIR }
    else                 { $VtolVrDir = Find-VtolVr }
}

if (-not $VtolVrDir -or -not (Test-Path $VtolVrDir)) {
    Write-Host ''
    Write-Host 'Could not find your VTOL VR install.' -ForegroundColor Red
    Write-Host 'Pass it explicitly, for example:' -ForegroundColor Red
    Write-Host '    .\build.ps1 -VtolVrDir "D:\SteamLibrary\steamapps\common\VTOL VR"' -ForegroundColor Red
    exit 1
}

$managed = Join-Path $VtolVrDir 'VTOLVR_Data\Managed'
if (-not (Test-Path $managed)) {
    Write-Host ''
    Write-Host "'$VtolVrDir' does not look like a VTOL VR install: no VTOLVR_Data\Managed folder." -ForegroundColor Red
    exit 1
}

Write-Ok $VtolVrDir

# ----------------------------------------------------------- locate the mod loader

Write-Step 'Locating the mod loader'

$modLoaderDll = Get-ChildItem -Path $VtolVrDir -Filter 'ModLoader.dll' -Recurse -ErrorAction SilentlyContinue |
                Select-Object -First 1

if (-not $modLoaderDll) {
    Write-Host ''
    Write-Host 'ModLoader.dll was not found anywhere under your VTOL VR folder.' -ForegroundColor Red
    Write-Host 'Install the VTOL VR Mod Loader from https://vtolvr-mods.com/ and run this again.' -ForegroundColor Red
    exit 1
}

$modLoaderDir = $modLoaderDll.DirectoryName
Write-Ok $modLoaderDir

# ------------------------------------------------------------------------- build

Write-Step 'Checking for the .NET SDK'

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host ''
    Write-Host 'The .NET SDK is not installed (or not on PATH).' -ForegroundColor Red
    Write-Host 'Install it from https://dotnet.microsoft.com/download and run this again.' -ForegroundColor Red
    exit 1
}

Write-Ok (& dotnet --version)

Write-Step "Building ($Configuration)"

$project = Join-Path $root 'src\DroneMod\DroneMod.csproj'
& dotnet build $project -c $Configuration `
    -p:VtolVrDir="$VtolVrDir" `
    -p:VtolModLoaderDir="$modLoaderDir" `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Build failed. See the errors above.' -ForegroundColor Red
    Write-Host 'If they name a missing ModLoader type, the loader API has moved on:' -ForegroundColor Yellow
    Write-Host 'src\DroneMod\Main.cs is the only file that references it, and' -ForegroundColor Yellow
    Write-Host 'docs\COMPATIBILITY.md explains what to change.' -ForegroundColor Yellow
    exit 1
}

$staged = Join-Path $root 'Builds\FpvDrone'
$builtDll = Join-Path $staged 'FpvDroneMod.dll'
if (-not (Test-Path $builtDll)) {
    Write-Host ''
    Write-Host "Build reported success but $builtDll is missing." -ForegroundColor Red
    exit 1
}

Write-Ok "Built $builtDll"

# ----------------------------------------------------------------------- install

if ($NoInstall) {
    Write-Step 'Skipping install (-NoInstall)'
    Write-Note "The mod is staged at $staged"
    exit 0
}

Write-Step 'Installing'

$modsRoot = Join-Path $modLoaderDir 'mods'
if (-not (Test-Path $modsRoot)) { New-Item -ItemType Directory -Path $modsRoot -Force | Out-Null }

$destination = Join-Path $modsRoot 'FpvDrone'
if (-not (Test-Path $destination)) { New-Item -ItemType Directory -Path $destination -Force | Out-Null }

Copy-Item -Path (Join-Path $staged '*') -Destination $destination -Recurse -Force

Write-Ok $destination
Write-Host ''
Write-Host 'Done. Next:' -ForegroundColor Cyan
Write-Host '  1. Launch VTOL VR through the mod loader.'
Write-Host '  2. Enable "FPV Drone" in the loader''s mod list.'
Write-Host '  3. Load into a flight scene, press F9 for the goggles, F8 to deploy.'
Write-Host ''
Write-Host '  docs\TESTING.md has a checklist that tells you which part is at fault'
Write-Host '  if any step does not behave.'
