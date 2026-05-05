# Phase 5 Deployment Checklist

## Pre-Deployment (Before Firebase Setup)

### Code & Tests
- [ ] All Phase 1-4 code committed
- [ ] All 19 unit tests passing
- [ ] No breaking changes to API
- [ ] No uncommitted changes in git

### Documentation
- [ ] PHASE_5_DEPLOYMENT_GUIDE.md created
- [ ] FIREBASE_SETUP_HELPER.md created
- [ ] Migration guide reviewed
- [ ] Rollback plan approved

### Team
- [ ] Backend team briefed
- [ ] DevOps team ready
- [ ] Mobile team coordinated
- [ ] On-call support ready

---

## Firebase Console Setup

### Firebase Project Creation
- [ ] Visit https://console.firebase.google.com
- [ ] Create project: `recruitment-platform-prod`
- [ ] Billing enabled
- [ ] Region: Southeast Asia
- [ ] Project ID recorded: `_________________`

### Android App Registration
- [ ] Package name: `com.recruitment.app`
- [ ] Download `google-services.json`
- [ ] Send to mobile team
- [ ] Stored securely (NOT in git)

### iOS App Registration
- [ ] Bundle ID: `com.recruitment.app`
- [ ] Download `GoogleService-Info.plist`
- [ ] Send to mobile team
- [ ] Stored securely (NOT in git)

### Cloud Messaging API
- [ ] Verified Cloud Messaging API enabled
- [ ] Verified status: ✅ Enabled

---

## Service Account Setup

### Credentials Generation
- [ ] Opened Project Settings → Service Accounts
- [ ] Clicked "Generate New Private Key"
- [ ] Downloaded JSON file
- [ ] Saved to secure location
- [ ] File contents verified (contains `private_key`, `project_id`, etc.)

### Service Account Permissions
- [ ] Verified Firebase Admin role
- [ ] Verified Cloud Messaging Editor role
- [ ] Permissions in place
- [ ] Service account ready

---

## Azure Key Vault Integration

### Secret Storage
- [ ] Opened Azure Key Vault
- [ ] Created secret: `FirebaseServiceAccountKey`
- [ ] Pasted entire JSON content
- [ ] Content-Type: `application/json`
- [ ] Secret created and stored

### Access Verification
- [ ] Retrieved secret from Key Vault
- [ ] Verified JSON content correct
- [ ] Service Account access validated
- [ ] Connection successful

### Key Vault URI
- [ ] URI recorded: `https://_____.vault.azure.net/secrets/FirebaseServiceAccountKey/`
- [ ] Managed Identity has read access
- [ ] Key Vault access tested

---

## Application Configuration

### appsettings.Production.json
- [ ] Created or updated appsettings file
- [ ] Firebase.Enabled: `true`
- [ ] Firebase.ProjectId: `recruitment-platform-prod`
- [ ] Firebase.CredentialPath: Key Vault URI
- [ ] FCM settings configured
- [ ] File reviewed for secrets (none should be hardcoded)

### Program.cs Updates
- [ ] Firebase initialization code added
- [ ] FirebaseApp.Create() implemented
- [ ] Credential loading from Key Vault
- [ ] Error handling for missing credentials
- [ ] Logging for initialization status

### DI Container
- [ ] FcmService registered
- [ ] DeviceTokenService registered
- [ ] FcmRetryService registered
- [ ] FcmAnalyticsService registered
- [ ] INotificationSettingsService registered

---

## Database Migrations

### Pre-Migration Backup
- [ ] Production database backed up
- [ ] Backup verified as readable
- [ ] Backup location: `_________________`
- [ ] Backup timestamp: `_________________`

### Staging Migration
- [ ] Connected to staging database
- [ ] Ran migration: `dotnet ef database update`
- [ ] Verified tables created:
  - [ ] DEVICE_TOKENS
  - [ ] PUSH_NOTIFICATION_LOG
  - [ ] NOTIFICATION_SETTINGS
- [ ] Verified indexes created
- [ ] No errors in migration

### Production Migration
- [ ] Connected to production database
- [ ] Ran migration: `dotnet ef database update`
- [ ] Verified tables created:
  - [ ] DEVICE_TOKENS
  - [ ] PUSH_NOTIFICATION_LOG
  - [ ] NOTIFICATION_SETTINGS
