# Azure Key Vault - Quick Setup Guide

## 📋 Tóm tắt Secrets cần tạo

### ✅ 14 Secrets Required

| # | Secret Name | Value Example | Source | Priority |
|---|-------------|---------------|--------|----------|
| 1 | `ConnectionStrings--DefaultConnection` | `Server=tcp:skillsnap-sql-server.database.windows.net,1433;Initial Catalog=skillsnap-db;User ID=sqladmin;Password=YourPassword123;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` | Azure SQL Database | 🔴 **REQUIRED** |
| 2 | `JwtSettings--Secret` | `your-super-secret-jwt-key-must-be-at-least-32-characters-long-for-security-purposes` | Tự tạo (min 32 chars) | 🔴 **REQUIRED** |
| 3 | `Redis--ConnectionString` | `skillsnap-redis.redis.cache.windows.net:6380,password=YOUR_REDIS_KEY_HERE,ssl=True,abortConnect=False` | Azure Redis Cache | 🟡 Optional* |
| 4 | `Cloudinary--CloudName` | `your-cloudinary-cloud-name` | Cloudinary Dashboard | 🟡 Optional** |
| 5 | `Cloudinary--ApiKey` | `123456789012345` | Cloudinary Dashboard | 🟡 Optional** |
| 6 | `Cloudinary--ApiSecret` | `abcdefghijklmnopqrstuvwxyz123456` | Cloudinary Dashboard | 🟡 Optional** |
| 7 | `PayOS--ClientId` | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | PayOS Dashboard | 🟡 Optional*** |
| 8 | `PayOS--ApiKey` | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | PayOS Dashboard | 🟡 Optional*** |
| 9 | `PayOS--ChecksumKey` | `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` | PayOS Dashboard | 🟡 Optional*** |
| 10 | `VNPay--TmnCode` | `YOUR_VNPAY_TMNCODE` | VNPay Portal | ⚪ Legacy |
| 11 | `VNPay--HashSecret` | `YOUR_VNPAY_HASHSECRET` | VNPay Portal | ⚪ Legacy |
| 12 | `MoMo--PartnerCode` | `YOUR_MOMO_PARTNERCODE` | MoMo Portal | ⚪ Legacy |
| 13 | `MoMo--AccessKey` | `YOUR_MOMO_ACCESSKEY` | MoMo Portal | ⚪ Legacy |
| 14 | `MoMo--SecretKey` | `YOUR_MOMO_SECRETKEY` | MoMo Portal | ⚪ Legacy |

**Priority:**
- 🔴 **REQUIRED** = Tất cả services cần
- 🟡 **Optional*** = Chỉ cần nếu deploy service tương ứng:
  - *Redis: Application, Subscription, Notification services
  - **Cloudinary: Media service
  - ***PayOS: Payment service
- ⚪ **Legacy** = VNPay/MoMo (không dùng nữa, PayOS thay thế)

---

## 🔑 Chi tiết từng Secret

### 1. ConnectionStrings--DefaultConnection

**Lấy từ:** Azure SQL Database → Connection strings

```
Server=tcp:skillsnap-sql-server.database.windows.net,1433;
Initial Catalog=skillsnap-db;
User ID=sqladmin;
Password=YOUR_SQL_PASSWORD;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

**Cách lấy:**
1. Vào Azure Portal → SQL databases → `skillsnap-db`
2. Click **Connection strings** (menu bên trái)
3. Copy **ADO.NET** connection string
4. Thay `{your_password}` bằng password thật

---

### 2. JwtSettings--Secret

**Tự tạo:** String ngẫu nhiên ≥ 32 ký tự

```bash
# Generate bằng PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})

