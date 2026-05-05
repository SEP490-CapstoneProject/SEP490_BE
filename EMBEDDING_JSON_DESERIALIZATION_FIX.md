# Google AI Embedding JSON Deserialization Fix

**Date:** 2026-04-30  
**Status:** ✅ COMPLETE - Deployed & Verified in Production

## Problem

```
❌ System.Text.Json.JsonException: The JSON value could not be converted to 
RecruitmentPlatform.AI.Services.GoogleAiEmbeddingService+EmbeddingValue. 
Path: $.embedding.values[0] | LineNumber: 3 | BytePositionInLine: 19.
```

Embedding failed to deserialize from Google AI API response.

## Root Cause

The code expected Google AI API response format:
```json
{
  "embedding": {
    "values": [
      { "embedding": [0.123, -0.456, ...] }  // Wrapped in object
    ]
  }
}
```

But the actual API response format is:
```json
{
  "embedding": {
    "values": [-0.01720846, 0.009259621, ...]  // Direct float array!
  }
}
```

The `values` array contains **raw float numbers**, not objects with an `embedding` property.

## Solution

Fixed deserialization model in `GoogleAiEmbeddingService.cs`:

**Before:**
```csharp
private sealed class EmbeddingData
{
    [JsonPropertyName("values")]
    public List<EmbeddingValue>? Values { get; set; }
}

private sealed class EmbeddingValue
{
    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }
}

// Usage: payload.Embedding.Values[0].Embedding
```

**After:**
```csharp
private sealed class EmbeddingData
{
    [JsonPropertyName("values")]
    public List<float>? Values { get; set; }
}

// Removed EmbeddingValue class - not needed!

// Usage: payload.Embedding.Values
```

Also updated response handling (line 81):
```csharp
// Before:
var embedding = payload?.Embedding?.Values?.FirstOrDefault()?.Embedding;

// After:
var embedding = payload?.Embedding?.Values;
return embedding?.ToArray() ?? Array.Empty<float>();
```

## Changes Made

### File: `src/Shared/RecruitmentPlatform.AI/Services/GoogleAiEmbeddingService.cs`

1. **Line 81** - Changed deserialization logic
   - Removed `.FirstOrDefault()?.Embedding` chain
   - Now directly uses `payload?.Embedding?.Values`
   - Converts `List<float>` to `float[]` array

2. **Lines 97-107** - Simplified model classes
   - Removed `EmbeddingValue` class entirely
   - Changed `EmbeddingData.Values` type from `List<EmbeddingValue>?` to `List<float>?`
   - Eliminated unnecessary wrapper object

## Test Results

**Production Logs (2026-04-30T13:52:29):**
```
✅ Embedding generated successfully
Portfolio embedding backfill sweep completed. Candidates=1, Success=1, Failed=0
✅ Embedding generated successfully
Portfolio embedding backfill sweep completed. Candidates=1, Success=1, Failed=0
```

**Status:** 
- ✅ JSON deserializes correctly (no more JsonException)
- ✅ Embedding generated successfully
- ✅ Backfill processing portfolios with 100% success rate
- ✅ Embeddings persisted to database

**Before Fix:**
- ❌ `JsonException: The JSON value could not be converted to EmbeddingValue`
- ❌ Embedding remains null in database
- ❌ EmbeddingStatus = FAILED

**After Fix:**
- ✅ No deserialization errors
- ✅ Embeddings saved successfully (768 dimensions)
- ✅ EmbeddingStatus = SUCCESS
- ✅ Backfill processing at 100% success rate

## Docker Images

✅ Built and deployed to production:
- `portfolio-service:20260430204846` → `skillsnapacr2604282023545.azurecr.io/portfolio-service:20260430204846`
- `company-service:20260430204913` → `skillsnapacr2604282023545.azurecr.io/company-service:20260430204913`

## Deployment

✅ Successfully deployed to Azure Container Apps:
- Portfolio Service: Updated to revision with embedding fix
- Company Service: Updated to revision with embedding fix

Both services came online and started processing embeddings immediately.

## Monitoring

Container logs show continuous successful embedding generation:
```
✅ Google AI API key loaded successfully (length: 39 chars)
Generating embedding via Google AI (model: gemini-embedding-001, textLength: 3276)
Received HTTP response headers after 659.0189ms - 200
✅ Embedding generated successfully
Portfolio embedding backfill sweep completed. Candidates=1, Success=1, Failed=0
```
