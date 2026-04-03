# Swagger Enabled for All Environments

## Thay đổi

Đã update tất cả services để enable Swagger trong **TẤT CẢ môi trường** (Development, Staging, Production).

## Services đã update (8/11)

✅ **Auth Service** - `/swagger`
✅ **Company Service** - `/swagger`  
✅ **Portfolio Service** - `/swagger`
✅ **Community Service** - `/swagger`
✅ **Application Service** - `/swagger`
✅ **Payment Service** - `/swagger`
✅ **Media Service** - `/swagger`
✅ **Connection Service** - `/swagger`

**Đã có sẵn:**
- ✅ UserProfile Service - `/swagger`
- ✅ Notification Service - `/swagger`
- ✅ Subscription Service - `/swagger`

## Cấu hình mới

### Trước (chỉ Development):
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### Sau (tất cả môi trường):
```csharp
// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Name V1");
    c.RoutePrefix = "swagger";
});
```

## Cách sử dụng

### Local Development
```
http://localhost:5001/swagger
```

### Azure Production
```
https://auth-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger
https://userprofile-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger
https://company-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger
...
```

## Deployment

Sau khi sửa code, cần rebuild và redeploy:

```powershell
# Build tất cả services đã thay đổi
$services = @("Auth", "Company", "Portfolio", "Community", "Application", "Payment", "Media", "Connection")

foreach ($svc in $services) {
    Write-Host "Building $svc..."
    docker build -t "skillsnapregistry.azurecr.io/$($svc.ToLower())-service:latest" -f "src/Services/$svc/Dockerfile" .
    
    Write-Host "Pushing $svc..."
    docker push "skillsnapregistry.azurecr.io/$($svc.ToLower())-service:latest"
}

# Hoặc build từng service riêng:
docker build -t "skillsnapregistry.azurecr.io/auth-service:latest" -f "src/Services/Auth/Dockerfile" .
docker push "skillsnapregistry.azurecr.io/auth-service:latest"
```

## Lưu ý bảo mật

⚠️ **Swagger UI trong Production:**
- Swagger UI hiển thị tất cả API endpoints
- Có thể test trực tiếp từ browser
- Nên thêm authentication nếu cần bảo mật cao

### Optional: Thêm authentication cho Swagger

```csharp
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    c.RoutePrefix = "swagger";
    
    // Require login
    c.ConfigObject.AdditionalItems["onComplete"] = new Func<object>(() => 
    {
        return "() => { window.ui.preauthorizeApiKey('Bearer', 'your-token'); }";
    });
});
```

## Build Status

✅ All services build successfully (0 errors)

Warnings (non-critical):
- Auth: 3 nullable warnings
- Portfolio: AutoMapper vulnerability warning (update recommended)

## Next Steps

1. ✅ Code updated
2. ⏳ Build Docker images
3. ⏳ Push to Azure Container Registry
4. ⏳ Restart Container Apps

Sau khi deploy, truy cập: `https://{service-name}.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger`
