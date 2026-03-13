# Community Service API Guide

**Base URL:** `http://localhost:5004` (dev) | `http://community-service:8080` (Docker)  
**Swagger UI:** `http://localhost:5004/swagger`

---

## Xác thực (Authentication)

Community Service dùng **JWT Bearer Token** lấy từ Auth Service sau khi đăng nhập.

Trên Swagger: click nút **Authorize 🔒** → nhập token (không cần prefix `Bearer`).

Với HTTP client:
```
Authorization: Bearer <access_token>
```

### Quy tắc phân quyền

| Nhóm | Yêu cầu |
|---|---|
| Xem feed, bài viết, comment | **Public** — không cần token |
| Tạo bài, comment, reply, save, favorite | **Bắt buộc đăng nhập** (mọi role) |
| Sửa bài viết | **Chủ bài viết** |
| Xóa bài / comment / reply | **Chủ sở hữu** hoặc **ADMIN / MODERATOR** |

### Error format chung
```json
{ "error": "Mô tả lỗi" }
```

---

## 1. Feed — Danh sách bài viết

### `GET /api/community/posts`

Lấy feed bài viết theo **cursor-based pagination**. Không cần đăng nhập.  
Nếu có token hợp lệ, tự động tính `isFavorited` và `isSaved` cho từng bài.

**Query Parameters:**

| Tên | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `pageSize` | int | 20 | Số bài mỗi trang |
| `cursor` | int? | — | ID bài cuối của trang trước (lấy từ `nextCursor`) |

**Request (trang đầu):**
```
GET /api/community/posts?pageSize=20
```

**Request (trang tiếp):**
```
GET /api/community/posts?pageSize=20&cursor=38
```

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": 42,
      "author": {
        "id": 7,
        "name": "Nguyễn Văn A",
        "avatar": "https://res.cloudinary.com/demo/image/upload/avatars/7.jpg",
        "role": "USER"
      },
      "description": "Chia sẻ kinh nghiệm phỏng vấn tại các công ty lớn...",
      "coverImageUrl": "https://res.cloudinary.com/demo/image/upload/community/posts/cover.jpg",
      "media": [
        "https://res.cloudinary.com/demo/image/upload/community/posts/abc123.jpg",
        "https://res.cloudinary.com/demo/video/upload/community/posts/xyz456.mp4"
      ],
      "portfolioId": 3,
      "portfolioPreview": {
        "type": "experience",
        "variant": "card",
        "data": { "title": "Software Engineer", "company": "Tech Corp" }
      },
      "favoriteCount": 15,
      "commentCount": 4,
      "isFavorited": true,
      "isSaved": false,
      "createdAt": "2026-03-10T08:00:00.0000000Z"
    },
    {
      "id": 41,
      "author": {
        "id": 12,
        "name": "Công ty ABC",
        "avatar": "https://res.cloudinary.com/demo/image/upload/companies/12.jpg",
        "role": "COMPANY"
      },
      "description": "Tuyển dụng Senior Developer...",
      "coverImageUrl": null,
      "media": [],
      "portfolioId": null,
      "portfolioPreview": null,
      "favoriteCount": 8,
      "commentCount": 2,
      "isFavorited": false,
      "isSaved": true,
      "createdAt": "2026-03-10T07:30:00.0000000Z"
    }
  ],
  "nextCursor": 41,
  "hasMore": true
}
```

> Khi `hasMore = false` thì đã hết dữ liệu, không cần gọi thêm.

**Author Role:**
- `"USER"` — người dùng thông thường (employee profile)
- `"COMPANY"` — nhà tuyển dụng (company profile)

---

## 2. Chi tiết bài viết

### `GET /api/community/posts/{id}`

Lấy đầy đủ thông tin một bài viết. Public.

**Request:**
```
GET /api/community/posts/42
```

**Response `200 OK`:**
```json
{
  "id": 42,
  "author": {
    "id": 7,
    "name": "Nguyễn Văn A",
    "avatar": "https://res.cloudinary.com/demo/image/upload/avatars/7.jpg",
    "role": "USER"
  },
  "description": "Chia sẻ kinh nghiệm phỏng vấn...",
  "coverImageUrl": "https://res.cloudinary.com/demo/image/upload/community/posts/cover.jpg",
  "media": [
    "https://res.cloudinary.com/demo/image/upload/community/posts/abc123.jpg"
  ],
  "portfolioId": 3,
  "portfolioPreview": {
    "type": "experience",
    "variant": "card",
    "data": { "title": "Software Engineer", "company": "Tech Corp" }
  },
  "favoriteCount": 15,
  "commentCount": 4,
  "isFavorited": false,
  "isSaved": false,
  "createdAt": "2026-03-10T08:00:00.0000000Z"
}
```

**Response `404 Not Found`:**
```json
{ "error": "Post 42 not found" }
```

---

### `GET /api/community/posts/user/{userId}`

Lấy tất cả bài viết của một user. Public.

**Request:**
```
GET /api/community/posts/user/7
```

**Response `200 OK`:** Mảng các entity `CommunityPost` (raw, không enriched):
```json
[
  {
    "id": 42,
    "userId": 7,
    "description": "Chia sẻ kinh nghiệm...",
    "coverImageVideo": "",
    "portfolioId": 3,
    "favoriteCount": 15,
    "status": 1,
    "createdAt": "2026-03-10T08:00:00Z",
    "updatedAt": null
  }
]
```

---

## 3. Tạo / Sửa / Xóa bài viết

### `POST /api/community/posts` 🔒

Tạo bài viết mới. Hỗ trợ upload **nhiều file** (ảnh/video) cùng lúc.

**Content-Type:** `multipart/form-data`

**Form Fields:**

| Field | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `postJson` | string (JSON) | **Có** | JSON object chứa dữ liệu bài viết |
| `files` | file[] | Không | Một hoặc nhiều file ảnh/video |

**Giá trị của `postJson`:**
```json
{
  "description": "Chia sẻ kinh nghiệm làm việc tại startup...",
  "portfolioId": 3,
  "status": 1,
  "coverImageKey": "cover.jpg"
}
```

| Field | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `description` | string | `""` | Nội dung bài viết |
| `portfolioId` | int? | `null` | ID portfolio đính kèm |
| `status` | int | `1` | `1` = public, `0` = ẩn |
| `coverImageKey` | string? | `null` | Tên file (trong `files[]`) dùng làm ảnh/video bìa |

> **`coverImageKey`**: Phải khớp chính xác tên file gửi lên trong `files[]` (so sánh không phân biệt hoa thường). File đó sẽ được lưu vào `coverImageUrl`. Các file còn lại → `media[]`.

**Ví dụ với curl:**
```bash
curl -X POST http://localhost:5004/api/community/posts \
  -H "Authorization: Bearer <token>" \
  -F 'postJson={"description":"Bài viết của tôi","status":1,"coverImageKey":"cover.jpg"}' \
  -F 'files=@cover.jpg' \
  -F 'files=@photo1.jpg' \
  -F 'files=@video1.mp4'
