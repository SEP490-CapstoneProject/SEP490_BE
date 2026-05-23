# Portfolio AI Preview - Imagen 4 Generate & Media Service Integration Guide

## Overview

The Portfolio AI Preview system now integrates **Google Cloud Imagen 4 Generate** for image generation and uses the **Media Service** (Cloudinary) for image storage. All configuration values are retrieved from **Azure KeyVault** for secure credential management.

---

## Architecture

```
User Portfolio Request
        ↓
Gemini 1.5 Pro (Text Analysis)
        ↓
VisualPromptService (Gemini 1.5 Pro)
        ↓
ImageGenerationService (Imagen 4 Generate)
        ↓
Media Service (Cloudinary Upload)
        ↓
Store Image URL in Database
```

---

## Configuration Setup

### 1. KeyVault Secrets Required

Add the following secrets to Azure KeyVault (`sskv2604282023545`):

#### Google Cloud Configuration
```
GoogleAI--ApiKey
  Description: Gemini API key for text generation
  Value: <your-gemini-api-key-from-google-cloud-console>

GoogleAI--GenerativeModel
  Description: Gemini model to use for text generation
  Value: gemini-1.5-pro

Google--ProjectId
  Description: Google Cloud Project ID for Imagen API
  Value: <your-gcp-project-id>

Google--Location
  Description: Google Cloud region for Imagen API
  Value: us-central1

Google--ServiceAccountKey
  Description: Service account JSON for Imagen API authentication
  Value: <your-service-account-json-as-string>
```

#### Media Service Configuration
```
ServiceUrls--MediaService
  Description: Media service endpoint
  Value: https://media-service.yourdomain.com  (or internal: http://media-service:8080)
```

### 2. Appsettings Configuration

The `appsettings.json` is already configured to read from KeyVault:

```json
{
  "GoogleAI": {
    "BaseUrl": "https://generativelanguage.googleapis.com",
    "ApiKey": "",  // Loaded from KeyVault
    "EmbeddingModel": "gemini-embedding-001",
    "GenerativeModel": "gemini-1.5-pro"
  },
  "Google": {
    "ProjectId": "",  // Loaded from KeyVault (Google--ProjectId)
    "Location": "us-central1",
    "ServiceAccountKey": ""  // Loaded from KeyVault (Google--ServiceAccountKey)
  }
}
```

The Portfolio.API is configured to load KeyVault secrets via:
```csharp
builder.Configuration.AddAzureKeyVault();
```

This uses Azure Identity to authenticate and retrieve secrets from KeyVault.

---

## Service Implementation Details

### ImageGenerationService

**Location**: `Portfolio.Application/Services/ImageGenerationService.cs`

**Imagen 4 Generate API Configuration**:
```
Endpoint: https://{location}-aiplatform.googleapis.com/v1/projects/{projectId}/locations/{location}/models/imagen-4.0-generate-001:predict
Model: imagen-4.0-generate-001
Region: us-central1 (configurable)
```

**Parameters**:
- `prompt` - The visual prompt describing the image
- `negative_prompt` - Elements to avoid (e.g., "low quality, blurry")
- `width` - 1024 pixels
- `height` - 1024 pixels
- `number_of_images` - 1
- `safety_settings` - Content filtering for hate speech, dangerous content, sexual content

**Authentication**:
- Uses OAuth2 Bearer token
- Token obtained from Google Cloud service account
- Implements `GetGoogleCredentialsAsync()` for token exchange (currently placeholder)

### VisualPromptService

**Location**: `Portfolio.Application/Services/VisualPromptService.cs`

**Features**:
- Analyzes portfolio preview JSON
- Generates structured visual directives for Imagen
- Supports themes: professional, creative, minimal, startup, corporate, cyberpunk
- Supports recruiter personas: FAANG, Startup, GameStudio, AICompany, Default
- Output: VisualPromptDto with visual theme, elements, colors, hero text, style

### PortfolioPreviewService

