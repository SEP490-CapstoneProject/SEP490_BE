# Portfolio API — Hướng dẫn sử dụng

> Phiên bản sau refactor **DataJson**: block data được lưu dưới dạng JSON thay vì các bảng quan hệ riêng. Hỗ trợ tính năng **Compliment** để công ty đánh giá portfolio của ứng viên.

---

## Danh sách endpoint

### Portfolio

| Method | URL | Mô tả | Auth |
|--------|-----|-------|------|
| `POST` | `/api/portfolio` | Tạo portfolio đầy đủ (full import) | ✅ Employee |
| `GET` | `/api/portfolio` | Lấy danh sách portfolio (hỗ trợ lọc compliment) | ❌ |
| `GET` | `/api/portfolio/{id}` | Lấy portfolio theo ID | ❌ |
| `GET` | `/api/portfolio/employee/{employeeId}` | Lấy tất cả portfolio của employee | ❌ |
| `GET` | `/api/portfolio/me` | Lấy portfolio của mình | ✅ Employee |
| `PUT` | `/api/portfolio/{id}` | Cập nhật tên / trạng thái portfolio | ✅ Employee |
| `PUT` | `/api/portfolio/{id}/full` | Cập nhật toàn bộ portfolio (thay thế tất cả block) | ✅ Employee |
| `DELETE` | `/api/portfolio/{id}` | Xóa portfolio | ✅ Employee |
| `POST` | `/api/portfolio/{portfolioId}/blocks` | Thêm block vào portfolio | ✅ Employee |
| `PUT` | `/api/portfolio/{portfolioId}/blocks/{blockId}` | Cập nhật block | ✅ Employee |
| `DELETE` | `/api/portfolio/{portfolioId}/blocks/{blockId}` | Xóa block | ✅ Employee |
| `PUT` | `/api/portfolio/{portfolioId}/blocks/reorder` | Sắp xếp lại thứ tự block | ✅ Employee |

### Compliment (đánh giá portfolio)

| Method | URL | Mô tả | Auth |
|--------|-----|-------|------|
| `POST` | `/api/compliments` | Tạo compliment cho portfolio | ✅ Company |
| `GET` | `/api/compliments?portfolioId=` | Xem compliment của công ty cho portfolio | ✅ Company |
| `PUT` | `/api/compliments/{id}` | Cập nhật nội dung / điểm đánh giá | ✅ Company |
| `PATCH` | `/api/compliments/{id}/state` | Đổi trạng thái compliment | ✅ Company |
| `DELETE` | `/api/compliments/{id}` | Xóa mềm compliment | ✅ Company |

---

## 1. POST /api/portfolio — Tạo portfolio đầy đủ

```
POST /api/portfolio
Authorization: Bearer <token>
Content-Type: multipart/form-data
```

### Fields

| Field | Type | Bắt buộc | Mô tả |
|-------|------|-----------|-------|
| `portfolioJson` | `string` | ✅ | JSON string chứa toàn bộ cấu trúc portfolio |
| `<tên_file>` | `IFormFile` | ❌ | File ảnh, tên field phải khớp với `avatarKey`/`imageKey` trong JSON |

### Cách upload file

- Upload mỗi file với **tên field riêng** (không dùng chung field `files`)
- Trong JSON, dùng `avatarKey` / `imageKey` bằng **đúng tên file** đã upload
- Ví dụ: upload file tên `avatar.jpg` → `"avatarKey": "avatar.jpg"` trong JSON

### Cấu trúc portfolioJson

```json
{
  "employeeId": 2,
  "name": "Tên portfolio",
  "blocks": [
    {
      "type": "BLOCK_TYPE_CODE",
      "variant": "VARIANTONE",
      "order": 1,
      "data": { }
    }
  ]
}
```

---

## Block Types

