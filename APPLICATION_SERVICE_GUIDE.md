# Application Service Guide

**Base URL:** `http://localhost:5013` (dev) | `http://application-service:8080` (Docker)  
**Swagger UI:** `http://localhost:5013/swagger`

---

## Tổng quan

Application Service quản lý việc ứng viên (Employee) nộp đơn ứng tuyển vào các vị trí tuyển dụng (Company Post). Service này:

- Cho phép ứng viên tạo đơn ứng tuyển với portfolio của mình
- Cho phép công ty xem danh sách đơn ứng tuyển và cập nhật trạng thái
- Hỗ trợ phân trang (OFFSET-based pagination)
- Xác thực quyền sở hữu (ownership verification)

```
Employee                          Company
    │                                │
    │  POST /api/applications        │
    │  (submit application)          │
    ▼                                │
┌──────────────────────────────────────────┐
│          Application Service             │
│  ┌─────────────────────────────────────┐ │
│  │  Application Table                  │ │
│  │  - EmployeeId (owner)               │ │
│  │  - CompanyId (denormalized)         │ │
│  │  - CompanyPostId                    │ │
│  │  - PortfolioId                      │ │
│  │  - Status (WAITING→ACCEPTED/REJECTED)│
│  └─────────────────────────────────────┘ │
└──────────────────────────────────────────┘
    │                                │
    │  GET /api/applications/me      │  GET /api/applications/company
    │  (my applications)             │  (applications for my company)
    ▼                                ▼
```

---

## Xác thực (Authentication)

Application Service dùng **JWT Bearer Token** từ Auth Service.

**Header:**
```
Authorization: Bearer <access_token>
```

**JWT Claims được sử dụng:**
- `employeeId` — ID của ứng viên (role: employee)
- `companyId` — ID của công ty (role: company)
- `role` — "employee" hoặc "company"

---

## Application Status

| Code | Value | Mô tả |
|------|-------|-------|
| `WAITING` | 0 | Đơn mới, chờ xử lý |
| `REVIEWING` | 1 | Đang xem xét |
| `ACCEPTED` | 2 | Đã chấp nhận |
| `REJECTED` | 3 | Đã từ chối |

**Status Transition Rules:**
- `WAITING` → `REVIEWING` / `ACCEPTED` / `REJECTED` ✅
- `REVIEWING` → `ACCEPTED` / `REJECTED` ✅
- `ACCEPTED` / `REJECTED` → any ❌ (không thể thay đổi sau khi quyết định)
- any → `WAITING` ❌ (không thể quay về WAITING)

---

## REST API Endpoints

| Method | URL | Auth | Mô tả |
|--------|-----|------|-------|
| `POST` | `/api/applications` | 🔒 Employee | Tạo đơn ứng tuyển |
| `GET` | `/api/applications/me` | 🔒 Employee | Xem đơn của mình (phân trang) |
| `GET` | `/api/applications/company` | 🔒 Company | Xem đơn cho công ty (phân trang) |
| `GET` | `/api/applications/{id}` | 🔒 Employee/Company | Xem chi tiết đơn (ownership check) |
| `PUT` | `/api/applications/{id}/status` | 🔒 Company | Cập nhật trạng thái đơn |

---

## 1. POST /api/applications — Tạo đơn ứng tuyển

🔒 **Yêu cầu:** Role `employee`

```http
POST /api/applications
Authorization: Bearer <token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "companyPostId": 15,
  "portfolioId": 8
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|----------|-------|
| `companyPostId` | int | ✅ | ID của bài đăng tuyển dụng |
| `portfolioId` | int | ✅ | ID của portfolio muốn gửi |

**⚠️ Security:** `employeeId` được lấy từ JWT token, KHÔNG từ request body.

**Validation:**
- Employee phải tồn tại (gọi UserProfile Service)
- CompanyPost phải tồn tại (gọi UserProfile Service)
- Portfolio phải thuộc về employee hiện tại
- Không được ứng tuyển 2 lần vào cùng 1 vị trí (UNIQUE constraint)

**Response `201 Created`:**
```json
{
  "applicationId": 42,
  "status": "WAITING",
  "appliedAt": "03/2026",
  "post": {
    "postId": 15,
    "position": "Frontend Developer",
    "salary": "15-25 triệu",
    "address": "Quận 1, TP.HCM",
    "media": "https://cdn.example.com/posts/15.jpg"
  },
  "company": {
    "companyId": 5,
    "companyName": "TechCorp Vietnam",
    "logo": "https://cdn.example.com/logos/techcorp.png"
  }
}
```

**Error Responses:**

| Status | Mô tả |
|--------|-------|
| `404` | Employee/CompanyPost không tồn tại |
| `403` | Portfolio không thuộc về employee |
| `409` | Đã ứng tuyển vị trí này rồi |

---

## 2. GET /api/applications/me — Xem đơn của tôi

🔒 **Yêu cầu:** Role `employee`

```http
GET /api/applications/me?page=1&pageSize=10
Authorization: Bearer <token>
```

**Query Parameters:**

| Tên | Kiểu | Mặc định | Mô tả |
|-----|------|----------|-------|
| `page` | int | 1 | Trang (>= 1) |
| `pageSize` | int | 10 | Số item/trang (1-50) |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "applicationId": 42,
      "status": "WAITING",
      "appliedAt": "03/2026",
      "post": {
        "postId": 15,
        "position": "Frontend Developer",
        "salary": "15-25 triệu",
        "address": "Quận 1, TP.HCM",
        "media": "https://cdn.example.com/posts/15.jpg"
      },
      "company": {
        "companyId": 5,
        "companyName": "TechCorp Vietnam",
        "logo": "https://cdn.example.com/logos/techcorp.png"
      }
    }
  ],
  "total": 25,
  "page": 1,
  "pageSize": 10
}
```

