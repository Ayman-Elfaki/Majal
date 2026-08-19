$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageOutput = Join-Path $repositoryRoot '.artifacts'
$packageProjects = @(
    'src/Majal/Majal.csproj',
    'src/Majal.DataTransferObjects/Majal.DataTransferObjects.csproj',
    'src/Majal.EntityFrameworkCore/Majal.EntityFrameworkCore.csproj'
)
$sampleProjects = @(
    'samples/Todo.Core/Todo.Core.csproj',
    'samples/Todo.Api/Todo.Api.csproj'
)

New-Item -ItemType Directory -Force -Path $packageOutput | Out-Null

foreach ($project in $packageProjects) {
    dotnet pack (Join-Path $repositoryRoot $project) -c Release -o $packageOutput --nologo
}

foreach ($project in $sampleProjects) {
    $sampleProject = Join-Path $repositoryRoot $project
    dotnet restore $sampleProject --source $packageOutput --source 'https://api.nuget.org/v3/index.json' --force --no-cache --nologo
    dotnet build $sampleProject -c Release --no-restore --nologo
}