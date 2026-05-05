# Firebase Cloud Messaging (FCM) - Mobile App Integration Guide

## Overview

This guide provides step-by-step instructions for integrating Firebase Cloud Messaging (FCM) into the mobile app. The backend has implemented dual-channel notifications: real-time (SignalR) when app is online and push notifications (FCM) when app is offline.

**Status:** Backend Phase 1 & 2 complete. Mobile team implements Phase 3.

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

## Prerequisites

- Flutter or React Native project set up
- Access to Firebase Console
- Google Play Services (Android) or APNs certificate (iOS)
- Backend notification service running (base URL: `{API_GATEWAY}/api/notifications`)

---

## Firebase Project Setup

### Step 1: Create Firebase Project (Backend Team)

**Backend should provide:**
- Firebase Project ID
- Service account JSON key (for backend only)
- Web API key (if needed)

### Step 2: Register App in Firebase Console

**For Android:**
1. Go to Firebase Console → Project Settings
2. Click "Add App" → Android
3. Enter package name: `com.skillsnap.app` (or your package)
4. Download `google-services.json`
5. Place in `android/app/` directory

**For iOS:**
1. Go to Firebase Console → Project Settings
2. Click "Add App" → iOS
3. Enter bundle ID: `com.skillsnap.app` (or your bundle ID)
4. Download `GoogleService-Info.plist`
5. Add to Xcode project

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
      if (authToken == null) {
        print('Auth token not found, cannot register device token');
        return;
      }
      
      // Register with backend
      final dio = Dio();
      dio.options.headers['Authorization'] = 'Bearer $authToken';
      
      final response = await dio.post(
        _tokenEndpoint,
        data: {
          'token': token,
          'deviceType': _getDeviceType(),
          'appVersion': '1.0.0', // Get from package_info
        },
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
          
          await dio.delete(
            '$_tokenEndpoint/$token',
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
    if (!authToken) {
      console.log('Auth token not found');
      return;
    }
    
    // Register with backend
    const response = await axios.post(TOKEN_ENDPOINT, {
      token: token,
      deviceType: Platform.OS === 'android' ? 'Android' : 'iOS',
      appVersion: '1.0.0',
    }, {
      headers: {
        'Authorization': `Bearer ${authToken}`,
      }
    });
    
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

**Headers:**
```
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "fcm_device_token_from_firebase",
  "deviceType": "Android",
  "appVersion": "1.0.0"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Device token registered successfully"
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

## Deep Linking Configuration

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
- Check backend logs: `/var/log/notification-service.log`
- Review Firebase console for delivery status
- Check database PUSH_NOTIFICATION_LOG for send status
- Contact backend team for API issues

Backend Status: Phase 1-2 Complete ✅
Mobile Status: Phase 3 In Progress 🔄