```

**Response `201 Created`:**
```json
{
  "id": 43,
  "userId": 7,
  "description": "Bài viết của tôi",
  "coverImageVideo": "https://res.cloudinary.com/demo/image/upload/community/posts/cover.jpg",
  "portfolioId": null,
  "favoriteCount": 0,
  "status": 1,
  "createdAt": "2026-03-10T09:00:00Z",
  "updatedAt": null
}
```

> - `coverImageVideo` trong response thô chứa URL ảnh/video bìa.  
> - Khi GET qua feed / chi tiết: field này được expose thành `coverImageUrl`.  
> - Các file không phải bìa → upload vào `CommunityPostMedia` → xuất hiện trong `media[]`.

**Responses:**

| Status | Mô tả |
|---|---|
| `201 Created` | Tạo thành công |
| `400 Bad Request` | `postJson` không hợp lệ |
| `401 Unauthorized` | Chưa đăng nhập |

---

### `PUT /api/community/posts/{id}` 🔒

Cập nhật bài viết. Chỉ **chủ bài viết** mới được sửa.

**Content-Type:** `application/json`

**Request body** (tất cả field đều optional — chỉ gửi field cần thay đổi):
```json
{
  "description": "Nội dung đã chỉnh sửa",
  "portfolioId": 5,
  "status": 0
}
```

**Responses:**

| Status | Mô tả |
|---|---|
| `204 No Content` | Cập nhật thành công |
| `400 Bad Request` | Body không hợp lệ |
| `401 Unauthorized` | Chưa đăng nhập |
| `403 Forbidden` | Không phải chủ bài |
| `404 Not Found` | Bài viết không tồn tại |

---

### `DELETE /api/community/posts/{id}` 🔒

Xóa bài viết. **Chủ bài** hoặc **ADMIN/MODERATOR**.

**Request:**
```
DELETE /api/community/posts/42
Authorization: Bearer <token>
```

**Responses:**

| Status | Mô tả |
|---|---|
| `204 No Content` | Xóa thành công |
| `401 Unauthorized` | Chưa đăng nhập |
| `403 Forbidden` | Không có quyền xóa |
| `404 Not Found` | Bài viết không tồn tại |

---

## 4. Yêu thích (Favorite)

### `POST /api/community/posts/{postId}/favorite` 🔒

Thêm bài vào danh sách yêu thích. Tự động tăng `favoriteCount`.

**Request:**
```
POST /api/community/posts/42/favorite
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
{ "message": "Post favorited" }
```

**Response `400 Bad Request`:**
```json
{ "error": "Post already favorited" }
```

---

### `DELETE /api/community/posts/{postId}/favorite` 🔒

Bỏ yêu thích. Tự động giảm `favoriteCount`.

**Response `204 No Content`:** Thành công.

**Response `404 Not Found`:**
```json
{ "error": "Favorite not found" }
```

---

### `GET /api/community/posts/favorited` 🔒

Lấy tất cả bài viết đã yêu thích của người đang đăng nhập.

**Response `200 OK`:** Mảng raw `CommunityPost` entities.

---

## 5. Lưu bài (Save / Bookmark)

### `POST /api/community/posts/{postId}/save` 🔒

Lưu bài viết vào bookmark cá nhân.

**Response `200 OK`:**
```json
{ "message": "Post saved" }
```

**Response `400 Bad Request`:**
```json
{ "error": "Post already saved" }
```

---

### `DELETE /api/community/posts/{postId}/save` 🔒

Bỏ lưu bài viết.

**Response `204 No Content`:** Thành công.

**Response `404 Not Found`:**
```json
{ "error": "Save not found" }
```

---

### `GET /api/community/posts/saved` 🔒

Lấy tất cả bài viết đã lưu của người đang đăng nhập.

**Response `200 OK`:** Mảng raw `CommunityPost` entities.

---

## 6. Bình luận (Comment)

### `GET /api/community/posts/{postId}/comments`

Lấy toàn bộ comment và replies lồng nhau của một bài viết. Public.

**Request:**
```
GET /api/community/posts/42/comments
```

**Response `200 OK`:**
```json
{
  "postId": 42,
  "comments": [
    {
      "id": 10,
      "author": {
        "id": 5,
        "name": "Trần Thị B",
        "avatar": "https://res.cloudinary.com/demo/image/upload/avatars/5.jpg"
      },
      "content": "Bài viết rất hay và bổ ích!",
      "createdAt": "2026-03-10T08:30:00.0000000Z",
      "replies": [
        {
          "id": 20,
          "author": {
            "id": 7,
            "name": "Nguyễn Văn A",
            "avatar": "https://res.cloudinary.com/demo/image/upload/avatars/7.jpg"
          },
          "replyToUser": {
            "id": 5,
            "name": "Trần Thị B",
            "avatar": "https://res.cloudinary.com/demo/image/upload/avatars/5.jpg"
          },
          "content": "Cảm ơn bạn đã ủng hộ!",
          "createdAt": "2026-03-10T08:35:00.0000000Z"
        }
      ]
    },
    {
      "id": 11,
      "author": {
        "id": 9,
        "name": "Lê Văn C",
        "avatar": ""
      },
      "content": "Cho mình hỏi thêm về phần phỏng vấn technical được không?",
      "createdAt": "2026-03-10T09:00:00.0000000Z",
      "replies": []
    }
  ]
}
```

> `commentCount` trong feed chỉ đếm **top-level comments** (không đếm replies).

---

### `POST /api/community/posts/{postId}/comments` 🔒

Thêm comment vào bài viết.

**Content-Type:** `application/json`

**Request body:**
```json
{
  "content": "Bài viết rất hữu ích, cảm ơn tác giả!"
}
```

**Response `201 Created`:**
```json
{
  "id": 12,
  "communityPostId": 42,
  "userId": 7,
  "content": "Bài viết rất hữu ích, cảm ơn tác giả!",
  "createdAt": "2026-03-10T09:10:00Z",
  "updatedAt": null
}
```

**Responses:**

| Status | Mô tả |
|---|---|
| `201 Created` | Tạo comment thành công |
| `401 Unauthorized` | Chưa đăng nhập |

---

### `DELETE /api/community/comments/{commentId}` 🔒

Xóa comment. **Chủ comment** hoặc **ADMIN/MODERATOR**.

**Request:**
```
DELETE /api/community/comments/10
Authorization: Bearer <token>
```

**Responses:**

| Status | Mô tả |
|---|---|
| `204 No Content` | Xóa thành công |
| `401 Unauthorized` | Chưa đăng nhập |
| `403 Forbidden` | Không có quyền xóa |
| `404 Not Found` | Comment không tồn tại |

---

## 7. Trả lời (Reply)

### `POST /api/community/comments/{commentId}/replies` 🔒

Thêm reply vào một comment.

**Content-Type:** `application/json`

**Request body:**
```json
{
  "content": "Mình có thể chia sẻ thêm nếu bạn cần!",
  "replyToUserId": 9
}
```

| Field | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `content` | string | **Có** | Nội dung reply |
| `replyToUserId` | int? | Không | ID user được reply trực tiếp trong thread |

**Response `201 Created`:**
```json
{
  "id": 21,
  "commentId": 11,
  "userId": 7,
  "replyToUserId": 9,
  "content": "Mình có thể chia sẻ thêm nếu bạn cần!",
  "createdAt": "2026-03-10T09:15:00Z",
  "updatedAt": null
}
```

---

### `DELETE /api/community/replies/{replyId}` 🔒

Xóa reply. **Chủ reply** hoặc **ADMIN/MODERATOR**.

**Responses:**

| Status | Mô tả |
|---|---|
| `204 No Content` | Xóa thành công |
| `401 Unauthorized` | Chưa đăng nhập |
| `403 Forbidden` | Không có quyền xóa |
| `404 Not Found` | Reply không tồn tại |

---

## Tóm tắt tất cả endpoint

| Method | Endpoint | Auth | Content-Type | Mô tả |
|---|---|---|---|---|
| GET | `/api/community/posts` | Public | — | Feed (cursor pagination) |
| GET | `/api/community/posts/{id}` | Public | — | Chi tiết bài viết |
| GET | `/api/community/posts/user/{userId}` | Public | — | Bài viết của user |
| GET | `/api/community/posts/{postId}/comments` | Public | — | Comments + replies |
| POST | `/api/community/posts` | 🔒 Login | multipart/form-data | Tạo bài viết + upload media |
| PUT | `/api/community/posts/{id}` | 🔒 Owner | application/json | Sửa bài viết |
| DELETE | `/api/community/posts/{id}` | 🔒 Owner/Admin | — | Xóa bài viết |
| POST | `/api/community/posts/{postId}/favorite` | 🔒 Login | — | Yêu thích bài |
| DELETE | `/api/community/posts/{postId}/favorite` | 🔒 Login | — | Bỏ yêu thích |
| GET | `/api/community/posts/favorited` | 🔒 Login | — | DS bài đã thích |
| POST | `/api/community/posts/{postId}/save` | 🔒 Login | — | Lưu bài |
| DELETE | `/api/community/posts/{postId}/save` | 🔒 Login | — | Bỏ lưu bài |
| GET | `/api/community/posts/saved` | 🔒 Login | — | DS bài đã lưu |
| POST | `/api/community/posts/{postId}/comments` | 🔒 Login | application/json | Thêm comment |
| DELETE | `/api/community/comments/{commentId}` | 🔒 Owner/Admin | — | Xóa comment |
| POST | `/api/community/comments/{commentId}/replies` | 🔒 Login | application/json | Thêm reply |
| DELETE | `/api/community/replies/{replyId}` | 🔒 Owner/Admin | — | Xóa reply |

---

## Lưu ý kỹ thuật

### Cursor Pagination (Feed)
```
Trang 1: GET /api/community/posts?pageSize=20
         → nextCursor: 38, hasMore: true