| Code | IsMultiple | Kiểu `data` | Mô tả |
|------|------------|-------------|-------|
| `INTRO` | ❌ (1 lần) | Object | Giới thiệu bản thân |
| `SKILL` | ✅ | Array | Kỹ năng |
| `EDUCATION` | ✅ | Array | Học vấn |
| `DIPLOMA` | ✅ | Array | Bằng cấp / Chứng chỉ |
| `EXPERIMENT` | ✅ | Array | Kinh nghiệm làm việc |
| `PROJECT` | ✅ | Array | Dự án |
| `AWARD` | ✅ | Array | Giải thưởng |
| `ACTIVITIES` | ✅ | Array | Hoạt động ngoại khóa |
| `OTHERINFO` | ✅ | Array | Thông tin khác |
| `REFERENCE` | ✅ | Array | Người tham khảo |

---

## Ví dụ portfolioJson đầy đủ

```json
{
  "employeeId": 5,
  "name": "Portfolio update",
  "blocks": [
    {
      "type": "INTRO",
      "variant": "INTROONE",
      "order": 1,
      "data": {
        "avatarKey": "avatar.jpg",
        "name": "Trang Tan Duoc",
        "studyField": "Frontend Developer",
        "description": "2 năm kinh nghiệm phát triển web và mobile",
        "email": "annhien@example.com",
        "phone": "0123456789"
      }
    },
    {
      "type": "SKILL",
      "variant": "SKILLONE",
      "order": 2,
      "data": [
        { "name": "React" },
        { "name": "React Native" },
        { "name": "Figma" }
      ]
    },
    {
      "type": "EDUCATION",
      "variant": "EDUCATIONONE",
      "order": 3,
      "data": [
        {
          "schoolName": "Đại học Bách Khoa TP.HCM",
          "department": "Khoa học Máy tính",
          "time": "2019 - 2023",
          "description": "Tốt nghiệp loại Giỏi"
        }
      ]
    },
    {
      "type": "DIPLOMA",
      "variant": "DIPLOMAONE",
      "order": 4,
      "data": [
        {
          "name": "AWS Certified Developer",
          "provider": "Amazon Web Services",
          "date": "2023-06-15",
          "link": "https://aws.amazon.com/certification/verify/ABC123"
        }
      ]
    },
    {
      "type": "EXPERIMENT",
      "variant": "EXPERIMENTONE",
      "order": 5,
      "data": [
        {
          "jobName": "Frontend Developer",
          "address": "Công ty ABC Tech, TP.HCM",
          "startDate": "2023-07-01",
          "endDate": "2025-01-01",
          "description": "Phát triển ứng dụng web với React và TypeScript"
        }
      ]
    },
    {
      "type": "PROJECT",
      "variant": "PROJECTONE",
      "order": 6,
      "data": [
        {
          "imageKey": "project1.png",
          "name": "OmniBank Mobile App",
          "description": "Ứng dụng ngân hàng di động",
          "role": "Frontend Developer",
          "technology": "React Native, TypeScript",
          "links": [
            { "type": "github", "link": "https://github.com/annhien/omnibank" },
            { "type": "demo", "link": "https://demo.omnibank.vn" }
          ]
        },
        {
          "imageKey": "project2.png",
          "name": "E-Commerce Dashboard",
          "description": "Dashboard quản lý bán hàng real-time",
          "role": "Lead Frontend",
          "technology": "React, TailwindCSS, Chart.js",
          "links": [
            { "type": "github", "link": "https://github.com/annhien/ecom-dashboard" }
          ]
        }
      ]
    },
    {
      "type": "AWARD",
      "variant": "AWARDONE",
      "order": 7,
      "data": [
        {
          "name": "Hackathon Winner 2023",
          "organization": "Google Developer Groups Vietnam",
          "date": "2023-11-20",
          "description": "Giải Nhất với dự án AI Healthcare"
        }
      ]
    },
    {
      "type": "ACTIVITIES",
      "variant": "ACTIVITIESONE",
      "order": 8,
      "data": [
        {
          "name": "Tình nguyện viên dạy lập trình cho trẻ em",
          "date": "2022-06-01",
          "description": "Chương trình Code for Kids tại TP.HCM"
        }
      ]
    },
    {
      "type": "OTHERINFO",
      "variant": "OTHERINFOONE",
      "order": 9,
      "data": [
        { "detail": "TOEIC 850" },
        { "detail": "Driving License B2" }
      ]
    },
    {
      "type": "REFERENCE",
      "variant": "REFERENCEONE",
      "order": 10,
      "data": [
        {
          "name": "Nguyễn Văn A",
          "position": "Engineering Manager tại ABC Tech",
          "mail": "nguyenvana@abctech.com",
          "phone": "0987654321"
        }
      ]
    }
  ]
}
```

