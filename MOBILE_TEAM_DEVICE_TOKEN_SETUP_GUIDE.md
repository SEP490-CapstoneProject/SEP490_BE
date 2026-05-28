# 📱 FCM Device Token Setup Guide for Mobile Team

**Document Version**: 1.0  
**Last Updated**: 2026-05-11  
**Status**: ✅ Production Ready

---

## 📖 Table of Contents
1. [What is a Device Token?](#what-is-a-device-token)
2. [How to Register Your Device](#how-to-register-your-device)
3. [How to Regenerate a Token](#how-to-regenerate-a-token)
4. [How to Verify Your Token](#how-to-verify-your-token)
5. [Troubleshooting](#troubleshooting)
6. [FAQ](#faq)

---

## What is a Device Token?

A **Device Token** is a unique identifier that Firebase Cloud Messaging (FCM) uses to send push notifications to your mobile device.

### Key Points
- ✅ **Unique per device**: Each phone has a different token
- ✅ **Auto-generated**: Created when app first installs
- ✅ **Required for push notifications**: Without it, you won't receive notifications
- ✅ **Temporary**: Can expire if not used for extended periods
- ✅ **Registered in backend**: Stored in our database for notification targeting

### Example Token Format
```
fpQ6ZToxTRWox8EzcQKtS8:APA91bGQ9qymJXZhBG0xyk8f2g5FldUxCprH3oqviF14A_4Qnb0wXEFPpc7uLuHy3R-FhPdgxYVVtLvSYysxHyoLuMKidaBAtaDzmrw9LLaQYWgHKKqUdWg
```

---

## How to Register Your Device

### Automatic Registration (Recommended)
The app automatically registers your device token when you:

1. **First time install**:
   - Install app from Play Store
   - Open app
   - App automatically registers device token
   - Token stored on server

2. **After login**:
   - Login with your account
   - Device token automatically sent to backend
   - Server stores token for your user account

### What Happens Behind the Scenes
```
Device Installed → Firebase generates token
           ↓
    App starts → Reads token from Firebase SDK
           ↓
    Backend API → Sends token to server
           ↓
    Database saved → Token ready for notifications
```

---

## How to Regenerate a Token

### Why Regenerate a Token?
- ❌ Not receiving notifications after long time
- ❌ Changed Firebase project config
- ❌ App hasn't worked for several months
- ❌ After major app update
- ✅ General maintenance

### Method 1: Clear App Data (Recommended)
This is the safest way to regenerate a fresh token:

**Steps**:
1. Open phone **Settings**
2. Navigate to **Apps** (or **Application Manager**)
3. Find **SkillSnap** (or your app name)
4. Tap **Storage** (or **Manage Storage**)
5. Click **Clear Cache**
6. Click **Clear Data** (or **Clear Storage**)
   - ⚠️ **Warning**: This will logout you from the app
7. Restart the phone
8. Open the app again
9. Login with your credentials
10. ✅ New device token generated and registered

**Time to take effect**: 30 seconds - 2 minutes

### Method 2: Uninstall & Reinstall
If Method 1 doesn't work:

**Steps**:
1. Long-press the app icon
2. Select **Uninstall** (or tap the ⊗ icon)
3. Confirm uninstall
4. Go to Play Store
5. Search for **SkillSnap**
6. Click **Install**
7. Wait for installation to complete
8. Open app
9. Login with credentials
10. ✅ New device token registered

**Time to take effect**: 1-3 minutes

### Method 3: Force Refresh (Advanced)
If you want to stay logged in:

**For React Native / Expo Apps**:
```javascript
// Developers: Add this to your notification setup
import messaging from '@react-native-firebase/messaging';

async function refreshDeviceToken() {
  try {
    const newToken = await messaging().getToken();
    console.log('New FCM Token:', newToken);
    
    // Send to backend
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
    
    console.log('Token refreshed successfully');
  } catch (error) {
    console.error('Error refreshing token:', error);
  }
}

// Call this function
await refreshDeviceToken();
```

---

## How to Verify Your Token

### Check if Token is Registered
**For Technical Users / QA Team**:

#### 1. Check App Logs
```bash
# Android Studio Logcat
adb logcat | grep "FCM\|DeviceToken\|notification"

# Look for logs like:
# [FCM_TOKEN] DeviceToken registered successfully
# [FCM_REGISTERED] Token: fpQ6ZToxT...
```

#### 2. Check Backend Database
Contact Backend Team to run:
```sql
SELECT [DeviceToken], [RegisteredAt], [LastUsedAt], [IsActive]
FROM [DEVICE_TOKENS]
WHERE [UserId] = <your_user_id>
ORDER BY [RegisteredAt] DESC;
```

#### 3. Test Notification Send
Ask backend team to send test notification to your user:
- Check if you receive push notification on device
- If yes ✅ → Token is valid
- If no ❌ → Token might be expired (go to Step: Regenerate)

### Signs of Valid Token
✅ You receive notifications immediately when events happen  
✅ No delays or missing notifications  
✅ Notification appears even when app is closed

### Signs of Invalid Token
❌ No notifications received  
❌ Notifications appear 30+ minutes late  
❌ Notifications never arrive  
❌ App shows "Failed to send" errors in logs

---

## Troubleshooting

### Problem: Not Receiving Notifications

**Solution 1**: Check notification settings
1. Open app settings
2. Check **Notification Preferences**
3. Ensure these are **enabled**:
   - ✅ Push Notifications
   - ✅ Chat Notifications (if applicable)
   - ✅ Sound Enabled
   - ✅ Vibrate Enabled

**Solution 2**: Regenerate device token
Follow [Method 1: Clear App Data](#method-1-clear-app-data-recommended) above

**Solution 3**: Force stop and restart app
1. Go to Settings → Apps → SkillSnap
2. Tap **Force Stop**
3. Wait 5 seconds
4. Open app again

**Solution 4**: Reinstall app
Follow [Method 2: Uninstall & Reinstall](#method-2-uninstall--reinstall)

---

### Problem: "Token Not Found" Error in Backend

**Cause**: Backend can't find your device token

**Solution**:
1. Logout from app
2. Login again (forces token re-registration)
3. Wait 2 minutes
4. Ask backend team to verify token was registered

---

### Problem: Token Keeps Expiring

**Cause**: Using old Firebase project configuration

**Solution**:
1. Check app's `google-services.json` is correct
   - Should match Firebase project: `skillsnap-notification`
2. If not, update `google-services.json`:
   - Download from Firebase Console
   - Replace in app source code
   - Rebuild and reinstall app
3. After update, regenerate token

---

### Problem: Multiple Tokens Registered (Duplicates)

**Cause**: App reinstalled multiple times without cleaning up old tokens

**Solution**:
1. Contact backend team
2. Ask them to delete old expired tokens
3. Keep only most recent valid token
4. Regenerate once more to ensure fresh token

---

## FAQ

### Q: Do I need to do anything to register my device?
**A**: No! It's automatic. When you install and use the app, it registers automatically.

### Q: How long does a token last?
**A**: Indefinitely, as long as you use the app regularly. If you don't use the app for 6+ months, the token might expire.

### Q: Can I see my device token?
**A**: Yes, but only through app logs (need developer tools). For security, tokens are not displayed in the UI.

### Q: Will clearing app data delete my account?
**A**: No, it only clears local data. Your account on the server stays intact. You just need to login again.

### Q: How many devices can I register?
**A**: Unlimited! Register the app on multiple phones/tablets, each gets its own token.

### Q: What if I reinstall the app on the same phone?
**A**: You'll get a new device token. The old one will be deleted after 6 months of inactivity.

### Q: Why isn't my notification arriving?
**A**: Common reasons:
- ❌ Device token not registered
- ❌ Notifications disabled in app settings
- ❌ Token expired (regenerate it)
- ❌ Backend app is down (check status)
- ❌ Network connectivity issues

### Q: Can I manually force a new token?
**A**: Yes, use Method 1 or 2 above.

### Q: Do I lose my messages after regenerating token?
**A**: No! Messages are stored in the backend. Only the notification delivery mechanism changes.

---

## Quick Reference

| Task | Steps | Time |
|------|-------|------|
| **Auto-register** | Install app + Login | 1-2 min |
| **Clear cache** | Settings → Apps → SkillSnap → Clear Cache | 30 sec |
| **Clear data** | Settings → Apps → SkillSnap → Clear Data | 1-2 min |
| **Uninstall/Reinstall** | Play Store → Uninstall → Install | 5-10 min |
| **Test notification** | Ask backend team to send test | Instant |
| **Check token status** | Contact backend team | 5 min |

---

## Contact & Support

### For Technical Issues
**Backend Team** (Push Notification Support):
- Slack: #notification-support
- Email: notification-team@skillsnap.com
- Issue Tracker: [GitHub Issues - Notification](https://github.com/SEP490-CapstoneProject/SEP490_BE/issues?q=label%3Anotification)

### For Firebase Configuration Issues
**DevOps Team**:
- Firebase Project: skillsnap-notification
- Slack: #devops
- Contact: devops@skillsnap.com

### For Mobile App Issues
**Mobile Team**:
- Android: [Android GitHub](https://github.com/SEP490-CapstoneProject/mobile-app)
- iOS: [iOS GitHub](https://github.com/SEP490-CapstoneProject/ios-app)

---

## Best Practices

✅ **DO**:
- Keep app updated to latest version
- Login regularly to maintain token freshness
- Report notification issues immediately
- Check notification settings are enabled
- Regenerate token if issues persist

❌ **DON'T**:
- Share your device token with others
- Manually edit token values
- Use tokens from other devices
- Ignore notification permission prompts
- Uninstall app without logging out first

---

## Appendix: Technical Details

### Android - Generating FCM Token

```kotlin
// Firebase Cloud Messaging Setup
import com.google.firebase.messaging.FirebaseMessaging

// Get current token
FirebaseMessaging.getInstance().token.addOnCompleteListener { task ->
    if (!task.isSuccessful) {
        Log.w("FCM", "Fetching FCM registration token failed", task.exception)
        return@addOnCompleteListener
    }
    
    val token = task.result
    Log.d("FCM_TOKEN", "Device Token: $token")
    
    // Send to backend
    sendTokenToBackend(token)
}

// Listen for new tokens
class MyFirebaseMessagingService : FirebaseMessagingService() {
    override fun onNewToken(token: String) {
        Log.d("FCM_NEW_TOKEN", "Refreshed token: $token")
        sendTokenToBackend(token)
    }
}
```

### iOS - Generating FCM Token

```swift
// Firebase Cloud Messaging Setup
import FirebaseMessaging

Messaging.messaging().token { token, error in
    if let error = error {
        print("Error fetching FCM token: \(error)")
    } else if let token = token {
        print("FCM token: \(token)")
        // Send to backend
        sendTokenToBackend(token)
    }
}
```

### Backend - Registering Token

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
        return Ok(new { message = "Device token registered successfully" });
    
    return BadRequest(new { message = "Failed to register device token" });
}
```

---

**Document Status**: ✅ Ready for Mobile Team  
**Last Tested**: 2026-05-11  
**Feedback**: Share issues in #notification-support Slack channel
