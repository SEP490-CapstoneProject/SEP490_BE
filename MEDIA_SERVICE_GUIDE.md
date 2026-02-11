# 🎉 Media Upload Service - Standalone Microservice

## ✅ What Was Created

A **separate, standalone Media Upload Service** that can be used by **all microservices** in your project!

### Architecture Benefits:
- ✅ **Centralized** - One service handles all uploads
- ✅ **Reusable** - Any microservice can call it
- ✅ **Scalable** - Can scale independently
- ✅ **Maintainable** - Update Cloudinary logic in one place
- ✅ **Secure** - Credentials stored in one location

---

## 📦 Service Structure

```
Media.API/
├── Controllers/
│   └── UploadController.cs      # Upload endpoints
├── Services/
│   ├── IMediaUploadService.cs   # Interface
│   └── CloudinaryUploadService.cs # Implementation
├── Models/
│   ├── CloudinarySettings.cs    # Config
│   └── UploadResponse.cs        # Response DTO
├── Program.cs                    # Startup
└── .env.local                    # Credentials (gitignored)
```

---

## 🌐 Service Details

**Port:** 5010 (HTTP)  
**Docker:** `media-service` container  
**Swagger:** http://localhost:5010/swagger  
**Gateway:** https://localhost:7000/api/upload/*

---

## 📍 API Endpoints

### 1. Upload Image
```http
POST /api/upload/image
Content-Type: multipart/form-data

Parameters:
- file: Image file (required)
- folder: Cloudinary folder (optional, default: "uploads/images")

Response:
{
  "success": true,
  "url": "https://res.cloudinary.com/...",
  "publicId": "uploads/images/abc123",
  "message": "Image uploaded successfully"
}
```

### 2. Upload Video
```http
POST /api/upload/video
Content-Type: multipart/form-data

Parameters:
- file: Video file (required)
- folder: Cloudinary folder (optional, default: "uploads/videos")

Response:
{
  "success": true,
  "url": "https://res.cloudinary.com/...",
  "publicId": "uploads/videos/xyz789",
  "message": "Video uploaded successfully"
}
```

### 3. Delete Media
```http
DELETE /api/upload/{publicId}

Response:
{
  "success": true,
  "message": "Media deleted successfully"
}
```

### 4. Health Check
```http
GET /api/upload/health

Response:
{
  "status": "healthy",
  "service": "Media Upload Service"
}
```

---

## 🧪 Testing

### Direct Access
```powershell
# Upload image
curl -X POST http://localhost:5010/api/upload/image `
  -F "file=@C:\path\to\image.jpg" `
  -F "folder=community/posts"

# Upload video
curl -X POST http://localhost:5010/api/upload/video `
  -F "file=@C:\path\to\video.mp4" `
  -F "folder=portfolio/demos"
```

### Through API Gateway
```powershell
# Upload via gateway (HTTPS)
curl -X POST https://localhost:7000/api/upload/image `
  -F "file=@C:\path\to\image.jpg" `
  -k
```

---

## 💡 Usage from Other Services

### From Community Service
```csharp
// In your controller
public class CommunityController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public async Task<IActionResult> CreatePost(IFormFile image, string description)
    {
        // Upload image to Media Service
        var formData = new MultipartFormDataContent();
        formData.Add(new StreamContent(image.OpenReadStream()), "file", image.FileName);
        formData.Add(new StringContent("community/posts"), "folder");

        var response = await _httpClient.PostAsync(
            "http://media-service:8080/api/upload/image", 
            formData);

        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();

        // Save post with Cloudinary URL
        var post = new CommunityPost
        {
            UserId = userId,
            Description = description,
            CoverImageVideo = result.Url, // Cloudinary URL
            Status = 1
        };
        
        await _service.CreatePostAsync(post);
        return Ok(post);
    }
}
```

### From Portfolio Service
```csharp
// Upload project screenshot
var response = await _httpClient.PostAsync(
    "http://media-service:8080/api/upload/image",
    formData);

var result = await response.Content.ReadFromJsonAsync<UploadResponse>();

// Save to database
project.Image = result.Url;
```

---

## 🚀 Deployment

### 1. Build the Service
```powershell
cd src/Services/Media/Media.API/Media.API
dotnet publish -c Release -o .\publish
```

### 2. Build Docker Image
```powershell
docker-compose build media-service
```

### 3. Start the Service
```powershell
docker-compose up -d media-service
```

### 4. Verify
```powershell
# Check container
docker ps | Select-String "media"

# Check logs
docker logs media-service

# Test health
curl http://localhost:5010/api/upload/health
```

---

## 🔐 Security

**Environment Variables (Docker):**
```yaml
environment:
  - CLOUDINARY_CLOUD_NAME=daxvhwsax
  - CLOUDINARY_API_KEY=934663792441256
  - CLOUDINARY_API_SECRET=gKYTnNC8EErJHieYhTjLjzj6L7k
```

**Local Development:**
- Credentials in `.env.local` (gitignored)
- Loaded automatically by `Program.cs`

---

## 📊 Validation Rules

### Images
- **Formats:** JPEG, PNG, GIF, WebP
- **Max Size:** 10MB
- **Auto-optimization:** Yes
- **Auto-format:** Best format selected

### Videos
- **Formats:** MP4, MPEG, MOV, AVI, WebM
- **Max Size:** 100MB
- **Auto-optimization:** Yes

---

## 🔄 API Gateway Integration

The Media Service is accessible through the API Gateway:

**Routes:**
- `/api/upload/*` → Media Service
- `/api/media/*` → Media Service (alternative)

**Example:**
```
https://localhost:7000/api/upload/image
https://localhost:7000/api/upload/video
```

---

## 🎯 Use Cases

### Community Service
- Upload post cover images
- Upload post media attachments
- Upload user avatars

### Portfolio Service
- Upload project screenshots
- Upload demo videos
- Upload certificates/diplomas

### Company Service
- Upload company logos
- Upload company cover images
- Upload job post media

### User Profile Service
- Upload profile pictures
- Upload cover images

---

## 📝 Next Steps

1. **Build and deploy:**
   ```powershell
   docker-compose build media-service
   docker-compose up -d media-service
   ```

2. **Test in Swagger:**
   - Open http://localhost:5010/swagger
   - Try uploading an image
   - Copy the returned URL

3. **Integrate with services:**
   - Add HttpClient to your services
   - Call Media Service before saving entities
   - Store returned Cloudinary URLs

4. **Update Community Service:**
   - Remove local upload logic
   - Call Media Service instead
   - Update controllers to use new endpoint

---

**Status:** ✅ Standalone Media Upload Service ready for all microservices!
