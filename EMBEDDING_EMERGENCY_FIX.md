# Google AI Embeddings - Emergency Fix (160 404 Errors) ✅

**Date:** 2026-04-30  
**Status:** 🚨 **INVESTIGATING** - Backfill disabled, logging enabled

---

## 🚨 Problem Summary

Google AI embeddings generated **160 failed requests (404 errors)** before we could intervene. Root cause unknown - likely one of:

1. ❌ **Key Vault integration failed** - API key not loaded in containers
2. ❌ **Network issue** - Containers can't reach Google AI endpoint
3. ❌ **API quota** - Rate limited despite free tier
4. ❌ **API format** - Request sent in wrong format

---

## ✅ Actions Taken

### 1. **DISABLED Backfill (URGENT)** ✅
Prevents further wasted requests to Google AI API.

**Changes:**
- Added `"Enabled": false` to `EmbeddingBackfill` config
- Updated `EmbeddingBackfillOptions` class with Enabled flag
- Updated both backfill workers to check flag before running
- Both workers log: "Portfolio embedding backfill is disabled"

**Files changed:**
- `src/Services/Portfolio/Portfolio.API/appsettings.json`
- `src/Services/Company/Company.API/appsettings.json`
- `src/Shared/RecruitmentPlatform.AI/Models/EmbeddingReadinessPolicy.cs`
- `src/Services/Portfolio/Portfolio.Infrastructure/Messaging/PortfolioEmbeddingBackfillWorker.cs`
- `src/Services/Company/Company.Infrastructure/Messaging/CompanyEmbeddingBackfillWorker.cs`

**Deployed:** ✅ `portfolio-service--0000012`, `company-service--0000014`

---

### 2. **Added VERBOSE LOGGING** ✅
Enables rapid diagnosis of the root cause.

**Changes to GoogleAiEmbeddingService:**
```csharp
// Constructor - Log if API key is loaded
if (string.IsNullOrEmpty(_apiKey)) {
    _logger.LogWarning("⚠️ Google AI API key is empty! Key Vault integration may have failed...");
} else {
    _logger.LogInformation("✅ Google AI API key loaded successfully (length: {KeyLength} chars)");
}

// CreateEmbeddingAsync - Log each step
_logger.LogInformation("Generating embedding via Google AI (model: {Model}, textLength: {TextLength})");
_logger.LogError("❌ Google AI embedding request failed: Status={Status}, Body={Body}");
_logger.LogInformation("✅ Embedding generated successfully");
```

**Expected logs will show:**
- If API key loads: "✅ Google AI API key loaded successfully (length: 39 chars)"
- If API key missing: "⚠️ Google AI API key is empty! Key Vault integration may have failed"
- Each embedding attempt: Request details, response status, success/failure

**Deployed:** ✅ `portfolio-service--0000013`, `company-service--0000015`

---

## 🔍 Next Steps to Diagnose

### Step 1: Check Logs (Next 5 minutes)
Monitor Portfolio and Company service logs for:
1. Check if "✅ Google AI API key loaded successfully" appears
   - If YES: Key Vault integration working, problem elsewhere
   - If NO: Key Vault integration failed, need to fix managed identity

2. Check if you see embedding attempt logs
   - With status 200: API is working
   - With status 404: API rejecting requests
   - With timeout: Network issue

### Step 2: Create Test Portfolio
Once deployed, create a test portfolio with minimal data and check:
- Does it generate embedding?
- What logs do we see?
- Is embedding stored (Status=SUCCESS or FAIL)?

### Step 3: Check Container Managed Identity
If API key isn't loading, run:
```bash
# Verify portfolio-service has managed identity
az containerapp identity show -n portfolio-service -g skillsnap-rg-2604282023

# Should output:
# "type": "SystemAssigned"
# "principalId": "..."

# Check if identity has Key Vault permission
az keyvault show -n sskv2604282023545
```

---

## 📊 Current Status

| Component | Status | Details |
|-----------|--------|---------|
| **Backfill** | ✅ DISABLED | Won't waste quota |
| **Logging** | ✅ ENABLED | Will show errors |
| **New Portfolios** | ⏸️ READY | Can be tested |
| **API Key** | ❓ UNKNOWN | Will see in logs |
| **Google AI API** | ❓ UNKNOWN | Will test with portfolio |

---

## ⏰ Timeline

| Time | Action |
|------|--------|
| 20:06 | User reports 160 failed requests (404 errors) |
| 20:10 | Disabled backfill (Enabled: false) → Deployed |
| 20:15 | Added verbose logging → Deployed |
| 20:20 | **← CURRENT: Waiting for logs** |
| 20:25 | Check logs, diagnose root cause |
| 20:30 | Create test portfolio, verify |
| 20:35 | Enable backfill if all working, or fix issue |

---

## 🎯 Success Criteria

To confirm fix is working:

1. ✅ **Logs show API key loaded** - "Google AI API key loaded successfully"
2. ✅ **New portfolio generates embedding** - Creates entry in PortfolioEmbeddings
3. ✅ **Embedding status is SUCCESS** - Not FAIL
4. ✅ **Embedding value is not NULL** - Contains 768 float values
5. ✅ **No 404 errors** - All requests succeed

---

## 📝 Key Vault Debug Commands

```bash
# Check Key Vault exists
az keyvault show --name sskv2604282023545

# Check if secret exists
az keyvault secret show --vault-name sskv2604282023545 --name "GoogleAI--ApiKey"

# Check container app managed identity permissions
az keyvault set-policy --name sskv2604282023545 \
  --object-id <CONTAINER_APP_IDENTITY_ID> \
  --secret-permissions get list
```

---

## 🔄 Rollback Plan

If we can't fix this quickly:
1. Keep backfill disabled (leave Enabled: false)
2. Switch embedding provider to OpenAI
3. Or disable embeddings entirely until API issue resolved

To switch providers:
```json
// In appsettings.json
"EmbeddingProvider": "OpenAI"  // Instead of "GoogleAI"
```

---

## 📞 References

- **Google AI Embedding API:** https://ai.google.dev/models/gemini-embedding
- **Azure Key Vault Integration:** Learn about managed identity
- **Error Log Location:** Azure Container Apps → Logs / Monitoring
