# 🔌 Hướng Dẫn Setup Real-time (Chat & Notification) Cho Front-End

Lỗi trước đây nguyên nhân là do **API Gateway (YARP) bị thiếu cấu hình Proxy cho đường dẫn Chat (`/hubs/chat`) và chưa bật WebSockets cho Connection Service**. Lỗi này đã được fix xong ở Backend. Bạn làm theo hướng dẫn dưới đây để config trên FE nhé!

---

## 1. Cài Đặt Thư Viện

Bạn cài @microsoft/signalr nếu chưa có:
```bash
npm install @microsoft/signalr
# hoặc
yarn add @microsoft/signalr
```

---

## 2. Các Endpoints Real-time Cần Kết Nối

Có 2 Hub chính cần kết nối ở hệ thống này:

| Tính Năng | Endpoint Gateway | Mục Đích |
|---|---|---|
| **Chat 1-1** | `wss://<GATEWAY_URL>/hubs/chat` | Chat real-time trong Connection Service |
| **Notifications** | `wss://<GATEWAY_URL>/hubs/realtime` | Nhận thông báo chung (Like, Comment, Notification...) của toàn hệ thống |

*(Đăng nhập xong lấy `accessToken` và truyền vào `accessTokenFactory`)*

---

## 3. Code Mẫu Tạo Connection Khuyến Nghị

Code mẫu này giúp tự động Reconnect và set config chuẩn nhất:

```typescript
import * as signalR from "@microsoft/signalr";

const API_GATEWAY_URL = "http://localhost:5000"; // Hoặc URL Gateway trên server

export class RealtimeClient {
  public chatConnection: signalR.HubConnection | null = null;
  public notifyConnection: signalR.HubConnection | null = null;

  async startConnections(accessToken: string) {
    // 1. Khởi tạo Chat Connect (Cho Chat Service)
    this.chatConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_GATEWAY_URL}/hubs/chat`, {
        accessTokenFactory: () => accessToken,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000]) // Retry ngay lập tức, 2s, 10s, 30s
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // 2. Khởi tạo Notify Connect (Cho Realtime Service)
    this.notifyConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_GATEWAY_URL}/hubs/realtime`, {
        accessTokenFactory: () => accessToken,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect()
      .build();

    // Start
    try {
      await Promise.all([
        this.chatConnection.start(),
        this.notifyConnection.start()
      ]);
      console.log("✅ Connect cả 2 Hubs (Chat & Realtime) thành công!");
    } catch (e) {
      console.error("❌ Kết nối Hub thất bại:", e);
    }
  }
}

export const realtimeClient = new RealtimeClient();
```

---

## 4. Các Hàm Và Event Cần Lắng Nghe (Chat Hub)

Trong Component Chat, bạn làm tương tự format này. Tham khảo các phương thức ở phía BE `/hubs/chat`:

```typescript
// 1. Join Room khi mở khung chat 1-1 (để hệ thống báo đã đọc tin nhắn)
await realtimeClient.chatConnection?.invoke("JoinRoom", roomId);

// 2. Gửi tin nhắn
// Lưu ý: Chỉ cần gọi hàm này, KHÔNG cần phải gọi Rest API Create Message. Hàm này sẽ tự động lưu DB và broadcast.
await realtimeClient.chatConnection?.invoke("SendMessage", roomId, "Nội dung tin nhắn");

// 3. Lắng nghe tin nhắn mới nhận
realtimeClient.chatConnection?.on("ReceiveMessage", (messageDto) => {
    console.log("Có tin nhắn mới", messageDto);
    // Cập nhật mảng message trong State (UI)
});

// 4. Lắng nghe người kia đã view room (Đã xem tin nhắn)
realtimeClient.chatConnection?.on("MessagesRead", (data) => {
    // data = { roomId: 1, userId: 2, messageIds: [21,22] }
    // Update trạng thái message thành READ (Hiển thị tick xanh)
});

// 5. Cập nhật Sidebar/Danh Sách Room khi nhận tin (Cho người không ở trong room chat hiện tại)
realtimeClient.chatConnection?.on("RoomUpdated", (roomSummaryData) => {
    console.log("Room có update tin nhắn cuối/unread count: ", roomSummaryData);
    // roomSummaryData = { roomId, profileId, lastContent, lastAt, unreadCount } 
});

// 6. Rời khỏi room hiện tại
await realtimeClient.chatConnection?.invoke("LeaveRoom", roomId);
```

---

## 5. Các Lỗi Phổ Biến & Cách Sửa Nhanh

- **Lỗi 404:** Hiện tại BE đã thêm URL proxy vô Gateway nên lỗi này sẽ hết. Đảm bảo bạn đang trỏ tới đúng URL của API Gateway (Project ApiGateway - Port mặc định 5000), không phải cổng lẻ của Service Connection (8080).
- **Lỗi Connection Disconnected/NegotiationFailed:** Chắc chắn bạn đã gửi đúng Token Bearer dạng `accessTokenFactory: () => accessToken`.
- **Lưu ý Send Message:** Đừng dùng Rest API `POST /api/connection/message` để gửi tin trên UI real-time. Bạn cứ mạnh dạn `invoke("SendMessage", roomId, content)`. Service sẽ tự động lưu và broadcast mọi thứ.