# Hoặc tự gõ (ví dụ):
my-super-secret-jwt-signing-key-for-production-use-only-2024
```

**Lưu ý:** 
- Phải ≥ 32 characters
- Nên dùng chữ + số + ký tự đặc biệt
- KHÔNG share hoặc commit vào Git

---

### 3. Redis--ConnectionString

**Lấy từ:** Azure Redis Cache → Access keys

```
skillsnap-redis.redis.cache.windows.net:6380,
password=YOUR_PRIMARY_KEY_HERE,
ssl=True,
abortConnect=False
```

**Cách lấy:**
1. Azure Portal → Azure Cache for Redis → `skillsnap-redis`
2. Click **Access keys** (menu bên trái)
3. Copy **Primary connection string (StackExchange.Redis)**

---

### 4-6. Cloudinary Credentials

**Lấy từ:** Cloudinary Dashboard

```
CloudName: your-cloud-name
ApiKey:    123456789012345
ApiSecret: abcdefGHIJKLmnopQRST_UVW1234567
```

**Cách lấy:**
1. Login vào [Cloudinary Console](https://console.cloudinary.com/)
2. Dashboard → **Account Details** section
3. Copy:
   - Cloud name
   - API Key
   - API Secret (click "👁️ Show" để hiện)

---

### 7-9. PayOS Credentials

**Lấy từ:** PayOS Merchant Dashboard

```
ClientId:     xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
ApiKey:       xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  
ChecksumKey:  64-character-hex-string
```

**Cách lấy:**
1. Login vào [PayOS Dashboard](https://my.payos.vn/)
2. **Cài đặt** → **Thông tin tích hợp**
3. Copy:
   - Client ID
   - API Key
   - Checksum Key

**Lưu ý:** Dùng **Sandbox credentials** để test, **Production credentials** khi live

---

## 🛠️ Cách thêm vào Key Vault

### Option 1: Azure Portal (GUI)

1. **Mở Key Vault**
   - Azure Portal → Search "Key vaults"
   - Click `skillsnap-keyvault`

2. **Mỗi secret làm như sau:**
   - Menu trái → **Secrets**
   - Click **+ Generate/Import**
   - **Name:** (copy từ bảng trên, VD: `ConnectionStrings--DefaultConnection`)
   - **Value:** (paste value tương ứng)
   - Click **Create**

3. **Lặp lại** cho tất cả 14 secrets

---

### Option 2: Azure CLI (faster)

```powershell
# Set variables
$vaultName = "skillsnap-keyvault"

# 1. SQL Connection String
az keyvault secret set `
  --vault-name $vaultName `
  --name "ConnectionStrings--DefaultConnection" `
  --value "Server=tcp:skillsnap-sql-server.database.windows.net,1433;Initial Catalog=skillsnap-db;User ID=sqladmin;Password=YOUR_PASSWORD;Encrypt=True;"

# 2. JWT Secret
az keyvault secret set `
  --vault-name $vaultName `
  --name "JwtSettings--Secret" `
  --value "your-super-secret-jwt-key-must-be-at-least-32-characters-long"

# 3. Redis Connection String
az keyvault secret set `
  --vault-name $vaultName `
  --name "Redis--ConnectionString" `
  --value "skillsnap-redis.redis.cache.windows.net:6380,password=YOUR_KEY,ssl=True,abortConnect=False"

# 4-6. Cloudinary
az keyvault secret set --vault-name $vaultName --name "Cloudinary--CloudName" --value "your-cloud-name"
az keyvault secret set --vault-name $vaultName --name "Cloudinary--ApiKey" --value "123456789012345"
az keyvault secret set --vault-name $vaultName --name "Cloudinary--ApiSecret" --value "your-api-secret"

# 7-9. PayOS
az keyvault secret set --vault-name $vaultName --name "PayOS--ClientId" --value "your-client-id"
az keyvault secret set --vault-name $vaultName --name "PayOS--ApiKey" --value "your-api-key"
az keyvault secret set --vault-name $vaultName --name "PayOS--ChecksumKey" --value "your-checksum-key"

# 10-14. Legacy (optional)
az keyvault secret set --vault-name $vaultName --name "VNPay--TmnCode" --value "YOUR_TMNCODE"
az keyvault secret set --vault-name $vaultName --name "VNPay--HashSecret" --value "YOUR_HASHSECRET"
az keyvault secret set --vault-name $vaultName --name "MoMo--PartnerCode" --value "YOUR_PARTNERCODE"
az keyvault secret set --vault-name $vaultName --name "MoMo--AccessKey" --value "YOUR_ACCESSKEY"
az keyvault secret set --vault-name $vaultName --name "MoMo--SecretKey" --value "YOUR_SECRETKEY"
```

