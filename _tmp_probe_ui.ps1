$ErrorActionPreference = 'Stop'
$base = 'http://10.230.8.170/UiFoundation'
$outDir = 'C:\Users\muhammad_ikram\tms-template-net8'
$rcPath = Join-Path $outDir '_tmp_runtime.js'
$rc = (Invoke-WebRequest -Uri "$base/ui/v1/runtime-core.js" -UseBasicParsing -TimeoutSec 60).Content
Set-Content -Path $rcPath -Value $rc -Encoding UTF8
Write-Output "runtime length: $($rc.Length)"

Select-String -Path $rcPath -Pattern 'datatable|ui-source|ajax|type:\s*"POST"|method' |
    Select-Object -First 60 |
    ForEach-Object { "{0}: {1}" -f $_.LineNumber, $_.Line.Trim().Substring(0, [Math]::Min(200, $_.Line.Trim().Length)) }

$compMatches = [regex]::Matches($rc, 'ui/v1/components/[A-Za-z0-9_\-\.]+')
Write-Output '--- components ---'
$compMatches | ForEach-Object { $_.Value } | Select-Object -Unique

# probe likely datatable paths
$paths = @(
    '/ui/v1/components/ui-datatable.js',
    '/ui/v1/components/ui-data-table.js',
    '/ui/v1/components/datatable.js',
    '/ui/v1/components/ui-table.js'
)
Write-Output '--- probe ---'
foreach ($p in $paths) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $p) -Method Head -UseBasicParsing -TimeoutSec 10
        Write-Output "$($r.StatusCode) $p"
    } catch {
        Write-Output "FAIL $p"
    }
}
