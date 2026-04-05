# PickURide API - Publish Script for SmarterASP.net
# This script publishes the application and verifies all dependencies are included

Write-Host "=== PickURide API - Publishing for SmarterASP.net ===" -ForegroundColor Cyan

# Set publish output directory
$publishDir = ".\publish-output"

# Clean previous publish
if (Test-Path $publishDir) {
    Write-Host "Cleaning previous publish output..." -ForegroundColor Yellow
    Remove-Item -Path $publishDir -Recurse -Force
}

# Publish the application
Write-Host "`nPublishing application..." -ForegroundColor Green
dotnet publish PickURide.API/PickURide.API.csproj -c Release -o $publishDir --no-self-contained

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Publish completed successfully!" -ForegroundColor Green

# Verify critical files
Write-Host "`nVerifying critical files..." -ForegroundColor Cyan

$criticalFiles = @(
    "Stripe.net.dll",
    "PickURide.API.dll",
    "PickURide.API.exe",
    "PickURide.API.runtimeconfig.json",
    "web.config",
    "appsettings.json"
)

$allPresent = $true
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $publishDir $file
    if (Test-Path $filePath) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file - MISSING!" -ForegroundColor Red
        $allPresent = $false
    }
}

if (-not $allPresent) {
    Write-Host "`n⚠️  Warning: Some critical files are missing!" -ForegroundColor Yellow
} else {
    Write-Host "`n✅ All critical files are present!" -ForegroundColor Green
}

# Count DLL files
$dllCount = (Get-ChildItem -Path $publishDir -Filter "*.dll" -Recurse).Count
Write-Host "`nTotal DLL files: $dllCount" -ForegroundColor Cyan

# Display publish directory size
$size = (Get-ChildItem -Path $publishDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Publish output size: $([math]::Round($size, 2)) MB" -ForegroundColor Cyan

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Review files in: $publishDir" -ForegroundColor White
Write-Host "2. Upload all files to SmarterASP.net via FTP" -ForegroundColor White
Write-Host "3. Ensure web.config and appsettings.json are configured correctly" -ForegroundColor White
Write-Host "4. Test your API at: https://yourdomain.com/swagger" -ForegroundColor White
Write-Host "`n📖 See DEPLOYMENT.md for detailed instructions" -ForegroundColor Yellow

