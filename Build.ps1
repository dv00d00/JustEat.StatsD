param(
    [Parameter(Mandatory=$false)][string] $Configuration    = "Release",
    [Parameter(Mandatory=$false)][string] $OutputPath       = "",
    [Parameter(Mandatory=$false)][bool]   $RunTests         = $true,
    [Parameter(Mandatory=$false)][bool]   $CreatePackages   = $true
)

$ErrorActionPreference = "Stop"

$solutionPath  = Split-Path $MyInvocation.MyCommand.Definition
$sdkFile       = Join-Path $solutionPath "global.json"

$dotnetVersion = (Get-Content $sdkFile | ConvertFrom-Json).sdk.version

if ($OutputPath -eq "") {
    $OutputPath = "$(Convert-Path "$PSScriptRoot")\artifacts"
}

$installDotNetSdk = $false;

if ((Get-Command "dotnet.exe" -ErrorAction SilentlyContinue) -eq $null)  {
    Write-Host "The .NET Core SDK is not installed."
    $installDotNetSdk = $true
}
else {
    $installedDotNetVersion = (dotnet --version | Out-String).Trim()
    if ($installedDotNetVersion -ne $dotnetVersion) {
        Write-Host "The required version of the .NET Core SDK is not installed. Expected $dotnetVersion but $installedDotNetVersion was found."
        $installDotNetSdk = $true
    }
}

if ($installDotNetSdk -eq $true) {
    $env:DOTNET_INSTALL_DIR = "$(Convert-Path "$PSScriptRoot")\.dotnetcli"

    if (!(Test-Path $env:DOTNET_INSTALL_DIR)) {
        mkdir $env:DOTNET_INSTALL_DIR | Out-Null
        $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.ps1"
        Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/v$dotnetVersion/scripts/obtain/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
        & $installScript -Version "$dotnetVersion" -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
    }

    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
    $dotnet   = "$env:DOTNET_INSTALL_DIR\dotnet"
} else {
    $dotnet   = "dotnet"
}

function DotNetBuild { param([string]$Project, [string]$Configuration, [string]$Framework)
    & $dotnet build $Project --output (Join-Path $OutputPath $Framework) --framework $Framework --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}

function DotNetTest { param([string]$Project)
    & $dotnet test $Project
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

function DotNetPack { param([string]$Project, [string]$Configuration)
    & $dotnet pack $Project --output $OutputPath --configuration $Configuration --include-symbols --include-source
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE"
    }
}

$projects = @(
    (Join-Path $solutionPath "src\JustEat.StatsD\JustEat.StatsD.csproj")
)

$testProjects = @(
    (Join-Path $solutionPath "src\JustEat.StatsD.Tests\JustEat.StatsD.Tests.csproj")
)

$packageProjects = @(
    (Join-Path $solutionPath "src\JustEat.StatsD\JustEat.StatsD.csproj")
)

Write-Host "Building $($projects.Count) projects..." -ForegroundColor Green
ForEach ($project in $projects) {
    DotNetBuild $project $Configuration "netstandard2.0"
    DotNetBuild $project $Configuration "net451"
}

if ($RunTests -eq $true) {
    Write-Host "Testing $($testProjects.Count) project(s)..." -ForegroundColor Green
    ForEach ($project in $testProjects) {
        DotNetTest $project
    }
}

if ($CreatePackages -eq $true) {
    Write-Host "Creating $($packageProjects.Count) package(s)..." -ForegroundColor Green
    ForEach ($project in $packageProjects) {
        DotNetPack $project $Configuration
    }
}
