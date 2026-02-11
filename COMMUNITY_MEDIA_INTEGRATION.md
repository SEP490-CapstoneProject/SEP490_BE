# ✅ Community Service Updated - Now Uses Media Service

## 🔄 Changes Made

### Removed Files:
- ❌ `MediaController.cs` - Deleted (use Media Service instead)
- ❌ `CloudinaryUploadService.cs` - Deleted
- ❌ `IMediaUploadService.cs` - Deleted
- ❌ `CloudinarySettings.cs` - Deleted
- ❌ `.env.local` - Deleted (credentials now in Media Service)

### Removed Packages:
- ❌ `CloudinaryDotNet` - No longer needed
- ❌ `DotNetEnv` - No longer needed

### Updated Files:
- ✅ `Program.cs` - Added HttpClient for Media Service
- ✅ `CommunityController.cs` - Now calls Media Service for uploads

---

## 📍 How It Works Now

### Before (Old Way):
```
Client → Community Service → Cloudinary
```

### After (New Way):
```
Client → Community Service → Media Service → Cloudinary
```

---

## 🧪 Usage Example

### Create Post with Image
```powershell
curl -X POST http://localhost:5007/api/community/posts `
  -F "userId=1" `
  -F "description=Check this out!" `
  -F "status=1" `
  -F "coverMedia=@C:\path\to\image.jpg"
```

**What happens:**
1. Community Service receives the request
2. Uploads `coverMedia` to Media Service
3. Media Service uploads to Cloudinary
4. Returns Cloudinary URL
5. Community Service saves post with URL

---

## 💡 Code Example

```csharp
// In CommunityController.CreatePost
if (coverMedia != null)
{
    // Call Media Service
    var client = _httpClientFactory.CreateClient("MediaService");
    var formData = new MultipartFormDataContent();
    formData.Add(new StreamContent(coverMedia.OpenReadStream()), "file", coverMedia.FileName);
    
    var endpoint = coverMedia.ContentType.StartsWith("video/") 
        ? "/api/upload/video" 
        : "/api/upload/image";
    
    var response = await client.PostAsync(endpoint, formData);
    var result = await response.Content.ReadFromJsonAsync<MediaUploadResponse>();
    
    mediaUrl = result?.Url; // Cloudinary URL
}

// Save post with Cloudinary URL
var post = new CommunityPost
{
    UserId = userId,
    Description = description,
    CoverImageVideo = mediaUrl ?? string.Empty,
    Status = status
};
```

---

## 🔧 Configuration

### Program.cs
```csharp
// HttpClient configured to call Media Service
builder.Services.AddHttpClient("MediaService", client =>
{
    client.BaseAddress = new Uri("http://media-service:8080");
    client.Timeout = TimeSpan.FromMinutes(5); // For large uploads
});
```

---

## ✅ Benefits

1. **Separation of Concerns** - Community Service focuses on community logic
2. **Reusability** - Media Service can be used by all services
3. **Maintainability** - Update Cloudinary logic in one place
4. **Scalability** - Media Service can scale independently
5. **Security** - Cloudinary credentials in one service only

---

## 🚀 Next Steps

1. **Rebuild Community Service:**
   ```powershell
   cd src/Services/Community/Community.API
   dotnet publish -c Release -o .\publish
   docker-compose build community-service
   docker-compose up -d community-service
   ```

2. **Test the integration:**
   ```powershell
   # Create post with image
   curl -X POST http://localhost:5007/api/community/posts `
     -F "userId=1" `
     -F "description=Test post" `
     -F "status=1" `
     -F "coverMedia=@image.jpg"
   ```

3. **Verify:**
   - Check Community Service logs
   - Check Media Service logs
   - Verify Cloudinary URL in database

---

**Status:** ✅ Community Service now uses centralized Media Service for all uploads!
