# Azure Key Vault Secrets - Complete List

Danh sách đầy đủ tất cả secrets cần thêm vào Azure Key Vault cho SkillSnap Platform.

## ⚠️ Quy tắc đặt tên

**Key Vault sử dụng `--` thay vì `:`**
- Key Vault: `JwtSettings--Secret`
- Config code: `JwtSettings:Secret`

Azure SDK tự động convert `--` → `:` khi load vào Configuration.

---

## 📋 Danh sách Secrets (14 secrets)

### 1. Database & Authentication

| # | Secret Name | Mô tả | Lấy từ đâu |
|---|-------------|-------|------------|
| 1 | `ConnectionStrings--DefaultConnection` | SQL Server connection string | Azure SQL Database |
| 2 | `JwtSettings--Secret` | JWT signing key (≥32 chars) | Tự tạo |

### 2. Payment Gateways

| # | Secret Name | Mô tả | Lấy từ đâu |
|---|-------------|-------|------------|
| 3 | `VNPay--TmnCode` | VNPay merchant code | VNPay Dashboard |
| 4 | `VNPay--HashSecret` | VNPay hash secret | VNPay Dashboard |
| 5 | `MoMo--PartnerCode` | MoMo partner code | MoMo Dashboard |
| 6 | `MoMo--AccessKey` | MoMo access key | MoMo Dashboard |
| 7 | `MoMo--SecretKey` | MoMo secret key | MoMo Dashboard |
| 12 | `PayOS--ClientId` | PayOS client ID | PayOS Dashboard |
| 13 | `PayOS--ApiKey` | PayOS API key | PayOS Dashboard |
| 14 | `PayOS--ChecksumKey` | PayOS checksum key | PayOS Dashboard |

### 3. Media & Storage

| # | Secret Name | Mô tả | Lấy từ đâu |
|---|-------------|-------|------------|
| 8 | `Cloudinary--CloudName` | Cloudinary cloud name | Cloudinary Dashboard |
| 9 | `Cloudinary--ApiKey` | Cloudinary API key | Cloudinary Dashboard |
| 10 | `Cloudinary--ApiSecret` | Cloudinary API secret | Cloudinary Dashboard |

### 4. Cache & Messaging

| # | Secret Name | Mô tả | Lấy từ đâu |
|---|-------------|-------|------------|
| 11 | `Redis--ConnectionString` | Redis connection string | Azure Redis Cache |

---

## 🔐 Example Values

### ConnectionStrings--DefaultConnection
```
Server=tcp:skillsnap-sql-server.database.windows.net,1433;Initial Catalog=skillsnap-db;User ID=sqladmin;Password=YourStrong@Passw0rd123;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### JwtSettings--Secret
```
your-super-secret-jwt-key-must-be-at-least-32-characters-long-for-security
```

### Redis--ConnectionString
```
skillsnap-redis.redis.cache.windows.net:6380,password=YOUR_REDIS_KEY,ssl=True,abortConnect=False
```

---

## 📌 Mapping trong Code

Services sử dụng secrets thông qua IConfiguration:

```csharp
// Key Vault: ConnectionStrings--DefaultConnection
// Code: 
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
// Hoặc:
var connString = builder.Configuration["ConnectionStrings:DefaultConnection"];

// Key Vault: JwtSettings--Secret
// Code:
var jwtSecret = builder.Configuration["JwtSettings:Secret"];

// Key Vault: PayOS--ClientId
// Code:
var clientId = builder.Configuration["PayOS:ClientId"];
```

---

## 🛠️ Cách thêm Secret vào Key Vault

### Portal UI
1. Vào Azure Portal
2. Mở Key Vault: `skillsnap-keyvault`
3. Click **Secrets** (thanh bên trái)
4. Click **+ Generate/Import**
5. Nhập:
   - **Name:** (từ bảng trên, ví dụ `JwtSettings--Secret`)
   - **Value:** (giá trị secret)
6. Click **Create**

### Azure CLI
```bash
# Ví dụ thêm JWT Secret
az keyvault secret set \
  --vault-name skillsnap-keyvault \
  --name "JwtSettings--Secret" \
  --value "your-super-secret-jwt-key-must-be-at-least-32-characters-long"

# Thêm SQL Connection String
az keyvault secret set \
  --vault-name skillsnap-keyvault \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Server=tcp:skillsnap-sql-server.database.windows.net,1433;Initial Catalog=skillsnap-db;User ID=sqladmin;Password=YourPassword;Encrypt=True;"
```

---

## ✅ Checklist

Sau khi setup, verify trong Key Vault:

- [ ] ConnectionStrings--DefaultConnection
- [ ] JwtSettings--Secret
- [ ] VNPay--TmnCode (nếu dùng VNPay)
- [ ] VNPay--HashSecret (nếu dùng VNPay)
- [ ] MoMo--PartnerCode (nếu dùng MoMo)
- [ ] MoMo--AccessKey (nếu dùng MoMo)
- [ ] MoMo--SecretKey (nếu dùng MoMo)
- [ ] Cloudinary--CloudName
- [ ] Cloudinary--ApiKey
- [ ] Cloudinary--ApiSecret
- [ ] Redis--ConnectionString
- [ ] PayOS--ClientId
- [ ] PayOS--ApiKey
- [ ] PayOS--ChecksumKey

---

## 🔍 Troubleshooting

### Service không kết nối được Key Vault

1. **Kiểm tra Managed Identity:**
   ```bash
   az containerapp identity show --name userprofile-service --resource-group CapStone
   ```

2. **Kiểm tra Access Policy:**
   - Vào Key Vault → Access policies
   - Đảm bảo Container App có quyền **Get** và **List** secrets

3. **Kiểm tra Key Vault URL:**
   ```bash
   az containerapp show --name userprofile-service --resource-group CapStone --query "properties.template.containers[0].env" -o table
   ```

### Secret name không đúng

Nếu code báo lỗi không tìm thấy config:
1. Kiểm tra tên secret trong Key Vault (phải có `--`)
2. Kiểm tra code đang dùng tên gì (có `:`)
3. Đảm bảo Azure SDK đã load Key Vault: `builder.Configuration.AddAzureKeyVault();`

---

## 📚 Tham khảo

- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/)
- [AZURE_PORTAL_GUI_GUIDE.md](./AZURE_PORTAL_GUI_GUIDE.md) - Hướng dẫn đầy đủ
