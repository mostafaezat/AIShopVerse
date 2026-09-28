# Scans the AIShopVerse repository for accidentally committed secrets.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\scan-secrets.ps1
#
# Exits 1 when a finding is reported (for CI-gating later) and 0 when clean.
# Allow-listed: the DEVELOPMENT-ONLY JWT key in appsettings.Development.json,
# the explicit test key in Application.Tests\TestDb.cs, and the documented test
# password fixture used by the test project. These are non-production by
# construction and never valid in any other environment.

param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$patterns = @(
    @{ Name = "JWT signing key (dev value leaked outside Development config)"; Pattern = 'ThisIsASecretKeyForJwtTokenGeneration2024' },
    @{ Name = "AWS access key id"; Pattern = 'AKIA[0-9A-Z]{16}' },
    @{ Name = "AWS secret access key"; Pattern = '(?i)aws[_-]?secret[_-]?access[_-]?key' },
    @{ Name = "Azure storage account key"; Pattern = '(?i)AccountKey\s*=\s*[A-Za-z0-9+/]{40,}' },
    @{ Name = "Private key block"; Pattern = '-----BEGIN (RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----' },
    @{ Name = "Inline password in connection string / URL"; Pattern = '(?i)(password|pwd)\s*=\s*(?!pwd|password)[^;\s]{3,}' },
    @{ Name = "Hardcoded password/secret value"; Pattern = '(?i)(api[_-]?key|secret|password|token|pwd)\s*[:=]\s*["'']{1}[^"'']{8,}["'']{1}' }
)

# Files where the DEVELOPMENT-ONLY JWT key legitimately lives.
$devKeyAllowList = @(
    (Join-Path $Root "AIShopVerse.EndUser\AIShopVerse.EndUser.Server\appsettings.Development.json"),
    (Join-Path $Root "AIShopVerse.AdminPanel\AIShopVerse.AdminPanel.Server\appsettings.Development.json")
)
$testKeyAllowList = @(
    (Join-Path $Root "Application.Tests\TestDb.cs")
)

# Non-secret fixture values the test project legitimately uses.
$valueAllowList = @(
    'Str0ng!Passw0rd'
)
$valueAllowRegex = '(?i)(unknown|placeholder|dummy|example|change-me|your-)'

$excludedSegments = @(
    "\node_modules\", "\bin\", "\obj\", "\dist\", "\.git\",
    "\wwwroot\images\", "\scripts", "\.angular"
)

$files = Get-ChildItem -Path $Root -Recurse -File -Include *.cs, *.json, *.csproj, *.yml, *.yaml, *.ps1, *.config, *.env, *.md
$findings = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $full = $file.FullName
    $skip = $false
    foreach ($seg in $excludedSegments) {
        if ($full.IndexOf($seg, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { $skip = $true; break }
    }
    if ($skip) { continue }

    $devKeyAllowed = ($devKeyAllowList | Where-Object { [string]::Equals($_, $full, [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
    $testKeyAllowed = ($testKeyAllowList | Where-Object { [string]::Equals($_, $full, [System.StringComparison]::OrdinalIgnoreCase) }).Count -gt 0

    foreach ($p in $patterns) {
        $matches = Select-String -LiteralPath $full -Pattern $p.Pattern -AllMatches
        foreach ($m in $matches) {
            foreach ($match in $m.Matches) {
                $value = $match.Value

                if ($devKeyAllowed -and $p.Name -like "JWT signing key*") {
                    Write-Host ("[OK] dev-only JWT key (allow-listed): {0}" -f $full) -ForegroundColor DarkGray
                    continue
                }
                if ($testKeyAllowed -and $p.Name -like "JWT signing key*") {
                    Write-Host ("[OK] test JWT key (allow-listed): {0}" -f $full) -ForegroundColor DarkGray
                    continue
                }

                $stripped = $value.Trim().Trim('"', "'")
                if (($valueAllowList | Where-Object { $value -like "*$_*" }) -or ($stripped -match $valueAllowRegex)) {
                    Write-Host ("[OK] test fixture value (allow-listed): {0}" -f $full) -ForegroundColor DarkGray
                    continue
                }

                $formatted = ("[{0}] {1}: {2}  (line {3})" -f $p.Name, $full, $value, $m.LineNumber)
                $findings.Add($formatted)
            }
        }
    }
}

if ($findings.Count -gt 0) {
    Write-Host "`nSecret scan FAILED - findings:" -ForegroundColor Red
    foreach ($f in $findings) { Write-Host $f -ForegroundColor Yellow }
    Write-Host "`nRemove the committed secrets (and purge history if already pushed)."
    exit 1
}

Write-Host "Secret scan passed: no secrets found." -ForegroundColor Green
exit 0