# ?? Quick Reference: schema.magicsuite.net Setup

## Current Status: 60% Complete ?

```
? DNS configured
? Domain linked to GitHub
? Site live at HTTP
? Waiting for SSL certificate (automated)
? HTTPS pending (1 command away)
```

## Your Site is LIVE! ??

```
http://schema.magicsuite.net
```

## What's Happening Now

GitHub is automatically provisioning an SSL certificate from Let's Encrypt.
This takes **15 minutes to 24 hours** (typically 1-4 hours).

## Quick Commands

### Check Certificate Status
```powershell
gh api repos/panoramicdata/SchemaMagic/pages --jq '.https_certificate'
```

### Enable HTTPS (when ready)
```powershell
gh api --method PUT repos/panoramicdata/SchemaMagic/pages `
  -f cname='schema.magicsuite.net' `
  -f source[branch]=gh-pages `
  -f source[path]=/ `
  -F https_enforced=true
```

### Run Automated Monitor
```powershell
.\monitor-certificate.ps1
```

### Test Your Site
```powershell
# HTTP (working now)
curl -I http://schema.magicsuite.net

# HTTPS (will work after cert)
curl -I https://schema.magicsuite.net
```

## Timeline

| Task | Status | ETA |
|------|--------|-----|
| DNS Setup | ? | Done |
| HTTP Access | ? | Done |
| SSL Cert | ? | 1-24 hrs |
| HTTPS | ? | +1 min after cert |

## Next Steps

### Option 1: Automated (Recommended)
```powershell
.\monitor-certificate.ps1
```
Checks every 30 min and auto-enables HTTPS when ready.

### Option 2: Manual
Check every hour:
```powershell
gh api repos/panoramicdata/SchemaMagic/pages --jq '.https_certificate'
```
When you see certificate info, run the enable HTTPS command above.

### Option 3: Web UI
Visit: https://github.com/panoramicdata/SchemaMagic/settings/pages
Wait for "Enforce HTTPS" checkbox to appear, then check it.

## Files Created

- ?? `DNS_PROGRESS_REPORT.md` - Detailed status
- ?? `STEPS_2_PLUS_COMPLETE.md` - Full summary
- ?? `monitor-certificate.ps1` - Auto-monitor script
- ?? `QUICK_REFERENCE.md` - This file

## Help

**Certificate not ready after 24 hours?**
See troubleshooting in `DNS_PROGRESS_REPORT.md`

**Want to test now?**
Use the original URL: https://panoramicdata.github.io/SchemaMagic

**Questions?**
Check: https://github.com/panoramicdata/SchemaMagic/settings/pages

## Success Indicators

? Certificate is ready when:
- `https://schema.magicsuite.net` loads
- Green padlock appears in browser
- No security warnings

## Bottom Line

Your site works NOW at `http://schema.magicsuite.net` ?

HTTPS will work automatically in 1-24 hours ?

Run `.\monitor-certificate.ps1` to auto-enable when ready ??

---

*Quick Reference Card*
*Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')*
