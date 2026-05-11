# 📱 FCM Device Token - Quick Reference Card

## One-Page Cheat Sheet for Mobile Team

---

## What is Device Token?
- Unique ID per device for push notifications
- Auto-generated when app installed
- Stored on our backend server
- Required to receive notifications

---

## How to Register Device Token?

### Automatic (Easiest)
1. Install app
2. Login
3. Done! ✅ Token registered automatically

---

## How to Regenerate Device Token?

### ✅ Method 1: Clear App Data (RECOMMENDED)
```
Settings → Apps → SkillSnap → Storage
→ Clear Cache → Clear Data
→ Restart Phone → Login Again
```
**Time**: 1-2 minutes
**Side Effect**: Logs you out (need to login again)

### ✅ Method 2: Uninstall & Reinstall
```
Long-press app icon → Uninstall
→ Play Store → Search SkillSnap → Install
→ Login Again
```
**Time**: 5-10 minutes

---

## How to Check Token?

### For QA Team
```sql
-- Ask Backend Team to run
SELECT DeviceToken, RegisteredAt, IsActive
FROM DEVICE_TOKENS
WHERE UserId = <your_id>
```

### For Tech Users
```bash
adb logcat | grep "FCM\|DeviceToken"
```

---

## Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| No notifications | Clear Cache → Clear Data → Restart |
| Notifications late | Regenerate token (Method 1) |
| Token not found error | Logout → Login → Wait 2 mins |
| Multiple tokens registered | Contact Backend Team |

---

## Signs Your Token Works ✅
- ✅ Get notifications instantly when events happen
- ✅ No delays or missing notifications
- ✅ Notifications appear even when app closed

---

## Signs Your Token Doesn't Work ❌
- ❌ Never receive notifications
- ❌ Notifications arrive 30+ minutes late
- ❌ App shows "Failed to send" errors

---

## Common Q&A

**Q: Do I need to do anything to register?**  
A: No! It's automatic when you install and use the app.

**Q: How long does a token last?**  
A: Indefinitely if you use app regularly. Expires after 6+ months of no use.

**Q: Will clearing app data delete my account?**  
A: No! Only local data cleared. Account on server is safe. Just login again.

**Q: Can I manually force a new token?**  
A: Yes! Use Method 1 or Method 2 above.

**Q: How many devices can I register?**  
A: Unlimited! Each device gets its own token.

---

## Contact Support

**Push Notification Issues**:
- Slack: #notification-support
- Email: notification-team@skillsnap.com

**Firebase Problems**:
- Slack: #devops

**Mobile App Issues**:
- GitHub: [Mobile Repo](https://github.com/SEP490-CapstoneProject/mobile-app)

---

## Best Practices

✅ **DO**:
- Keep app updated
- Login regularly
- Report issues quickly

❌ **DON'T**:
- Share your token with others
- Manually edit token values
- Ignore notification permission prompts

---

## Emergency: Nuclear Option
If nothing works:

1. Uninstall app
2. Restart phone
3. Reinstall app
4. Login
5. Wait 3 minutes
6. Contact Backend Team if still failing

**Success Rate**: 99%

---

**Version**: 1.0  
**Last Updated**: 2026-05-11  
**Need Help?** → Slack #notification-support
