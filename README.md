# 🚀 Production Setup: .NET App + Cloudflare Tunnel on Ubuntu

> Full production guide to deploy a .NET app on Ubuntu Server and expose it via Cloudflare Tunnel using your domain.

---

## 📋 Table of Contents

- [Requirements](#requirements)
- [Architecture](#architecture)
- [Step 1 — Update Ubuntu](#step-1--update-ubuntu)
- [Step 2 — Install .NET](#step-2--install-net)
- [Step 3 — Install Cloudflared](#step-3--install-cloudflared)
- [Step 4 — Login to Cloudflare](#step-4--login-to-cloudflare)
- [Step 5 — Create Tunnel](#step-5--create-tunnel)
- [Step 6 — Configure Tunnel](#step-6--configure-tunnel)
- [Step 7 — Route DNS](#step-7--route-dns)
- [Step 8 — Deploy .NET App](#step-8--deploy-net-app)
- [Step 9 — Create .NET Systemd Service](#step-9--create-net-systemd-service)
- [Step 10 — Create Cloudflared Systemd Service](#step-10--create-cloudflared-systemd-service)
- [Step 11 — Verify Everything](#step-11--verify-everything)
- [Useful Commands](#useful-commands)
- [Deploy Script](#deploy-script)
- [Logs & Monitoring](#logs--monitoring)
- [Security Hardening](#security-hardening)
- [Troubleshooting](#troubleshooting)

---

## Requirements

| Item | Details |
|------|---------|
| OS | Ubuntu 22.04 LTS or 24.04 LTS |
| .NET | .NET 8 or later |
| Domain | Managed by Cloudflare (e.g. `aholtp0444.notcode.uk`) |
| App URL | `http://localhost:5291` |
| User | Non-root sudo user recommended |

---

## Architecture

```
Internet
    │
    ▼
Cloudflare Edge  ←  WAF, DDoS, SSL, Bot Protection
    │ (encrypted tunnel)
    ▼
cloudflared      ←  systemd service (auto-start)
    │ (localhost only)
    ▼
.NET Kestrel :5291  ←  systemd service (auto-start)
```

> Your server **never opens inbound ports**. Cloudflare Tunnel is outbound-only — the safest production setup.

---

## Step 1 — Update Ubuntu

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl wget git unzip
```

---

## Step 2 — Install .NET

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK and Runtime
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Verify
dotnet --version
```

---

## Step 3 — Install Cloudflared

```bash
# Add Cloudflare GPG key and repository
curl -fsSL https://pkg.cloudflare.com/cloudflare-main.gpg | \
  sudo tee /usr/share/keyrings/cloudflare-main.gpg > /dev/null

echo "deb [signed-by=/usr/share/keyrings/cloudflare-main.gpg] \
  https://pkg.cloudflare.com/cloudflared $(lsb_release -cs) main" | \
  sudo tee /etc/apt/sources.list.d/cloudflared.list

# Install
sudo apt update
sudo apt install -y cloudflared

# Verify
cloudflared --version
```

---

## Step 4 — Login to Cloudflare

```bash
cloudflared tunnel login
```

> A URL will appear in the terminal. Open it in your browser and click **Authorize** on your domain `notcode.uk`.

The certificate will be saved to:
```
~/.cloudflared/cert.pem
```

---

## Step 5 — Create Tunnel

```bash
cloudflared tunnel create my-dotnet-app
```

Output example:
```
Tunnel credentials written to /home/USER/.cloudflared/abc-123-xyz.json
Created tunnel my-dotnet-app with id abc-123-xyz
```

> ⚠️ **Save your Tunnel ID** — you'll need it in the next step.

List tunnels anytime:
```bash
cloudflared tunnel list
```

---

## Step 6 — Configure Tunnel

```bash
# Create config directory
sudo mkdir -p /etc/cloudflared

# Create config file
sudo nano /etc/cloudflared/config.yml
```

Paste the following (replace values):

```yaml
tunnel: YOUR_TUNNEL_ID
credentials-file: /home/YOUR_USERNAME/.cloudflared/YOUR_TUNNEL_ID.json

ingress:
  - hostname: aholtp0444.notcode.uk
    service: http://localhost:5291
    originRequest:
      connectTimeout: 30s
      noTLSVerify: false
  - service: http_status:404
```

> ⚠️ Replace:
> - `YOUR_TUNNEL_ID` with the UUID from Step 5
> - `YOUR_USERNAME` with your Linux username (`echo $USER`)

Secure the config:
```bash
sudo chmod 600 /etc/cloudflared/config.yml
sudo chown root:root /etc/cloudflared/config.yml
```

---

## Step 7 — Route DNS

```bash
cloudflared tunnel route dns my-dotnet-app aholtp0444.notcode.uk
```

This auto-creates a **CNAME record** in Cloudflare pointing your subdomain to the tunnel.

Verify in Cloudflare Dashboard → DNS → you should see:
```
CNAME  aholtp0444  →  <TUNNEL_ID>.cfargotunnel.com
```

---

## Step 8 — Deploy .NET App

```bash
# Create app directory
sudo mkdir -p /var/www/myapp
sudo chown $USER:$USER /var/www/myapp

# On your local machine — publish and transfer
dotnet publish -c Release -o ./publish

# Copy to server (run this on your local machine)
scp -r ./publish/* user@your-server-ip:/var/www/myapp/

# OR if developing directly on server
cd /path/to/your/project
dotnet publish -c Release -o /var/www/myapp
```

Create production settings:

```bash
nano /var/www/myapp/appsettings.Production.json
```

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "aholtp0444.notcode.uk;localhost",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5291"
      }
    }
  }
}
```

---

## Step 9 — Create .NET Systemd Service

```bash
sudo nano /etc/systemd/system/myapp.service
```

Paste:

```ini
[Unit]
Description=My .NET Production App
After=network.target
Wants=network.target

[Service]
Type=simple
User=www-data
Group=www-data
WorkingDirectory=/var/www/myapp
ExecStart=/usr/bin/dotnet /var/www/myapp/MyApp.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=myapp

# Environment
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5291
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Logging
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

> ⚠️ Replace `MyApp.dll` with your actual DLL filename.

Set permissions and enable:

```bash
# Set ownership
sudo chown -R www-data:www-data /var/www/myapp

# Reload systemd, enable and start
sudo systemctl daemon-reload
sudo systemctl enable myapp
sudo systemctl start myapp

# Check status
sudo systemctl status myapp
```

---

## Step 10 — Create Cloudflared Systemd Service

```bash
# Copy credentials to system location
sudo cp ~/.cloudflared/YOUR_TUNNEL_ID.json /etc/cloudflared/
sudo chmod 600 /etc/cloudflared/YOUR_TUNNEL_ID.json

# Update credentials path in config
sudo nano /etc/cloudflared/config.yml
# Change credentials-file to: /etc/cloudflared/YOUR_TUNNEL_ID.json

# Install as system service
sudo cloudflared service install

# Enable and start
sudo systemctl enable cloudflared
sudo systemctl start cloudflared

# Check status
sudo systemctl status cloudflared
```

---

## Step 11 — Verify Everything

```bash
# Check both services are running
sudo systemctl status myapp
sudo systemctl status cloudflared

# Check .NET app is listening on port 5291
ss -tlnp | grep 5291

# Test locally
curl http://localhost:5291/WeatherForecast

# Test via domain
curl https://aholtp0444.notcode.uk/WeatherForecast
```

Expected output from both commands — your JSON weather data ✅

---

## Useful Commands

### Service Management

```bash
# Start
sudo systemctl start myapp
sudo systemctl start cloudflared

# Stop
sudo systemctl stop myapp
sudo systemctl stop cloudflared

# Restart
sudo systemctl restart myapp
sudo systemctl restart cloudflared

# Status
sudo systemctl status myapp
sudo systemctl status cloudflared
```

### Tunnel Management

```bash
# List tunnels
cloudflared tunnel list

# List tunnel connections
cloudflared tunnel info my-dotnet-app

# Delete a tunnel
cloudflared tunnel delete my-dotnet-app
```

---

## Deploy Script

Save this as `~/deploy.sh` for one-command deployments:

```bash
#!/bin/bash
set -e

APP_DIR="/var/www/myapp"
PROJECT_DIR="/path/to/your/project"  # Change this
SERVICE_NAME="myapp"

echo "🛑 Stopping app..."
sudo systemctl stop $SERVICE_NAME

echo "📦 Building..."
cd $PROJECT_DIR
dotnet publish -c Release -o $APP_DIR

echo "🔐 Setting permissions..."
sudo chown -R www-data:www-data $APP_DIR

echo "🚀 Starting app..."
sudo systemctl start $SERVICE_NAME

echo "⏳ Waiting for startup..."
sleep 3

echo "✅ Status:"
sudo systemctl status $SERVICE_NAME --no-pager

echo ""
echo "🌐 Testing endpoint..."
curl -s http://localhost:5291/WeatherForecast | head -c 200

echo ""
echo "✅ Deploy complete!"
```

Make executable:

```bash
chmod +x ~/deploy.sh
```

Run deploy anytime:

```bash
~/deploy.sh
```

> 💡 Cloudflare Tunnel keeps running — **no downtime for the tunnel** during deploys!

---

## Logs & Monitoring

### View Logs

```bash
# .NET app logs (live)
sudo journalctl -u myapp -f

# .NET app logs (last 100 lines)
sudo journalctl -u myapp -n 100

# Cloudflared logs (live)
sudo journalctl -u cloudflared -f

# Cloudflared logs (last 100 lines)
sudo journalctl -u cloudflared -n 100

# Both together
sudo journalctl -u myapp -u cloudflared -f
```

### Add Health Check to .NET App

In `Program.cs`:

```csharp
// Add health check
builder.Services.AddHealthChecks();

// Map endpoint
app.MapHealthChecks("/health");
```

Test:
```bash
curl https://aholtp0444.notcode.uk/health
# Output: Healthy
```

---

## Security Hardening

### 1. Cloudflare Dashboard Settings

| Setting | Location | Value |
|---------|---------|-------|
| SSL/TLS Mode | SSL/TLS tab | **Full (Strict)** |
| Always HTTPS | Edge Certificates | ✅ On |
| HSTS | Edge Certificates | ✅ On |
| Bot Fight Mode | Security tab | ✅ On |
| Min TLS Version | Edge Certificates | TLS 1.2 |

### 2. Ubuntu Firewall (UFW)

```bash
# Enable firewall — block all inbound except SSH
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh
sudo ufw enable

# Verify — port 5291 should NOT be open (tunnel handles it)
sudo ufw status
```

### 3. Fail2Ban (Brute Force Protection)

```bash
sudo apt install -y fail2ban
sudo systemctl enable fail2ban --now
```

### 4. Auto Security Updates

```bash
sudo apt install -y unattended-upgrades
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

---

## Troubleshooting

### App not starting

```bash
# Check detailed logs
sudo journalctl -u myapp -n 50 --no-pager

# Common fix: wrong DLL name
ls /var/www/myapp/*.dll

# Common fix: permissions
sudo chown -R www-data:www-data /var/www/myapp
```

### Tunnel not connecting

```bash
# Check cloudflared logs
sudo journalctl -u cloudflared -n 50 --no-pager

# Verify config file
sudo cat /etc/cloudflared/config.yml

# Test tunnel manually
cloudflared tunnel run my-dotnet-app
```

### Domain not resolving

```bash
# Check DNS
dig aholtp0444.notcode.uk

# Should show CNAME pointing to cfargotunnel.com
# Wait up to 5 minutes for DNS propagation
```

### Port already in use

```bash
# Find what's using port 5291
sudo lsof -i :5291

# Kill the process if needed
sudo kill -9 <PID>
```

---

## ✅ Final Checklist

```
✅ Ubuntu updated
✅ .NET 8 installed
✅ cloudflared installed
✅ Cloudflare login authorized
✅ Tunnel created
✅ config.yml configured with correct Tunnel ID and username
✅ DNS routed (CNAME visible in Cloudflare dashboard)
✅ .NET app published to /var/www/myapp
✅ myapp systemd service = Active (running)
✅ cloudflared systemd service = Active (running)
✅ UFW firewall enabled
✅ curl https://aholtp0444.notcode.uk/WeatherForecast returns data
✅ deploy.sh script created and tested
```

---

## 📁 File Structure

```
/
├── etc/
│   ├── cloudflared/
│   │   ├── config.yml              ← Tunnel configuration
│   │   └── YOUR_TUNNEL_ID.json     ← Tunnel credentials
│   └── systemd/system/
│       ├── myapp.service           ← .NET app service
│       └── cloudflared.service     ← Tunnel service (auto-created)
├── var/www/myapp/                  ← Published .NET app
└── home/USER/
    ├── deploy.sh                   ← Deploy script
    └── .cloudflared/
        └── cert.pem                ← Cloudflare certificate
```

---

*Generated for Ubuntu 22.04/24.04 LTS — .NET 8 — Cloudflare Tunnel*