- [ ] Verified data integrity
- [ ] No errors in migration

### Post-Migration Verification
- [ ] Ran validation query: `SELECT COUNT(*) FROM DEVICE_TOKENS`
- [ ] Ran validation query: `SELECT COUNT(*) FROM PUSH_NOTIFICATION_LOG`
- [ ] Ran validation query: `SELECT COUNT(*) FROM NOTIFICATION_SETTINGS`
- [ ] All tables empty (as expected for new deployment)

---

## Staging Deployment

### Docker Image Build
- [ ] Built Docker image: `notification-service:staging`
- [ ] Image size reasonable (~200-500MB)
- [ ] Build completed without errors
- [ ] Image tested locally (optional)

### ACR Push
- [ ] Tagged image for staging registry
- [ ] Pushed to Azure Container Registry
- [ ] Verified in ACR portal
- [ ] Image available for deployment

### Kubernetes Deployment
- [ ] Updated deployment image to staging version
- [ ] Deployment initiated
- [ ] Pods started (replicas count: `_____`)
- [ ] All pods in Running state
- [ ] No CrashLoopBackOff errors

### Staging Verification
- [ ] Pod logs clean (no exceptions)
- [ ] Firebase initialized successfully
- [ ] Database connection successful
- [ ] Health check endpoint responding
- [ ] Service accessible at staging URL

### Firebase Connection Test
- [ ] Registered test device token
- [ ] Received 200 OK response
- [ ] Token stored in DEVICE_TOKENS table
- [ ] Token retrieval works

### Push Notification Test
- [ ] Sent test push from Firebase Console
- [ ] Received push on test device
- [ ] Push appeared in notification tray
- [ ] Notification tapped successfully
- [ ] Log entry created in PUSH_NOTIFICATION_LOG

---

## Production Canary Deployment (5%)

### Docker Image Build
- [ ] Built Docker image: `notification-service:prod-v1.0`
- [ ] Tagged with version
- [ ] Build completed without errors

### ACR Push
- [ ] Pushed to Azure Container Registry
- [ ] Tagged as production version
- [ ] Image available in ACR

### Canary Deployment
- [ ] Updated deployment to new image
- [ ] Set replica count to 1 (5% of fleet)
- [ ] Pod started successfully
- [ ] Health checks passing

### Canary Monitoring (30 minutes)
- [ ] Monitored error rate: `_____% (target < 0.5%)`
- [ ] Monitored CPU usage: `____% (target < 70%)`
- [ ] Monitored memory: `____ MB (target < 500MB)`
- [ ] Monitored FCM success rate: `____% (target > 99%)`
- [ ] No exceptions in logs
- [ ] No alerts triggered

### Canary Results
- [ ] Success: All metrics green
- [ ] Decision: Scale to 100%
- [ ] Timestamp: `_________________`
- [ ] Approved by: `_________________`

---

## Production Full Deployment (100%)

### Scale Up
- [ ] Scaled deployment to full replicas (20 pods)
- [ ] Rollout initiated
- [ ] All pods in Running state
- [ ] No pending pods

### Production Verification
- [ ] Pod logs clean
- [ ] No exceptions
- [ ] All pods healthy
- [ ] Load balanced traffic

### Metrics Monitoring
- [ ] Error rate < 0.5%
- [ ] Success rate > 99.5%
- [ ] Latency p95 < 5 seconds
- [ ] No quota exceeded warnings

### Production Results
- [ ] Deployment successful
- [ ] All systems green
- [ ] Ready for user rollout
- [ ] Timestamp: `_________________`

---

## Feature Flag Configuration

### Android 15% Rollout
- [ ] Feature flag set: `FCM_ANDROID_PERCENTAGE=15`
- [ ] iOS flag set: `FCM_IOS_PERCENTAGE=0`
- [ ] Configuration deployed
- [ ] Monitoring for 24 hours
- [ ] Metrics tracked:
  - [ ] Registration success rate
  - [ ] Push delivery rate
  - [ ] Error rate
  - [ ] User feedback

