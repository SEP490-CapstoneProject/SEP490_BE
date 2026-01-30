# ✅ HTTPS Configuration Complete

## 🎉 API Gateway Now Running with HTTPS

Your API Gateway is configured with SSL termination. External clients use HTTPS, internal services use HTTP.

---

## 🌐 Access URLs

### API Gateway
- **HTTPS (Recommended)**: https://localhost:7000/swagger
- **HTTP**: http://localhost:5000/swagger (redirects to HTTPS)

### Microservices (Internal HTTP - Not Exposed)
- Auth Service: http://auth-service:8080 (internal only)
- UserProfile Service: http://userprofile-service:8080 (internal only)
- Other services: HTTP within Docker network

---

## 🔒 Certificate Information

**Development Certificate:**
- Location: `%USERPROFILE%\.aspnet\https\aspnetapp.pfx`
- Password: `YourSecurePassword123!`
- Type: Self-signed ASP.NET Core development certificate
- Trusted: Yes (via `dotnet dev-certs https --trust`)

**Browser Warning:**
- You may see a certificate warning on first access
- This is normal for self-signed certificates
- Click "Advanced" → "Proceed to localhost" to continue

---

## 📊 Architecture

```
┌─────────────────┐
│  External       │
│  Clients        │
│  (Browser/App)  │
└────────┬────────┘
         │
         │ HTTPS (Port 7000)
         │ SSL Termination
         ▼
┌─────────────────────────┐
│   API Gateway           │
│   - HTTPS: 7000         │
│   - HTTP:  5000         │
└────────┬────────────────┘
         │
         │ HTTP (Internal Docker Network)
         │
    ┌────┴────┬────────┬─────────┐
    │         │        │         │
    ▼         ▼        ▼         ▼
┌────────┐ ┌────────┐ ┌────────┐ ...
│ Auth   │ │Profile │ │Company │
│Service │ │Service │ │Service │
│:8080   │ │:8080   │ │:8080   │
└────────┘ └────────┘ └────────┘
```

---

## 🧪 Testing

### 1. Test HTTPS Endpoint

**PowerShell:**
```powershell
# Test HTTPS (ignore cert warning with -k)
curl https://localhost:7000/swagger/index.html -k

# Test HTTP redirect
curl http://localhost:5000/swagger/index.html -L
```

**Browser:**
1. Open: https://localhost:7000/swagger
2. Accept certificate warning (if shown)
3. You should see Swagger UI

### 2. Test API Endpoints

Once you have API endpoints configured:
```powershell
# Example: Call Auth service through gateway
curl https://localhost:7000/api/auth/users -k
```

---

## 📝 Configuration Files Updated

### 1. appsettings.json
Added Kestrel HTTPS endpoint configuration with certificate path.

### 2. Dockerfile.runtime
Exposed both HTTP (8080) and HTTPS (8081) ports.

### 3. docker-compose.yml
Added API Gateway service with:
- Port mappings: 5000 (HTTP), 7000 (HTTPS)
- Certificate volume mount
- Environment variables for HTTPS

### 4. Program.cs
Added HTTPS redirection configuration.

---

## 🔄 Rebuild Instructions

If you make changes to API Gateway:

```powershell
# 1. Publish
cd d:\Capstone\src\ApiGateway
dotnet publish -c Release -o .\publish

# 2. Rebuild Docker image
cd d:\Capstone
docker build -t capstone-api-gateway -f src/ApiGateway/Dockerfile.runtime .

# 3. Restart container
docker-compose restart api-gateway

# 4. View logs
docker logs api-gateway
```

---

## 🚀 Production Considerations

> [!WARNING]
> **For Production Deployment:**
> 
> 1. **Use Real Certificates**
>    - Obtain from Let's Encrypt, DigiCert, or cloud provider
>    - Automate renewal (Certbot, ACME protocol)
> 
> 2. **Use Reverse Proxy**
>    - Nginx, Traefik, or cloud load balancer
>    - Better SSL termination performance
>    - Advanced features (rate limiting, caching)
> 
> 3. **Security Headers**
>    - HSTS (HTTP Strict Transport Security)
>    - CSP (Content Security Policy)
>    - X-Frame-Options, X-Content-Type-Options
> 
> 4. **Remove HTTP Port**
>    - Only expose HTTPS in production
>    - Force all traffic through HTTPS

---

## 🔍 Troubleshooting

### Certificate Not Found
```powershell
# Regenerate certificate
dotnet dev-certs https --clean
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p "YourSecurePassword123!"
dotnet dev-certs https --trust
```

### Container Won't Start
```powershell
# Check logs
docker logs api-gateway

# Common issues:
# - Certificate path incorrect
# - Password mismatch
# - Port already in use
```

### Browser Certificate Warning
This is expected for self-signed certificates. Options:
1. Click "Advanced" → "Proceed to localhost"
2. Add certificate to trusted root (Windows)
3. Use production certificates

---

## ✅ Verification Checklist

- [x] Development certificate generated
- [x] Certificate trusted on local machine
- [x] API Gateway appsettings.json updated
- [x] Dockerfile.runtime created with HTTPS port
- [x] docker-compose.yml updated with API Gateway
- [x] Program.cs configured for HTTPS redirection
- [x] API Gateway container running
- [x] HTTPS port 7000 accessible

---

**Status**: 🟢 HTTPS fully configured and operational!

**Access your API**: https://localhost:7000/swagger
