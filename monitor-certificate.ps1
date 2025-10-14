# SchemaMagic SSL Certificate Monitor
# This script monitors the SSL certificate provisioning for schema.magicsuite.net
# and automatically enables HTTPS when the certificate is ready.

param(
    [int]$CheckIntervalMinutes = 30,
    [int]$MaxChecks = 48
)

Write-Host @"
??????????????????????????????????????????????????????????????????
?   SchemaMagic SSL Certificate Monitor                         ?
?   Monitoring: schema.magicsuite.net                           ?
??????????????????????????????????????????????????????????????????
"@ -ForegroundColor Cyan

Write-Host "`nConfiguration:" -ForegroundColor Yellow
Write-Host "  Check Interval: $CheckIntervalMinutes minutes"
Write-Host "  Maximum Checks: $MaxChecks (up to $($MaxChecks * $CheckIntervalMinutes / 60) hours)"
Write-Host "  Start Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

$checkInterval = $CheckIntervalMinutes * 60  # Convert to seconds

for ($i = 1; $i -le $MaxChecks; $i++) {
    Write-Host "`n$('=' * 70)" -ForegroundColor DarkGray
    Write-Host "[Check $i/$MaxChecks] $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Yellow
    Write-Host "$('=' * 70)" -ForegroundColor DarkGray
    
    try {
        # Check certificate status
        Write-Host "`nQuerying GitHub API..." -ForegroundColor Gray
        $pagesInfo = gh api repos/panoramicdata/SchemaMagic/pages | ConvertFrom-Json
        
        Write-Host "  CNAME: $($pagesInfo.cname)" -ForegroundColor Gray
        Write-Host "  Status: $($pagesInfo.status)" -ForegroundColor Gray
        Write-Host "  HTTPS Enforced: $($pagesInfo.https_enforced)" -ForegroundColor Gray
        
        if ($pagesInfo.https_certificate -ne $null) {
            Write-Host "`n? Certificate Found!" -ForegroundColor Green
            Write-Host "  State: $($pagesInfo.https_certificate.state)" -ForegroundColor Green
            Write-Host "  Description: $($pagesInfo.https_certificate.description)" -ForegroundColor Green
            
            if ($pagesInfo.https_enforced -eq $false) {
                Write-Host "`nEnabling HTTPS enforcement..." -ForegroundColor Cyan
                
                try {
                    gh api --method PUT repos/panoramicdata/SchemaMagic/pages `
                      -f cname='schema.magicsuite.net' `
                      -f source[branch]=gh-pages `
                      -f source[path]=/ `
                      -F https_enforced=true
                    
                    Write-Host "`n??????????????????????????????????????????????????????????????" -ForegroundColor Green
                    Write-Host "?  ? SUCCESS! HTTPS has been enabled!                      ?" -ForegroundColor Green
                    Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Green
                    Write-Host "`nYour site is now available at:" -ForegroundColor Green
                    Write-Host "  ?? https://schema.magicsuite.net" -ForegroundColor Green
                    
                    Write-Host "`nVerifying..." -ForegroundColor Cyan
                    Start-Sleep -Seconds 5
                    
                    $response = curl -I https://schema.magicsuite.net 2>&1 | Select-Object -First 15
                    Write-Host "`nHTTPS Response:" -ForegroundColor Gray
                    $response | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
                    
                } catch {
                    Write-Host "`n??  HTTPS enforcement command failed:" -ForegroundColor Yellow
                    Write-Host "  Error: $_" -ForegroundColor Yellow
                    Write-Host "`nYou can manually enable HTTPS at:" -ForegroundColor Yellow
                    Write-Host "  https://github.com/panoramicdata/SchemaMagic/settings/pages" -ForegroundColor Yellow
                }
                
            } else {
                Write-Host "`n? HTTPS is already enforced!" -ForegroundColor Green
                Write-Host "`nYour site is available at:" -ForegroundColor Green
                Write-Host "  ?? https://schema.magicsuite.net" -ForegroundColor Green
            }
            
            Write-Host "`n??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
            Write-Host "?  Monitoring Complete!                                      ?" -ForegroundColor Cyan
            Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
            break
            
        } else {
            Write-Host "`n? Certificate not provisioned yet" -ForegroundColor Yellow
            Write-Host "  This is normal and can take 15 minutes to 24 hours" -ForegroundColor Gray
            
            # Test HTTP access
            Write-Host "`nTesting HTTP access..." -ForegroundColor Gray
            try {
                $httpResponse = curl -I http://schema.magicsuite.net 2>&1 | Select-String "HTTP/1.1" | Select-Object -First 1
                if ($httpResponse -match "200") {
                    Write-Host "  ? HTTP access working: $httpResponse" -ForegroundColor Green
                } else {
                    Write-Host "  ??  HTTP response: $httpResponse" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "  ? HTTP access failed" -ForegroundColor Red
            }
        }
        
    } catch {
        Write-Host "`n? Error querying GitHub API:" -ForegroundColor Red
        Write-Host "  $($_)" -ForegroundColor Red
    }
    
    if ($i -lt $MaxChecks) {
        $nextCheck = (Get-Date).AddSeconds($checkInterval)
        Write-Host "`n? Next check in $CheckIntervalMinutes minutes (at $($nextCheck.ToString('HH:mm:ss')))" -ForegroundColor Gray
        Write-Host "  Press Ctrl+C to stop monitoring" -ForegroundColor DarkGray
        
        # Sleep with countdown
        for ($s = $checkInterval; $s -gt 0; $s -= 60) {
            $minutesLeft = [Math]::Ceiling($s / 60)
            Write-Progress -Activity "Waiting for next check" `
                          -Status "$minutesLeft minutes remaining" `
                          -PercentComplete ((($checkInterval - $s) / $checkInterval) * 100)
            Start-Sleep -Seconds 60
        }
        Write-Progress -Activity "Waiting for next check" -Completed
    }
}

if ($i -gt $MaxChecks) {
    Write-Host "`n??????????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "?  ??  Monitoring time limit reached                        ?" -ForegroundColor Yellow
    Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Yellow
    Write-Host "`nThe SSL certificate has not been provisioned yet." -ForegroundColor Yellow
    Write-Host "This can take up to 24-48 hours in some cases." -ForegroundColor Yellow
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  1. Check GitHub Pages settings:" -ForegroundColor Gray
    Write-Host "     https://github.com/panoramicdata/SchemaMagic/settings/pages" -ForegroundColor Gray
    Write-Host "  2. Verify DNS propagation:" -ForegroundColor Gray
    Write-Host "     nslookup schema.magicsuite.net 8.8.8.8" -ForegroundColor Gray
    Write-Host "  3. Check GitHub status:" -ForegroundColor Gray
    Write-Host "     https://www.githubstatus.com/" -ForegroundColor Gray
    Write-Host "  4. Run this script again later:" -ForegroundColor Gray
    Write-Host "     .\monitor-certificate.ps1" -ForegroundColor Gray
}

Write-Host "`n"
