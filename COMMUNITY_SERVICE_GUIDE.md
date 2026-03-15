# Community Service API Guide

**Base URL:** `http://localhost:5004` (dev) | `http://community-service:8080` (Docker)  
**Swagger UI:** `http://localhost:5004/swagger`

---

## Xác thực (Authentication)

Community Service dùng **JWT Bearer Token** lấy từ Auth Service sau khi đăng nhập.

**Trên Swagger UI:** Click nút **Authorize 🔒** → nhập token (không cần prefix `Bearer`).

**Với HTTP client:**
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

Lấy feed bài viết theo **cursor-based pagination**. Public.  
Nếu có token hợp lệ, tự động tính `isFavorited` và `isSaved` cho từng bài.

**Query Parameters:**

| Tên | Kiểu | Mặc định | Mô tả |
|---|---|---|---|
| `pageSize` | int | 20 | Số bài mỗi trang (tối đa 100) |
| `cursor` | int? | — | ID bài cuối của trang trước (lấy từ `nextCursor`) |

**Request — trang đầu:**
```
GET /api/community/posts?pageSize=20
```

**Request — trang tiếp theo:**
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
      "description": "Tuyển dụng Senior Developer — Remote toàn thời gian...",
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

> Khi `hasMore = false` thì đã hết dữ liệu.  
> `isFavorited` / `isSaved` luôn là `false` nếu request không có token.

**Author Role:**
- `"USER"` — người dùng thông thường (employee profile)
- `"COMPANY"` — nhà tuyển dụng (company profile)

---

## 2. Chi tiết bài viết

### `GET /api/community/posts/{id}`

Lấy đầy đủ thông tin một bài viết theo ID. Public.

**Request:**
```
GET /api/community/posts/42
Authorization: Bearer <token>   (tuỳ chọn — để có isFavorited/isSaved)
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

Lấy tất cả bài viết của một user cụ thể. Public.  
Trả về raw entity (không enriched).

**Request:**
```
GET /api/community/posts/user/7
```

**Response `200 OK`:**
```json
[
  {
    "id": 42,
    "userId": 7,
    "description": "Chia sẻ kinh nghiệm phỏng vấn...",
    "coverImageVideo": "https://res.cloudinary.com/demo/image/upload/community/posts/cover.jpg",
    "portfolioId": 3,
    "favoriteCount": 15,
    "status": 1,
    "createdAt": "2026-03-10T08:00:00Z",
    "updatedAt": null,
    "saves": [],
    "favorites": [],
    "media": [],
    "comments": []
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

> **`coverImageKey`**: Phải khớp chính xác tên file gửi lên trong `files[]` (so sánh không phân biệt hoa thường).  
> File đó → lưu vào `coverImageUrl`. Các file còn lại → lưu vào `media[]`.

**Ví dụ — curl:**
```bash
curl -X POST http://localhost:5004/api/community/posts \
  -H "Authorization: Bearer <token>" \
  -F 'postJson={"description":"Bài viết của tôi","status":1,"coverImageKey":"cover.jpg"}' \
  -F 'files=@/path/to/cover.jpg' \
  -F 'files=@/path/to/photo1.jpg' \
  -F 'files=@/path/to/video1.mp4'
```

**Ví dụ — JavaScript (fetch):**
```javascript
const formData = new FormData();
formData.append('postJson', JSON.stringify({
  description: 'Bài viết của tôi',
  status: 1,
  coverImageKey: 'cover.jpg'
}));
formData.append('files', coverFile);   // File object, tên file phải là "cover.jpg"
formData.append('files', photo1File);
formData.append('files', video1File);

const res = await fetch('http://localhost:5004/api/community/posts', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` },
  body: formData   // KHÔNG set Content-Type thủ công
});
```

**Ví dụ — Swagger UI:**
1. Click **Authorize 🔒** → nhập token → **Authorize**
2. Mở `POST /api/community/posts` → **Try it out**
3. Điền `postJson`:
   ```json
   {"description":"Test post","status":1}
   ```
4. Upload file ở ô `files` (tuỳ chọn)
5. Click **Execute**

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
  "updatedAt": null,
  "saves": [],
  "favorites": [],
  "media": [],
  "comments": []
}
```

