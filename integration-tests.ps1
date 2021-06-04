function Invoke-Cli {
    <#
    https://stackoverflow.com/a/48877892/37725
    #>
    $ErrorActionPreference = 'Continue'
    try { & dotnet run --project Canopy.Cli.Executable -- $Args } catch { Throw } # catch is triggered ONLY if $exe can't be found, never for errors reported by $exe itself
    if ($LASTEXITCODE) { Throw "$exe indicated failure (exit code $LASTEXITCODE; full command: $Args)." }
}
$guid = New-Guid
$guidString = $guid.ToString("n")
$testUserId = "u-" + $guidString.Substring(0, 18)
$guid = New-Guid
$testUserPassword = $guid.ToString("n")

Write-Output "Connecting to API"
Invoke-Cli connect --client-id "canopy" --client-secret "$($env:CANOPY_CLIENT_SECRET)"

Write-Output "Authenticating as master"
Invoke-Cli authenticate --username master --company test --password "$($env:CANOPY_TEST_ACCOUNT_MASTER_PASSWORD)"

Write-Output "Creating test user"
Invoke-Cli create-user --username "$testUserId" --email "$testUserId@testing.canopysimulations.com" --password "$testUserPassword"

Write-Output "Authenticating as test user"
Invoke-Cli authenticate --username "$testUserId" --company test --password "$testUserPassword"

Write-Output "Running integration tests"
Invoke-Cli integration-tests
