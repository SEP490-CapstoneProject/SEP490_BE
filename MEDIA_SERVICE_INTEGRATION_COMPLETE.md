# ✅ Media Service Integration - Complete!

## 🎉 Summary

Successfully created a **standalone Media Upload Service** and integrated it with the Community Service!

---

## 📊 Architecture

```
┌─────────────────────────────────────────────────────┐
│                   API Gateway                        │
│              https://localhost:7000                  │
└──────────────┬──────────────────────┬────────────────┘
               │                      │
               ▼                      ▼
    ┌──────────────────┐   ┌──────────────────┐
    │ Community Service│   │  Media Service   │
    │  Port: 5007      │──▶│  Port: 5010      │
    └──────────────────┘   └────────┬─────────┘
                                    │
                                    ▼
                            ┌───────────────┐
                            │  Cloudinary   │
                            └───────────────┘
```

---

## 🚀 Services Running

### Media Service
- **Port:** 5010
- **Container:** `media-service`
- **Swagger:** http://localhost:5010/swagger
- **Gateway:** https://localhost:7000/api/upload/*
- **Status:** ✅ Running

### Community Service
- **Port:** 5007
- **Container:** `community-service`
- **Swagger:** http://localhost:5007/swagger
- **Gateway:** https://localhost:7000/api/community/*
- **Status:** ✅ Running (now calls Media Service)

---

## 📍 Media Service Endpoints

### Upload Image
```http
POST /api/upload/image
Content-Type: multipart/form-data

Parameters:
- file: Image file (JPEG, PNG, GIF, WebP - max 10MB)
- folder: Optional (default: "uploads/images")
```

### Upload Video
```http
POST /api/upload/video
Content-Type: multipart/form-data

Parameters:
- file: Video file (MP4, MPEG, MOV, AVI, WebM - max 100MB)
- folder: Optional (default: "uploads/videos")
```

### Delete Media
```http
DELETE /api/upload/{publicId}
```

---

## 🧪 Testing the Integration

### 1. Test Media Service Directly
```powershell
# Upload an image
curl -X POST http://localhost:5010/api/upload/image `
  -F "file=@C:\path\to\image.jpg" `
  -F "folder=test"
```

**Expected Response:**
```json
{
  "success": true,
  "url": "https://res.cloudinary.com/daxvhwsax/image/upload/...",
  "publicId": "test/filename",
  "message": "Image uploaded successfully"
}
```

### 2. Test Community Service Integration
```powershell
# Create post with image (Community Service calls Media Service)
curl -X POST http://localhost:5007/api/community/posts `
  -F "userId=1" `
  -F "description=My first post!" `
  -F "status=1" `
  -F "coverMedia=@C:\path\to\image.jpg"
```

**What happens:**
1. Community Service receives request
2. Uploads `coverMedia` to Media Service
3. Media Service uploads to Cloudinary
4. Returns Cloudinary URL
5. Community Service saves post with URL

### 3. Verify in Database
```powershell
# Check the post
curl http://localhost:5007/api/community/posts/1
```

**Expected:**
```json
{
  "id": 1,
  "userId": 1,
  "description": "My first post!",
  "coverImageVideo": "https://res.cloudinary.com/daxvhwsax/...",
  "status": 1
}
```

---

## 💡 Using Media Service from Other Services

### Example: Portfolio Service

```csharp
// In PortfolioController
public class PortfolioController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject(
        IFormFile screenshot, 
        string title)
    {
        // Upload to Media Service
        var client = _httpClientFactory.CreateClient("MediaService");
        var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(screenshot.OpenReadStream()), 
            "file", screenshot.FileName);
        formData.Add(new StringContent("portfolio/projects"), "folder");

        var response = await client.PostAsync("/api/upload/image", formData);
        var result = await response.Content
            .ReadFromJsonAsync<MediaUploadResponse>();

        // Save project with URL
        var project = new Project
        {
            Title = title,
            Screenshot = result.Url
        };
        
        await _service.CreateAsync(project);
        return Ok(project);
    }
}
```

### Setup HttpClient in Program.cs
```csharp
builder.Services.AddHttpClient("MediaService", client =>
{
    client.BaseAddress = new Uri("http://media-service:8080");
    client.Timeout = TimeSpan.FromMinutes(5);
});
```

---

## 🔐 Security

**Cloudinary Credentials:**
- Stored in Media Service only
- Environment variables in docker-compose.yml
- Not exposed to other services
- Gitignored in .env.local for local development

**Configuration:**
```yaml
# docker-compose.yml
media-service:
  environment:
    - CLOUDINARY_CLOUD_NAME=daxvhwsax
    - CLOUDINARY_API_KEY=934663792441256
    - CLOUDINARY_API_SECRET=gKYTnNC8EErJHieYhTjLjzj6L7k
```

---

## 📦 Files Structure

### Media Service
```
Media.API/
├── Controllers/
│   └── UploadController.cs       # Upload endpoints
├── Services/
│   ├── IMediaUploadService.cs
│   └── CloudinaryUploadService.cs
├── Models/
│   ├── CloudinarySettings.cs
│   └── UploadResponse.cs
├── Program.cs
└── .env.local                     # Gitignored
```

### Community Service (Updated)
```
Community.API/
├── Controllers/
│   └── CommunityController.cs     # Now calls Media Service
├── Program.cs                      # HttpClient configured
└── (Removed MediaController.cs)
```

---

## ✅ Benefits Achieved

1. **Centralized Upload Logic**
   - One service handles all uploads
   - Consistent validation and error handling

2. **Reusability**
   - Any microservice can use Media Service
   - No code duplication

3. **Scalability**
   - Media Service can scale independently
   - Handle high upload traffic

4. **Maintainability**
   - Update Cloudinary logic in one place
   - Easy to switch providers

5. **Security**
   - Credentials in one location
   - Better access control

---

## 🎯 Next Steps

### For Other Services:
1. Add HttpClient configuration
2. Call Media Service before saving entities
3. Store returned Cloudinary URLs

### Example Services to Update:
- **UserProfile Service** - Profile pictures, cover images
- **Portfolio Service** - Project screenshots, demo videos
- **Company Service** - Company logos, job post media
- **Advertisement Service** - Ad images/videos

---

## 📚 Documentation

- **Media Service Guide:** [MEDIA_SERVICE_GUIDE.md](file:///d:/Capstone/MEDIA_SERVICE_GUIDE.md)
- **Community Integration:** [COMMUNITY_MEDIA_INTEGRATION.md](file:///d:/Capstone/COMMUNITY_MEDIA_INTEGRATION.md)

---

**Status:** 🟢 Media Service fully operational and integrated with Community Service!