### Gửi qua Postman

1. Method: `POST` | URL: `http://localhost:5003/api/portfolio`
2. Header: `Authorization: Bearer <token>`
3. Body: **form-data**

| Key | Type | Value |
|-----|------|-------|
| `portfolioJson` | Text | *(JSON ở trên)* |
| `avatar.jpg` | File | *(chọn file avatar)* |
| `project1.png` | File | *(chọn file ảnh project 1)* |
| `project2.png` | File | *(chọn file ảnh project 2)* |

> ⚠️ Tên field của file phải **khớp chính xác** với `avatarKey`/`imageKey` trong JSON (ở đây là `avatar.jpg`, `project1.png`, `project2.png`)

### Gửi qua cURL

```bash
curl -X POST http://localhost:5003/api/portfolio \
  -H "Authorization: Bearer <token>" \
  -F 'portfolioJson={
    "employeeId": 2,
    "name": "My Portfolio",
    "blocks": [
      {
        "type": "INTRO",
        "variant": "INTROONE",
        "order": 1,
        "data": {
          "avatarKey": "avatar.jpg",
          "name": "Phạm An Nhiên",
          "studyField": "Frontend Developer",
          "email": "test@example.com",
          "phone": "0123456789"
        }
      }
    ]
  }' \
  -F "avatar.jpg=@/path/to/avatar.jpg"
```

### Gửi qua Swagger

1. Mở `http://localhost:5003/swagger`
2. Chọn `POST /api/portfolio` → **Try it out**
3. Điền `portfolioJson`
4. Upload file qua field tên **chính xác là tên file** (VD: field `avatar.jpg` → chọn file avatar.jpg)

### Response — `201 Created`

```json
{ "portfolioId": 15 }
```

---

## 2. GET /api/portfolio — Danh sách portfolio

```
GET /api/portfolio?page=1&pageSize=10&status=active
```

### Query params

| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `page` | `int` | `1` | Trang |
| `pageSize` | `int` | `10` | Số item mỗi trang (tối đa 100) |
| `status` | `string` | `null` | Lọc theo status (`active`, `inactive`) |
| `includeCompliments` | `bool` | `false` | Trả kèm danh sách compliment (yêu cầu JWT company) |
| `complimentState` | `int` | `null` | Lọc portfolio có compliment theo trạng thái (`0`=Pending, `1`=Approved, `2`=Rejected) |
| `hasCompliment` | `bool` | `null` | `true` = chỉ portfolio đã có compliment; `false` = chưa có |

> Khi có bất kỳ param compliment nào (`includeCompliments`, `complimentState`, `hasCompliment`), response sẽ dùng schema `PortfolioWithComplimentDto` (xem bên dưới). Nếu không có, response dùng schema `PortfolioDto` thông thường.

### Response — `200 OK` (không có compliment params)

```json
{
  "items": [
    {
      "portfolioId": 15,
      "employeeId": 2,
      "portfolioName": "Portfolio của Phạm An Nhiên",
      "status": "active",
      "createdAt": "2026-03-05T10:00:00Z",
      "updatedAt": null,
      "blocks": []
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 10
}
```

### Response — `200 OK` (có compliment params)

```json
{
  "items": [
    {
      "id": 15,
      "employeeId": 2,
      "name": "Portfolio của Phạm An Nhiên",
      "status": "active",
      "createdAt": "2026-03-05T10:00:00Z",
      "updatedAt": null,
      "complimentCount": 2,
      "approvedComplimentCount": 1,
      "averageScore": 4.5,
      "compliments": [
        {
          "id": 3,
          "portfolioId": 15,
          "companyId": 7,
          "content": "Ứng viên có kỹ năng frontend rất tốt",
          "score": 5,
          "state": 1,
          "createdAt": "2026-03-10T09:00:00Z",
          "updatedAt": null
        }
      ]
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 10
}
```

