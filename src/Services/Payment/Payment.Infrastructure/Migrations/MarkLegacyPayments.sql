-- =====================================================================================
-- PayOS Migration: Mark Legacy Payments
-- =====================================================================================
-- Purpose: Update all pre-migration payments to Legacy provider (99)
--          to prevent enum collision (old VNPay=0 vs new PayOS=0)
--
-- Run BEFORE deploying PayOS code!
-- =====================================================================================

USE PaymentDb;
GO

-- Start transaction
BEGIN TRANSACTION;

-- Display before state
PRINT '=== BEFORE MIGRATION ==='
SELECT 
    Provider,
    COUNT(*) as Count,
    MIN(CreatedAt) as OldestPayment,
    MAX(CreatedAt) as NewestPayment
FROM PaymentEntities
GROUP BY Provider
ORDER BY Provider;

-- Update all payments created before migration date to Legacy (99)
UPDATE PaymentEntities 
SET 
    Provider = 99,  -- Legacy
    UpdatedAt = GETUTCDATE()
WHERE 
    CreatedAt < '2026-03-26 00:00:00'  -- Migration date
    AND Provider IN (0, 1);  -- Old VNPay=0, MoMo=1

PRINT ''
PRINT '=== Rows Updated: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' ==='
PRINT ''

-- Display after state
PRINT '=== AFTER MIGRATION ==='
SELECT 
    CASE Provider
        WHEN 0 THEN 'PayOS (NEW)'
        WHEN 99 THEN 'Legacy (VNPay/MoMo)'
        ELSE 'Unknown'
    END as ProviderName,
    Provider,
    COUNT(*) as Count,
    MIN(CreatedAt) as OldestPayment,
    MAX(CreatedAt) as NewestPayment
FROM PaymentEntities
GROUP BY Provider
ORDER BY Provider;

-- Verify no active PayOS payments yet
IF EXISTS (SELECT 1 FROM PaymentEntities WHERE Provider = 0 AND Status IN (0, 1))
BEGIN
    PRINT ''
    PRINT '⚠️  WARNING: Found active PayOS payments before deployment!'
    PRINT 'Expected: 0 PayOS payments before migration'
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Commit if all checks pass
COMMIT TRANSACTION;

PRINT ''
PRINT '✅ Migration completed successfully'
PRINT 'All pre-2026-03-26 payments marked as Legacy (Provider=99)'
PRINT ''
PRINT 'Next steps:'
PRINT '1. Run cancel-pending-payments.sql to cancel pending Legacy payments'
PRINT '2. Deploy PayOS code with USE_PAYOS=false'
PRINT '3. Enable USE_PAYOS=true for gradual rollout'

GO