Trang 2: GET /api/community/posts?pageSize=20&cursor=38
         → nextCursor: 18, hasMore: true

Trang 3: GET /api/community/posts?pageSize=20&cursor=18
         → nextCursor: null, hasMore: false  ← Hết dữ liệu
```

### Cover Image vs. Additional Media (CreatePost)
- **`coverImageKey`** trong `postJson` = tên file (trong `files[]`) được chỉ định làm **ảnh/video bìa**
- File bìa → upload → URL lưu vào cột `CoverImageVideo` → expose thành `coverImageUrl` trong response
- Các file còn lại → upload → lưu vào bảng `communityPostMedia` → expose thành `media[]` trong response
- Nếu không có `coverImageKey`, tất cả files đều vào `media[]`; `coverImageUrl` = `null`

### Upload nhiều media (CreatePost)
- Mỗi file được upload riêng lên Media Service → Cloudinary
- Nếu 1 file lỗi, các file còn lại vẫn được upload (không rollback)
- File ảnh (`image/*`) → `/api/upload/image`
- File video (`video/*`) → `/api/upload/video`
- `CommunityPostMedia.Name` lưu **PublicId** từ Cloudinary (dùng để xóa file sau này)

### Post Status
| Giá trị | Ý nghĩa |
|---|---|
| `1` | Public — hiển thị trên feed |
| `0` | Ẩn — không hiển thị trên feed |

### `portfolioPreview`
Trả về block đầu tiên của portfolio đính kèm. `null` nếu không có `portfolioId` hoặc portfolio trống.
```json
{
  "type": "experience",
  "variant": "card",
  "data": { ... }
}
```

### `isFavorited` / `isSaved`
- Chỉ có giá trị đúng khi request **có JWT token hợp lệ**
- Nếu không có token → luôn `false`