> `compliments` chỉ xuất hiện khi `includeCompliments=true`. Mỗi công ty chỉ thấy compliment của **chính công ty mình**. Admin thấy tất cả.

---

## 3. GET /api/portfolio/{id} — Lấy portfolio theo ID

```
GET /api/portfolio/15
```

### Response — `200 OK`

```json
{
  "portfolioId": 15,
  "employeeId": 2,
  "portfolioName": "Portfolio của Phạm An Nhiên",
  "status": "active",
  "createdAt": "2026-03-05T10:00:00Z",
  "updatedAt": null,
  "blocks": [
    {
      "id": 1,
      "type": "INTRO",
      "variant": "INTROONE",
      "order": 1,
      "data": {
        "avatar": "https://media-service/files/avatar.jpg",
        "name": "Phạm An Nhiên",
        "studyField": "Frontend Developer",
        "description": "2 năm kinh nghiệm",
        "email": "annhien@example.com",
        "phone": "0123456789"
      }
    },
    {
      "id": 2,
      "type": "SKILL",
      "variant": "SKILLONE",
      "order": 2,
      "data": [
        { "name": "React" },
        { "name": "TypeScript" }
      ]
    }
  ]
}
```

---

## 4. PUT /api/portfolio/{id} — Cập nhật portfolio

```
PUT /api/portfolio/15
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "name": "Tên mới",
  "status": "active"
}
```

---

## 5. DELETE /api/portfolio/{id} — Xóa portfolio

```
DELETE /api/portfolio/15
Authorization: Bearer <token>
```

Response: `204 No Content`

---

## 6. POST /api/portfolio/{portfolioId}/blocks — Thêm block

```
POST /api/portfolio/15/blocks
Authorization: Bearer <token>
Content-Type: multipart/form-data
```

### Fields

| Field | Type | Bắt buộc | Mô tả |
|-------|------|-----------|-------|
| `blockJson` | `string` | ✅ | JSON string chứa thông tin block |
| `<tên_file>` | `IFormFile` | ❌ | File ảnh (nếu block có ảnh) |

### Cấu trúc blockJson

```json
{
  "blockTypeCode": "BLOCK_TYPE_CODE",
  "variant": "VARIANTONE",
  "displayOrder": 1,
  "data": { }
}
```

### Ví dụ — Thêm INTRO block

```json
{
  "blockTypeCode": "INTRO",
  "variant": "INTROONE",
  "displayOrder": 1,
  "data": {
    "avatarKey": "avatar.jpg",
    "name": "Phạm An Nhiên",
    "studyField": "Frontend Developer",
    "description": "2 năm kinh nghiệm",
    "email": "test@example.com",
    "phone": "0123456789"
  }
}
```

### Ví dụ — Thêm SKILL block

```json
{
  "blockTypeCode": "SKILL",
  "variant": "SKILLONE",
  "displayOrder": 2,
  "data": [
    { "name": "React" },
    { "name": "TypeScript" },
    { "name": "Node.js" }
  ]
}
```

### Ví dụ — Thêm PROJECT block

```json
{
  "blockTypeCode": "PROJECT",
  "variant": "PROJECTONE",
  "displayOrder": 3,
  "data": [
    {
      "imageKey": "project1.png",
      "name": "OmniBank",
      "description": "Ứng dụng ngân hàng",
      "role": "Frontend Dev",
      "technology": "React Native",
      "links": [
        { "type": "github", "link": "https://github.com/..." }
      ]
    }
  ]
}
```

### Gửi qua Postman

| Key | Type | Value |
|-----|------|-------|
| `blockJson` | Text | *(JSON ở trên)* |
| `avatar.jpg` | File | *(file ảnh — nếu có)* |

### Response — `201 Created`

