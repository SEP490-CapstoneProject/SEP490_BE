# PayOS Setup Guide

Hướng dẫn chi tiết để tích hợp PayOS vào Payment Service của SkillSnap Platform.

## Mục lục

1. [Đăng ký tài khoản PayOS](#1-đăng-ký-tài-khoản-payos)
2. [Lấy API Credentials](#2-lấy-api-credentials)
3. [Cấu hình Payment Service](#3-cấu-hình-payment-service)
4. [Đăng ký Webhook](#4-đăng-ký-webhook)
5. [Test với Sandbox](#5-test-với-sandbox)
6. [Deploy Production](#6-deploy-production)
7. [Troubleshooting](#7-troubleshooting)

---

## 1. Đăng ký tài khoản PayOS

### Bước 1: Truy cập PayOS

1. Mở trình duyệt và truy cập [https://payos.vn](https://payos.vn)
2. Click **"Đăng ký"** hoặc **"Bắt đầu miễn phí"**

### Bước 2: Tạo tài khoản

1. Điền thông tin:
   - Email doanh nghiệp
   - Số điện thoại
   - Mật khẩu
2. Xác minh email
3. Đăng nhập vào Dashboard

### Bước 3: Xác minh doanh nghiệp (Production)

Để sử dụng môi trường Production, cần:
- Giấy phép kinh doanh
- CMND/CCCD người đại diện
- Thông tin tài khoản ngân hàng

> **Lưu ý:** Sandbox không cần xác minh, có thể test ngay sau khi đăng ký.

---

## 2. Lấy API Credentials

### Bước 1: Truy cập API Settings

1. Đăng nhập [PayOS Dashboard](https://my.payos.vn)
2. Vào menu **Cài đặt** → **API Keys** (hoặc **Kênh thanh toán**)

### Bước 2: Copy Credentials

Bạn sẽ thấy 3 giá trị quan trọng:

| Field | Mô tả | Ví dụ |
|-------|-------|-------|
| **Client ID** | ID định danh merchant | `12345678` |
| **API Key** | Key để gọi API | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` |
| **Checksum Key** | Key để verify webhook signature | `xyz789abc123def456ghi789jkl012mno` |

> ⚠️ **QUAN TRỌNG:** Không chia sẻ API Key và Checksum Key với bất kỳ ai!

### Bước 3: Lưu trữ an toàn

**Development:** Lưu vào `appsettings.Development.json`

**Production:** Lưu vào:
- Azure Key Vault (khuyến nghị)
- Environment Variables
- Docker Secrets

---

## 3. Cấu hình Payment Service

### Option A: appsettings.json (Development)

Mở file `src/Services/Payment/Payment.API/appsettings.json`:

```json
{
  "PayOS": {
    "ClientId": "YOUR_PAYOS_CLIENT_ID",
    "ApiKey": "YOUR_PAYOS_API_KEY",
    "ChecksumKey": "YOUR_PAYOS_CHECKSUM_KEY",
    "BaseUrl": "https://api-merchant.payos.vn",
    "ReturnUrl": "http://localhost:3000/payment/result",
    "CancelUrl": "http://localhost:3000/payment/cancel",
    "WebhookUrl": "http://localhost:5014/api/payments/webhook/payos"
  }
}
```

### Option B: Environment Variables (Production)

```bash
# Linux/macOS
export PayOS__ClientId="your_client_id"
export PayOS__ApiKey="your_api_key"
export PayOS__ChecksumKey="your_checksum_key"
export PayOS__ReturnUrl="https://yourapp.com/payment/result"
export PayOS__CancelUrl="https://yourapp.com/payment/cancel"
export PayOS__WebhookUrl="https://yourapi.com/api/payments/webhook/payos"
```

```powershell
# Windows PowerShell
$env:PayOS__ClientId = "your_client_id"
$env:PayOS__ApiKey = "your_api_key"
$env:PayOS__ChecksumKey = "your_checksum_key"
$env:PayOS__ReturnUrl = "https://yourapp.com/payment/result"
$env:PayOS__CancelUrl = "https://yourapp.com/payment/cancel"
$env:PayOS__WebhookUrl = "https://yourapi.com/api/payments/webhook/payos"
```

### Option C: Docker Compose

```yaml
services:
  payment-service:
    image: payment-service:latest
    environment:
      - PayOS__ClientId=your_client_id
      - PayOS__ApiKey=your_api_key
      - PayOS__ChecksumKey=your_checksum_key
      - PayOS__ReturnUrl=https://yourapp.com/payment/result
      - PayOS__CancelUrl=https://yourapp.com/payment/cancel
      - PayOS__WebhookUrl=https://yourapi.com/api/payments/webhook/payos
```

### Option D: Azure Key Vault (Recommended for Production)

1. Tạo Key Vault trong Azure Portal
2. Thêm secrets:
   ```
   PayOS--ClientId
   PayOS--ApiKey
   PayOS--ChecksumKey
   ```
3. Cấp quyền cho App Service với Managed Identity
4. Payment Service sẽ tự động load từ Key Vault

---

## 4. Đăng ký Webhook

### Bước 1: Expose Local Server (Development)

Để PayOS gửi webhook đến localhost, dùng ngrok:

```bash
# Cài đặt ngrok
choco install ngrok  # Windows
brew install ngrok   # macOS

# Chạy ngrok
ngrok http 5014

# Output: https://abc123.ngrok.io -> http://localhost:5014
```

### Bước 2: Đăng ký Webhook URL

1. Vào PayOS Dashboard → **Webhooks** (hoặc **Cấu hình**)
2. Click **"Thêm webhook"**
3. Nhập URL:
   - Development: `https://abc123.ngrok.io/api/payments/webhook/payos`
   - Production: `https://your-api.com/api/payments/webhook/payos`
4. Chọn events:
   - ✅ `PAYMENT_SUCCESS` - Thanh toán thành công
   - ✅ `PAYMENT_CANCELLED` - Thanh toán bị hủy
5. Save

### Bước 3: Test Webhook

PayOS Dashboard thường có nút **"Test webhook"** để gửi test payload.

---

## 5. Test với Sandbox

### Bước 1: Chạy Payment Service

```bash
cd src/Services/Payment/Payment.API
dotnet run
```

### Bước 2: Tạo Payment

```bash
# Lấy JWT token (từ Auth Service)
TOKEN="your_jwt_token"

# Tạo payment
curl -X POST http://localhost:5014/api/payments/create \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"planId": 2}'
```

Response:
```json
{
  "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "paymentUrl": "https://pay.payos.vn/web/abc123xyz",
  "orderCode": "1711529123456789",
  "expiresAt": "2024-03-27T10:30:00Z"
}
```

### Bước 3: Thanh toán Test

1. Mở `paymentUrl` trong trình duyệt
2. PayOS Sandbox sẽ hiển thị form thanh toán giả lập
3. Chọn phương thức thanh toán và hoàn tất
4. PayOS sẽ gửi webhook về server của bạn

### Bước 4: Verify Payment

```bash
# Check payment status
curl http://localhost:5014/api/payments/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer $TOKEN"
```

### Bước 5: Check Metrics

```bash
# JSON format
curl http://localhost:5014/api/metrics

# Prometheus format
curl http://localhost:5014/api/metrics/prometheus
```

---

## 6. Deploy Production

### Checklist trước khi deploy

- [ ] Đã xác minh doanh nghiệp trên PayOS
- [ ] Đã switch sang Production credentials (không phải Sandbox)
- [ ] Webhook URL là HTTPS và public accessible
- [ ] Đã test full flow với Sandbox
- [ ] Credentials được lưu trong Key Vault/Secrets

### Cấu hình URLs Production

```json
{
  "PayOS": {
    "ClientId": "PRODUCTION_CLIENT_ID",
    "ApiKey": "PRODUCTION_API_KEY",
    "ChecksumKey": "PRODUCTION_CHECKSUM_KEY",
    "BaseUrl": "https://api-merchant.payos.vn",
    "ReturnUrl": "https://skillsnap.com/payment/result",
    "CancelUrl": "https://skillsnap.com/payment/cancel",
    "WebhookUrl": "https://api.skillsnap.com/api/payments/webhook/payos"
  }
}
```

### Monitoring

1. **Logs:** Structured logging với correlation IDs
2. **Metrics:** `/api/metrics/prometheus` cho Prometheus scraping
3. **Alerts:** Cấu hình `ALERT_WEBHOOK_URL` để nhận alerts
4. **Grafana:** Import `grafana-dashboard.json` từ Payment.API folder

---

## 7. Troubleshooting

### Webhook không nhận được

**Nguyên nhân:**
- URL không public accessible
- Firewall chặn incoming requests
- HTTPS certificate không hợp lệ

**Giải pháp:**
1. Test URL với curl từ máy khác
2. Kiểm tra firewall rules
3. Dùng Let's Encrypt cho SSL certificate

### Signature validation failed

**Nguyên nhân:**
- Checksum Key sai
- Request body bị modified (middleware)

**Giải pháp:**
1. Double-check Checksum Key trong PayOS Dashboard
2. Đảm bảo middleware không modify raw body trước khi validate

```csharp
// Trong Program.cs - đọc raw body trước
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});
```

### Payment stuck in Pending

**Nguyên nhân:**
- Webhook không được gửi
- Webhook bị reject (signature fail)

**Giải pháp:**
1. Kiểm tra PayOS Dashboard → Webhook logs
2. Kiểm tra server logs cho webhook errors
3. ReconciliationService sẽ auto-check sau 5 phút

### Amount mismatch error

**Nguyên nhân:**
- Fraud attempt
- Price thay đổi giữa lúc tạo payment và webhook

**Giải pháp:**
1. Kiểm tra logs cho FRAUD ALERT
2. Payment bị reject là đúng behavior
3. Investigate source của mismatch

---

## Tài liệu tham khảo

- [PayOS API Documentation](https://payos.vn/docs)
- [PayOS Dashboard](https://my.payos.vn)
- [PAYMENT_SERVICE_GUIDE.md](./PAYMENT_SERVICE_GUIDE.md) - Chi tiết kiến trúc Payment Service

---

## Liên hệ hỗ trợ

- **PayOS Support:** support@payos.vn
- **SkillSnap Team:** dev@skillsnap.com