### Android 15% Results
- [ ] Success rate > 99%
- [ ] No critical issues
- [ ] User feedback positive
- [ ] Decision: Scale to 100%

### Android 100% Rollout
- [ ] Feature flag set: `FCM_ANDROID_PERCENTAGE=100`
- [ ] Configuration deployed
- [ ] Monitoring for 48 hours
- [ ] Metrics all green

### iOS 5% Rollout
- [ ] Feature flag set: `FCM_ANDROID_PERCENTAGE=100`
- [ ] Feature flag set: `FCM_IOS_PERCENTAGE=5`
- [ ] Configuration deployed
- [ ] Monitoring for 24 hours

### iOS 5% Results
- [ ] Success rate > 99%
- [ ] APNS working correctly
- [ ] No iOS-specific issues
- [ ] Decision: Scale to 100%

### iOS 100% Rollout
- [ ] Feature flag set: `FCM_IOS_PERCENTAGE=100`
- [ ] Configuration deployed
- [ ] Final monitoring
- [ ] All systems operational

---

## Post-Deployment Tasks

### Monitoring Setup
- [ ] Application Insights connected
- [ ] Alerts configured for:
  - [ ] Error rate > 5%
  - [ ] Success rate < 99%
  - [ ] FCM quota exceeded
  - [ ] Service unavailable
- [ ] Alert recipients configured
- [ ] On-call rotation updated

### Documentation
- [ ] Deployment documented
- [ ] Lessons learned captured
- [ ] Runbook updated
- [ ] Team briefed on operations

### Communication
- [ ] Team notified of go-live
- [ ] Mobile team notified of available API
- [ ] Support team trained
- [ ] Customer communication prepared

### Daily Monitoring (First Week)
- [ ] Monday: Error logs reviewed
- [ ] Tuesday: Metrics dashboard checked
- [ ] Wednesday: User feedback collected
- [ ] Thursday: Performance analysis
- [ ] Friday: Weekly summary

---

## Rollback Verification

### FCM Disable (Emergency)
- [ ] Tested disabling FCM: `FCM_ENABLED=false`
- [ ] Realtime notifications still working
- [ ] No user impact during disable
- [ ] Disable/enable time: < 2 minutes

### Database Restore (if needed)
- [ ] Tested backup restore procedure
- [ ] Restore time: `_____ minutes`
- [ ] Data integrity verified
- [ ] Rollback tested successfully

### Previous Version Deployment
- [ ] Tested rollback to previous version
- [ ] Rolled back without data loss
- [ ] No user-facing errors
- [ ] Restoration procedure validated

---

## Sign-Off

### Technical Review
- [ ] Backend lead reviewed: `_________________`
- [ ] Date: `_________________`
- [ ] DevOps lead reviewed: `_________________`
- [ ] Date: `_________________`

### Business Sign-Off
- [ ] Product owner approved: `_________________`
- [ ] Date: `_________________`
- [ ] VP Engineering approved: `_________________`
- [ ] Date: `_________________`

### Deployment Status
- [ ] Ready for production: `YES / NO`
- [ ] Approved for gradual rollout: `YES / NO`
- [ ] All checks passed: `YES / NO`

---

## Issues & Notes

### Issue 1
- Severity: `Low / Medium / High / Critical`
- Description: `_________________`
- Resolution: `_________________`
- Status: `Open / Closed`

### Issue 2
- Severity: `Low / Medium / High / Critical`
- Description: `_________________`
- Resolution: `_________________`
- Status: `Open / Closed`

### Notes
```
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________
```

---

## Final Status

**Phase 5 Deployment Status:** [ ] Complete / [ ] In Progress / [ ] Blocked

**Overall Project Status:** 
- Phase 1 (Backend): ✅ Complete
- Phase 2 (Publishing): ✅ Complete
- Phase 3 (Mobile): 📖 Guide Ready
- Phase 4 (Testing): ✅ Complete
- Phase 5 (Deployment): ⏳ In Progress / ✅ Complete

**Go-Live Ready:** [ ] Yes [ ] No [ ] Pending Items

---

**Prepared by:** `_________________`
**Date:** `_________________`
**Deployment Date:** `_________________`
**Deployment Duration:** `_____ hours`
**Deployment Status:** `_________________`