**Error Responses:**

| Status | Mô tả |
|--------|-------|
| `400` | page < 1 hoặc pageSize không hợp lệ |

---

## 3. GET /api/applications/company — Xem đơn cho công ty

🔒 **Yêu cầu:** Role `company`

```http
GET /api/applications/company?page=1&pageSize=20
Authorization: Bearer <token>
```

**Query Parameters:**

| Tên | Kiểu | Mặc định | Mô tả |
|-----|------|----------|-------|
| `page` | int | 1 | Trang (>= 1) |
| `pageSize` | int | 10 | Số item/trang (1-50) |

**⚠️ Security:** `companyId` được lấy từ JWT token, KHÔNG từ URL path.

**Response `200 OK`:**
```json
{
  "items": [
    {
      "applicationId": 42,
      "status": "NEW",
      "appliedAt": "2026-03-15T10:30:00Z",
      "portfolioId": 8,
      "roomId": null,
      "candidate": {
        "userId": 123,
        "name": "Nguyễn Văn A",
        "avatar": "https://cdn.example.com/avatars/123.jpg"
      },
      "post": {
        "postId": 15,
        "position": "Frontend Developer",
        "salary": "15-25 triệu",
        "address": "Quận 1, TP.HCM",
        "media": "https://cdn.example.com/posts/15.jpg"
      }
    }
  ],
  "total": 50,
  "page": 1,
  "pageSize": 20
}
```

**Note:** Status `WAITING` được hiển thị là `"NEW"` cho company view.

---

## 4. GET /api/applications/{id} — Xem chi tiết đơn

🔒 **Yêu cầu:** Role `employee` hoặc `company`

```http
GET /api/applications/42
Authorization: Bearer <token>
```

**Ownership Verification:**
- Nếu role = `employee`: chỉ xem được đơn của chính mình (`application.EmployeeId == currentEmployeeId`)
- Nếu role = `company`: chỉ xem được đơn gửi cho công ty mình (`application.CompanyId == currentCompanyId`)

**Response `200 OK`:**
```json
{
  "applicationId": 42,
  "status": "REVIEWING",
  "appliedAt": "03/2026",
  "post": {
    "postId": 15,
    "position": "Frontend Developer",
    "salary": "15-25 triệu",
    "address": "Quận 1, TP.HCM",
    "media": "https://cdn.example.com/posts/15.jpg"
  },
  "company": {
    "companyId": 5,
    "companyName": "TechCorp Vietnam",
    "logo": "https://cdn.example.com/logos/techcorp.png"
  }
}
```

**Error Responses:**

| Status | Mô tả |
|--------|-------|
| `404` | Application không tồn tại |
| `403` | Không có quyền xem đơn này |

---

## 5. PUT /api/applications/{id}/status — Cập nhật trạng thái

🔒 **Yêu cầu:** Role `company`

```http
PUT /api/applications/42/status
Authorization: Bearer <token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "status": 2
}
```

| Status Value | Enum |
|--------------|------|
| 0 | WAITING |
| 1 | REVIEWING |
| 2 | ACCEPTED |
| 3 | REJECTED |

**Validation:**
- Công ty phải sở hữu đơn này (`application.CompanyId == currentCompanyId`)
- Không thể đổi trạng thái của đơn đã ACCEPTED/REJECTED
- Không thể đặt lại thành WAITING

**Response `200 OK`:**
```json
{
  "applicationId": 42,
  "status": "ACCEPTED",
  "appliedAt": "03/2026",
  "post": { ... },
  "company": { ... }
}
```

**Error Responses:**

| Status | Mô tả |
|--------|-------|
| `404` | Application không tồn tại |
| `403` | Không có quyền cập nhật đơn này |
| `400` | Status transition không hợp lệ |

---

## Database Schema

```sql
CREATE TABLE Applications (
    ApplicationId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    CompanyId INT NOT NULL,           -- Denormalized from CompanyPost
    CompanyPostId INT NOT NULL,
    PortfolioId INT NOT NULL,
    RoomId INT NULL,                  -- For interview room (future)
    Status INT NOT NULL DEFAULT 0,    -- ApplicationStatus enum
    AppliedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL
);

-- Indexes
CREATE UNIQUE INDEX UX_Application_Employee_Post 
    ON Applications(EmployeeId, CompanyPostId);

CREATE INDEX IX_Application_EmployeeId ON Applications(EmployeeId);
CREATE INDEX IX_Application_CompanyId ON Applications(CompanyId);
CREATE INDEX IX_Application_Status ON Applications(Status);
```