> **Lưu ý:** Response trả về entity thô sau khi tạo. Media đã được upload nhưng không được nhúng trong response này.  
> Để lấy bài đầy đủ (kèm `coverImageUrl` và `media[]`), gọi `GET /api/community/posts/43`.

**Responses:**

| Status | Mô tả |
|---|---|
| `201 Created` | Tạo thành công |
| `400 Bad Request` | `postJson` không hợp lệ |
| `401 Unauthorized` | Chưa đăng nhập hoặc token không hợp lệ |

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

| Field | Kiểu | Mô tả |
|---|---|---|
| `description` | string? | Nội dung mới (bỏ qua nếu null) |
| `portfolioId` | int? | Portfolio đính kèm mới (bỏ qua nếu null) |
| `status` | int? | `1` = public, `0` = ẩn (bỏ qua nếu null) |

**Ví dụ — chỉ ẩn bài viết:**
```json
{ "status": 0 }
```

**Responses:**

| Status | Mô tả |
|---|---|
| `204 No Content` | Cập nhật thành công |
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

Thêm bài vào danh sách yêu thích. Tự động tăng `favoriteCount` +1.

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

Bỏ yêu thích. Tự động giảm `favoriteCount` -1.

**Request:**
```
DELETE /api/community/posts/42/favorite
Authorization: Bearer <token>
```

**Response `204 No Content`:** Thành công.

**Response `404 Not Found`:**
```json
{ "error": "Favorite not found" }
```

---

### `GET /api/community/posts/favorited` 🔒

Lấy tất cả bài viết đã yêu thích của người đang đăng nhập. Trả về raw entities.

**Request:**
```
GET /api/community/posts/favorited
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
[
  {
    "id": 42,
    "userId": 7,
    "description": "Chia sẻ kinh nghiệm phỏng vấn...",
    "coverImageVideo": "https://res.cloudinary.com/demo/image/upload/community/posts/cover.jpg",
    "portfolioId": 3,
    "favoriteCount": 15,
    "status": 1,
    "createdAt": "2026-03-10T08:00:00Z",
    "updatedAt": null,
    "saves": [],
    "favorites": [],
    "media": [],
    "comments": []
  }
]
```

---

## 5. Lưu bài (Save / Bookmark)

### `POST /api/community/posts/{postId}/save` 🔒

Lưu bài viết vào bookmark cá nhân.

**Request:**
```
POST /api/community/posts/42/save
Authorization: Bearer <token>
```

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

**Request:**
```
DELETE /api/community/posts/42/save
Authorization: Bearer <token>
```

**Response `204 No Content`:** Thành công.

**Response `404 Not Found`:**
```json
{ "error": "Save not found" }
```

---

### `GET /api/community/posts/saved` 🔒

Lấy tất cả bài viết đã lưu của người đang đăng nhập. Trả về raw entities.

**Request:**
```
GET /api/community/posts/saved
Authorization: Bearer <token>
```

**Response `200 OK`:**
```json
[
  {
    "id": 41,
    "userId": 12,
    "description": "Tuyển dụng Senior Developer...",
    "coverImageVideo": "",
    "portfolioId": null,
    "favoriteCount": 8,
    "status": 1,
    "createdAt": "2026-03-10T07:30:00Z",
    "updatedAt": null,
    "saves": [],
    "favorites": [],
    "media": [],
    "comments": []
  }
]
```

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

**Request:**
```
POST /api/community/posts/42/comments
Authorization: Bearer <token>
Content-Type: application/json
```

**Request body:**
```json
{
  "content": "Bài viết rất hữu ích, cảm ơn tác giả!"
}
```

| Field | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `content` | string | **Có** | Nội dung bình luận |

