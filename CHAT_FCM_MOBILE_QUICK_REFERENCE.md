# Chat FCM Mobile Implementation - Quick Reference

**Status:** ✅ Backend deployed, 📱 Mobile implementation ready

---

## 📖 What Was Updated

File: `FCM_MOBILE_SETUP_GUIDE.md`  
Change: Added comprehensive Chat Message FCM section (230+ lines)  
Result: Mobile team now has full implementation guide

---

## 🎯 What Mobile Needs to Do

### 1. Add Two Notification Handlers

**Handler 1: Aggregated Messages** (Multiple messages)
```dart
if (message.data['type'] == 'aggregated_messages') {
  int count = int.parse(message.data['messageCount'] ?? '1');
  print('You have $count new messages');
  _navigateToChatList();
}
```

**Handler 2: Single Message** (One message or first in batch)
```dart
if (message.data['type'] == 'chat_message') {
  String roomId = message.data['roomId'] ?? '';
  String senderName = message.data['senderName'] ?? '';
  print('Message from $senderName');
  _navigateToChatRoom(roomId: int.parse(roomId));
}
```

### 2. Implement Deep Linking

When user taps notification:
```dart
final deepLink = remoteMessage.data['deepLink'];

if (deepLink?.contains('app://chat/') ?? false) {
  // Extract room ID: "app://chat/45" → "45"
  final roomId = deepLink.split('/').last;
  _navigateToChatRoom(roomId: int.parse(roomId));
} else if (deepLink == 'app://chat') {
  _navigateToChatList();
}
```

### 3. Test the Implementation

**Test Case 1: Single Message**
- Verify: App closed → 1 message sent → Push notification appears
- Expected: Title has sender name, body has message preview
- Action: Tap notification → App opens to correct chat room

**Test Case 2: Multiple Messages**
- Verify: App closed → 5 messages sent within 2 seconds → Single notification
- Expected: "You have 5 new messages" (not 5 notifications!)
- Action: Tap → Opens chat list or chat room

**Test Case 3: Deep Linking**
- Verify: Tap notification → App navigates correctly
- Expected: Aggregated → Chat list, Single → Chat room #45

**Test Case 4: App Open**
- Verify: App open → Message sent → No push (only realtime)
- Expected: Instant update via SignalR (no notification tray)

---

## 🔄 Backend Flow (Already Done)

```
User A sends message to User B
                ↓
Backend processes message
                ↓
Check: Is app closed?
    ├─ YES → Send FCM push (notification tray)
    └─ NO  → Send SignalR realtime (instant)
                ↓
Message batched (2-second window)
                ↓
Multiple messages? 
    ├─ YES → Aggregated notification ("You have 5 messages")
    └─ NO  → Single notification ("Message from John")
                ↓
Mobile app receives → Shows notification → User taps → App opens
```

---

## 📋 Notification Payload Examples

### Aggregated Message (Multiple)
```json
{
  "notification": {
    "title": "New Messages",
    "body": "You have 3 new messages"
  },
  "data": {
    "type": "aggregated_messages",
    "messageCount": "3",
    "deepLink": "app://chat"
  }
}
```

### Single Message
```json
{
  "notification": {
    "title": "Message from John Doe",
    "body": "Hey, how are you today?"
  },
  "data": {
    "type": "chat_message",
    "messageId": "msg-123",
    "roomId": "45",
    "senderName": "John Doe",
    "deepLink": "app://chat/45"
  }
}
```

---

## ✅ Checklist for Implementation

### Code Changes
- [ ] Add `aggregated_messages` handler to foreground listener
- [ ] Add `chat_message` handler to foreground listener
- [ ] Parse `roomId` from deep link
- [ ] Navigate to chat list or specific room
- [ ] Handle tap on notification (background)

### Testing
- [ ] Compile without errors
- [ ] Test with mock notifications
- [ ] Test with real Firebase tokens
- [ ] Verify navigation works
- [ ] Verify no duplicate messages

### Validation
- [ ] All 4 test cases pass
- [ ] No message loss
- [ ] Deep linking works
- [ ] Notification appears when app closed
- [ ] No notification when app open (realtime only)

---

## 🚫 What DOESN'T Change

✓ Device token registration (same endpoint, same flow)  
✓ Firebase configuration (same setup)  
✓ LocalNotification setup (same)  
✓ SignalR realtime (still works)  
✓ Authentication (unchanged)  

---

## 🎓 Reference Guide Location

**Full Guide:** `D:\Capstone\FCM_MOBILE_SETUP_GUIDE.md`

**Key Sections:**
- Lines 54-283: Chat Message Notifications (complete guide)
- Lines 154-189: Flutter code examples
- Lines 191-231: React Native code examples
- Lines 233-257: Testing procedures (4 test cases)
- Lines 268-281: Troubleshooting

---

## 💡 Key Concepts

### Message Batching (2-Second Window)
- Multiple messages within 2 seconds = 1 notification
- Reduces spam: 5 messages = 1 notification, not 5
- All messages preserved (not lost)

### Dual Delivery
- **App Open:** SignalR realtime (instant)
- **App Closed:** FCM push (notification tray)
- **App Minimized:** Both (realtime + push as backup)

### Deep Linking
- Aggregated: `app://chat` (no room ID)
- Single: `app://chat/{roomId}` (e.g., `app://chat/45`)

---

## 🆘 Common Issues & Solutions

**Issue:** Push notifications not appearing
- Check: Device token registered in app?
- Check: App notification permissions enabled?
- Check: Firebase Console → Cloud Messaging enabled?

**Issue:** Deep linking not working
- Check: Deep link URLs match your routing config?
- Check: GoRouter or navigation framework configured?

**Issue:** Messages appearing multiple times
- Check: Notification handler called twice?
- Solution: Store message IDs and deduplicate

---

## 📊 Implementation Timeline

1. ✅ Backend: Chat FCM deployed (May 5)
2. ⏳ Mobile: Implement handlers (this week)
3. ⏳ Mobile: Test with real notifications (this week)
4. ⏳ QA: Validate end-to-end (next week)
5. ⏳ Release: Mobile app with chat FCM (when ready)

---

## 🎉 Expected User Experience

**Before:** Chat messages lost when app closed  
**After:** Chat messages delivered via push even when app closed

**User Benefits:**
- No message loss
- Never miss a chat
- Smart notifications (not spammy)
- Deep linking to correct conversation
- Works with app closed, open, or minimized

---

**Ready for Implementation?** Check the full guide at `FCM_MOBILE_SETUP_GUIDE.md` section "Chat Message Notifications"