---

## ✅ Verification Checklist

Sau khi thêm secrets:

```powershell
# List tất cả secrets trong Key Vault
az keyvault secret list --vault-name skillsnap-keyvault --query "[].name" -o table
```

**Phải có ít nhất 2 secrets (minimum):**
- ✅ ConnectionStrings--DefaultConnection
- ✅ JwtSettings--Secret

**Full deployment (14 secrets):**
- ✅ ConnectionStrings--DefaultConnection
- ✅ JwtSettings--Secret
- ✅ Redis--ConnectionString
- ✅ Cloudinary--CloudName
- ✅ Cloudinary--ApiKey
- ✅ Cloudinary--ApiSecret
- ✅ PayOS--ClientId
- ✅ PayOS--ApiKey
- ✅ PayOS--ChecksumKey
- ⚪ VNPay--TmnCode (optional)
- ⚪ VNPay--HashSecret (optional)
- ⚪ MoMo--PartnerCode (optional)
- ⚪ MoMo--AccessKey (optional)
- ⚪ MoMo--SecretKey (optional)

---

## 🔍 Verify Secret Value

```powershell
# Xem value của 1 secret (để verify)
az keyvault secret show `
  --vault-name skillsnap-keyvault `
  --name "ConnectionStrings--DefaultConnection" `
  --query "value" -o tsv
```

---

## ⚙️ Services sử dụng Secret nào?

| Service | Secrets Required |
|---------|------------------|
| **Auth** | ConnectionStrings--DefaultConnection, JwtSettings--Secret |
| **UserProfile** | ConnectionStrings--DefaultConnection, JwtSettings--Secret |
| **Portfolio** | ConnectionStrings--DefaultConnection, JwtSettings--Secret |
| **Company** | ConnectionStrings--DefaultConnection, JwtSettings--Secret |
| **Community** | ConnectionStrings--DefaultConnection, JwtSettings--Secret |
| **Connection** | ConnectionStrings--DefaultConnection, JwtSettings--Secret |
| **Application** | ConnectionStrings--DefaultConnection, JwtSettings--Secret, Redis--ConnectionString |
| **Subscription** | ConnectionStrings--DefaultConnection, JwtSettings--Secret, Redis--ConnectionString |
| **Notification** | ConnectionStrings--DefaultConnection, JwtSettings--Secret, Redis--ConnectionString |
| **Payment** | ConnectionStrings--DefaultConnection, JwtSettings--Secret, PayOS--* |
| **Media** | ConnectionStrings--DefaultConnection, JwtSettings--Secret, Cloudinary--* |

---

## 🚨 Common Issues

### Issue 1: "Secret name contains invalid characters"

**Problem:** Dùng `:` thay vì `--`

❌ Wrong: `ConnectionStrings:DefaultConnection`  
✅ Correct: `ConnectionStrings--DefaultConnection`

---

### Issue 2: "Access denied to Key Vault"

**Solution:**
```powershell
# Login lại Azure CLI
az login

# Hoặc grant access cho bản thân
az keyvault set-policy `
  --name skillsnap-keyvault `
  --upn your-email@domain.com `
  --secret-permissions get list set delete
```

---

### Issue 3: Container Apps không load được secrets

**Checklist:**
1. ✅ Container App có **Managed Identity** enabled?
2. ✅ Managed Identity có **Access Policy** trong Key Vault?
3. ✅ Environment variable `Azure__KeyVault__Url` được set?
4. ✅ Secret name dùng `--` chứ không phải `:`?

---

## 📚 Reference

- Full documentation: `AZURE_KEYVAULT_SECRETS.md`
- Deployment guide: `AZURE_PORTAL_GUI_GUIDE.md`
- Verification report: `KEYVAULT_VERIFICATION_COMPLETE.md`