**Response `201 Created`:**
```json
{
  "id": 12,
  "communityPostId": 42,
  "userId": 7,
  "content": "Bài viết rất hữu ích, cảm ơn tác giả!",
  "createdAt": "2026-03-10T09:10:00Z",
  "updatedAt": null,
  "replies": []
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

**Request:**
```
POST /api/community/comments/11/replies
Authorization: Bearer <token>
Content-Type: application/json
```

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
| `replyToUserId` | int? | Không | ID user được reply trực tiếp trong thread (để hiển thị "@username") |

**Ví dụ reply không đề cập user cụ thể:**
```json
{
  "content": "Cảm ơn tất cả mọi người!"
}
```

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

**Responses:**

| Status | Mô tả |
|---|---|
| `201 Created` | Tạo reply thành công |
| `401 Unauthorized` | Chưa đăng nhập |

---

### `DELETE /api/community/replies/{replyId}` 🔒

Xóa reply. **Chủ reply** hoặc **ADMIN/MODERATOR**.

**Request:**
```
DELETE /api/community/replies/21
Authorization: Bearer <token>
```

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
| POST | `/api/community/posts` | 🔒 Login | `multipart/form-data` | Tạo bài viết + upload media |
| PUT | `/api/community/posts/{id}` | 🔒 Owner | `application/json` | Sửa bài viết |
| DELETE | `/api/community/posts/{id}` | 🔒 Owner/Admin | — | Xóa bài viết |
| POST | `/api/community/posts/{postId}/favorite` | 🔒 Login | — | Yêu thích bài |
| DELETE | `/api/community/posts/{postId}/favorite` | 🔒 Login | — | Bỏ yêu thích |
| GET | `/api/community/posts/favorited` | 🔒 Login | — | DS bài đã yêu thích |
| POST | `/api/community/posts/{postId}/save` | 🔒 Login | — | Lưu bài |
| DELETE | `/api/community/posts/{postId}/save` | 🔒 Login | — | Bỏ lưu bài |
| GET | `/api/community/posts/saved` | 🔒 Login | — | DS bài đã lưu |
| POST | `/api/community/posts/{postId}/comments` | 🔒 Login | `application/json` | Thêm comment |
| DELETE | `/api/community/comments/{commentId}` | 🔒 Owner/Admin | — | Xóa comment |
| POST | `/api/community/comments/{commentId}/replies` | 🔒 Login | `application/json` | Thêm reply |
| DELETE | `/api/community/replies/{replyId}` | 🔒 Owner/Admin | — | Xóa reply |

---

## Lưu ý kỹ thuật

### Cursor Pagination (Feed)
```
Trang 1: GET /api/community/posts?pageSize=20
         → { items: [...], nextCursor: 38, hasMore: true }

Trang 2: GET /api/community/posts?pageSize=20&cursor=38
         → { items: [...], nextCursor: 18, hasMore: true }

Trang 3: GET /api/community/posts?pageSize=20&cursor=18
         → { items: [...], nextCursor: null, hasMore: false }  ← Hết dữ liệu
```

### Cover Image vs. Additional Media (CreatePost)

```
files = [cover.jpg, photo1.jpg, video1.mp4]
postJson.coverImageKey = "cover.jpg"

Kết quả:
  cover.jpg  →  upload  →  post.CoverImageVideo (URL)  →  coverImageUrl trong response
  photo1.jpg →  upload  →  CommunityPostMedia (type: image)  →  media[0] trong response
  video1.mp4 →  upload  →  CommunityPostMedia (type: video)  →  media[1] trong response
```

- Nếu không có `coverImageKey`, tất cả files đều vào `media[]`, `coverImageUrl` = `null`
- Mỗi file upload riêng lên Media Service → Cloudinary — nếu 1 file lỗi, các file khác vẫn tiếp tục

### Post Status
| Giá trị | Ý nghĩa |
|---|---|
| `1` | Public — hiển thị trên feed |
| `0` | Ẩn — không hiển thị trên feed |

### Sự khác biệt giữa Feed DTO và Raw Entity

| | Feed / GetById | GetByUser / Saved / Favorited |
|---|---|---|
| Format | `CommunityPostDto` (enriched) | `CommunityPost` (raw entity) |
| author | Object `{ id, name, avatar, role }` | Chỉ có `userId` (int) |
| coverImage | `coverImageUrl` (string?) | `coverImageVideo` (string) |
| media | `media` (string[]) | Empty array (lazy load) |
| isFavorited/isSaved | Có | Không |

### `portfolioPreview`
Trả về block đầu tiên của portfolio đính kèm. `null` nếu không có `portfolioId` hoặc portfolio trống.
```json
{
  "type": "experience",
  "variant": "card",
  "data": { "title": "Software Engineer", "company": "Tech Corp" }
}
```

### `isFavorited` / `isSaved`
- Chỉ có giá trị đúng khi request **có JWT token hợp lệ**
- Nếu không có token → luôn `false`
