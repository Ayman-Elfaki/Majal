$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'generate-llms.mjs'
node $scriptPath
