# Run from the solution directory after a Release build. No SQL Server required.
$ErrorActionPreference = 'Stop'
$apiDll = Join-Path $PWD 'src/CSharpShop.Api/bin/Release/net10.0/CSharpShop.Api.dll'
$logs = Join-Path $PWD 'artifacts/smoke'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$oldEnvironment = $env:ASPNETCORE_ENVIRONMENT
$oldUrls = $env:ASPNETCORE_URLS
$process = $null
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://127.0.0.1:5199'
    $process = Start-Process -FilePath 'dotnet' -ArgumentList @($apiDll) -WorkingDirectory (Join-Path $PWD 'src/CSharpShop.Api') -PassThru -RedirectStandardOutput (Join-Path $logs 'stdout.log') -RedirectStandardError (Join-Path $logs 'stderr.log')
    $response = $null
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($process.HasExited) { throw 'API exited before becoming ready. See startup logs.' }
        try {
            $response = Invoke-WebRequest 'http://127.0.0.1:5199/weatherforecast' -TimeoutSec 2
            break
        } catch { Start-Sleep -Seconds 1 }
    }
    if ($null -eq $response -or $response.StatusCode -ne 200) { throw 'API did not become ready within the allowed time.' }
    $forecast = @($response.Content | ConvertFrom-Json)
    if ($forecast.Count -ne 5 -or $null -eq $forecast[0].temperatureC) { throw 'Unexpected weatherforecast response.' }
    $exceptionResponse = Invoke-WebRequest 'http://127.0.0.1:5199/api/test-error' -SkipHttpErrorCheck -TimeoutSec 5
    $body = $exceptionResponse.Content | ConvertFrom-Json
    if ($exceptionResponse.StatusCode -ne 500 -or $body.success -ne $false -or [string]::IsNullOrWhiteSpace($body.message)) {
        throw 'Global exception handler returned an unexpected response.'
    }
    Write-Host 'API startup, HTTP response, and global exception wrapper verified.'
} finally {
    if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id -Force }
    $env:ASPNETCORE_ENVIRONMENT = $oldEnvironment
    $env:ASPNETCORE_URLS = $oldUrls
}
