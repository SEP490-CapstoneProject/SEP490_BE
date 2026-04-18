# FE Subscription + Payment Flow (Production)

Base URL (Gateway):

`https://api-gateway.grayforest-11aba44e.southeastasia.azurecontainerapps.io`

## 1) Login lấy JWT

`POST /api/auth/login`

```json
{
  "email": "user@example.com",
  "password": "your-password"
}
```

Dùng `data.accessToken` làm Bearer token cho các bước sau.

## 2) Lấy danh sách gói

`GET /api/plans`

FE hiển thị danh sách plan để user chọn `planId`.

## 3) Tạo subscription pending

`POST /api/subscriptions/subscribe` (Bearer)

```json
{
  "planId": 2,
  "autoRenew": true
}
```

Response thành công trả `id` của subscription (trạng thái thường là `Pending` trước khi thanh toán):

```json
{
  "id": 123,
  "userId": 4,
  "planId": 2,
  "status": "Pending",
  "paymentStatus": "Pending"
}
```

## 4) Tạo payment cho subscription vừa tạo

`POST /api/payments/create` (Bearer)

```json
{
  "planId": 2,
  "subscriptionId": 123
}
```

Response thành công:

```json
{
  "paymentId": "GUID",
  "paymentUrl": "https://payos.vn/...",
  "orderCode": "..."
}
```

FE cần redirect user sang `paymentUrl`.

## 5) Return/Cancel page ở frontend

PayOS return/cancel URL đang cấu hình về FE:

1. `https://sep-490-web-fork.vercel.app/payment/result`
2. `https://sep-490-web-fork.vercel.app/payment/cancel`

Backend sẽ tự gắn thêm query params vào return/cancel URL:

1. `paymentId`
2. `subscriptionId`
3. `orderCode`

Tại các trang này, FE nên gọi lại API để đọc trạng thái thật từ backend (không tin trạng thái chỉ từ query param redirect).

## 6) Poll/check trạng thái sau thanh toán

### 6.1 Payment status

`GET /api/payments/{paymentId}` (Bearer)

- `Pending` / `Processing`: tiếp tục polling.
- `Succeeded`: thanh toán thành công.
- `Failed` / `Cancelled` / `Expired`: hiển thị thất bại và cho phép thanh toán lại.

Fallback nếu không có `paymentId` nhưng có `orderCode`:

`GET /api/payments/by-order/{orderCode}` (Bearer)

### 6.2 Subscription active check

`GET /api/subscriptions/current` (Bearer)

- `200`: đã active.
- `404`: chưa active (webhook/event chưa xử lý xong), tiếp tục polling ngắn hạn.

### 6.3 Entitlements sau khi active

`GET /api/subscriptions/my-entitlements` (Bearer)

Dùng để cập nhật feature gate trong UI.

## 7) Retry strategy đề xuất cho FE

1. Sau khi user quay về từ PayOS, poll `GET /api/payments/{paymentId}` mỗi 2-3 giây, tối đa 60-90 giây.
2. Khi payment `Succeeded`, poll thêm `GET /api/subscriptions/current` mỗi 2-3 giây cho đến khi `200`.
3. Nếu quá timeout, hiển thị trạng thái "Đang đồng bộ thanh toán" + nút refresh thủ công.

## 8) Error handling cần có

1. `401`: token hết hạn -> login lại.
2. `400` ở create payment: hiển thị message backend trả về.
3. `409` ở subscribe (nếu có subscription active): điều hướng user về trang quản lý gói hiện tại.
4. Network timeout: giữ `subscriptionId` + `paymentId` ở local state để user resume flow.
5. Nếu trang `pay.payos.vn/.../success` bị `application error`, FE vẫn xử lý bình thường bằng polling backend ở trang return/cancel của FE.

## 9) Lưu ý tích hợp

1. Luôn gọi `subscribe` trước rồi mới `payments/create` (vì cần `subscriptionId`).
2. Không tự tính amount phía FE; backend tự lấy giá plan.
3. Không active subscription từ FE; activation chạy qua webhook + event nội bộ.
