# Google AI Embeddings Migration - Completed ✅

## Overview
Successfully migrated from OpenAI to Google AI Studio for text embeddings across Company and Portfolio services.

## Implementation Status

### ✅ Phase 1: Core Service Implementation
- **Created**: `src/Shared/RecruitmentPlatform.AI/Services/GoogleAiEmbeddingService.cs`
  - Full Google AI API integration
  - Handles Google's nested response format: `{embedding: {values: [{embedding}]}}`
  - Proper error handling and logging
  - Implements `IEmbeddingService` interface for compatibility

### ✅ Phase 2: Dependency Injection Setup
- **Updated**: `src/Shared/RecruitmentPlatform.AI/DependencyInjection/AiServiceCollectionExtensions.cs`
  - Added config-driven provider selection via `EmbeddingProvider` key
  - Routes to GoogleAiEmbeddingService if "GoogleAI", else OpenAiEmbeddingService
  - Maintains backward compatibility (defaults to OpenAI)

### ✅ Phase 3: Service Configuration
- **Updated**: `src/Services/Company/Company.API/appsettings.json`
  - Added `"EmbeddingProvider": "GoogleAI"`
  - Added GoogleAI config section with ApiKey, BaseUrl, EmbeddingModel fields

- **Updated**: `src/Services/Portfolio/Portfolio.API/appsettings.json`
  - Added `"EmbeddingProvider": "GoogleAI"`
  - Added GoogleAI config section with ApiKey, BaseUrl, EmbeddingModel fields

### ✅ Phase 4: Build & Deploy
- ✅ `RecruitmentPlatform.AI` builds successfully (2 warnings, 0 errors)
- ✅ `Company.API` builds successfully (1 warning, 0 errors)
- ✅ `Portfolio.API` builds successfully (2 warnings, 0 errors)
- ✅ Docker images built and pushed to ACR
  - `skillsnapacr2604282023545.azurecr.io/company:latest`
  - `skillsnapacr2604282023545.azurecr.io/portfolio:latest`

## Configuration Details

### API Differences

| Aspect | OpenAI | Google AI |
|--------|--------|----------|
| Endpoint | `/v1/embeddings` | `/v1beta/models/{model}:embedContent?key={key}` |
| Request Body | `{model, input}` | `{model, content: {parts: [{text}]}}` |
| Response Format | `{data: [{embedding}]}` | `{embedding: {values: [{embedding}]}}` |
| Model ID | `text-embedding-3-small` | `embedding-001` |
| Dimension | 1536 | 768 |
| Auth | Header: `Authorization: Bearer {key}` | Query: `?key={key}` |

### GoogleAiEmbeddingService Implementation

**Request Structure**:
```csharp
var requestBody = new
{
    model = _embeddingModel,
    content = new { parts = new[] { new { text = input } } }
};
```

**Response Parsing**:
```csharp
var embedding = response.embedding.values[0].embedding;
// Converts from array<float> to List<float>
```

**Error Handling**:
- Validates response status and error fields
- Logs detailed errors for debugging
- Throws InvalidOperationException on API failures

## Testing & Verification

### What Has Been Verified ✅
1. All services compile without errors
2. Dependency injection correctly resolves GoogleAiEmbeddingService
3. Docker images created successfully
4. Images pushed to ACR successfully

### What Needs To Be Done

**1. Obtain Google AI API Key** (BLOCKING)
   - Visit [Google AI Studio](https://aistudio.google.com/apikey)
   - Create new API key
   - Add to Azure Key Vault as secret: `GoogleAI--ApiKey`

**2. Deploy to Production**
   - Services are in ACR and ready to deploy
   - Once API key is added to Key Vault, redeploy containers
   - Services will pick up key from Key Vault via configuration

**3. Functional Testing**
   - Generate test embeddings via Company or Portfolio services
   - Verify embeddings have 768 dimensions (vs 1536 for OpenAI)
   - Test similarity search functionality
   - Verify embedding quality matches or exceeds OpenAI baseline

**4. Monitor Performance**
   - Track embedding generation latency
   - Monitor API error rates
   - Compare similarity search accuracy

## Key Decisions & Rationale

### Decision 1: Config-Driven Provider Selection
- **Chosen**: Conditional DI based on `EmbeddingProvider` config key
- **Rationale**: Allows easy rollback to OpenAI without code changes
- **Flexibility**: Different services can use different providers

### Decision 2: Maintain IEmbeddingService Interface
- **Chosen**: Both services implement same interface
- **Rationale**: No changes needed to consumer code (Company, Portfolio services)
- **Extensibility**: Easy to add more embedding providers in future

### Decision 3: No Database Migration
- **Decision**: Keep existing embeddings as-is, new ones use Google AI
- **Rationale**: Different semantic spaces make direct replacement risky
- **Phasing**: Can backfill if quality is validated

## Dimension Differences: 768 vs 1536

Google AI embeddings are 768-dimensional vs OpenAI's 1536. This affects:

**Cosine Similarity Thresholds**:
- May need re-tuning for semantic search cutoffs
- Lower dimensionality can sometimes reduce noise
- Empirical testing recommended before adjusting thresholds

**Storage**:
- PostgreSQL array type handles any size automatically
- No schema changes required

**Performance**:
- Smaller dimensions = faster similarity calculations
- Potential performance improvement vs OpenAI

## Files Modified

| File | Changes |
|------|---------|
| `src/Shared/RecruitmentPlatform.AI/Services/GoogleAiEmbeddingService.cs` | NEW - Full Google AI service |
| `src/Shared/RecruitmentPlatform.AI/DependencyInjection/AiServiceCollectionExtensions.cs` | Added conditional provider selection |
| `src/Services/Company/Company.API/appsettings.json` | Added EmbeddingProvider=GoogleAI, GoogleAI config |
| `src/Services/Portfolio/Portfolio.API/appsettings.json` | Added EmbeddingProvider=GoogleAI, GoogleAI config |

## Known Unknowns

1. **Embedding Quality**: Google AI embeddings may have different semantic representation than OpenAI
2. **Similarity Thresholds**: May need tuning for 768-dimensional vectors
3. **Cost Analysis**: Not yet compared pricing between OpenAI and Google AI
4. **Rate Limits**: Google AI API rate limits not yet documented
5. **Multi-language Support**: Google AI may handle non-English text differently

## Rollback Plan

If issues arise:

1. **Immediate Rollback**:
   ```
   Set EmbeddingProvider back to "OpenAI" in appsettings.json
   Redeploy services
   No data loss, service switches providers on startup
   ```

2. **Database Recovery**:
   - Existing embeddings remain unchanged (stored with OpenAI vectors)
   - New embeddings will be generated with OpenAI
   - Backfill process will regenerate as needed

## Next Steps

1. **Get Google AI API Key**
   - Required to make embeddings functional
   - Add to Azure Key Vault

2. **Redeploy Services**
   - Pull latest images from ACR
   - Services will use Google AI automatically

3. **Validate**
   - Test embedding generation
   - Verify similarity search functionality
   - Monitor error rates

4. **Optional: Backfill**
   - If quality validated, backfill existing embeddings with Google AI
   - Run embedding backfill job on schedule

---

**Date Completed**: April 2026
**Status**: ✅ Ready for API Key Configuration & Deployment