**Design Notes:**
- `CompanyId` được denormalized để tránh join với external service khi query by company
- `EmployeeId` + `CompanyPostId` có UNIQUE constraint để ngăn duplicate applications
- Không có FK constraint vì EmployeeId, CompanyPostId là từ external services

---

## Integration với các Service khác

### Khi tạo đơn ứng tuyển (POST /api/applications)

Application Service gọi **UserProfile Service** để validate:

```
1. GET /api/employees/{employeeId} → Kiểm tra employee tồn tại
2. GET /api/company-posts/{companyPostId} → Lấy thông tin post + companyId
3. GET /api/portfolios/{portfolioId}/validate?employeeId={employeeId} → Kiểm tra ownership
```

### Khi lấy danh sách (GET /me, GET /company)

Application Service batch fetch từ **UserProfile Service**:

```
1. Get paged applications từ DB
2. Collect unique IDs: employeeIds, companyIds, postIds
3. Batch call: GET /api/employees?ids=1,2,3
4. Batch call: GET /api/companies?ids=5,6
5. Batch call: GET /api/company-posts?ids=10,11,12
6. Map và trả về kết quả
```

**⚠️ Performance:** Pagination xảy ra ở DB level (OFFSET + FETCH), sau đó mới batch fetch để enrich data.

---

## Tích hợp với Notification Service

Khi status của application thay đổi, có thể publish event để Notification Service gửi thông báo cho ứng viên:

**RabbitMQ Event (Optional - chưa implement):**

```json
// Exchange: skillsnap.events
// Routing key: application.status_changed

{
  "eventType": "application.status_changed",
  "applicationId": 42,
  "employeeId": 123,
  "companyId": 5,
  "oldStatus": "REVIEWING",
  "newStatus": "ACCEPTED",
  "timestamp": "2026-03-15T14:30:00Z"
}
```

**Notification Service** sẽ:
1. Nhận event
2. Tạo notification cho employee
3. Push realtime qua SignalR

---

## Ví dụ Flow hoàn chỉnh

### 1. Ứng viên nộp đơn

```bash
# Employee đăng nhập, lấy token
POST /api/auth/login
{
  "email": "candidate@example.com",
  "password": "password123"
}
# Response: { "accessToken": "eyJ..." }

# Nộp đơn ứng tuyển
POST /api/applications
Authorization: Bearer eyJ...
{
  "companyPostId": 15,
  "portfolioId": 8
}
# Response: { "applicationId": 42, "status": "WAITING", ... }
```

### 2. Công ty xem và xử lý đơn

```bash
# Company đăng nhập
POST /api/auth/login
{
  "email": "hr@techcorp.com",
  "password": "password123"
}

# Xem danh sách đơn
GET /api/applications/company?page=1&pageSize=20
Authorization: Bearer eyJ...
# Response: { "items": [...], "total": 50, ... }

# Chuyển sang trạng thái đang xem xét
PUT /api/applications/42/status
Authorization: Bearer eyJ...
{
  "status": 1
}
# Response: { "status": "REVIEWING", ... }

# Chấp nhận ứng viên
PUT /api/applications/42/status
Authorization: Bearer eyJ...
{
  "status": 2
}
# Response: { "status": "ACCEPTED", ... }
```

### 3. Ứng viên kiểm tra kết quả

```bash
GET /api/applications/me
Authorization: Bearer eyJ...
# Response: { "items": [{ "status": "ACCEPTED", ... }], ... }
```

---

## Error Handling

| HTTP Status | Mô tả |
|-------------|-------|
| `400 Bad Request` | Invalid input (page < 1, invalid status transition) |
| `401 Unauthorized` | Missing/invalid JWT token |
| `403 Forbidden` | Không có quyền (ownership check failed) |
| `404 Not Found` | Resource không tồn tại |
| `409 Conflict` | Duplicate application (đã ứng tuyển rồi) |
| `500 Internal Server Error` | Lỗi server |

---

## Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ApplicationDb;..."
  },
  "Jwt": {
    "Authority": "http://localhost:5001",
    "Audience": "skillsnap-api"
  },
  "Services": {
    "UserProfileService": "http://localhost:5002"
  }
}
```

---

## Docker

```yaml
# docker-compose.yml
application-service:
  build:
    context: .
    dockerfile: src/Services/Application/Dockerfile
  ports:
    - "5013:8080"
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ApplicationServiceDb;...
    - JwtSettings__SecretKey=your-256-bit-secret-key-here-make-it-long
    - JwtSettings__Issuer=SkillSnapAuth
    - JwtSettings__Audience=SkillSnapUsers
    - ServiceUrls__UserProfileService=http://userprofile-service:8080
  depends_on:
    sqlserver:
      condition: service_healthy
    userprofile-service:
      condition: service_started
```