```json
{
  "id": 5,
  "type": "INTRO",
  "variant": "INTROONE",
  "order": 1,
  "data": {
    "avatar": "https://media-service/files/avatar.jpg",
    "name": "Phạm An Nhiên",
    "studyField": "Frontend Developer",
    "description": "2 năm kinh nghiệm",
    "email": "test@example.com",
    "phone": "0123456789"
  }
}
```

---

## 7. PUT /api/portfolio/{portfolioId}/blocks/{blockId} — Cập nhật block

Tương tự Add Block, nhưng dùng `UpdateBlockRequest`:

```json
{
  "variant": "INTROONE",
  "isVisible": true,
  "data": {
    "name": "Tên mới",
    "studyField": "Full Stack Developer",
    "email": "new@example.com",
    "phone": "0999888777"
  }
}
```

---

## 8. DELETE /api/portfolio/{portfolioId}/blocks/{blockId} — Xóa block

```
DELETE /api/portfolio/15/blocks/5
Authorization: Bearer <token>
```

Response: `204 No Content`

---

## 9. PUT /api/portfolio/{portfolioId}/blocks/reorder — Sắp xếp block

```
PUT /api/portfolio/15/blocks/reorder
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "items": [
    { "blockId": 1, "displayOrder": 1 },
    { "blockId": 3, "displayOrder": 2 },
    { "blockId": 2, "displayOrder": 3 }
  ]
}
```

Response: `204 No Content`

---

## Compliment — Đánh giá portfolio

> Chỉ tài khoản có role **`company`** mới sử dụng được các endpoint này.  
> Mỗi công ty chỉ được tạo **1 compliment** cho mỗi portfolio (UNIQUE constraint). Khi xóa và tạo lại vẫn được.

### Trạng thái compliment (`state`)

| Giá trị | Tên | Ý nghĩa |
|---------|-----|---------|
| `0` | `Pending` | Đang chờ duyệt (mặc định khi tạo) |
| `1` | `Approved` | Đã chấp thuận |
| `2` | `Rejected` | Bị từ chối |

---

## 10. POST /api/compliments — Tạo compliment

```
POST /api/compliments
Authorization: Bearer <company_token>
Content-Type: application/json
```

### Request body

```json
{
  "portfolioId": 15,
  "content": "Ứng viên có kỹ năng frontend xuất sắc, kinh nghiệm thực tế phong phú",
  "score": 5
}
```

| Field | Type | Bắt buộc | Mô tả |
|-------|------|-----------|-------|
| `portfolioId` | `int` | ✅ | ID portfolio cần đánh giá |
| `content` | `string` | ✅ | Nội dung nhận xét |
| `score` | `int?` | ❌ | Điểm đánh giá từ 1–5 (null nếu không chấm điểm) |

### Response — `201 Created`

```json
{
  "id": 3,
  "portfolioId": 15,
  "companyId": 7,
  "content": "Ứng viên có kỹ năng frontend xuất sắc, kinh nghiệm thực tế phong phú",
  "score": 5,
  "state": 0,
  "createdAt": "2026-03-17T08:00:00Z",
  "updatedAt": null
}
```

### Lỗi

| HTTP | Lỗi | Nguyên nhân |
|------|-----|-------------|
| `401` | Unauthorized | Thiếu JWT |
| `403` | Forbidden | Không có role `company` |
| `409` | `"This company has already complimented this portfolio."` | Công ty đã tạo compliment cho portfolio này |

---

## 11. GET /api/compliments?portfolioId= — Xem compliment

```
GET /api/compliments?portfolioId=15
Authorization: Bearer <company_token>
```

> Chỉ trả về compliment của **công ty đang đăng nhập** cho portfolio đó.

### Response — `200 OK`

```json
[
  {
    "id": 3,
    "portfolioId": 15,
    "companyId": 7,
    "content": "Ứng viên có kỹ năng frontend xuất sắc",
    "score": 5,
    "state": 0,
    "createdAt": "2026-03-17T08:00:00Z",
    "updatedAt": null
  }
]
```

---

## 12. PUT /api/compliments/{id} — Cập nhật nội dung / điểm

```
PUT /api/compliments/3
Authorization: Bearer <company_token>
Content-Type: application/json
```

