$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    dotnet test Tests/SmartDocs.CounterpartyIntegration.Tests/SmartDocs.CounterpartyIntegration.Tests.csproj --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Server tests failed' }
    Push-Location Client
    try {
        npm.cmd run check
        if ($LASTEXITCODE -ne 0) { throw 'TypeScript check failed' }
        npm.cmd run build
        if ($LASTEXITCODE -ne 0) { throw 'Client build failed' }
        npm.cmd test
        if ($LASTEXITCODE -ne 0) { throw 'Client tests failed' }
    } finally { Pop-Location }
} finally { Pop-Location }
