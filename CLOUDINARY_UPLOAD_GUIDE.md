# Cloudinary Upload Service - Setup Guide

## ✅ What Was Created

### Files Created:
1. **`.env.local`** - Cloudinary credentials (gitignored)
2. **`CloudinarySettings.cs`** - Configuration DTO
3. **`IMediaUploadService.cs`** - Service interface
4. **`CloudinaryUploadService.cs`** - Upload implementation
5. **`MediaController.cs`** - API endpoints

### Packages Installed:
- `CloudinaryDotNet` - Cloudinary SDK
- `DotNetEnv` - Environment variable loader

---

## 🔐 Security

Your Cloudinary credentials are stored in `.env.local` which is:
- ✅ **Gitignored** - Won't be committed to Git
- ✅ **Local only** - Each developer needs their own copy
- ✅ **Environment-based** - Loaded at runtime

**Credentials:**
```
Cloud Name: daxvhwsax
API Key: 934663792441256
API Secret: gKYTnNC8EErJHieYhTjLjzj6L7k
```

---

## 📍 API Endpoints

### Upload Image
```http
POST /api/media/upload/image
Content-Type: multipart/form-data

Parameters:
- file: Image file (JPEG, PNG, GIF, WebP)
- folder: Optional folder path (default: "community/images")

Max size: 10MB
```

**Example:**
```powershell
curl -X POST http://localhost:5007/api/media/upload/image `
  -F "file=@C:\path\to\image.jpg" `
  -F "folder=community/posts"
```

### Upload Video
```http
POST /api/media/upload/video
Content-Type: multipart/form-data

Parameters:
- file: Video file (MP4, MPEG, MOV, AVI, WebM)
- folder: Optional folder path (default: "community/videos")

Max size: 100MB
```

**Example:**
```powershell
curl -X POST http://localhost:5007/api/media/upload/video `
  -F "file=@C:\path\to\video.mp4" `
  -F "folder=community/posts"
```

### Delete Media
```http
DELETE /api/media/delete/{publicId}

Parameters:
- publicId: Cloudinary public ID of the media
```

---

## 🧪 Testing in Swagger

1. Open http://localhost:5007/swagger
2. Find `MediaController`
3. Try `/api/media/upload/image`:
   - Click "Try it out"
   - Choose a file
   - Execute
   - You'll get back a Cloudinary URL

---

## 💡 Usage in Your App

### 1. Upload Image for Post
```csharp
// User uploads image
var imageUrl = await _mediaService.UploadImageAsync(file, "community/posts");

// Save to database
var post = new CommunityPost
{
    UserId = userId,
    Description = "Check this out!",
    CoverImageVideo = imageUrl, // Cloudinary URL
    Status = 1
};
```

### 2. Upload Multiple Media
```csharp
// Upload multiple images/videos
foreach (var file in files)
{
    var url = file.ContentType.StartsWith("video/")
        ? await _mediaService.UploadVideoAsync(file)
        : await _mediaService.UploadImageAsync(file);
    
    // Save to CommunityPostMedia table
    var media = new CommunityPostMedia
    {
        CommunityPostId = postId,
        Type = file.ContentType.StartsWith("video/") ? "video" : "image",
        Name = file.FileName,
        Address = url // Cloudinary URL
    };
}
```

---

## 🔧 Features

### Image Upload
- ✅ Validates file type (JPEG, PNG, GIF, WebP)
- ✅ Validates file size (max 10MB)
- ✅ Auto-optimizes quality
- ✅ Auto-converts to best format
- ✅ Returns secure HTTPS URL

### Video Upload
- ✅ Validates file type (MP4, MPEG, MOV, AVI, WebM)
- ✅ Validates file size (max 100MB)
- ✅ Auto-optimizes quality
- ✅ Returns secure HTTPS URL

### Delete
- ✅ Removes media from Cloudinary
- ✅ Frees up storage space

---

## 🚀 Next Steps

1. **Rebuild the service:**
   ```powershell
   cd src\Services\Community\Community.API
   dotnet publish -c Release -o .\publish
   docker-compose build community-service
   docker-compose up -d community-service
   ```

2. **Test upload:**
   - Open Swagger: http://localhost:5007/swagger
   - Try uploading an image
   - Copy the returned URL
   - Use it in `coverImageVideo` or `CommunityPostMedia.address`

3. **Integrate with frontend:**
   - Call `/api/media/upload/image` before creating post
   - Save returned URL to database
   - Display images using Cloudinary URLs

---

## 📦 Docker Configuration

The `.env.local` file is loaded automatically in `Program.cs`. For Docker, you can:

**Option 1:** Mount .env.local as volume (for development)
```yaml
volumes:
  - ./src/Services/Community/Community.API/.env.local:/app/.env.local
```

**Option 2:** Use environment variables in docker-compose.yml
```yaml
environment:
  - CLOUDINARY_CLOUD_NAME=daxvhwsax
  - CLOUDINARY_API_KEY=934663792441256
  - CLOUDINARY_API_SECRET=gKYTnNC8EErJHieYhTjLjzj6L7k
```

---

**Status:** ✅ Cloudinary upload service ready to use!