**Location**: `Portfolio.Application/Services/PortfolioPreviewService.cs`

**Integration Pipeline**:
```csharp
1. GoogleAiPreviewGenerator.GeneratePreviewAsync()
   ↓ Returns: {title, shortPreview, highlights[], summary}

2. VisualPromptService.GenerateVisualPromptAsync()
   ↓ Returns: {visualTheme, mainElements[], colorPalette[], heroText, style}

3. ImageGenerationService.GenerateAndUploadImageAsync()
   ├─ Calls Imagen 4 Generate API
   ├─ Converts image bytes to IFormFile
   ├─ Uploads to Media Service
   ├─ Returns Cloudinary URL
   └─ Stores in database

4. PortfolioPreview entity updated with:
   - VisualPrompt (JSON)
   - ImageUrl (Cloudinary URL)
   - ImagegenModel (imagen-4.0-generate-001)
   - CacheKey (SHA256 hash for validation)
```

---

## Database Schema

**PortfolioPreview Table** (Extended Fields):

```sql
-- New columns (added via migration)
visualPrompt NVARCHAR(MAX)           -- Visual prompt JSON from VisualPromptService
imageUrl NVARCHAR(MAX)               -- Cloudinary URL from Media Service
recruiterSummary NVARCHAR(MAX)       -- Recruiter-specific variant
selectedTheme NVARCHAR(50)           -- professional|creative|minimal|startup|corporate|cyberpunk
socialCaption NVARCHAR(MAX)          -- LinkedIn variant
imagegen_model NVARCHAR(50)          -- Current: imagen-4.0-generate-001
cacheKey NVARCHAR(MAX)               -- SHA256(PreviewJson + VisualPrompt)
```

**Migration**:
```
File: 20260521_AddPreviewImageAndAdvancedFields.cs
Status: Ready to apply (db.Database.Migrate() on startup)
```

---

## API Endpoint

### Generate Portfolio Preview

**Endpoint**:
```
POST /api/portfolio/{portfolioId}/preview/generate
```

**Request**:
```json
{
  "highlightsDescription": "Full-stack developer with AI expertise",
  "theme": "professional",
  "recruiterPersona": "FAANG"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Preview generated successfully",
  "preview": {
    "id": 1,
    "portfolioId": 123,
    "title": "Senior Full-Stack Developer",
    "shortPreview": "...",
    "highlights": ["Full-stack development", "AI/ML", "..."],
    "recruiterSummary": "...",
    "socialCaption": "...",
    "visualStyleSuggestion": "...",
    "visualPrompt": {
      "visualTheme": "Modern tech aesthetic",
      "mainElements": ["Code editor", "AI patterns", "..."],
      "colorPalette": ["#007AFF", "#34C759", "..."],
      "heroText": "Full-Stack AI Developer",
      "style": "photorealistic"
    },
    "theme": "professional",
    "imageUrl": "https://res.cloudinary.com/skillsnap/image/upload/...",
    "version": 1,
    "regeneratedCount": 0,
    "createdAt": "2026-05-21T...",
    "updatedAt": "2026-05-21T...",
    "generationModel": "gemini-1.5-pro",
    "tokensUsed": 450,
    "imagegenModel": "imagen-4.0-generate-001"
  }
}
```

---

## Error Handling

### Graceful Degradation

If image generation fails:
- ✅ Text preview still generated (via Gemini)
- ✅ Visual prompt still generated (via Gemini)
- ⚠️ Image generation skipped
- ✅ Preview returned with text only
- 🔍 Error logged for troubleshooting

### Recovery

- Retry logic with exponential backoff
- Proper error messages and logging
- Support for re-triggering image generation later

---

## Security Considerations

1. **Service Account Key**: 
   - Stored in KeyVault
   - Never committed to source code
   - Accessed via Azure Managed Identity

2. **API Keys**:
   - Gemini API key stored in KeyVault
   - Accessed via environment variable loading
   - Never hardcoded in application

