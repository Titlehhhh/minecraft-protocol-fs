param(
    [Parameter(Mandatory = $true)]
    [string]$Project,

    [Parameter(Mandatory = $true)]
    [string]$MutexName
)

$mutex = [System.Threading.Mutex]::new($false, $MutexName)
$buildLog = Join-Path $env:TEMP ("packetgenerator-build-{0}-{1}.log" -f $PID, [Guid]::NewGuid().ToString("N"))

try {
    $null = $mutex.WaitOne()
    & dotnet build $Project -maxcpucount:1 -nologo -v:q *> $buildLog
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        Get-Content $buildLog | ForEach-Object { [Console]::Error.WriteLine($_) }
        exit $exitCode
    }
}
finally {
    if (Test-Path $buildLog) {
        Remove-Item $buildLog -Force -ErrorAction SilentlyContinue
    }

    try {
        $mutex.ReleaseMutex()
    }
    catch {
    }

    $mutex.Dispose()
}