### Request body

```json
{
  "content": "Nội dung cập nhật — ứng viên rất phù hợp với vị trí Frontend Lead",
  "score": 4
}
```

### Response — `200 OK`

```json
{
  "id": 3,
  "portfolioId": 15,
  "companyId": 7,
  "content": "Nội dung cập nhật — ứng viên rất phù hợp với vị trí Frontend Lead",
  "score": 4,
  "state": 0,
  "createdAt": "2026-03-17T08:00:00Z",
  "updatedAt": "2026-03-17T09:30:00Z"
}
```

---

## 13. PATCH /api/compliments/{id}/state — Đổi trạng thái

```
PATCH /api/compliments/3/state
Authorization: Bearer <company_token>
Content-Type: application/json
```

### Request body

```json
{
  "state": 1
}
```

> `state`: `0` = Pending, `1` = Approved, `2` = Rejected

### Response — `200 OK`

```json
{
  "id": 3,
  "portfolioId": 15,
  "companyId": 7,
  "content": "Ứng viên có kỹ năng frontend xuất sắc",
  "score": 5,
  "state": 1,
  "createdAt": "2026-03-17T08:00:00Z",
  "updatedAt": "2026-03-17T10:00:00Z"
}
```

> Khi state chuyển sang `Approved`, `approvedComplimentCount` trên portfolio sẽ được **tự động cập nhật**.

---

## 14. DELETE /api/compliments/{id} — Xóa compliment (soft delete)

```
DELETE /api/compliments/3
Authorization: Bearer <company_token>
```

Response: `204 No Content`

> Compliment không bị xóa vật lý — chỉ đánh dấu `isDeleted=true`. Sau khi xóa, công ty có thể tạo lại compliment mới cho cùng portfolio.

---

## Lỗi phổ biến

| HTTP | Lỗi | Nguyên nhân |
|------|-----|-------------|
| `400` | `"Invalid JSON: ..."` | JSON không hợp lệ |
| `400` | `"Block type 'INTRO' does not allow multiple blocks"` | INTRO xuất hiện hơn 1 lần |
| `400` | `"Unknown block type: ..."` | `type` không tồn tại trong hệ thống |
| `401` | Unauthorized | Thiếu hoặc sai JWT token |
| `403` | Forbidden | Token hợp lệ nhưng không có quyền |
| `404` | `"Portfolio X not found"` | Portfolio ID không tồn tại |
| `404` | `"Compliment X not found"` | Compliment ID không tồn tại |
| `409` | `"This company has already complimented this portfolio."` | Vi phạm UNIQUE constraint compliment |
| `500` | `"Internal server error"` | Lỗi server |

---

## Lưu ý quan trọng

- `employeeId` phải là Employee ID tồn tại trong hệ thống
- `INTRO` block chỉ được xuất hiện **1 lần** trong một portfolio
- Khi upload file ảnh, **tên field trong form-data** phải khớp với `avatarKey` / `imageKey` trong JSON
- Nếu không có file ảnh, bỏ qua `avatarKey` / `imageKey` hoặc để `null`
- `order` / `displayOrder` xác định thứ tự hiển thị, nên đặt liên tiếp (1, 2, 3, ...)
- Có thể tạo portfolio không có block nào (chỉ cần `"blocks": []`)
- Block data được lưu dạng JSON tự do — schema linh hoạt, không cần migration khi thêm field mới

### Compliment — Lưu ý

- Chỉ role **`company`** mới tạo/sửa/xóa được compliment
- Mỗi công ty chỉ có **tối đa 1 compliment đang hoạt động** cho mỗi portfolio
- `complimentCount`, `approvedComplimentCount`, `averageScore` trên portfolio được **tự động tính lại** sau mỗi thao tác tạo/sửa/xóa compliment
- Khi gọi `GET /api/portfolio` với `includeCompliments=true`, mỗi công ty chỉ thấy compliment của **chính mình** — hoàn toàn cô lập đa tenant
- `score` là tuỳ chọn, nhận giá trị `null` hoặc số nguyên 1–5
