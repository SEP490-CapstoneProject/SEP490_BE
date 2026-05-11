# 📱 FCM Device Token Documentation - Complete Package

**Release Date**: 2026-05-11  
**Status**: ✅ Ready for Mobile Team  
**Commit**: c7bd057

---

## 📦 What's Included

### 1. **MOBILE_TEAM_DEVICE_TOKEN_SETUP_GUIDE.md** (English)
Comprehensive guide covering:
- ✅ What is a device token
- ✅ How to register (automatic process)
- ✅ How to regenerate tokens (3 methods)
- ✅ How to verify tokens work
- ✅ Troubleshooting common issues
- ✅ FAQ with 12+ common questions
- ✅ Best practices
- ✅ Technical implementation details (Kotlin, Swift, C#)
- ✅ Contact information for support

**Best For**: English-speaking team, developers, QA

---

### 2. **MOBILE_TEAM_DEVICE_TOKEN_SETUP_GUIDE_VI.md** (Vietnamese)
Same comprehensive guide translated to Vietnamese:
- ✅ 所有内容翻译成越南语
- ✅ Tailored for Vietnamese team members
- ✅ Maintains all technical accuracy

**Best For**: Vietnamese-speaking team members

---

### 3. **DEVICE_TOKEN_QUICK_REFERENCE.md** (One-Page Cheat Sheet)
Quick reference card:
- ✅ What is device token (one paragraph)
- ✅ How to register (one method - automatic)
- ✅ How to regenerate (2 quick methods)
- ✅ Quick troubleshooting table
- ✅ Emergency "nuclear option"
- ✅ Q&A (short answers)
- ✅ Support contacts

**Best For**: Quick lookup, printing, posting on Slack

---

### 4. **cleanup-invalid-tokens.sql**
SQL script to clean up invalid tokens:
- Delete 4 expired tokens identified in v27 testing
- Include verification queries
- Ready to execute on production database

**Usage**: Backend team executes when ready

---

### 5. **test-fcm-tokens.cs**
C# console application for token validation:
- Tests tokens directly with Firebase Admin SDK
- Returns detailed error information
- Provides recommendations based on results

**Used For**: Technical validation (already executed v27)

---

### 6. **test-fcm-tokens.ps1**
PowerShell script to build and run C# token tester:
- Handles dependency installation
- Compiles and runs tests
- Auto-cleanup temporary files

**Used For**: Technical troubleshooting

---

## 🎯 How Mobile Team Should Use This

### For End Users (Mobile App Users)
1. ✅ Share **DEVICE_TOKEN_QUICK_REFERENCE.md** in Slack
2. ✅ Direct users to troubleshooting section when issues arise
3. ✅ Common fix: Method 1 (Clear Cache/Data)

### For QA & Testers
1. ✅ Read **MOBILE_TEAM_DEVICE_TOKEN_SETUP_GUIDE.md** fully
2. ✅ Use "How to Verify Your Token" section for testing
3. ✅ Contact Backend Team with specific user IDs when needed

### For Mobile Developers
1. ✅ Review technical implementation sections
2. ✅ Check code examples (Kotlin, Swift)
3. ✅ Implement token refresh mechanisms if not already done
4. ✅ Use test utilities for validation

### For Vietnamese Team Members
1. ✅ Use **MOBILE_TEAM_DEVICE_TOKEN_SETUP_GUIDE_VI.md**
2. ✅ Share quick reference in Vietnamese

---

## 🔄 Background: Why This Documentation?

### Problem Found (v27)
- 4 test device tokens were all invalid (ErrorCode: NotFound)
- All tokens were expired/from wrong Firebase project
- Needed to provide clear guidance to mobile team for resolution

### Solution Provided
1. ✅ Document what device tokens are
2. ✅ Explain 3 methods to regenerate
3. ✅ Provide troubleshooting steps
4. ✅ Create quick reference for fast lookup
5. ✅ Include technical details for developers

### Expected Outcome
- ✅ Mobile team can resolve token issues independently
- ✅ Reduced back-and-forth on token problems
- ✅ Users know how to regenerate tokens
- ✅ Clear escalation path to backend team if needed

---

## 📊 Document Statistics

| Document | Size | Sections | Languages |
|----------|------|----------|-----------|
| GUIDE (English) | ~12KB | 14 sections | English |
| GUIDE (Vietnamese) | ~12.5KB | 14 sections | Vietnamese |
| Quick Reference | ~3KB | 12 sections | English |
| Total | ~27.5KB | - | 2 languages |

---

## 🚀 Next Steps

### For Backend Team
- [ ] Run cleanup SQL when ready
- [ ] Delete 4 invalid tokens from DEVICE_TOKENS
- [ ] Coordinate with mobile team on deployment

### For Mobile Team
- [ ] Review guides (especially developers)
- [ ] Share quick reference with team
- [ ] Distribute to QA/testing team
- [ ] Update app if token refresh needed

### For DevOps
- [ ] Archive these docs in team wiki/knowledge base
- [ ] Link from Firebase setup documentation
- [ ] Include in mobile dev onboarding

---

## 📋 Document Maintenance

### How to Update
1. Edit markdown file
2. Update version number
3. Update "Last Updated" date
4. Commit to Git with clear message
5. Notify team of changes in Slack

### When to Update
- ✅ When Firebase setup changes
- ✅ When new token issues discovered
- ✅ When new best practices identified
- ✅ When team structure changes

---

## 🔗 Related Documentation

- **FCM Notification System Setup**: [FIREBASE_SETUP_HELPER.md](FIREBASE_SETUP_HELPER.md)
- **Notification API Reference**: [NOTIFICATION_SERVICE_GUIDE.md](NOTIFICATION_SERVICE_GUIDE.md)
- **Push Notification Implementation**: [FCM_MOBILE_SETUP_GUIDE.md](FCM_MOBILE_SETUP_GUIDE.md)
- **Token Testing Utilities**: Available in repo root

---

## ✅ Quality Checklist

- ✅ Documentation is complete and accurate
- ✅ Covers all 3 token regeneration methods
- ✅ Includes troubleshooting for common issues
- ✅ Provides technical details for developers
- ✅ Available in English and Vietnamese
- ✅ Quick reference for fast lookup
- ✅ Testing utilities included
- ✅ Cleanup script provided
- ✅ Contact information included
- ✅ Best practices documented

---

## 📞 Support & Questions

**For Documentation Issues**:
- Open GitHub Issue: [Notification Documentation Issues](https://github.com/SEP490-CapstoneProject/SEP490_BE/issues)
- Tag: `documentation`, `fcm`, `mobile`

**For FCM/Token Technical Issues**:
- Slack: #notification-support
- Backend Team Email: notification-team@skillsnap.com

**For Mobile App Issues**:
- Mobile GitHub: [Mobile Repo](https://github.com/SEP490-CapstoneProject/mobile-app)

---

**Created by**: Backend Team  
**Status**: ✅ Production Ready  
**Last Updated**: 2026-05-11  
**Version**: 1.0
