# Portfolio API — Hướng dẫn sử dụng

## Endpoint

```
POST /api/portfolio
Authorization: Bearer <token>
Content-Type: multipart/form-data
```

---

## Request Format

| Field | Type | Bắt buộc | Mô tả |
|-------|------|-----------|-------|
| `portfolioJson` | `string` | ✅ | JSON string chứa toàn bộ thông tin portfolio |
| `files` | `IFormFile[]` | ❌ | Danh sách file ảnh (avatar, ảnh project) |

### Cách upload file

- Upload file qua field `files` (chọn nhiều file cùng lúc)
- Trong JSON, tham chiếu file bằng **tên file thực tế** (VD: `avatar.jpg`)
- Field hỗ trợ: `avatarKey` (INTRO block), `imageKey` (PROJECT block)

---

## Cấu trúc portfolioJson

```json
{
  "userId": <int>,
  "name": "<tên portfolio>",
  "blocks": [
    {
      "type": "<BLOCK_TYPE_CODE>",
      "variant": "<tên variant>",
      "order": <số thứ tự>,
      "data": <object hoặc array tùy block type>
    }
  ]
}
```

---

## Block Types

| Code | IsMultiple | Mô tả | Kiểu `data` |
|------|------------|-------|-------------|
| `INTRO` | ❌ (chỉ 1) | Giới thiệu bản thân | Object |
| `SKILL` | ✅ | Kỹ năng | Array |
| `EDUCATION` | ✅ | Học vấn | Array |
| `DIPLOMA` | ✅ | Bằng cấp / chứng chỉ | Array |
| `EXPERIMENT` | ✅ | Kinh nghiệm làm việc | Array |
| `PROJECT` | ✅ | Dự án | Array |
| `AWARD` | ✅ | Giải thưởng | Array |
| `ACTIVITIES` | ✅ | Hoạt động | Array |
| `OTHERINFO` | ✅ | Thông tin khác | Array |
| `REFERENCE` | ✅ | Người tham khảo | Array |

---

## Ví dụ đầy đủ

### portfolioJson

```json
{
  "userId": 2,
  "name": "Portfolio của Phạm An Nhiên",
  "blocks": [
    {
      "type": "INTRO",
      "variant": "INTROONE",
      "order": 1,
      "data": {
        "avatarKey": "avatar.jpg",
        "name": "Phạm An Nhiên",
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
        { "name": "TypeScript" },
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
        },
        {
          "name": "React Certificate",
          "provider": "Meta",
          "date": "2022-12-01",
          "link": "https://coursera.org/verify/XYZ789"
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
        },
        {
          "jobName": "Junior Frontend Developer",
          "address": "Công ty XYZ Solutions, TP.HCM",
          "startDate": "2022-01-01",
          "endDate": "2023-06-30",
          "description": "Xây dựng UI cho các ứng dụng nội bộ"
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
          "description": "Ứng dụng ngân hàng di động với tính năng chuyển khoản, thanh toán QR",
          "role": "Frontend Developer",
          "technology": "React Native, TypeScript, Redux",
          "links": [
            {
              "type": "github",
              "link": "https://github.com/annhien/omnibank"
            },
            {
              "type": "demo",
              "link": "https://demo.omnibank.vn"
            }
          ]
        },
        {
          "imageKey": "project2.png",
          "name": "E-Commerce Dashboard",
          "description": "Dashboard quản lý bán hàng với biểu đồ thống kê real-time",
          "role": "Lead Frontend",
          "technology": "React, TailwindCSS, Chart.js",
          "links": [
            {
              "type": "github",
              "link": "https://github.com/annhien/ecom-dashboard"
            }
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
          "description": "Giải Nhất cuộc thi hackathon với dự án AI Healthcare"
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
          "description": "Tham gia chương trình Code for Kids tại TP.HCM"
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

---

## Cách gửi request qua Swagger UI

1. Mở Swagger: `http://localhost:5003/swagger`
2. Chọn `POST /api/portfolio`
3. Click **Authorize** → nhập JWT token
4. Click **Try it out**
5. Điền `portfolioJson`: dán JSON như ví dụ trên
6. Chọn `files`: click chọn các file ảnh (avatar.jpg, project1.png, project2.png)
7. Click **Execute**

---

## Cách gửi request qua Postman

1. Method: `POST`
2. URL: `http://localhost:5003/api/portfolio`
3. Header: `Authorization: Bearer <token>`
4. Body: chọn **form-data**

| Key | Type | Value |
|-----|------|-------|
| `portfolioJson` | Text | *(dán JSON ở trên)* |
| `files` | File | avatar.jpg |
| `files` | File | project1.png |
| `files` | File | project2.png |

---

## Cách gửi request qua cURL

```bash
curl -X POST http://localhost:5003/api/portfolio \
  -H "Authorization: Bearer <token>" \
  -F 'portfolioJson={
    "userId": 2,
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
  -F "files=@/path/to/avatar.jpg"
```

---

## Response

### Thành công — `201 Created`

```json
{
  "portfolioId": 15
}
```

### Lỗi validation — `400 Bad Request`

```json
{
  "error": "Block type 'INTRO' does not allow multiple blocks (IsMultiple=false)."
}
```

```json
{
  "error": "Unknown block type: UNKNOWN_TYPE"
}
```

### Lỗi server — `500 Internal Server Error`

```json
{
  "error": "Internal server error"
}
```

---

## Lưu ý quan trọng

- `userId` phải là ID của Employee tồn tại trong hệ thống
- `INTRO` block chỉ được xuất hiện **1 lần** trong danh sách blocks
- Nếu block có file, tên trong `avatarKey` / `imageKey` phải **khớp chính xác** với tên file được upload
- Nếu không có file, bỏ qua `avatarKey` / `imageKey` hoặc để `null`
- `order` xác định thứ tự hiển thị của block, nên đặt liên tiếp (1, 2, 3, ...)
- Tất cả block đều optional — có thể tạo portfolio chỉ với INTRO hoặc thậm chí không có block nào
