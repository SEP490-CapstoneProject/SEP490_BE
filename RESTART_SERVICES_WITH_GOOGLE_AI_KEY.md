# Restart Services to Load Google AI Key from Key Vault

## Overview
After adding the Google AI API key to Azure Key Vault, the services need to be restarted to load the key into memory.

## Services to Restart

1. **Portfolio Service**
   - Current URL: `https://portfolio-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
   - Configuration: `appsettings.json` has `EmbeddingProvider: GoogleAI`
   - Key Reference: Reads `GoogleAI--ApiKey` from Key Vault

2. **Company Service**
   - Current URL: `https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
   - Configuration: `appsettings.json` has `EmbeddingProvider: GoogleAI`
   - Key Reference: Reads `GoogleAI--ApiKey` from Key Vault

## How Services Load the Key

**Startup Process**:
```
1. Container starts
   ↓
2. Program.cs runs
   ↓
3. Configuration built from:
   - appsettings.json (local config)
   - appsettings.{Environment}.json (environment-specific)
   - Key Vault secrets (via Azure.Identity)
   ↓
4. Dependency Injection initializes
   ↓
5. AiServiceCollectionExtensions.AddRecruitmentPlatformAi() runs
   ↓
6. Reads configuration["EmbeddingProvider"]
   ↓
7. If "GoogleAI": Creates GoogleAiEmbeddingService with HttpClient
   ↓
8. GoogleAiEmbeddingService constructor:
   - Reads configuration["GoogleAI:ApiKey"] from Key Vault
   - Stores in _apiKey field
   ↓
9. Service ready to generate embeddings
```

## Steps to Restart Services

### Option 1: Azure Container Apps (Recommended)
Use Azure CLI to restart containers:

```powershell
# Restart Portfolio Service
az containerapp revision list --name portfolio --resource-group <resource-group> --query "[0].name" -o tsv | ForEach-Object {
    Write-Host "Restarting portfolio revision: $_"
}

# Or simpler approach - redeploy current image
az containerapp update `
    --name portfolio `
    --resource-group redmushroom `
    --image skillsnapacr2604282023545.azurecr.io/portfolio:latest

# Restart Company Service  
az containerapp update `
    --name company `
    --resource-group redmushroom `
    --image skillsnapacr2604282023545.azurecr.io/company:latest
```

### Option 2: Docker Desktop (if running locally)
```powershell
# Stop and remove containers
docker stop portfolio-service company-service
docker rm portfolio-service company-service

# Restart with docker-compose
cd D:\Capstone
docker-compose up -d portfolio company
```

### Option 3: Manual Container Apps Scaling (Force Restart)
```powershell
# Scale down to 0, then back up
az containerapp update --name portfolio --resource-group redmushroom --min-replicas 0
Start-Sleep -Seconds 5
az containerapp update --name portfolio --resource-group redmushroom --min-replicas 1

az containerapp update --name company --resource-group redmushroom --min-replicas 0
Start-Sleep -Seconds 5
az containerapp update --name company --resource-group redmushroom --min-replicas 1
```

## Verification

After restart, verify services loaded the key:

### 1. Check Service Health
```powershell
# Both services should respond
Invoke-WebRequest -Uri "https://portfolio-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/portfolio?page=1"
Invoke-WebRequest -Uri "https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/company-posts?page=1"
```

### 2. Check Service Logs
Look for confirmation that:
- Key Vault connection successful ✅
- Google AI API key loaded ✅
- GoogleAiEmbeddingService initialized ✅
- No "key not configured" errors ❌

### 3. Create Test Portfolio
Run the test script after restart to verify embeddings generate successfully:
```powershell
cd D:\Capstone
.\TEST_PORTFOLIO_EMBEDDING_GENERATION.ps1
```

## Expected Service Behavior After Restart

✅ **Before Embedding Generation Attempted**:
- Service starts normally
- Configuration loaded from Key Vault
- GoogleAiEmbeddingService ready with valid API key

✅ **During Portfolio Creation**:
- Portfolio saved to database
- Embedding event published to RabbitMQ
- Embedding consumer processes event
- GoogleAiEmbeddingService.CreateEmbeddingAsync() called
- Sends request to Google AI API (NOT 429 error ❌)

✅ **After Embeddings Generated**:
- 768-dimensional embedding returned
- Stored in SQL Server
- Ready for similarity matching

## Troubleshooting

### ❌ If Services Still Fail

**Check 1: Key Vault Access**
```powershell
# Verify Container App has managed identity with Key Vault access
az containerapp show --name portfolio --resource-group redmushroom --query identity
```

**Check 2: API Key Secret Name**
- Secret must be named: `GoogleAI--ApiKey`
- Double-dash format converts to colon in config
- So `GoogleAI--ApiKey` → `config["GoogleAI:ApiKey"]` ✅

**Check 3: Service Logs**
- Check Portfolio/Company service logs for errors
- Look for "Google AI API key is not configured" message
- If present, key loading failed

**Check 4: RabbitMQ Connection**
- Even if key is loaded, embeddings won't generate if RabbitMQ unreachable
- Check both services can connect to RabbitMQ
- Previous tests showed Portfolio had RabbitMQ issues

## Success Indicators

After restart and before test, services should:

✅ Start successfully (no Key Vault connection errors)  
✅ Respond to API requests  
✅ Have GoogleAiEmbeddingService initialized  
✅ Logs show "GoogleAiEmbeddingService created" or similar  
✅ No "API key not configured" errors  

## Next Steps After Successful Restart

1. Run embedding test: `.\TEST_PORTFOLIO_EMBEDDING_GENERATION.ps1`
2. Verify portfolio created
3. Check SQL Server for embeddings (768 dimensions)
4. Test similarity matching
5. Compare results with OpenAI baseline (if available)

---

## Quick Command Reference

**Restart Portfolio (Azure)**:
```powershell
az containerapp update --name portfolio --resource-group redmushroom --image skillsnapacr2604282023545.azurecr.io/portfolio:latest
```

**Restart Company (Azure)**:
```powershell
az containerapp update --name company --resource-group redmushroom --image skillsnapacr2604282023545.azurecr.io/company:latest
```

**Run Test**:
```powershell
cd D:\Capstone
.\TEST_PORTFOLIO_EMBEDDING_GENERATION.ps1
```

**Check Logs** (for specific service):
```powershell
az containerapp logs show --name portfolio --resource-group redmushroom
```
