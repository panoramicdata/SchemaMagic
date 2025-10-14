# Setting Up Custom Domain for GitHub Pages

## Overview
You want to configure `schema.magicsuite.net` to point to your GitHub Pages site for SchemaMagic.

## Prerequisites
- GitHub Pages site is published (typically at `https://panoramicdata.github.io/SchemaMagic`)
- Access to DNS settings for `magicsuite.net` domain
- Domain ownership verification

## Step 1: Configure DNS Records

You have two options for DNS configuration:

### Option A: CNAME Record (Recommended for subdomains)

Add the following DNS record in your DNS provider (e.g., Cloudflare, GoDaddy, Route53):

```
Type:  CNAME
Name:  schema (or schema.magicsuite.net depending on provider)
Value: panoramicdata.github.io
TTL:   Auto or 3600 (1 hour)
```

### Option B: A Records (Alternative approach)

If CNAME is not available, use GitHub's IP addresses:

```
Type:  A
Name:  schema (or schema.magicsuite.net)
Value: 185.199.108.153
TTL:   Auto or 3600

Type:  A
Name:  schema (or schema.magicsuite.net)
Value: 185.199.109.153
TTL:   Auto or 3600

Type:  A
Name:  schema (or schema.magicsuite.net)
Value: 185.199.110.153
TTL:   Auto or 3600

Type:  A
Name:  schema (or schema.magicsuite.net)
Value: 185.199.111.153
TTL:   Auto or 3600
```

**Note:** GitHub's IP addresses can change. CNAME is preferred as it automatically follows GitHub's infrastructure changes.

## Step 2: Configure Custom Domain in GitHub

1. Go to your GitHub repository: `https://github.com/panoramicdata/SchemaMagic`
2. Navigate to **Settings** ? **Pages**
3. Under "Custom domain", enter: `schema.magicsuite.net`
4. Click **Save**
5. Wait for DNS check to complete (can take a few minutes to 48 hours)

## Step 3: Enable HTTPS

After DNS propagation:

1. In the same GitHub Pages settings page
2. Check the box: **Enforce HTTPS**
3. GitHub will automatically provision an SSL certificate via Let's Encrypt

## Step 4: Add CNAME File to Repository

GitHub Pages requires a `CNAME` file in your repository root (or in the published directory):

### For Blazor WebAssembly Projects

The `CNAME` file should be in `SchemaMagic.Web/wwwroot/`:

```
schema.magicsuite.net
```

This file will be included in the published output and tell GitHub Pages which custom domain to use.

## DNS Provider-Specific Instructions

### Cloudflare
1. Log in to Cloudflare Dashboard
2. Select your domain: `magicsuite.net`
3. Go to **DNS** ? **Records**
4. Click **Add record**
5. Type: `CNAME`
6. Name: `schema`
7. Target: `panoramicdata.github.io`
8. Proxy status: DNS only (gray cloud) - **Important for GitHub Pages**
9. Click **Save**

**Important:** Set Cloudflare to "DNS only" mode (not proxied) for GitHub Pages custom domains.

### GoDaddy
1. Log in to GoDaddy
2. Go to **My Products** ? **Domains**
3. Click **DNS** next to magicsuite.net
4. Click **Add** under Records
5. Type: `CNAME`
6. Name: `schema`
7. Value: `panoramicdata.github.io`
8. TTL: 1 Hour
9. Click **Save**

### AWS Route 53
1. Open Route 53 console
2. Select **Hosted zones**
3. Select `magicsuite.net`
4. Click **Create record**
5. Record name: `schema`
6. Record type: `CNAME`
7. Value: `panoramicdata.github.io`
8. TTL: 300
9. Click **Create records**

### Namecheap
1. Log in to Namecheap
2. Go to **Domain List** ? **Manage** for magicsuite.net
3. Go to **Advanced DNS** tab
4. Click **Add New Record**
5. Type: `CNAME Record`
6. Host: `schema`
7. Value: `panoramicdata.github.io`
8. TTL: Automatic
9. Click the checkmark to save

## Verification Steps

### 1. Check DNS Propagation

Wait 5-60 minutes (sometimes up to 48 hours for full propagation), then test:

**Command Line:**
```bash
# Windows (PowerShell)
nslookup schema.magicsuite.net

# macOS/Linux
dig schema.magicsuite.net

# Or use online tool
# https://www.whatsmydns.net/#CNAME/schema.magicsuite.net
```

Expected result should show: `panoramicdata.github.io`

### 2. Check GitHub Pages Status

In your repository's Settings ? Pages:
- DNS check should show: ? DNS check successful
- HTTPS should show: ? HTTPS certificate provisioned

### 3. Test the Website

Open in browser:
```
http://schema.magicsuite.net
```

If HTTPS is enabled:
```
https://schema.magicsuite.net
```

## Common Issues & Solutions

### Issue 1: "DNS check unsuccessful"
**Solution:** 
- Verify DNS records are correct
- Wait longer for DNS propagation (up to 48 hours)
- Use `nslookup` or `dig` to verify DNS is resolving
- Clear your browser cache and DNS cache

### Issue 2: "CNAME already in use by another repository"
**Solution:**
- Another GitHub repository is using this domain
- Remove it from the other repository first
- Or contact GitHub support if you don't have access

