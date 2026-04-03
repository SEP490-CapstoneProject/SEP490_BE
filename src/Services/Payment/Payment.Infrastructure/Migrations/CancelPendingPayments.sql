-- =====================================================================================
-- PayOS Migration: Cancel Pending Legacy Payments
-- =====================================================================================
-- Purpose: Cancel all pending VNPay/MoMo payments before migration
--          to prevent orphaned payments and webhook issues
--
-- Run AFTER MarkLegacyPayments.sql
-- =====================================================================================

USE PaymentDb;
GO

-- Start transaction
BEGIN TRANSACTION;

-- Display current pending Legacy payments
PRINT '=== PENDING LEGACY PAYMENTS ==='
SELECT 
    Status,
    Provider,
    COUNT(*) as Count,
    MIN(CreatedAt) as Oldest,
    MAX(CreatedAt) as Newest
FROM PaymentEntities
WHERE Provider = 99  -- Legacy
  AND Status = 0     -- Pending
GROUP BY Status, Provider;

PRINT ''
PRINT 'Cancelling all pending Legacy payments...'
PRINT ''

-- Cancel all pending Legacy payments
UPDATE PaymentEntities
SET 
    Status = 4,  -- Cancelled
    UpdatedAt = GETUTCDATE()
WHERE 
    Provider = 99  -- Legacy (VNPay/MoMo)
    AND Status = 0;  -- Pending

DECLARE @RowCount INT = @@ROWCOUNT;
PRINT '=== Cancelled ' + CAST(@RowCount AS VARCHAR) + ' pending Legacy payments ==='
PRINT ''

-- Export affected users for notification
PRINT '=== AFFECTED USERS (notify these users) ==='
SELECT DISTINCT
    UserId,
    COUNT(*) as CancelledPayments,
    SUM(Amount) as TotalAmount
FROM PaymentEntities
WHERE Provider = 99
  AND Status = 4  -- Cancelled
  AND UpdatedAt >= DATEADD(MINUTE, -5, GETUTCDATE())  -- Just cancelled
GROUP BY UserId
ORDER BY UserId;

-- Verify no more pending Legacy payments
IF EXISTS (SELECT 1 FROM PaymentEntities WHERE Provider = 99 AND Status = 0)
BEGIN
    PRINT ''
    PRINT '⚠️  WARNING: Still have pending Legacy payments after cancellation!'
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Commit
COMMIT TRANSACTION;

PRINT ''
PRINT '✅ All pending Legacy payments cancelled successfully'
PRINT ''
PRINT 'Next steps:'
PRINT '1. Notify affected users to retry payment with PayOS'
PRINT '2. Deploy PayOS code'
PRINT '3. Enable USE_PAYOS feature flag'

GO

-- Optional: Export to CSV for email notification
-- Run this separately if needed
/*
SELECT 
    u.UserId,
    u.Email,
    COUNT(p.Id) as CancelledPayments,
    SUM(p.Amount) as TotalAmount,
    MAX(p.CreatedAt) as LastPaymentDate
FROM PaymentEntities p
INNER JOIN Users u ON p.UserId = u.Id  -- Adjust table/column names
WHERE p.Provider = 99
  AND p.Status = 4
  AND p.UpdatedAt >= DATEADD(MINUTE, -10, GETUTCDATE())
GROUP BY u.UserId, u.Email
ORDER BY TotalAmount DESC;
*/
