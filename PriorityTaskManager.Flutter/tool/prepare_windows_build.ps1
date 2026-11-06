param(
    [Parameter(Mandatory = $true)]
    [string]$FlutterProjectDirectory
)

$ErrorActionPreference = 'Stop'
$projectDirectory = $FlutterProjectDirectory.TrimEnd([char[]]@('\', '/'))

Push-Location -LiteralPath $projectDirectory
try {
    & flutter pub get
    $pubGetExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($pubGetExitCode -ne 0) {
    Write-Output "flutter pub get failed with exit code $pubGetExitCode."
    exit $pubGetExitCode
}

function ConvertTo-CMakeComparablePath {
    param([string]$Path)

    return $Path.Trim().Trim('"').Replace('/', '\').TrimEnd([char[]]@('\', '/'))
}

$windowsBuildDirectory = Join-Path $projectDirectory 'build\windows'
$cachePath = Join-Path $windowsBuildDirectory 'x64\CMakeCache.txt'

if (Test-Path -LiteralPath $cachePath) {
    $cachePaths = @{}
    foreach ($line in Get-Content -LiteralPath $cachePath) {
        if ($line -match '^(CMAKE_CACHEFILE_DIR|CMAKE_HOME_DIRECTORY):INTERNAL=(.*)$') {
            $cachePaths[$matches[1]] = $matches[2]
        }
    }

    $cacheMatchesProject = $false
    if ($cachePaths.ContainsKey('CMAKE_CACHEFILE_DIR') -and $cachePaths.ContainsKey('CMAKE_HOME_DIRECTORY')) {
        $cachedBuildDirectory = ConvertTo-CMakeComparablePath -Path $cachePaths['CMAKE_CACHEFILE_DIR']
        $expectedBuildDirectory = ConvertTo-CMakeComparablePath -Path (Join-Path $windowsBuildDirectory 'x64')
        $cachedSourceDirectory = ConvertTo-CMakeComparablePath -Path $cachePaths['CMAKE_HOME_DIRECTORY']
        $expectedSourceDirectory = ConvertTo-CMakeComparablePath -Path (Join-Path $projectDirectory 'windows')

        $buildDirectoryMatches = [string]::Equals(
            $cachedBuildDirectory,
            $expectedBuildDirectory,
            [StringComparison]::OrdinalIgnoreCase
        )
        $sourceDirectoryMatches = [string]::Equals(
            $cachedSourceDirectory,
            $expectedSourceDirectory,
            [StringComparison]::OrdinalIgnoreCase
        )
        $cacheMatchesProject = $buildDirectoryMatches -and $sourceDirectoryMatches
    }

    if (-not $cacheMatchesProject) {
        Write-Output 'The Windows CMake cache belongs to a different worktree path. Removing generated Windows build output.'
        Remove-Item -LiteralPath $windowsBuildDirectory -Recurse -Force
    }
}

$pluginHeaderPath = Join-Path $projectDirectory 'windows\flutter\ephemeral\.plugin_symlinks\flutter_secure_storage_windows\windows\include\flutter_secure_storage_windows\flutter_secure_storage_windows_plugin.h'
if ($pluginHeaderPath.Length -ge 260) {
    Write-Output "The generated Windows plugin header path is $($pluginHeaderPath.Length) characters; Windows builds require paths shorter than 260 characters."
    Write-Output 'Create the worktree under a short, stable path such as E:\wt\<branch>, and always open it through that same path.'
    exit 1
}

Write-Output 'Windows Flutter build path is ready.'
