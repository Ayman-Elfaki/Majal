$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageOutput = Join-Path $repositoryRoot '.artifacts'
$packageProjects = @(
    'src/Majal/Majal.csproj',
    'src/Majal.DataTransferObjects/Majal.DataTransferObjects.csproj',
    'src/Majal.EntityFrameworkCore/Majal.EntityFrameworkCore.csproj'
)
$sampleProjects = @(
    'samples/EShop/EShop.csproj'
)

New-Item -ItemType Directory -Force -Path $packageOutput | Out-Null

foreach ($project in $packageProjects) {
    dotnet pack (Join-Path $repositoryRoot $project) -c Release -o $packageOutput --nologo
}

# NOTE: NuGet's global-packages cache treats a given package id+version as immutable once extracted, so
# repacking the same $(PackageVersion) during local iteration won't be picked up by --force/--no-cache on
# restore below (those flags only bypass the HTTP/remote-feed cache, not ~/.nuget/packages). If a sample
# build seems to be using a stale generator, clear that cache manually, e.g.:
#   dotnet nuget locals global-packages --clear
# (clearing it from within this script was tried and made the very next restore fail in stranger ways than
# the staleness it was meant to fix, so it's left as a manual step instead.)

foreach ($project in $sampleProjects) {
    $sampleProject = Join-Path $repositoryRoot $project
    dotnet restore $sampleProject --source $packageOutput --source 'https://api.nuget.org/v3/index.json' --force --no-cache --nologo
    dotnet build $sampleProject -c Release --no-restore --nologo
}
