# 🎓 Azure Student Deployment Guide - Giao Diện Portal
## Hướng dẫn Deploy không dùng Command Line

> **Dành cho người mới bắt đầu**: Hướng dẫn này sử dụng giao diện web Azure Portal thay vì dòng lệnh.

---

## 📋 Mục Lục
1. [Chuẩn Bị Tài Khoản Azure Student](#1-chuẩn-bị-tài-khoản-azure-student)
2. [Tạo Resource Group](#2-tạo-resource-group)
3. [Tạo SQL Database](#3-tạo-sql-database)
4. [Tạo Container Registry](#4-tạo-container-registry)
5. [Tạo Key Vault (Lưu Mật Khẩu)](#5-tạo-key-vault-lưu-mật-khẩu)
6. [Tạo Redis Cache](#6-tạo-redis-cache)
7. [Upload Docker Images](#7-upload-docker-images)
8. [Deploy Container Apps](#8-deploy-container-apps)
9. [Cấu Hình Security](#9-cấu-hình-security)
10. [Test và Monitor](#10-test-và-monitor)

---

## 1. Chuẩn Bị Tài Khoản Azure Student

### 1.1. Đăng Ký Azure Student (Miễn Phí $100)

1. Mở trình duyệt, truy cập: **https://azure.microsoft.com/free/students/**
2. Click nút **"Activate now"** (màu xanh)
3. Đăng nhập bằng **email trường học** (.edu hoặc email sinh viên)
4. Xác thực danh tính:
   - Upload **Thẻ sinh viên** có ảnh
   - Hoặc email xác nhận từ nhà trường
5. **Không cần thẻ tín dụng!**
6. Chờ 5-10 phút để kích hoạt

### 1.2. Kiểm Tra Tài Khoản

1. Vào **https://portal.azure.com**
2. Đăng nhập bằng tài khoản vừa tạo
3. Góc trên bên phải → Click vào **tên bạn** → **Switch directory**
4. Chọn directory có chữ **"Azure for Students"**
5. Kiểm tra credit:
   - Click **"Cost Management + Billing"** (thanh bên trái)
   - Xem **"Credits remaining"** → Phải có **$100**

✅ **Hoàn thành!** Bạn đã có tài khoản Azure Student với $100 credit.

---

## 2. Tạo Resource Group

> **Resource Group** = Thư mục chứa tất cả tài nguyên của project

### 2.1. Tạo Mới

1. Vào **https://portal.azure.com**
2. Thanh tìm kiếm (trên cùng) → Gõ **"Resource groups"** → Enter
3. Click nút **"+ Create"** (góc trên bên trái)
4. Điền thông tin:
   ```
   Subscription:      Azure for Students
   Resource group:    skillsnap-rg
   Region:            Southeast Asia  (gần Việt Nam nhất)
   ```
5. Click **"Review + create"** → **"Create"**
6. Đợi 5 giây → Thấy thông báo **"Deployment complete"**

✅ **Hoàn thành!** Resource group đã sẵn sàng.

---

## 3. Tạo SQL Database

> **SQL Database** = Cơ sở dữ liệu lưu trữ thông tin người dùng, bài viết, thanh toán...

### 3.1. Tạo SQL Server

1. Thanh tìm kiếm → Gõ **"SQL servers"** → Enter
2. Click **"+ Create"**
3. Điền thông tin:
   ```
   Subscription:         Azure for Students
   Resource group:       skillsnap-rg  (chọn từ dropdown)
   Server name:          skillsnap-sql-server  (phải unique, có thể thêm số)
   Location:             Southeast Asia
   Authentication:       Use SQL authentication
   Server admin login:   sqladmin
   Password:             YourStrong@Passw0rd123  (LƯU LẠI MẬT KHẨU NÀY!)
   Confirm password:     YourStrong@Passw0rd123
   ```
4. Click **"Next: Networking"**
5. Firewall rules:
   - ✅ Tích **"Allow Azure services and resources to access this server"**
   - ✅ Tích **"Add current client IP address"** (để bạn kết nối từ máy tính)
6. Click **"Review + create"** → **"Create"**
7. Đợi 2-3 phút

### 3.2. Tạo Database

1. Sau khi SQL Server tạo xong, click **"Go to resource"**
2. Thanh bên trái → Click **"SQL databases"**
3. Click **"+ Create database"**
4. Điền thông tin:
   ```
   Database name:        skillsnap-db
   Server:               skillsnap-sql-server  (đã chọn sẵn)
   Want to use elastic pool?:  No
   Compute + storage:    Click "Configure database"
   ```
5. Trong "Configure":
   ```
   Service tier:         Basic  (RẺ NHẤT - $5/tháng)
   Data max size:        2 GB
   ```
   Click **"Apply"**
6. Click **"Review + create"** → **"Create"**
7. Đợi 2-3 phút

### 3.3. Lấy Connection String

1. Vào database vừa tạo → Click **"Connection strings"** (thanh bên trái)
2. Copy đoạn text trong ô **"ADO.NET (SQL authentication)"**
3. Nó sẽ giống thế này:
   ```
   Server=tcp:skillsnap-sql-server.database.windows.net,1433;Initial Catalog=skillsnap-db;Persist Security Info=False;User ID=sqladmin;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
4. **LƯU LẠI** connection string này (cần dùng sau)
5. Thay `{your_password}` bằng mật khẩu thật: `YourStrong@Passw0rd123`

✅ **Hoàn thành!** Database đã sẵn sàng.

---

## 4. Tạo Container Registry

> **Container Registry** = Kho lưu trữ Docker images (giống như GitHub nhưng cho Docker)

### 4.1. Tạo Registry

1. Thanh tìm kiếm → Gõ **"Container registries"** → Enter
2. Click **"+ Create"**
3. Điền thông tin:
   ```
   Subscription:         Azure for Students
   Resource group:       skillsnap-rg
   Registry name:        skillsnapregistry  (chỉ chữ thường, không dấu, phải unique)
   Location:             Southeast Asia
   SKU:                  Basic  (RẺ NHẤT - $5/tháng)
   ```
4. Click **"Review + create"** → **"Create"**
5. Đợi 1-2 phút

### 4.2. Bật Admin User (Để Upload Images)

1. Sau khi tạo xong, click **"Go to resource"**
2. Thanh bên trái → Click **"Access keys"**
3. ✅ Tích **"Admin user"** (Enable)
4. Copy và **LƯU LẠI**:
   ```
   Login server:  skillsnapregistry.azurecr.io
   Username:      skillsnapregistry
   Password:      (password1 - copy giá trị này)
   ```

✅ **Hoàn thành!** Container Registry đã sẵn sàng.

---

## 5. Tạo Key Vault (Lưu Mật Khẩu)

> **Key Vault** = Két sắt số lưu mật khẩu, API keys, secrets an toàn

### 5.1. Tạo Key Vault

1. Thanh tìm kiếm → Gõ **"Key vaults"** → Enter
2. Click **"+ Create"**
3. Điền thông tin:
   ```
   Subscription:         Azure for Students
   Resource group:       skillsnap-rg
   Key vault name:       skillsnap-keyvault  (phải unique, có thể thêm số)
   Region:               Southeast Asia
   Pricing tier:         Standard  (Miễn phí trong giới hạn)
   ```
4. Click **"Next: Access configuration"**
5. Permission model:
   - Chọn **"Vault access policy"** (đơn giản hơn)
6. Click **"Review + create"** → **"Create"**
7. Đợi 1 phút

### 5.2. Thêm Secrets (Mật Khẩu)

1. Sau khi tạo xong, click **"Go to resource"**
2. Thanh bên trái → Click **"Secrets"**
3. Click **"+ Generate/Import"**

#### Thêm từng secret sau:

**Secret 1: SQL Connection String**
```
Name:   ConnectionStrings--DefaultConnection
Value:  Server=tcp:skillsnap-sql-server.database.windows.net,1433;Initial Catalog=skillsnap-db;User ID=sqladmin;Password=YourStrong@Passw0rd123;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```
Click **"Create"**

**Secret 2: JWT Secret**
```
Name:   JwtSettings--Secret
Value:  your-super-secret-jwt-key-must-be-at-least-32-characters-long-for-security
```
Click **"Create"**

**Secret 3: VNPay TMN Code**
```
Name:   VNPay--TmnCode
Value:  [Lấy từ VNPay Dashboard]
```
Click **"Create"**

**Secret 4: VNPay Hash Secret**
```
Name:   VNPay--HashSecret
Value:  [Lấy từ VNPay Dashboard]
```
Click **"Create"**

**Secret 5: MoMo Partner Code**
```
Name:   MoMo--PartnerCode
Value:  [Lấy từ MoMo Dashboard]
```
Click **"Create"**

**Secret 6: MoMo Access Key**
```
Name:   MoMo--AccessKey
Value:  [Lấy từ MoMo Dashboard]
```
Click **"Create"**

**Secret 7: MoMo Secret Key**
```
Name:   MoMo--SecretKey
Value:  [Lấy từ MoMo Dashboard]
```
Click **"Create"**

**Secret 8: Cloudinary Cloud Name**
```
Name:   Cloudinary--CloudName
Value:  [Lấy từ Cloudinary Dashboard]
```
Click **"Create"**

**Secret 9: Cloudinary API Key**
```
Name:   Cloudinary--ApiKey
Value:  [Lấy từ Cloudinary Dashboard]
```
Click **"Create"**

**Secret 10: Cloudinary API Secret**
```
Name:   Cloudinary--ApiSecret
Value:  [Lấy từ Cloudinary Dashboard]
```
Click **"Create"**

**Secret 11: Redis Connection String**
```
Name:   Redis--ConnectionString
Value:  [Lấy từ Azure Redis - Primary connection string]
```
Click **"Create"**

**Secret 12: PayOS Client ID**
```
Name:   PayOS--ClientId
Value:  [Lấy từ PayOS Dashboard]
```
Click **"Create"**

**Secret 13: PayOS API Key**
```
Name:   PayOS--ApiKey
Value:  [Lấy từ PayOS Dashboard]
```
Click **"Create"**

**Secret 14: PayOS Checksum Key**
```
Name:   PayOS--ChecksumKey
Value:  [Lấy từ PayOS Dashboard]
```
Click **"Create"**

✅ **Hoàn thành!** Tất cả secrets đã được lưu an toàn.

> **Lưu ý**: Tên secret dùng `--` thay vì `:` vì Azure Key Vault không cho phép dấu `:`. Azure SDK sẽ tự động convert `--` → `:` khi load vào Configuration.

---

## 6. Tạo Redis Cache

> **Redis** = Bộ nhớ đệm tốc độ cao, lưu session, cache dữ liệu

### 6.1. Tạo Redis

1. Thanh tìm kiếm → Gõ **"Azure Cache for Redis"** → Enter
2. Click **"+ Create"**
3. Điền thông tin:
   ```
   Subscription:         Azure for Students
   Resource group:       skillsnap-rg
   DNS name:             skillsnap-redis  (phải unique)
   Location:             Southeast Asia
   Cache type:           Basic C0 (250 MB)  (RẺ NHẤT - $16/tháng)
   ```
4. Click **"Review + create"** → **"Create"**
5. **Đợi 10-15 phút** (Redis mất thời gian tạo)

### 6.2. Lấy Connection String

1. Sau khi tạo xong (status = "Running"), click **"Go to resource"**
2. Thanh bên trái → Click **"Access keys"**
3. Copy **"Primary connection string (StackExchange.Redis)"**
4. Nó sẽ giống:
   ```
   skillsnap-redis.redis.cache.windows.net:6380,password=ABC123xyz...,ssl=True,abortConnect=False
   ```
5. **LƯU LẠI** connection string này

### 6.3. Thêm vào Key Vault

1. Quay lại **Key Vault** (skillsnap-keyvault)
2. Click **"Secrets"** → **"+ Generate/Import"**
3. Tạo secret mới:
   ```
   Name:   RedisConnection
   Value:  skillsnap-redis.redis.cache.windows.net:6380,password=ABC123xyz...,ssl=True,abortConnect=False
   ```
4. Click **"Create"**

✅ **Hoàn thành!** Redis đã sẵn sàng.

---

## 7. Upload Docker Images

> Bước này cần dùng **Command Line** một chút (không tránh được)

### 7.1. Cài Đặt Azure CLI (Chỉ 1 lần)

**Windows:**
1. Download: https://aka.ms/installazurecliwindows
2. Double click file .msi → Next → Next → Install
3. Restart máy tính

**Mac:**
```bash
brew install azure-cli
```

**Kiểm tra:**
```powershell
az --version
```

### 7.2. Login và Upload Images

Mở **PowerShell** hoặc **Terminal**, chạy từng lệnh sau:

```powershell
# 1. Login vào Azure
az login
# → Trình duyệt sẽ mở, đăng nhập bằng tài khoản Azure Student

# 2. Login vào Container Registry
az acr login --name skillsnapregistry

# 3. Build tất cả services
cd D:\Capstone

# Build từng service (thay đổi đường dẫn nếu cần)
$services = @("Auth", "UserProfile", "Portfolio", "Company", "Community", "Subscription", "Payment", "Notification", "Media", "Application")

foreach ($svc in $services) {
    Write-Host "Building $svc..."
    docker build -t "skillsnapregistry.azurecr.io/$($svc.ToLower())-service:latest" -f "src/Services/$svc/Dockerfile" .
    
    Write-Host "Pushing $svc..."
    docker push "skillsnapregistry.azurecr.io/$($svc.ToLower())-service:latest"
}
```

**Ước tính thời gian:** 20-30 phút (tùy tốc độ mạng)

✅ **Hoàn thành!** Tất cả images đã upload lên Azure.

---

## 8. Deploy Container Apps

> **Container Apps** = Chạy Docker containers trên Azure (auto-scale, pay-per-use)

### 8.1. Tạo Container Apps Environment

1. Thanh tìm kiếm → Gõ **"Container Apps"** → Enter
2. Click **"+ Create"**
3. Tab **"Basics"**:
   ```
   Subscription:         Azure for Students
   Resource group:       skillsnap-rg
   Container app name:   payment-service
   Region:               Southeast Asia
   
   Container Apps Environment:
   - Click "Create new"
   - Name: skillsnap-env
   - Click "Create"
   ```

### 8.2. Deploy Payment Service (Ví Dụ)

4. Tab **"Container"**:
   ```
   Use quickstart image:     ❌ Uncheck
   Name:                     payment-service
   Image source:             Azure Container Registry
   Registry:                 skillsnapregistry
   Image:                    payment-service
   Image tag:                latest
   CPU and Memory:           0.5 CPU cores, 1.0 Gi memory
   ```

5. Click **"Environment variables"**:
   - Click **"+ Add"**
   - Thêm từng biến sau:

   | Name | Value | Type |
   |------|-------|------|
   | ASPNETCORE_ENVIRONMENT | Production | Manual entry |
   | Azure__KeyVault__Url | https://skillsnap-keyvault.vault.azure.net/ | Manual entry |

6. Tab **"Ingress"**:
   ```
   Ingress:              Enabled
   Ingress traffic:      Accept traffic from anywhere
   Ingress type:         HTTP
   Target port:          8080
   ```

7. Click **"Review + create"** → **"Create"**
8. Đợi 3-5 phút

### 8.3. Cấu Hình Managed Identity

1. Sau khi tạo xong, click **"Go to resource"**
2. Thanh bên trái → Click **"Identity"**
3. Tab **"System assigned"**:
   - Status: Click **"On"** → **"Save"** → **"Yes"**
4. Copy **"Object (principal) ID"** (cần dùng sau)

### 8.4. Cho Phép App Đọc Key Vault

1. Quay lại **Key Vault** (skillsnap-keyvault)
2. Thanh bên trái → Click **"Access policies"**
3. Click **"+ Create"**
4. Tab **"Permissions"**:
   - Secret permissions: ✅ Tích **Get** và **List**
   - Click **"Next"**
5. Tab **"Principal"**:
   - Paste **Object ID** vừa copy
   - Click vào kết quả tìm được → **"Next"**
6. Tab **"Application"**: Bỏ qua → **"Next"**
7. Tab **"Review + create"**: Click **"Create"**

### 8.5. Lặp Lại Cho Các Services Khác

Lặp lại bước 8.2 → 8.4 cho các services:
- auth-service
- userprofile-service
- portfolio-service
- company-service
- community-service
- subscription-service
- notification-service
- media-service
- application-service

**Thay đổi cho từng service:**
- Container app name
- Image name
- (Giữ nguyên các phần còn lại)

✅ **Hoàn thành!** Tất cả services đã deploy.

---

## 9. Cấu Hình Security

### 9.1. Kiểm Tra Tất Cả Services Có Managed Identity

1. Vào **Container Apps** → Chọn từng service
2. Click **"Identity"** → Tab **"System assigned"**
3. Đảm bảo Status = **"On"** (màu xanh)
4. Nếu **"Off"** → Click **"On"** → **"Save"**

### 9.2. Kiểm Tra Key Vault Access Policies

1. Vào **Key Vault** (skillsnap-keyvault)
2. Click **"Access policies"**
3. Phải thấy **10 policies** (1 cho mỗi service + bạn)
4. Mỗi policy phải có:
   - Secret permissions: **Get**, **List**

### 9.3. Kiểm Tra SQL Firewall

1. Vào **SQL Server** (skillsnap-sql-server)
2. Click **"Networking"** (thanh bên trái)
3. Firewall rules phải có:
   - ✅ **"Allow Azure services and resources to access this server"** = ON
   - Rule **"AllowMyIP"** với IP của bạn

### 9.4. Test Security

1. Mở một service bất kỳ, ví dụ **payment-service**
2. Click **"Application Url"** (ở Overview)
3. Thêm `/health` vào cuối URL
   ```
   https://payment-service.xxx.azurecontainerapps.io/health
   ```
4. Nếu thấy **HTTP 200 OK** → Service đang chạy ✅
5. Nếu thấy lỗi → Xem logs (bước 10)

✅ **Hoàn thành!** Security đã được cấu hình đúng.

---

## 10. Test và Monitor

### 10.1. Xem Application URL

1. Vào **Container Apps** → Chọn service
2. Tab **"Overview"** → Copy **"Application Url"**
3. Mở trình duyệt, truy cập URL

### 10.2. Xem Logs (Nếu Có Lỗi)

1. Vào service có lỗi
2. Thanh bên trái → Click **"Log stream"**
3. Click **"Connect"** → Đợi 10 giây
4. Xem logs hiển thị realtime
5. Tìm dòng có chữ **"ERROR"** hoặc **"Exception"**

### 10.3. Kiểm Tra Chi Phí

1. Thanh tìm kiếm → Gõ **"Cost Management"**
2. Click **"Cost analysis"**
3. Xem biểu đồ chi phí theo ngày
4. **Ước tính**: $34-61/tháng (trong $100 credit)

### 10.4. Scale Service (Tăng/Giảm)

**Giảm xuống 0 (tiết kiệm tiền):**
1. Vào Container App
2. Click **"Scale"** (thanh bên trái)
3. Scale rule:
   ```
   Min replicas: 0  (giảm xuống 0 khi không dùng)
   Max replicas: 1
   ```
4. Click **"Save"**

**Tăng lên (khi có traffic cao):**
```
Min replicas: 2
Max replicas: 5
```

### 10.5. Stop Tất Cả (Tiết Kiệm $$$)

**Khi không test:**
1. Vào từng Container App
2. Click **"Stop"** (góc trên)
3. Confirm **"Yes"**

**Để start lại:**
1. Click **"Start"**
2. Đợi 1-2 phút

✅ **Hoàn thành!** Bạn đã deploy thành công!

---

## 📊 Dashboard Monitoring

### Tạo Dashboard Theo Dõi

1. Thanh tìm kiếm → Gõ **"Dashboard"**
2. Click **"+ Create"** → **"Dashboard"**
3. Click **"+ Add tile"**
4. Chọn **"Metric chart"**
5. Cấu hình:
   ```
   Scope:     payment-service
   Metric:    CPU Usage
   ```
6. Lặp lại cho:
   - Memory Usage
   - HTTP Requests
   - HTTP Response Time

---

## 💰 Chi Phí Dự Kiến

| Dịch Vụ | Cấu Hình | Chi Phí/Tháng |
|---------|----------|---------------|
| SQL Database | Basic (2GB) | $5 |
| Container Registry | Basic | $5 |
| Redis Cache | Basic C0 (250MB) | $16 |
| Container Apps | 10 services, 0.5 CPU each | $15-30 |
| Key Vault | Standard | $3 |
| **TỔNG** | | **$44-59/tháng** |

💡 **Còn dư $41-56 trong $100 credit**

---

## 🆘 Troubleshooting

### Lỗi: "Service không start được"

1. Vào service → Click **"Log stream"**
2. Xem logs, tìm dòng **ERROR**
3. Thường gặp:
   - **Connection string sai** → Kiểm tra Key Vault
   - **Không connect được SQL** → Kiểm tra firewall
   - **Image không tìm thấy** → Re-push Docker image

### Lỗi: "Can't read secrets from Key Vault"

1. Kiểm tra Managed Identity = **ON**
2. Kiểm tra Access Policy trong Key Vault
3. Kiểm tra environment variable `Azure__KeyVault__Url` đúng chưa

### Lỗi: "Payment webhook không hoạt động"

1. Lấy Application URL của **payment-service**
2. Vào VNPay/MoMo Dashboard
3. Cập nhật Webhook URL:
   ```
   https://payment-service.xxx.azurecontainerapps.io/api/payments/webhook/vnpay
   ```

### Chi Phí Cao Hơn Dự Kiến

1. Stop các service không dùng
2. Scale xuống 0 replicas
3. Xóa resources không cần thiết
4. Chuyển SQL từ Basic → Serverless (chỉ trả khi dùng)

---

## 🎉 Checklist Hoàn Thành

- [ ] Tài khoản Azure Student kích hoạt ($100 credit)
- [ ] Resource Group tạo xong
- [ ] SQL Database tạo xong + lấy connection string
- [ ] Container Registry tạo xong + lấy credentials
- [ ] Key Vault tạo xong + thêm 10 secrets
- [ ] Redis Cache tạo xong + lấy connection string
- [ ] Docker images build + push lên ACR
- [ ] 10 Container Apps deploy xong
- [ ] Managed Identity enable cho tất cả apps
- [ ] Key Vault access policies đã cấu hình
- [ ] Test services qua Application URL
- [ ] VNPay/MoMo webhook URLs cập nhật
- [ ] Dashboard monitoring tạo xong
- [ ] Chi phí trong giới hạn ($44-59/tháng)

---

## 📞 Liên Hệ Hỗ Trợ

- **Azure Support**: https://azure.microsoft.com/support/
- **Azure Documentation**: https://docs.microsoft.com/azure/
- **Azure Student FAQ**: https://azure.microsoft.com/free/students/faq/

---

**🎊 Chúc mừng!** Bạn đã deploy thành công SkillSnap lên Azure với bảo mật đầy đủ!
