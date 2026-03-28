# Portfolio Service Migration Fix - IDENTITY_INSERT Error

**Date:** 2026-03-27  
**Status:** ✅ **FIXED & DEPLOYED**

---

## 🐛 Problem

Portfolio service deployment failed với lỗi:

```
Cannot insert explicit value for identity column in table 'BlockType' 
when IDENTITY_INSERT is set to OFF.
```

**Root cause:**  
Migration `20260305151547_InitialDataJson.cs` cố gắng insert explicit Id values (1-10) vào table `BlockType` có IDENTITY column, nhưng không bật `IDENTITY_INSERT` trước khi insert.

---

## 🔧 Solution

### File changed:
`src/Services/Portfolio/Portfolio.Infrastructure/Migrations/20260305151547_InitialDataJson.cs`

### What was fixed:
Thêm `SET IDENTITY_INSERT [BlockType] ON/OFF` wrap around seed data inserts:

**Before (lines 81-103):**
```sql
-- ── Seed BlockType ────────────────────────────────────────────────
migrationBuilder.Sql(@"
    IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 1)
        INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (1, N'INTRO', 0);
    IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 2)
        INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (2, N'SKILL', 1);
    -- ... (8 more inserts)
");
```

**After:**
```sql
-- ── Seed BlockType ────────────────────────────────────────────────
migrationBuilder.Sql(@"
    SET IDENTITY_INSERT [BlockType] ON;
    
    IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 1)
        INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (1, N'INTRO', 0);
    IF NOT EXISTS (SELECT 1 FROM [BlockType] WHERE [Id] = 2)
        INSERT INTO [BlockType] ([Id], [Code], [IsMultiple]) VALUES (2, N'SKILL', 1);
    -- ... (8 more inserts)
    
    SET IDENTITY_INSERT [BlockType] OFF;
");
```

---

## 📦 Deployment

### 1. Rebuild Docker image
```powershell
docker build -t "skillsnapregistry.azurecr.io/portfolio-service:latest" `
  -f "src/Services/Portfolio/Dockerfile" .
```

**Result:** ✅ Build successful (7.0s build + 5.4s publish)

### 2. Push to Azure Container Registry
```powershell
docker push "skillsnapregistry.azurecr.io/portfolio-service:latest"
```

**Result:** ✅ Pushed  
**Digest:** `sha256:2835b3987f49a73a1354c172f251297345da5955cb86832444c4802ad014ce1a`

### 3. Update Container App
```powershell
# Deactivate old failing revision
az containerapp revision deactivate `
  --name portfolio-service `
  --resource-group CapStone `
  --revision "portfolio-service--9islemw"

# Force create new revision
az containerapp update `
  --name portfolio-service `
  --resource-group CapStone `
  --set-env-vars "FORCE_UPDATE=20260327072700"
```

**New revision:** `portfolio-service--0000001`  
**Status:** ✅ Running

---

## ✅ Verification

### 1. Migration logs (successful)
```
Executed DbCommand (5ms) [CommandType='Text']
ALTER TABLE [Portfolio] ADD [ComplimentCount] int NOT NULL DEFAULT 0;

Executed DbCommand (5ms) [CommandType='Text']
CREATE TABLE [Compliment] ( ... );

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260317083228_AddComplimentFeature', N'8.0.0');
```

✅ No IDENTITY_INSERT errors  
✅ All migrations applied successfully  
✅ Compliment feature tables created

### 2. Application startup
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080

info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.

info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
```

✅ Service started successfully

### 3. Swagger endpoint
```powershell
Invoke-WebRequest https://portfolio-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger
```

**Response:** `200 OK`  
✅ Swagger UI accessible

---

## 📝 Technical Details

### Why IDENTITY_INSERT is needed

**IDENTITY columns** in SQL Server auto-generate values. When you want to insert **explicit values** (like 1, 2, 3), you must:

1. **Enable:** `SET IDENTITY_INSERT [Table] ON;`
2. **Insert:** `INSERT INTO [Table] ([Id], ...) VALUES (1, ...);`
3. **Disable:** `SET IDENTITY_INSERT [Table] OFF;`

**Without this:**
- SQL Server rejects explicit Id values
- Error: `Cannot insert explicit value for identity column`

### Why we need explicit Ids for BlockType

BlockType table uses **predefined constants** referenced in code:

```csharp
public static class BlockTypeIds
{
    public const int INTRO = 1;
    public const int SKILL = 2;
    public const int EDUCATION = 3;
    // ...
}
```

Business logic relies on these specific Ids, so we must seed them with exact values (not auto-generated).

---

## 🎯 Impact

**Services affected:** Portfolio service only

**Database changes:**
- ✅ BlockType table: 10 seed records (Ids 1-10)
- ✅ Compliment table: Created with indexes
- ✅ Portfolio table: Added ComplimentCount column

**Downtime:** ~5 minutes (during revision switch)

**Rollback:** Not needed (migration successful)

---

## 🔍 Lessons Learned

1. **Always test migrations locally** before deploying to Azure
2. **IDENTITY_INSERT** required when seeding with explicit Ids
3. **Container App logs** essential for debugging deployment failures
4. **Revision management** helps quick rollback if needed

---

## 📚 Related Files

- ✅ `src/Services/Portfolio/Portfolio.Infrastructure/Migrations/20260305151547_InitialDataJson.cs` - Fixed
- ✅ `src/Services/Portfolio/Portfolio.Infrastructure/Migrations/20260317083228_AddComplimentFeature.cs` - Applied
- ✅ `src/Services/Portfolio/Dockerfile` - Builds successfully

---

## ✅ Status Summary

| Item | Status | Details |
|------|--------|---------|
| **Bug identified** | ✅ Done | IDENTITY_INSERT missing |
| **Code fixed** | ✅ Done | SET IDENTITY_INSERT ON/OFF added |
| **Docker build** | ✅ Done | Image built successfully |
| **ACR push** | ✅ Done | Image pushed to registry |
| **Deployment** | ✅ Done | New revision running |
| **Migration** | ✅ Done | All migrations applied |
| **Service health** | ✅ Done | Application started |
| **Swagger** | ✅ Done | HTTP 200 OK |

**Overall:** 🎉 **FULLY RESOLVED**