### Issue 3: "404 - There isn't a GitHub Pages site here"
**Solution:**
- Ensure GitHub Pages is enabled in repository settings
- Verify the CNAME file is in the published output
- Check that the site is being published from the correct branch

### Issue 4: Certificate provisioning fails
**Solution:**
- Ensure DNS is fully propagated first
- Try unchecking and re-checking "Enforce HTTPS"
- Wait 24 hours and try again
- Verify CAA DNS records (if any) allow Let's Encrypt

### Issue 5: Cloudflare "Too many redirects"
**Solution:**
- Set Cloudflare to "DNS only" mode (gray cloud, not orange)
- Or configure SSL/TLS mode to "Full" in Cloudflare

## DNS Cache Clearing

If changes aren't visible:

**Windows:**
```powershell
ipconfig /flushdns
```

**macOS:**
```bash
sudo dscacheutil -flushcache
sudo killall -HUP mDNSResponder
```

**Linux:**
```bash
sudo systemd-resolve --flush-caches
```

**Browser:**
- Chrome: `chrome://net-internals/#dns` ? Clear host cache
- Firefox: Settings ? Privacy & Security ? Clear Data ? Cached Web Content
- Edge: `edge://net-internals/#dns` ? Clear host cache

## File Structure for GitHub Pages

Ensure your repository includes:

```
SchemaMagic.Web/
??? wwwroot/
    ??? CNAME              # Contains: schema.magicsuite.net
    ??? index.html
    ??? css/
    ??? js/
    ??? images/
```

## Creating the CNAME File

Create `SchemaMagic.Web/wwwroot/CNAME` with the following content:

```
schema.magicsuite.net
```

**Important:** 
- No `http://` or `https://`
- No trailing slash
- Just the domain name
- Single line, no extra lines

## GitHub Actions Workflow (Optional)

If using GitHub Actions for deployment, ensure the workflow preserves the CNAME file:

```yaml
- name: Publish Blazor WebAssembly
  run: dotnet publish SchemaMagic.Web/SchemaMagic.Web.csproj -c Release -o release --nologo

- name: Add CNAME file
  run: echo "schema.magicsuite.net" > release/wwwroot/CNAME

- name: Deploy to GitHub Pages
  uses: peaceiris/actions-gh-pages@v3
  with:
    github_token: ${{ secrets.GITHUB_TOKEN }}
    publish_dir: ./release/wwwroot
    force_orphan: true
```

## Timeline

| Step | Expected Time |
|------|---------------|
| Add DNS record | Immediate |
| DNS propagation (initial) | 5-60 minutes |
| DNS propagation (worldwide) | Up to 48 hours |
| GitHub DNS check | 5-15 minutes after propagation |
| SSL certificate provisioning | 15 minutes - 24 hours |
| Full HTTPS availability | 1-48 hours total |

## Testing Checklist

- [ ] DNS records added to provider
- [ ] DNS propagation verified with `nslookup`/`dig`
- [ ] CNAME file added to repository
- [ ] Custom domain configured in GitHub Pages settings
- [ ] GitHub shows "DNS check successful"
- [ ] Site accessible at `http://schema.magicsuite.net`
- [ ] HTTPS enabled and enforced
- [ ] SSL certificate provisioned
- [ ] Site accessible at `https://schema.magicsuite.net`
- [ ] HTTP redirects to HTTPS
- [ ] Certificate is valid (green padlock in browser)

## Monitoring & Maintenance

### Regular Checks
- Monitor DNS record status monthly
- Verify SSL certificate auto-renewal (Let's Encrypt renews every 60-90 days)
- Check GitHub Pages status if site becomes unavailable

### Analytics
Consider adding analytics to track custom domain usage:
- Google Analytics
- Plausible Analytics
- Simple Analytics

## Security Considerations

1. **HTTPS Only:** Always enforce HTTPS after certificate is provisioned
2. **CAA Records:** Optionally add CAA DNS records to specify Let's Encrypt as allowed CA
3. **HSTS:** GitHub Pages automatically adds HSTS headers for custom domains
4. **Subresource Integrity:** Consider adding SRI hashes for external resources

## Additional Resources

- [GitHub Pages Documentation](https://docs.github.com/en/pages)
- [Managing a custom domain for your GitHub Pages site](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site)
- [Troubleshooting custom domains and GitHub Pages](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/troubleshooting-custom-domains-and-github-pages)
- [DNS Propagation Checker](https://www.whatsmydns.net/)
- [SSL Labs Server Test](https://www.ssllabs.com/ssltest/)

## Quick Reference Commands

```bash
# Check DNS resolution
nslookup schema.magicsuite.net

# Test HTTPS certificate
curl -I https://schema.magicsuite.net

# Check GitHub Pages status
git log --all --grep="pages build"

# Verify CNAME file in published site
curl https://panoramicdata.github.io/SchemaMagic/CNAME
```

## Summary

**To set up schema.magicsuite.net:**

1. **Add CNAME DNS record** pointing `schema` to `panoramicdata.github.io`
2. **Create CNAME file** in `SchemaMagic.Web/wwwroot/` containing `schema.magicsuite.net`
3. **Configure custom domain** in GitHub repository Settings ? Pages
4. **Enable HTTPS** after DNS propagation and certificate provisioning
5. **Test and verify** site is accessible at `https://schema.magicsuite.net`

Total time: 1-48 hours depending on DNS propagation and certificate provisioning.
