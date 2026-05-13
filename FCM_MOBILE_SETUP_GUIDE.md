# Firebase Cloud Messaging (FCM) - Mobile App Integration Guide

## Overview

This guide provides step-by-step instructions for integrating Firebase Cloud Messaging (FCM) into the mobile app. The backend has implemented dual-channel notifications: real-time (SignalR) when app is online and push notifications (FCM) when app is offline.

**Status:** Backend Phase 1-5 complete and production ready. Chat FCM implemented (May 5, 2026). Mobile team implements Phase 3 (mobile integration).

**Last Updated:** 2026-05-05 (Chat message FCM support added)
**Service Status:** ✅ All FCM endpoints working + Chat messaging enabled
**Current Production URL:** https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io

### 📝 Recent Updates to This Guide

**NEW:** Chat Message FCM Notifications Implemented (May 5, 2026)
- Chat messages now sent via push notification when app closed
- Message batching: Multiple messages → Single notification
- Deep linking to chat rooms included
- See [Chat Message Notifications](#-chat-message-notifications-new--may-2026) section
- Mobile: Just add chat message handlers, no setup changes needed!

**NEW:** Endpoint now supports **anonymous device token registration** without JWT
- Can register tokens before user logs in
- Pass `userId` in request body for pre-login registration
- Use Bearer token for post-login registration
- See [Authentication Scenarios](#authentication-scenarios-for-device-token-registration) section

**FIXED:** Flutter code examples corrected
- Delete endpoint path now correct
- Token refresh handling improved
- Both anonymous and authenticated flows documented

**REPLACED:** All `{API_GATEWAY}` placeholders with actual production URL

---

## Architecture

```
Backend (Notification Service)
    ├─ Save notification to database
    ├─ Send FCM push notification
    └─ Publish SignalR real-time event

Mobile App
    ├─ Register FCM token at startup
    ├─ Listen for FCM messages (background + foreground)
    ├─ Display notification in tray
    └─ Handle user tap (deep link to details)
```

---

## 🆕 Chat Message Notifications (NEW - May 2026)

### Overview

As of May 5, 2026, the backend now sends **Firebase Cloud Messaging (FCM) push notifications for chat messages** when the app is closed. This complements the existing real-time SignalR delivery for when the app is online.

**Key Features:**
- ✅ Chat messages delivered even when app is closed
- ✅ No message loss - messages arrive via push notification
- ✅ Automatic message batching: Multiple messages within 2 seconds → Single notification
- ✅ Deep linking: Tapping notification opens correct chat room
- ✅ **NO CODE CHANGES NEEDED** - Just add notification handlers!

### How It Works

1. **Message Sent:** User A sends message to User B
2. **Backend Processing:** Message batched with others (2-second window)
3. **Dual Delivery:**
   - If app is OPEN → SignalR realtime delivery (instant)
   - If app is CLOSED → FCM push notification (offline)
4. **User Receives:** Push notification in notification tray (even if app closed)
5. **User Action:** Tapping notification opens app to correct chat room

### Message Batching

The backend intelligently batches messages for each recipient within a 2-second window:

- **1 message:** Push shows sender name
  - Title: "Message from John Doe"
  - Body: "How are you?"

- **3-5 messages:** Aggregated notification
  - Title: "New Messages"
  - Body: "You have 3 new messages"

- **5+ messages:** Single aggregated notification
  - Title: "New Messages"
  - Body: "You have 8 new messages"

**Benefit:** Reduces notification spam while ensuring messages aren't missed.

### Notification Types

#### Type 1: Aggregated Message Notification
Sent for multiple messages within the 2-second window:

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

#### Type 2: Single Message Notification
Sent for the first message or when appropriate:

```json
{
  "notification": {
    "title": "Message from John Doe",
    "body": "Hey, how are you?"
  },
  "data": {
    "type": "chat_message",
    "messageId": "123",
    "roomId": "45",
    "senderName": "John Doe",
    "deepLink": "app://chat/45"
  }
}
```

### Mobile Implementation Requirements

#### For Flutter
Add handlers for chat notification types:

```dart
static Future<void> _handleForegroundMessage(RemoteMessage message) async {
  final messageType = message.data['type'];
  
  // Handle aggregated notifications
  if (messageType == 'aggregated_messages') {
    int count = int.parse(message.data['messageCount'] ?? '1');
    print('Received $count new messages');
    // Navigate to chat list
    _navigateToChatList();
  }
  
  // Handle single message notifications
  if (messageType == 'chat_message') {
    String roomId = message.data['roomId'] ?? '';
    String senderName = message.data['senderName'] ?? '';
    print('Message from $senderName in room $roomId');
    // Navigate to specific chat room
    _navigateToChatRoom(roomId: int.parse(roomId));
  }
  
  // Existing notification display code...
}

static Future<void> _handleNotificationTap(NotificationResponse response) async {
  final deepLink = response.payload;
  
  if (deepLink != null && deepLink.contains('app://chat')) {
    // Extract room ID from deep link (e.g., "app://chat/45" → "45")
    if (deepLink.contains('app://chat/')) {
      final roomId = deepLink.split('/').last;
      _navigateToChatRoom(roomId: int.parse(roomId));
    } else {
      // Navigate to chat list
      _navigateToChatList();
    }
  }
}
```

#### For React Native
Add handlers for chat notification types:

```javascript
async function handleForegroundMessage(remoteMessage) {
  const messageType = remoteMessage.data?.type;
  
  // Handle aggregated notifications
  if (messageType === 'aggregated_messages') {
    const count = parseInt(remoteMessage.data?.messageCount ?? '1');
    console.log(`Received ${count} new messages`);
    // Navigate to chat list
    navigateToChatList();
  }
  
  // Handle single message notifications
  if (messageType === 'chat_message') {
    const roomId = remoteMessage.data?.roomId ?? '';
    const senderName = remoteMessage.data?.senderName ?? '';
    console.log(`Message from ${senderName} in room ${roomId}`);
    // Navigate to specific chat room
    navigateToChatRoom({ roomId: parseInt(roomId) });
  }
  
  // Existing notification display code...
}

async function handleNotificationTap(remoteMessage) {
  const deepLink = remoteMessage.data?.deepLink;
  
  if (deepLink?.includes('app://chat')) {
    // Extract room ID from deep link
    if (deepLink.includes('app://chat/')) {
      const roomId = deepLink.split('/').pop();
      navigateToChatRoom({ roomId: parseInt(roomId) });
    } else {
      // Navigate to chat list
      navigateToChatList();
    }
  }
}
```

### Testing Chat Notifications

#### Test 1: Single Message
1. Register device token from mobile app
2. Send 1 message to this user from another device
3. Close the receiving app
4. Send the message
5. **Expected:** Push notification appears with sender name
6. **Action:** Tap notification
7. **Expected:** App opens directly to chat with sender

#### Test 2: Message Batching
1. Close the receiving app
2. Send 5 messages from another device within 2 seconds
3. **Expected:** Single push notification appears (not 5!)
   - Title: "New Messages"
   - Body: "You have 5 new messages"
4. **Action:** Tap notification
5. **Expected:** App opens to chat list (or chat room if in deep-link)
6. **Action:** Open chat with sender
7. **Expected:** All 5 messages are there (no data loss)

#### Test 3: Deep Linking
1. App is closed
2. Another user sends message in a specific room (e.g., Room #45)
3. **Expected:** Push notification arrives
4. **Action:** Tap notification
5. **Expected:** App opens AND navigates directly to Room #45
6. **Expected:** New message is visible

#### Test 4: Realtime vs Offline
1. **With app open:**
   - Message appears instantly in chat (SignalR realtime)
   - No push notification (already online)
2. **With app closed:**
   - Message arrives via push notification
   - User taps notification
   - App opens with message visible

### Important Notes

✅ **Already Works** - Backend sends FCM automatically  
✅ **No Configuration Changes** - Device token registration unchanged  
✅ **No New Endpoints** - Mobile just receives notifications  
✅ **Backward Compatible** - Realtime (SignalR) still works  
❌ **Not Optional** - Chat messages are now sent via FCM when app closed

### Troubleshooting

**Problem:** Push notifications not appearing
- **Solution 1:** Verify device token is registered (check device-tokens endpoint)
- **Solution 2:** Check app notification permissions (Android/iOS)
- **Solution 3:** Check Firebase Console → Cloud Messaging enabled

**Problem:** Deep linking not working
- **Solution:** Verify deep link URLs in notification payload match your app's deep link configuration
- **Solution:** Use FirebaseDeepLinkingPlugin or similar for routing

**Problem:** Messages appearing multiple times
- **Solution:** Ensure your notification handler checks for duplicate notifications
- **Solution:** Store message IDs to deduplicate if needed

---

## Prerequisites

- Flutter or React Native project set up
- Access to Firebase Console
- Google Play Services (Android) or APNs certificate (iOS)
- Backend notification service running
  - **Production:** `https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
  - **Staging:** Contact backend team for staging URL
  - **Development:** `http://localhost:5000`

---

## Firebase Project Setup

### Step 1: Create/Access Firebase Project (Backend Team)

**Backend has provided:**
- Firebase Project ID: `skillsnap-notification`
- Service account JSON key (stored in Azure Key Vault)
- Android package name: `com.skillsnap.app`
- iOS bundle ID: `com.skillsnap.app` (iOS support TBA)

**Contact backend team to:**
- Get Firebase configuration for your environment
- Access the Firebase project in Firebase Console
- Retrieve google-services.json and GoogleService-Info.plist

### Step 2: Register App in Firebase Console

**For Android:**
1. Go to Firebase Console (https://console.firebase.google.com/)
2. Select project: `skillsnap-notification`
3. Click "Add App" → Android
4. Enter package name: `com.skillsnap.app`
5. Download `google-services.json`
6. Place in `android/app/` directory

**For iOS:**
1. Go to Firebase Console (https://console.firebase.google.com/)
2. Select project: `skillsnap-notification`
3. Click "Add App" → iOS
4. Enter bundle ID: `com.skillsnap.app`
5. Download `GoogleService-Info.plist`
6. Add to Xcode project

---

## Flutter Implementation

### Step 1: Add Dependencies

```yaml
# pubspec.yaml
dependencies:
  firebase_core: ^2.24.0
  firebase_messaging: ^14.7.0
  flutter_local_notifications: ^15.1.0
  shared_preferences: ^2.2.0
```

### Step 2: Initialize Firebase

```dart
// main.dart
import 'package:firebase_core/firebase_core.dart';
import 'firebase_options.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  await Firebase.initializeApp(
    options: DefaultFirebaseOptions.currentPlatform,
  );
  
  await _initializeNotifications();
  
  runApp(const MyApp());
}

Future<void> _initializeNotifications() async {
  // Initialize local notifications for displaying push messages
  const AndroidInitializationSettings androidSettings = 
    AndroidInitializationSettings('@mipmap/ic_launcher');
  
  const DarwinInitializationSettings iosSettings = 
    DarwinInitializationSettings();
  
  final InitializationSettings settings = InitializationSettings(
    android: androidSettings,
    iOS: iosSettings,
  );
  
  await FlutterLocalNotificationsPlugin().initialize(settings);
}
```

### Step 3: Register Device Token

```dart
// notification_service.dart
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:dio/dio.dart';

class NotificationService {
  static final FirebaseMessaging _messaging = FirebaseMessaging.instance;
  static const String _tokenEndpoint = 
    '{API_GATEWAY}/api/device-tokens/register';
  static const String _tokenStorageKey = 'fcm_device_token';
  
  static Future<void> initialize() async {
    // Request iOS notification permission
    await _messaging.requestPermission(
      alert: true,
      announcement: false,
      badge: true,
      carplay: false,
      criticalAlert: false,
      provisional: false,
      sound: true,
    );
    
    // Get initial token
    final token = await _messaging.getToken();
    if (token != null) {
      await _registerToken(token);
    }
    
    // Listen for token refresh
    _messaging.onTokenRefresh.listen((newToken) async {
      await _registerToken(newToken);
    });
    
    // Handle foreground messages
    FirebaseMessaging.onMessage.listen(_handleForegroundMessage);
    
    // Handle background message tap (when app opened from notification)
    FirebaseMessaging.onMessageOpenedApp.listen(_handleNotificationTap);
  }
  
  static Future<void> _registerToken(String token) async {
    try {
      // Check if token already registered
      final prefs = await SharedPreferences.getInstance();
      final savedToken = prefs.getString(_tokenStorageKey);
      
      if (savedToken == token) {
        print('Token already registered');
        return;
      }
      
      // Get auth token from storage
      final authToken = prefs.getString('auth_token');
      
      // Two scenarios:
      // 1. If auth token exists, use authenticated registration
      // 2. If no auth token, use anonymous registration with userId
      
      final dio = Dio();
      
      late final Map<String, dynamic> requestData;
      
      if (authToken != null && authToken.isNotEmpty) {
        // Scenario 1: Authenticated (user logged in)
        dio.options.headers['Authorization'] = 'Bearer $authToken';
        
        requestData = {
          'token': token,
          'deviceType': _getDeviceType(),
          'appVersion': '1.0.0',
        };
      } else {
        // Scenario 2: Anonymous registration (before user logs in)
        // Try to get userId from storage or preferences
        final userId = prefs.getString('user_id');
        
        if (userId == null || userId.isEmpty) {
          print('Cannot register device token: no auth token and no user ID');
          return;
        }
        
        requestData = {
          'token': token,
          'userId': userId,  // Include userId for anonymous registration
          'deviceType': _getDeviceType(),
          'appVersion': '1.0.0',
        };
      }
      
      // Register with backend
      final response = await dio.post(
        '{API_GATEWAY}/api/device-tokens/register',
        data: requestData,
      );
      
      if (response.statusCode == 200) {
        await prefs.setString(_tokenStorageKey, token);
        print('Device token registered successfully');
      }
    } catch (e) {
      print('Error registering device token: $e');
    }
  }
  
  static String _getDeviceType() {
    // Platform.isAndroid ? 'Android' : 'iOS'
    import 'dart:io' show Platform;
    return Platform.isAndroid ? 'Android' : 'iOS';
  }
  
  static Future<void> _handleForegroundMessage(RemoteMessage message) async {
    print('Received foreground message: ${message.notification?.title}');
    
    // Show local notification
    const AndroidNotificationDetails androidDetails = 
      AndroidNotificationDetails(
        'skillsnap_notifications',
        'SkillSnap Notifications',
        channelDescription: 'Notifications from SkillSnap',
        importance: Importance.high,
        priority: Priority.high,
      );
    
    const DarwinNotificationDetails iosDetails = 
      DarwinNotificationDetails(
        presentAlert: true,
        presentBadge: true,
        presentSound: true,
      );
    
    await FlutterLocalNotificationsPlugin().show(
      message.hashCode,
      message.notification?.title,
      message.notification?.body,
      NotificationDetails(
        android: androidDetails,
        iOS: iosDetails,
      ),
      payload: message.data.toString(),
    );
  }
  
  static Future<void> _handleNotificationTap(RemoteMessage message) async {
    print('User tapped notification: ${message.data}');
    
    // Navigate based on deep link
    final deepLink = message.data['deepLink'];
    if (deepLink != null) {
      // Use your navigation package to handle deep link
      // Example: GoRouter or Firebase Dynamic Links
      // navigatorKey.currentState?.pushNamed(deepLink);
    }
  }
  
  // Call when user logs out
  static Future<void> cleanup() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString(_tokenStorageKey);
      
      if (token != null) {
        // Unregister token from backend
        final authToken = prefs.getString('auth_token');
        if (authToken != null) {
          final dio = Dio();
          dio.options.headers['Authorization'] = 'Bearer $authToken';
          
          // DELETE /api/device-tokens/register/{token}
          await dio.delete(
            '{API_GATEWAY}/api/device-tokens/register/$token',
          );
        }
      }
      
      await prefs.remove(_tokenStorageKey);
    } catch (e) {
      print('Error cleaning up device token: $e');
    }
  }
}
```

### Step 4: Handle Background Messages

```dart
// main.dart (top level)
@pragma('vm:entry-point')
Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
  
  print('Handling background message: ${message.messageId}');
  
  // Show notification
  const AndroidNotificationDetails androidDetails = 
    AndroidNotificationDetails(
      'skillsnap_notifications',
      'SkillSnap Notifications',
      channelDescription: 'Notifications from SkillSnap',
    );
  
  const DarwinNotificationDetails iosDetails = 
    DarwinNotificationDetails();
  
  await FlutterLocalNotificationsPlugin().show(
    message.hashCode,
    message.notification?.title,
    message.notification?.body,
    NotificationDetails(
      android: androidDetails,
      iOS: iosDetails,
    ),
    payload: message.data.toString(),
  );
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  await Firebase.initializeApp();
  
  // Set background message handler
  FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);
  
  await NotificationService.initialize();
  
  runApp(const MyApp());
}
```

### Step 5: Call in App Lifecycle

```dart
// app.dart or main navigation file
class MyApp extends StatefulWidget {
  const MyApp({Key? key}) : super(key: key);

  @override
  State<MyApp> createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  @override
  void initState() {
    super.initState();
    NotificationService.initialize();
  }
  
  @override
  void dispose() {
    // Optional: cleanup when app closes completely
    // NotificationService.cleanup();
    super.dispose();
  }
  
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      // ... your app config
    );
  }
}
```

---

## React Native Implementation

### Step 1: Add Dependencies

```bash
npm install @react-native-firebase/app @react-native-firebase/messaging react-native-notifee
# or
yarn add @react-native-firebase/app @react-native-firebase/messaging react-native-notifee
```

### Step 2: Initialize Firebase

```javascript
// index.js or main entry point
import messaging from '@react-native-firebase/messaging';
import notifee from '@react-native-notifee/react-native-notifee';

// Request permissions
async function requestNotificationPermissions() {
  const granted = await messaging().requestPermission();
  if (granted) {
    console.log('Notification permissions granted');
  }
}

// Initialize
export async function initializeNotifications() {
  await requestNotificationPermissions();
  
  // Get initial token
  const token = await messaging().getToken();
  await registerDeviceToken(token);
  
  // Listen for token refresh
  messaging().onTokenRefresh(async (token) => {
    await registerDeviceToken(token);
  });
  
  // Handle foreground messages
  messaging().onMessage(async (remoteMessage) => {
    await handleForegroundMessage(remoteMessage);
  });
  
  // Handle background message tap
  messaging().onNotificationOpenedApp(async (remoteMessage) => {
    await handleNotificationTap(remoteMessage);
  });
}
```

### Step 3: Register Device Token

```javascript
// notificationService.js
import AsyncStorage from '@react-native-async-storage/async-storage';
import axios from 'axios';

const TOKEN_ENDPOINT = '{API_GATEWAY}/api/device-tokens/register';
const TOKEN_STORAGE_KEY = 'fcm_device_token';

export async function registerDeviceToken(token) {
  try {
    // Check if already registered
    const savedToken = await AsyncStorage.getItem(TOKEN_STORAGE_KEY);
    if (savedToken === token) {
      console.log('Token already registered');
      return;
    }
    
    // Get auth token
    const authToken = await AsyncStorage.getItem('auth_token');
    
    // Two scenarios:
    // 1. If auth token exists, use authenticated registration
    // 2. If no auth token, use anonymous registration with userId
    
    let requestData;
    const requestConfig = {
      headers: {
        'Content-Type': 'application/json',
      }
    };
    
    if (authToken) {
      // Scenario 1: Authenticated (user logged in)
      requestConfig.headers['Authorization'] = `Bearer ${authToken}`;
      
      requestData = {
        token: token,
        deviceType: Platform.OS === 'android' ? 'Android' : 'iOS',
        appVersion: '1.0.0',
      };
    } else {
      // Scenario 2: Anonymous registration (before user logs in)
      const userId = await AsyncStorage.getItem('user_id');
      
      if (!userId) {
        console.log('Cannot register device token: no auth token and no user ID');
        return;
      }
      
      requestData = {
        token: token,
        userId: userId,  // Include userId for anonymous registration
        deviceType: Platform.OS === 'android' ? 'Android' : 'iOS',
        appVersion: '1.0.0',
      };
    }
    
    // Register with backend
    const response = await axios.post(
      '{API_GATEWAY}/api/device-tokens/register',
      requestData,
      requestConfig
    );
    
    if (response.status === 200) {
      await AsyncStorage.setItem(TOKEN_STORAGE_KEY, token);
      console.log('Device token registered successfully');
    }
  } catch (error) {
    console.error('Error registering device token:', error);
  }
}

export async function handleForegroundMessage(remoteMessage) {
  console.log('Foreground message:', remoteMessage);
  
  // Create notification channel (Android)
  const channelId = await notifee.createChannel({
    id: 'skillsnap_notifications',
    name: 'SkillSnap Notifications',
    importance: AndroidImportance.HIGH,
    sound: 'default',
  });
  
  // Display notification
  await notifee.displayNotification({
    title: remoteMessage.notification?.title,
    body: remoteMessage.notification?.body,
    data: remoteMessage.data,
    android: {
      channelId: channelId,
      pressAction: {
        id: 'default',
      },
    },
    ios: {
      sound: 'default',
    },
  });
}

export async function handleNotificationTap(remoteMessage) {
  console.log('User tapped notification:', remoteMessage.data);
  
  // Navigate based on deep link
  const deepLink = remoteMessage.data?.deepLink;
  if (deepLink) {
    // Use your navigation package (React Navigation, etc.)
    // navigationRef.navigate('NotificationDetails', { id: notificationId });
  }
}

export async function cleanup() {
  try {
    const token = await AsyncStorage.getItem(TOKEN_STORAGE_KEY);
    if (token) {
      const authToken = await AsyncStorage.getItem('auth_token');
      if (authToken) {
        await axios.delete(`${TOKEN_ENDPOINT}/${token}`, {
          headers: {
            'Authorization': `Bearer ${authToken}`,
          }
        });
      }
    }
    await AsyncStorage.removeItem(TOKEN_STORAGE_KEY);
  } catch (error) {
    console.error('Error cleaning up device token:', error);
  }
}
```

### Step 4: Initialize in App

```javascript
// App.js
import { useEffect } from 'react';
import { initializeNotifications } from './notificationService';

export default function App() {
  useEffect(() => {
    initializeNotifications();
  }, []);
  
  return (
    // ... your app
  );
}
```

---

## API Endpoints for Mobile

### Device Token Registration

**Endpoint:** `POST /api/device-tokens/register`

**Authentication:** ⚠️ OPTIONAL (Not Required)
This endpoint allows **both authenticated and anonymous** device token registration.

**Option 1: Anonymous Registration (No JWT required)**
Use this when registering token before user logs in.

```
Headers:
Content-Type: application/json

Request Body:
{
  "token": "fcm_device_token_from_firebase",
  "deviceType": "Android",
  "appVersion": "1.0.0",
  "userId": "user_id_here"  // REQUIRED for anonymous registration
}

Response:
{
  "success": true,
  "message": "Device token registered successfully"
}
```

**Option 2: Authenticated Registration (With JWT)**
Use this when user is already logged in.

```
Headers:
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json

Request Body:
{
  "token": "fcm_device_token_from_firebase",
  "deviceType": "Android",
  "appVersion": "1.0.0"
  // userId extracted from JWT token automatically
}

Response:
{
  "success": true,
  "message": "Device token registered successfully"
}
```

**Error Response (if userId not provided and not authenticated):**
```json
{
  "error": "User ID is required (either from token or in request body)"
}
```

### Device Token Unregistration

**Endpoint:** `DELETE /api/device-tokens/register/{token}`

**Headers:**
```
Authorization: Bearer {JWT_TOKEN}
```

**Response:**
```json
{
  "success": true,
  "message": "Device token unregistered successfully"
}
```

### Get Notification Settings

**Endpoint:** `GET /api/device-tokens/settings`

**Headers:**
```
Authorization: Bearer {JWT_TOKEN}
```

**Response:**
```json
{
  "pushNotificationsEnabled": true,
  "soundEnabled": true,
  "vibrateEnabled": true,
  "chatNotificationsEnabled": true,
  "mentionNotificationsEnabled": true
}
```

### Update Notification Settings

**Endpoint:** `PUT /api/device-tokens/settings`

**Headers:**
```
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "pushNotificationsEnabled": true,
  "soundEnabled": true,
  "vibrateEnabled": true,
  "chatNotificationsEnabled": true,
  "mentionNotificationsEnabled": true
}
```

**Response:**
```json
{
  "success": true,
  "message": "Notification settings updated successfully"
}
```

---

## Push Notification Payload Format

When user receives a push notification, the data structure is:

```json
{
  "notification": {
    "title": "User A liked your post",
    "body": "3 people liked your post in total"
  },
  "data": {
    "notificationId": "123",
    "notificationType": "POST_FAVORITE",
    "actorId": "user456",
    "objectId": "post789",
    "deepLink": "app://notification/123",
    "eventId": "event123"
  },
  "android": {
    "priority": "high"
  },
  "apns": {
    "headers": {
      "apns-priority": "10"
    }
  }
}
```

---

## Authentication Scenarios for Device Token Registration

The `POST /api/device-tokens/register` endpoint supports two authentication approaches to handle different app lifecycle stages.

### Scenario 1: Pre-Login Token Registration (Anonymous)

**When:** User hasn't logged in yet but app is starting
**Use:** Anonymous registration with `userId` in request body

**Flow:**
```
App Start → Get FCM token → Check local storage for userId → Register anonymously
```

**Example (Flutter):**
```dart
// Register before user logs in
final userId = 'user123'; // Could come from local storage, QR code, etc.
final token = await FirebaseMessaging.instance.getToken();

final dio = Dio();
final response = await dio.post(
  'https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/device-tokens/register',
  data: {
    'token': token,
    'userId': userId,  // Required for anonymous registration
    'deviceType': 'Android',
    'appVersion': '1.0.0',
  },
);
```

**Example (React Native):**
```javascript
// Register before user logs in
const userId = 'user123';
const token = await messaging().getToken();

const response = await axios.post(
  'https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/device-tokens/register',
  {
    token: token,
    userId: userId,  // Required for anonymous registration
    deviceType: Platform.OS === 'android' ? 'Android' : 'iOS',
    appVersion: '1.0.0',
  }
);
```

### Scenario 2: Post-Login Token Registration (Authenticated)

**When:** User has logged in and JWT token is available
**Use:** Authenticated registration with Bearer token

**Flow:**
```
User Login → Save JWT → Re-register FCM token with JWT → userId extracted from token
```

**Example (Flutter):**
```dart
// Register after user logs in
final authToken = prefs.getString('auth_token'); // JWT from login
final token = await FirebaseMessaging.instance.getToken();

final dio = Dio();
dio.options.headers['Authorization'] = 'Bearer $authToken';

final response = await dio.post(
  'https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/device-tokens/register',
  data: {
    'token': token,
    'deviceType': 'Android',
    'appVersion': '1.0.0',
    // userId is extracted from JWT token automatically
  },
);
```

**Example (React Native):**
```javascript
// Register after user logs in
const authToken = await AsyncStorage.getItem('auth_token'); // JWT from login
const token = await messaging().getToken();

const response = await axios.post(
  'https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/device-tokens/register',
  {
    token: token,
    deviceType: Platform.OS === 'android' ? 'Android' : 'iOS',
    appVersion: '1.0.0',
    // userId is extracted from JWT token automatically
  },
  {
    headers: {
      'Authorization': `Bearer ${authToken}`,
    }
  }
);
```

### Scenario 3: Token Refresh During App Lifecycle

**When:** Firebase refreshes the token while app is running
**Use:** Current authentication method (whatever was used before)

**Example (Flutter):**
```dart
// Listen for token refresh at any time
FirebaseMessaging.instance.onTokenRefresh.listen((newToken) async {
  // Get current auth token if available
  final prefs = await SharedPreferences.getInstance();
  final authToken = prefs.getString('auth_token');
  
  final dio = Dio();
  
  if (authToken != null) {
    // User is logged in, use authenticated registration
    dio.options.headers['Authorization'] = 'Bearer $authToken';
    
    await dio.post(
      'https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/device-tokens/register',
      data: {
        'token': newToken,
        'deviceType': 'Android',
        'appVersion': '1.0.0',
      },
    );
  } else {
    // User not logged in, use anonymous registration
    final userId = prefs.getString('user_id');
    if (userId != null) {
      await dio.post(
        'https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/device-tokens/register',
        data: {
          'token': newToken,
          'userId': userId,
          'deviceType': 'Android',
          'appVersion': '1.0.0',
        },
      );
    }
  }
});
```

---

### Flutter Deep Link Setup

**android/app/src/main/AndroidManifest.xml:**
```xml
<intent-filter>
  <action android:name="android.intent.action.VIEW" />
  <category android:name="android.intent.category.DEFAULT" />
  <category android:name="android.intent.category.BROWSABLE" />
  <data android:scheme="app" android:host="notification" />
</intent-filter>
```

**iOS Info.plist:**
```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>app</string>
    </array>
  </dict>
</array>
```

### React Native Deep Link Setup

Use `react-native-deep-linking` or similar package and configure routes to handle `app://notification/{notificationId}` deep links.

---

## Testing

### Manual Testing

1. **Token Registration:**
   - Register device token via `/api/device-tokens/register`
   - Verify token stored in database (DEVICE_TOKENS table)

2. **Send Notification:**
   - Create notification event in backend
   - Verify push notification appears on device

3. **Notification Tap:**
   - Tap notification from tray
   - Verify deep link works and app navigates correctly

### Automated Testing

```dart
// Flutter example
test('Device token registration', () async {
  final service = NotificationService();
  final token = 'test_token_12345';
  
  await service.registerToken(token);
  
  // Verify token was registered with backend
  // Check SharedPreferences
});
```

---

## Troubleshooting

### Token Not Registered

**Problem:** Device token not appearing in DEVICE_TOKENS table

**Solutions:**
- Check auth token is valid (must be JWT)
- Verify device has internet connection
- Check app has notification permissions granted
- Review backend logs for registration errors

### Notification Not Received

**Problem:** Push notification not appearing on device

**Solutions:**
- Verify device token is active and valid
- Check Firebase project configuration
- Verify notification was sent (check PUSH_NOTIFICATION_LOG table)
- Check device battery saver or do-not-disturb settings
- Verify app has notification channel configured (Android)

### Token Refresh Issues

**Problem:** Token not refreshing when Firebase refreshes it

**Solutions:**
- Ensure you're listening to `onTokenRefresh` event
- Verify listener is registered before Firebase initializes
- Check for race conditions in token registration

---

## Best Practices

1. **Token Management:**
   - Register token on app startup
   - Re-register on app resume (in case token expired)
   - Unregister on logout

2. **Notification Display:**
   - Show local notification even if app is in foreground
   - Use proper notification channels (Android)
   - Respect user settings before displaying

3. **Deep Linking:**
   - Always validate deep link data
   - Handle malformed deep links gracefully
   - Test deep linking on both platforms

4. **Error Handling:**
   - Catch all network errors during registration
   - Log all errors for debugging
   - Implement retry logic with exponential backoff

5. **Performance:**
   - Don't block main thread during registration
   - Use async/await properly
   - Cache auth tokens securely

---

## Support

For issues or questions:
- Check backend logs for registration/sending errors
- Review Firebase console for token delivery status
- Check database DEVICE_TOKENS table for token registration
- Contact backend team for API issues

Backend Status: Phase 1-5 Complete ✅
Mobile Status: Phase 3 Ready for Implementation 🟢

---

## Important Notes for Mobile Team

### What Changed Recently (May 5, 2026)

1. **POST /api/device-tokens/register is now [AllowAnonymous]**
   - No longer requires JWT token
   - Can pass `userId` in request body
   - Enables registration before user logs in
   - See [Authentication Scenarios](#authentication-scenarios-for-device-token-registration)

2. **Code Examples Updated**
   - Flutter code now handles both auth and anonymous scenarios
   - React Native code updated with userId support
   - Token refresh now properly maintains authentication state

3. **Production URL Confirmed**
   - All endpoints now use actual production URL
   - No more {API_GATEWAY} placeholders
   - Replace with local URL for development

### Pre-Integration Checklist

Before starting mobile implementation:

- [ ] Read [Architecture](#architecture) section
- [ ] Review [Authentication Scenarios](#authentication-scenarios-for-device-token-registration)
- [ ] Understand the two registration flows (pre-login + post-login)
- [ ] Test both authentication approaches on staging
- [ ] Verify firebase_messaging/FCM dependencies installed
- [ ] Test notification reception in foreground + background
- [ ] Implement deep linking to notification details
- [ ] Test on real Android device (emulator FCM issues common)

### Common Issues & Solutions

**Issue:** POST returns 400 "User ID is required"
- **Solution:** Ensure you're passing `userId` in request body for anonymous registration OR using valid Bearer token

**Issue:** Endpoint returns 401 Unauthorized
- **Solution:** Check if endpoint expects [Authorize] (all except POST). See endpoint docs.

**Issue:** Notifications not appearing
- **Solution:** Verify device token registered successfully first. Check DEVICE_TOKENS table.

**Issue:** App crashes on startup
- **Solution:** Ensure firebase_core initialized BEFORE NotificationService

---
