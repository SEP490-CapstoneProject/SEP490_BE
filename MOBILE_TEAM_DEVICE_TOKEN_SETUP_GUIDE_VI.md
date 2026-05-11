# 📱 Hướng Dẫn Thiết Lập Device Token FCM Cho Mobile Team

**Phiên Bản Tài Liệu**: 1.0  
**Cập Nhật Lần Cuối**: 2026-05-11  
**Trạng Thái**: ✅ Sẵn Sàng Sản Xuất

---

## 📖 Mục Lục
1. [Device Token là gì?](#device-token-là-gì)
2. [Cách Đăng Ký Thiết Bị](#cách-đăng-ký-thiết-bị)
3. [Cách Tạo Lại Token](#cách-tạo-lại-token)
4. [Cách Kiểm Tra Token](#cách-kiểm-tra-token)
5. [Khắc Phục Sự Cố](#khắc-phục-sự-cố)
6. [Câu Hỏi Thường Gặp](#câu-hỏi-thường-gặp)

---

## Device Token là gì?

**Device Token** là một mã định danh duy nhất mà Firebase Cloud Messaging (FCM) sử dụng để gửi push notification đến điện thoại của bạn.

### Điểm Chính
- ✅ **Duy nhất cho mỗi thiết bị**: Mỗi điện thoại có một token khác nhau
- ✅ **Tự động tạo**: Được tạo khi cài đặt ứng dụng lần đầu
- ✅ **Cần thiết để nhận thông báo**: Không có nó, bạn sẽ không nhận được thông báo
- ✅ **Tạm thời**: Có thể hết hạn nếu không sử dụng trong thời gian dài
- ✅ **Lưu trữ trên server**: Được lưu trong cơ sở dữ liệu để gửi thông báo

### Ví Dụ Format Token
```
fpQ6ZToxTRWox8EzcQKtS8:APA91bGQ9qymJXZhBG0xyk8f2g5FldUxCprH3oqviF14A_4Qnb0wXEFPpc7uLuHy3R-FhPdgxYVVtLvSYysxHyoLuMKidaBAtaDzmrw9LLaQYWgHKKqUdWg
```

---

## Cách Đăng Ký Thiết Bị

### Đăng Ký Tự Động (Được Khuyến Nghị)
Ứng dụng sẽ tự động đăng ký device token của bạn khi:

1. **Lần đầu tiên cài đặt**:
   - Cài đặt ứng dụng từ Play Store
   - Mở ứng dụng
   - Ứng dụng tự động đăng ký device token
   - Token được lưu trên server

2. **Sau khi đăng nhập**:
   - Đăng nhập bằng tài khoản của bạn
   - Device token tự động được gửi tới backend
   - Server lưu token cho tài khoản của bạn

### Những Gì Xảy Ra Phía Sau

```
Cài Đặt Ứng Dụng → Firebase tạo token
           ↓
Ứng Dụng Khởi Động → Đọc token từ Firebase SDK
           ↓
Backend API → Gửi token tới server
           ↓
Lưu Vào DB → Token sẵn sàng gửi thông báo
```

---

## Cách Tạo Lại Token

### Tại Sao Cần Tạo Lại Token?
- ❌ Không nhận được thông báo sau thời gian dài
- ❌ Đã thay đổi cấu hình Firebase
- ❌ Ứng dụng không hoạt động trong vài tháng
- ❌ Sau bản cập nhật lớn của ứng dụng
- ✅ Bảo trì định kỳ

### Phương Pháp 1: Xóa Dữ Liệu Ứng Dụng (Được Khuyến Nghị)
Đây là cách an toàn nhất để tạo lại token mới:

**Các Bước**:
1. Mở **Cài Đặt** trên điện thoại
2. Đi tới **Ứng Dụng** (hoặc **Trình Quản Lý Ứng Dụng**)
3. Tìm **SkillSnap** (hoặc tên ứng dụng của bạn)
4. Chọn **Bộ Nhớ** (hoặc **Quản Lý Bộ Nhớ**)
5. Chọn **Xóa Bộ Nhớ Cache**
6. Chọn **Xóa Dữ Liệu** (hoặc **Xóa Bộ Nhớ**)
   - ⚠️ **Cảnh Báo**: Điều này sẽ đăng xuất bạn khỏi ứng dụng
7. Khởi Động Lại Điện Thoại
8. Mở lại ứng dụng
9. Đăng nhập lại bằng thông tin đăng nhập của bạn
10. ✅ Device token mới được tạo và đăng ký

**Thời Gian Hiệu Lực**: 30 giây - 2 phút

### Phương Pháp 2: Gỡ Cài Đặt & Cài Đặt Lại
Nếu Phương Pháp 1 không hoạt động:

**Các Bước**:
1. Nhấn giữ biểu tượng ứng dụng
2. Chọn **Gỡ Cài Đặt** (hoặc nhấn biểu tượng ⊗)
3. Xác Nhận Gỡ Cài Đặt
4. Đi tới Play Store
5. Tìm Kiếm **SkillSnap**
6. Chọn **Cài Đặt**
7. Chờ cài đặt hoàn tất
8. Mở ứng dụng
9. Đăng nhập lại bằng thông tin của bạn
10. ✅ Device token mới được đăng ký

**Thời Gian Hiệu Lực**: 1-3 phút

### Phương Pháp 3: Làm Mới Bắt Buộc (Nâng Cao)
Nếu bạn muốn ở trạng thái đã đăng nhập:

**Cho Ứng Dụng React Native / Expo**:
```javascript
// Nhà Phát Triển: Thêm điều này vào thiết lập thông báo
import messaging from '@react-native-firebase/messaging';

async function refreshDeviceToken() {
  try {
    const newToken = await messaging().getToken();
    console.log('FCM Token Mới:', newToken);
    
    // Gửi tới backend
    await fetch('https://api.skillsnap.com/api/device-tokens/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${userToken}`
      },
      body: JSON.stringify({
        deviceToken: newToken,
        deviceType: 'Android',
        appVersion: appVersion
      })
    });
    
    console.log('Token được làm mới thành công');
  } catch (error) {
    console.error('Lỗi khi làm mới token:', error);
  }
}

// Gọi hàm này
await refreshDeviceToken();
```

---

## Cách Kiểm Tra Token

### Kiểm Tra Nếu Token Đã Được Đăng Ký
**Cho Người Dùng Kỹ Thuật / QA Team**:

#### 1. Kiểm Tra Nhật Ký Ứng Dụng
```bash
# Android Studio Logcat
adb logcat | grep "FCM\|DeviceToken\|notification"

# Tìm nhật ký như:
# [FCM_TOKEN] Device token đã đăng ký thành công
# [FCM_REGISTERED] Token: fpQ6ZToxT...
```

#### 2. Kiểm Tra Database Backend
Yêu cầu Backend Team chạy:
```sql
SELECT [DeviceToken], [RegisteredAt], [LastUsedAt], [IsActive]
FROM [DEVICE_TOKENS]
WHERE [UserId] = <your_user_id>
ORDER BY [RegisteredAt] DESC;
```

#### 3. Gửi Thông Báo Kiểm Tra
Yêu cầu backend team gửi thông báo kiểm tra cho tài khoản của bạn:
- Kiểm Tra Xem Bạn Có Nhận Được Push Notification Trên Thiết Bị
- Nếu có ✅ → Token hợp lệ
- Nếu không ❌ → Token có thể hết hạn (làm lại token)

### Dấu Hiệu Token Hợp Lệ
✅ Bạn nhận được thông báo ngay khi có sự kiện xảy ra  
✅ Không có độ trễ hoặc thông báo bị mất  
✅ Thông báo xuất hiện ngay cả khi ứng dụng đóng

### Dấu Hiệu Token Không Hợp Lệ
❌ Không nhận được thông báo  
❌ Thông báo xuất hiện muộn 30+ phút  
❌ Thông báo không bao giờ tới  
❌ Ứng dụng hiển thị lỗi "Không Thể Gửi" trong nhật ký

---

## Khắc Phục Sự Cố

### Vấn Đề: Không Nhận Được Thông Báo

**Giải Pháp 1**: Kiểm Tra Cài Đặt Thông Báo
1. Mở cài đặt ứng dụng
2. Kiểm Tra **Tùy Chọn Thông Báo**
3. Đảm Bảo Những Điều Này Được **Bật**:
   - ✅ Push Notifications
   - ✅ Chat Notifications (nếu có)
   - ✅ Sound Enabled
   - ✅ Vibrate Enabled

**Giải Pháp 2**: Tạo Lại Device Token
Làm theo [Phương Pháp 1: Xóa Dữ Liệu Ứng Dụng](#phương-pháp-1-xóa-dữ-liệu-ứng-dụng-được-khuyến-nghị) ở trên

**Giải Pháp 3**: Buộc Dừng Và Khởi Động Lại Ứng Dụng
1. Đi tới Cài Đặt → Ứng Dụng → SkillSnap
2. Chọn **Buộc Dừng**
3. Chờ 5 giây
4. Mở lại ứng dụng

**Giải Pháp 4**: Cài Đặt Lại Ứng Dụng
Làm theo [Phương Pháp 2: Gỡ Cài Đặt & Cài Đặt Lại](#phương-pháp-2-gỡ-cài-đặt--cài-đặt-lại)

---

### Vấn Đề: Lỗi "Token Không Tìm Thấy" Trong Backend

**Nguyên Nhân**: Backend không thể tìm thấy device token của bạn

**Giải Pháp**:
1. Đăng Xuất Khỏi Ứng Dụng
2. Đăng Nhập Lại (bắt buộc đăng ký token lại)
3. Chờ 2 phút
4. Yêu Cầu Backend Team Xác Minh Token Đã Được Đăng Ký

---

### Vấn Đề: Token Liên Tục Hết Hạn

**Nguyên Nhân**: Sử Dụng Cấu Hình Firebase Cũ

**Giải Pháp**:
1. Kiểm Tra File `google-services.json` của ứng dụng Có Đúng Không
   - Nên Khớp Firebase Project: `skillsnap-notification`
2. Nếu Không, Cập Nhật `google-services.json`:
   - Tải Xuống Từ Firebase Console
   - Thay Thế Trong Source Code Ứng Dụng
   - Biên Dịch Lại Và Cài Đặt Lại Ứng Dụng
3. Sau Khi Cập Nhật, Tạo Lại Token

---

### Vấn Đề: Có Nhiều Token Được Đăng Ký (Trùng Lặp)

**Nguyên Nhân**: Ứng dụng được cài đặt lại nhiều lần mà không dọn sạch token cũ

**Giải Pháp**:
1. Liên Hệ Backend Team
2. Yêu Cầu Họ Xóa Các Token Cũ Đã Hết Hạn
3. Giữ Lại Chỉ Token Hợp Lệ Mới Nhất
4. Tạo Lại Một Lần Nữa Để Đảm Bảo Token Mới

---

## Câu Hỏi Thường Gặp

### Q: Tôi Có Cần Làm Gì Để Đăng Ký Thiết Bị?
**A**: Không! Nó là tự động. Khi bạn cài đặt và sử dụng ứng dụng, nó sẽ đăng ký tự động.

### Q: Device Token Tồn Tại Bao Lâu?
**A**: Vô thời hạn, miễn là bạn sử dụng ứng dụng thường xuyên. Nếu bạn không sử dụng ứng dụng trong 6+ tháng, token có thể hết hạn.

### Q: Tôi Có Thể Xem Device Token Của Tôi Không?
**A**: Có, nhưng chỉ thông qua nhật ký ứng dụng (cần công cụ nhà phát triển). Để an toàn, token không được hiển thị trong giao diện người dùng.

### Q: Xóa Dữ Liệu Ứng Dụng Có Xóa Tài Khoản Của Tôi Không?
**A**: Không, nó chỉ xóa dữ liệu cục bộ. Tài khoản của bạn trên server vẫn còn. Bạn chỉ cần đăng nhập lại.

### Q: Tôi Có Thể Đăng Ký Bao Nhiêu Thiết Bị?
**A**: Không giới hạn! Đăng ký ứng dụng trên nhiều điện thoại/máy tính bảng, mỗi thiết bị sẽ có token của riêng nó.

### Q: Nếu Tôi Cài Đặt Lại Ứng Dụng Trên Cùng Một Điện Thoại Sẽ Xảy Ra Điều Gì?
**A**: Bạn sẽ nhận được một device token mới. Token cũ sẽ được xóa sau 6 tháng không hoạt động.

### Q: Tại Sao Thông Báo Của Tôi Không Tới?
**A**: Những lý do phổ biến:
- ❌ Device token chưa được đăng ký
- ❌ Thông báo bị vô hiệu hóa trong cài đặt ứng dụng
- ❌ Token đã hết hạn (tạo lại nó)
- ❌ Backend app không hoạt động (kiểm tra trạng thái)
- ❌ Vấn đề về kết nối mạng

### Q: Tôi Có Thể Buộc Tạo Token Mới Không?
**A**: Có, sử dụng Phương Pháp 1 hoặc 2 ở trên.

### Q: Tôi Có Mất Tin Nhắn Sau Khi Tạo Lại Token Không?
**A**: Không! Tin nhắn được lưu trữ trong backend. Chỉ cơ chế gửi thông báo thay đổi.

---

## Tham Chiếu Nhanh

| Nhiệm Vụ | Bước | Thời Gian |
|------|-------|------|
| **Đăng Ký Tự Động** | Cài Đặt Ứng Dụng + Đăng Nhập | 1-2 phút |
| **Xóa Cache** | Cài Đặt → Ứng Dụng → SkillSnap → Xóa Cache | 30 giây |
| **Xóa Dữ Liệu** | Cài Đặt → Ứng Dụng → SkillSnap → Xóa Dữ Liệu | 1-2 phút |
| **Gỡ/Cài Lại** | Play Store → Gỡ Cài Đặt → Cài Đặt | 5-10 phút |
| **Kiểm Tra Thông Báo** | Yêu Cầu Backend Team Gửi Kiểm Tra | Tức Thì |
| **Kiểm Tra Trạng Thái Token** | Liên Hệ Backend Team | 5 phút |

---

## Liên Hệ & Hỗ Trợ

### Cho Vấn Đề Kỹ Thuật
**Backend Team** (Hỗ Trợ Push Notification):
- Slack: #notification-support
- Email: notification-team@skillsnap.com
- Issue Tracker: [GitHub Issues - Notification](https://github.com/SEP490-CapstoneProject/SEP490_BE/issues?q=label%3Anotification)

### Cho Vấn Đề Cấu Hình Firebase
**DevOps Team**:
- Firebase Project: skillsnap-notification
- Slack: #devops
- Contact: devops@skillsnap.com

### Cho Vấn Đề Ứng Dụng Mobile
**Mobile Team**:
- Android: [Android GitHub](https://github.com/SEP490-CapstoneProject/mobile-app)
- iOS: [iOS GitHub](https://github.com/SEP490-CapstoneProject/ios-app)

---

## Thực Hành Tốt Nhất

✅ **NÊN**:
- Giữ ứng dụng cập nhật lên phiên bản mới nhất
- Đăng nhập thường xuyên để giữ token tươi
- Báo cáo vấn đề thông báo ngay lập tức
- Kiểm Tra Cài Đặt Thông Báo Được Bật
- Tạo Lại Token Nếu Sự Cố Tiếp Tục

❌ **KHÔNG NÊN**:
- Chia Sẻ Device Token Của Bạn Với Người Khác
- Chỉnh Sửa Thủ Công Các Giá Trị Token
- Sử Dụng Token Từ Các Thiết Bị Khác
- Bỏ Qua Lời Nhắc Về Quyền Thông Báo
- Gỡ Cài Đặt Ứng Dụng Mà Không Đăng Xuất Trước

---

## Phụ Lục: Thông Tin Kỹ Thuật

### Android - Tạo FCM Token

```kotlin
// Thiết Lập Firebase Cloud Messaging
import com.google.firebase.messaging.FirebaseMessaging

// Nhận Token Hiện Tại
FirebaseMessaging.getInstance().token.addOnCompleteListener { task ->
    if (!task.isSuccessful) {
        Log.w("FCM", "Lỗi Khi Lấy Token FCM", task.exception)
        return@addOnCompleteListener
    }
    
    val token = task.result
    Log.d("FCM_TOKEN", "Device Token: $token")
    
    // Gửi Tới Backend
    sendTokenToBackend(token)
}

// Lắng Nghe Token Mới
class MyFirebaseMessagingService : FirebaseMessagingService() {
    override fun onNewToken(token: String) {
        Log.d("FCM_NEW_TOKEN", "Token Được Làm Mới: $token")
        sendTokenToBackend(token)
    }
}
```

### iOS - Tạo FCM Token

```swift
// Thiết Lập Firebase Cloud Messaging
import FirebaseMessaging

Messaging.messaging().token { token, error in
    if let error = error {
        print("Lỗi Khi Lấy FCM Token: \(error)")
    } else if let token = token {
        print("FCM Token: \(token)")
        // Gửi Tới Backend
        sendTokenToBackend(token)
    }
}
```

### Backend - Đăng Ký Token

```csharp
// C# Backend API
[HttpPost("device-tokens/register")]
[Authorize]
public async Task<IActionResult> RegisterDeviceToken(
    [FromBody] RegisterDeviceTokenRequest request)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    var result = await _deviceTokenService.RegisterTokenAsync(
        userId,
        request.DeviceToken,
        request.DeviceType,
        request.AppVersion
    );
    
    if (result)
        return Ok(new { message = "Device token được đăng ký thành công" });
    
    return BadRequest(new { message = "Không thể đăng ký device token" });
}
```

---

**Trạng Thái Tài Liệu**: ✅ Sẵn Sàng Cho Mobile Team  
**Đã Kiểm Tra Lần Cuối**: 2026-05-11  
**Phản Hồi**: Chia Sẻ Vấn Đề Trong Kênh #notification-support Slack
