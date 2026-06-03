#Requires -Version 5.1
<#
.SYNOPSIS
    Construye DJI Cloud Server, descarga ffmpeg y (opcionalmente) compila el instalador .exe.

.DESCRIPTION
    1. dotnet publish → self-contained win-x64 en installer/input/app/
    2. Descarga ffmpeg-essentials (win64, ~70 MB) → installer/input/tools/ffmpeg.exe
    3. Si ISCC.exe está disponible, compila DjiCloudServer.iss → installer/output/DjiCloudServerSetup.exe

.PARAMETER SkipFfmpeg
    No descarga ffmpeg (útil si ya está en installer/input/tools/ffmpeg.exe).

.PARAMETER SkipCompile
    No ejecuta ISCC aunque esté disponible (solo prepara los inputs).

.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -SkipFfmpeg
#>
param(
    [switch]$SkipFfmpeg,
    [switch]$SkipCompile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root        = $PSScriptRoot
$ProjectPath = Join-Path $Root 'src\DjiCloudServer\DjiCloudServer.csproj'
$InputApp    = Join-Path $Root 'installer\input\app'
$InputTools  = Join-Path $Root 'installer\input\tools'
$IssFile     = Join-Path $Root 'installer\DjiCloudServer.iss'

# ─── Colores ─────────────────────────────────────────────────────────────────

function Write-Step  { param($msg) Write-Host "`n▶ $msg" -ForegroundColor Cyan }
function Write-OK    { param($msg) Write-Host "  ✓ $msg" -ForegroundColor Green }
function Write-Warn  { param($msg) Write-Host "  ⚠ $msg" -ForegroundColor Yellow }
function Write-Fail  { param($msg) Write-Host "  ✗ $msg" -ForegroundColor Red }

# ─── 1. Compilar y publicar ──────────────────────────────────────────────────

Write-Step 'Publicando aplicación (self-contained, win-x64)...'

if (Test-Path $InputApp) { Remove-Item $InputApp -Recurse -Force }
New-Item -ItemType Directory -Path $InputApp -Force | Out-Null

$publishArgs = @(
    'publish', $ProjectPath,
    '--configuration', 'Release',
    '--runtime',       'win-x64',
    '--self-contained', 'true',
    '--output',        $InputApp,
    '-p:PublishSingleFile=false',
    '-p:PublishTrimmed=false',
    '--nologo'
)

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { Write-Fail 'dotnet publish falló.'; exit 1 }
Write-OK "Publicado en: $InputApp"

# ─── 2. Descargar ffmpeg ─────────────────────────────────────────────────────

New-Item -ItemType Directory -Path $InputTools -Force | Out-Null
$ffmpegDest = Join-Path $InputTools 'ffmpeg.exe'

if ($SkipFfmpeg) {
    if (Test-Path $ffmpegDest) {
        Write-OK "ffmpeg ya existe — omitiendo descarga."
    } else {
        Write-Warn "-SkipFfmpeg indicado pero $ffmpegDest no existe. El instalador no incluirá ffmpeg."
    }
} elseif (Test-Path $ffmpegDest) {
    Write-OK "ffmpeg ya existe en $ffmpegDest — omitiendo descarga."
} else {
    Write-Step 'Descargando ffmpeg-essentials (win64)...'

    # URL del último release de gyan.dev (build estática, sin dependencias)
    $ffmpegVersion = '7.1.1'
    $ffmpegZipName = "ffmpeg-${ffmpegVersion}-essentials_build.zip"
    $ffmpegUrl     = "https://github.com/GyanD/codexffmpeg/releases/download/${ffmpegVersion}/${ffmpegZipName}"
    $ffmpegZip     = Join-Path $env:TEMP $ffmpegZipName
    $ffmpegExtract = Join-Path $env:TEMP "ffmpeg-${ffmpegVersion}-essentials_build"

    try {
        Write-Host "  → $ffmpegUrl" -ForegroundColor DarkGray
        Invoke-WebRequest -Uri $ffmpegUrl -OutFile $ffmpegZip -UseBasicParsing

        Write-Host '  Extrayendo...' -ForegroundColor DarkGray
        Expand-Archive -Path $ffmpegZip -DestinationPath $env:TEMP -Force

        $extracted = Get-Item (Join-Path $ffmpegExtract 'bin\ffmpeg.exe') -ErrorAction SilentlyContinue
        if (-not $extracted) {
            # Algunos releases cambian la estructura interna; búsqueda recursiva
            $extracted = Get-ChildItem $env:TEMP -Filter 'ffmpeg.exe' -Recurse |
                         Where-Object { $_.FullName -notlike '*ffprobe*' } |
                         Select-Object -First 1
        }

        if ($extracted) {
            Copy-Item $extracted.FullName $ffmpegDest -Force
            Write-OK "ffmpeg.exe copiado a $ffmpegDest"
        } else {
            Write-Fail "No se encontró ffmpeg.exe en el ZIP descargado."
            exit 1
        }
    } catch {
        Write-Fail "Error descargando ffmpeg: $_"
        Write-Warn "Descarga manualmente ffmpeg.exe y colócalo en: $ffmpegDest"
        exit 1
    } finally {
        Remove-Item $ffmpegZip    -Force -ErrorAction SilentlyContinue
        Remove-Item $ffmpegExtract -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# ─── 3. Compilar instalador con Inno Setup ───────────────────────────────────

if ($SkipCompile) {
    Write-Warn '-SkipCompile indicado — omitiendo Inno Setup.'
    Write-Host "`n  Los inputs están listos en:" -ForegroundColor White
    Write-Host "    App   : $InputApp"   -ForegroundColor DarkGray
    Write-Host "    Tools : $InputTools" -ForegroundColor DarkGray
    exit 0
}

Write-Step 'Buscando Inno Setup Compiler (ISCC.exe)...'

$isccPaths = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    (Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue)?.Source
) | Where-Object { $_ -and (Test-Path $_) }

if (-not $isccPaths) {
    Write-Warn 'Inno Setup no encontrado. Instálalo desde https://jrsoftware.org/isdl.php'
    Write-Host "`n  Los inputs están listos. Una vez instalado Inno Setup, ejecuta:" -ForegroundColor White
    Write-Host "  ISCC.exe `"$IssFile`"" -ForegroundColor DarkGray
    exit 0
}

$iscc = $isccPaths[0]
Write-OK "ISCC encontrado: $iscc"

Write-Step 'Compilando instalador...'
$outputDir = Join-Path $Root 'installer\output'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

& $iscc $IssFile
if ($LASTEXITCODE -ne 0) { Write-Fail 'ISCC falló. Revisa los mensajes anteriores.'; exit 1 }

$setup = Get-ChildItem $outputDir -Filter '*.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($setup) {
    $sizeMb = [math]::Round($setup.Length / 1MB, 1)
    Write-OK "Instalador generado: $($setup.FullName) ($sizeMb MB)"
} else {
    Write-Warn 'No se encontró el instalador en la carpeta output/.'
}

Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Proceso completado. Distribuye el archivo Setup .exe" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════`n" -ForegroundColor Cyan