3. **Token Exchange**:
   - Service account key used to generate OAuth2 tokens
   - Tokens have limited lifetime (1 hour)
   - Automatic token refresh on expiration

4. **Content Safety**:
   - Imagen safety settings configured
   - Blocks harmful content (hate speech, dangerous, sexual)
   - Medium and above severity violations blocked

---

## Deployment Checklist

- [ ] Azure KeyVault secrets configured
- [ ] Service account JSON created in Google Cloud Console
- [ ] Imagen 4 Generate API enabled in Google Cloud Project
- [ ] Vertex AI API enabled in Google Cloud Project
- [ ] Database migration applied (`dotnet ef database update`)
- [ ] Docker image built and pushed to ACR
- [ ] Azure Container App deployed with KeyVault integration
- [ ] Test preview generation with sample portfolio
- [ ] Verify image generation and upload to Media Service
- [ ] Monitor API costs and usage

---

## Testing

### Unit Test
```csharp
[Fact]
public async Task GeneratePreview_CreatesImageAndUploadsToMediaService()
{
    // Arrange
    var portfolioId = 123;
    var previewService = new PortfolioPreviewService(...);

    // Act
    var result = await previewService.GeneratePreviewAsync(portfolioId);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Preview.ImageUrl);
    Assert.StartsWith("https://res.cloudinary.com/", result.Preview.ImageUrl);
    Assert.Equal("imagen-4.0-generate-001", result.Preview.ImagegenModel);
}
```

### Integration Test
```bash
# Generate preview
curl -X POST https://portfolio-service/api/portfolio/123/preview/generate \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "highlightsDescription": "Test preview",
    "theme": "professional",
    "recruiterPersona": "FAANG"
  }'

# Verify response includes image URL and Imagen 4 model
```

---

## Monitoring & Logging

**Log Entries**:
```
✅ Visual prompt generated successfully
✅ Image generated from Imagen API: {SizeBytes} bytes
✅ Image uploaded to Media service: {Url}
✅ Portfolio preview created successfully
```

**Metrics to Monitor**:
- Average image generation time
- Imagen API success rate
- Media Service upload success rate
- Average tokens used per preview

**Alert Conditions**:
- Image generation failures > 5%
- Media Service upload failures > 5%
- Average generation time > 30 seconds
- API quota exceeded

---

## Troubleshooting

### Issue: "Google ProjectId not configured"
**Solution**: Ensure `Google--ProjectId` is set in KeyVault

### Issue: "Failed to get Google Cloud credentials"
**Solution**: 
1. Verify service account JSON is valid
2. Check OAuth2 token exchange implementation
3. Ensure service account has Imagen API permissions

### Issue: "Image generation failed: API returned 403"
**Solution**:
1. Verify service account has `roles/aiplatform.user` role
2. Check Imagen 4 Generate API is enabled in GCP
3. Verify project quota not exceeded

### Issue: "Empty image data received"
**Solution**:
1. Check visual prompt is valid
2. Verify safety settings not blocking image
3. Check Imagen API quota

---

## Cost Estimation

**Gemini API**:
- ~$0.075 per 1M input tokens (text-embedding)
- ~$0.30 per 1M input tokens (gemini-1.5-pro)
- ~$1.20 per 1M output tokens (gemini-1.5-pro)

**Imagen 4 Generate**:
- ~$0.04 per image (1024x1024)

**Media Service (Cloudinary)**:
- Free tier: 25 credits/month (~300 images)
- Pay-as-you-go: $0.10-0.15 per image

**Estimated Cost per Preview**:
- Gemini text analysis: ~$0.01
- Gemini visual prompt: ~$0.01
- Imagen generation: ~$0.04
- **Total: ~$0.06 per preview**

---

## References

- **Imagen 4 Generate**: https://cloud.google.com/vertex-ai/generative-ai/docs/image/generate-images
- **Gemini API**: https://ai.google.dev/
- **Azure KeyVault**: https://learn.microsoft.com/en-us/azure/key-vault/
- **Media Service**: Uses existing Cloudinary integration
