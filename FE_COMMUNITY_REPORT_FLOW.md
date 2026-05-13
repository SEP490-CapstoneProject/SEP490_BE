# FE Guide: Community Report & Review Flow

Tài liệu này giúp Frontend tích hợp luồng:
1. User **report bài đăng**
2. Admin/Moderator **nhận thông báo report**
3. Admin/Moderator **review report** (approve/reject)

## 1. Auth và quyền

- Mọi API bên dưới dùng `Authorization: Bearer <accessToken>`.
- Role hợp lệ để review:
  - `ADMIN`
  - `MODERATOR`

## 2. API report bài đăng (User)

### Endpoint
`POST /api/community/posts/{postId}/report`

### Request body
```json
{
  "reason": "spam",
  "description": "Nội dung vi phạm..."
}
```

### Response thành công (201)
```json
{
  "id": 5,
  "communityPostId": 53,
  "postOwnerUserId": 15,
  "reporterUserId": 17,
  "reason": "spam",
  "description": "Nội dung vi phạm...",
  "status": "Pending",
  "reviewedByUserId": null,
  "reviewedAt": null,
  "reviewNote": null,
  "createdAt": "2026-04-18T21:25:09.3478873",
  "updatedAt": null
}
```

### Lỗi thường gặp
- `400`: đã report bài này trước đó / reason rỗng / dữ liệu không hợp lệ
- `404`: bài không tồn tại
- `401`: token không hợp lệ

---

## 3. Notification khi có report mới (Admin/Moderator)

Khi có report mới, hệ thống gửi notification cho role `ADMIN, MODERATOR` với cơ chế:

1. **Report đầu tiên** trên `(postId, recipient)` => gửi notification **ngay**
2. Report tiếp theo trong **10 phút** => gom lại
3. Hết cửa sổ 10 phút => gửi 1 notification tổng hợp “có thêm N report”

### Notification type
`COMMUNITY_REPORT_REVIEW`

### Ví dụ notification item
```json
{
  "id": 176,
  "userId": "19",
  "title": "Bài đăng bị báo cáo",
  "content": "Bài đăng #53 có báo cáo mới cần được kiểm duyệt.",
  "type": "COMMUNITY_REPORT_REVIEW",
  "objectId": "53",
  "actor": {
    "id": 17,
    "name": "testuser2@gmail.com",
    "avatar": "",
    "Role": "USER"
  },
  "createdAt": "2026-04-18T21:25:09.3478873",
  "isRead": false
}
```

> Lưu ý: account USER thường (ví dụ `testuser2@gmail.com`) **không nhận** loại notification này.

---

## 4. API lấy notification (FE cho Admin/Moderator)

### 4.1 Danh sách notification
`GET /api/notifications?cursor=<id>&limit=20`

- `limit`: 1..50
- Dùng `cursor` để phân trang kiểu infinite scroll

### 4.2 Unread count
`GET /api/notifications/unread-count`

Response:
```json
{ "count": 3 }
```

### 4.3 Đánh dấu đã đọc
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

---

## 5. API review report (Admin/Moderator)

### 5.1 Lấy danh sách report
`GET /api/community/admin/reports?status=Pending&pageNumber=1&pageSize=20`

Filter hỗ trợ:
- `postId`
- `reporterUserId`
- `status`: `Pending | Approved | Rejected`
- `pageNumber`, `pageSize`

### 5.2 Review report
`POST /api/community/admin/reports/{reportId}/review`

Request body:
```json
{
  "action": "approve_violation",
  "reviewNote": "Vi phạm tiêu chuẩn cộng đồng"
}
```

`action` hợp lệ:
- `approve_violation`
- `reject`

Khi `approve_violation`:
- Post bị ẩn khỏi feed user/public
- Chủ post nhận notification moderation

---

## 6. Gợi ý FE flow

1. User bấm Report -> gọi `POST /api/community/posts/{postId}/report`
2. FE admin/moderator:
   - polling `GET /api/notifications` hoặc dùng realtime channel hiện có
   - lọc item `type === "COMMUNITY_REPORT_REVIEW"` để hiển thị badge/report inbox
3. Admin mở report queue:
   - gọi `GET /api/community/admin/reports?status=Pending`
4. Admin duyệt:
   - gọi `POST /api/community/admin/reports/{reportId}/review`
5. Refresh notification unread:
   - gọi `GET /api/notifications/unread-count`

---

## 7. Checklist FE nhanh

- [ ] Luôn gửi Bearer token
- [ ] UI report gửi đúng `reason`, `description`
- [ ] Notification center hiển thị `COMMUNITY_REPORT_REVIEW`
- [ ] Badge unread dùng `GET /api/notifications/unread-count`
- [ ] Màn hình moderation dùng `GET /api/community/admin/reports`
- [ ] Action review gọi đúng `approve_violation` / `reject`
