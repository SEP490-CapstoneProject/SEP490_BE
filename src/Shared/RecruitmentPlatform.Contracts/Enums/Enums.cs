namespace RecruitmentPlatform.Contracts.Enums;

public enum UserRole
{
    USER = 1,
    RECRUITER = 2,
    ADMIN = 3,
    MODERATOR = 4,
    EXPERT = 5
}

public enum UserStatus
{
    Locked = 0,
    Active = 1
}

public enum PortfolioStatus
{
    DRAFT,
    PENDING,
    APPROVED,
    REJECTED
}

public enum JobPostStatus
{
    PENDING,
    APPROVED,
    REJECTED
}

public enum ApplicationStatus
{
    PENDING,
    ACCEPTED,
    REJECTED
}

public enum ConnectionStatus
{
    PENDING,
    MATCHED,
    REJECTED
}

public enum CommunityPostStatus
{
    PENDING,
    APPROVED,
    REJECTED
}

public enum EmploymentType
{
    FULL_TIME,
    PART_TIME,
    CONTRACT,
    INTERNSHIP
}

public enum SubscriptionStatus
{
    ACTIVE,
    EXPIRED,
    CANCELLED
}

public enum PaymentStatus
{
    PENDING,
    COMPLETED,
    FAILED
}

public enum PaymentMethod
{
    CREDIT_CARD,
    BANK_TRANSFER,
    E_WALLET
}

public enum AdvertisementType
{
    IMAGE,
    VIDEO
}

public enum AdvertisementStatus
{
    PENDING,
    APPROVED,
    REJECTED,
    ACTIVE,
    EXPIRED
}

public enum ReportStatus
{
    PENDING,
    RESOLVED,
    DISMISSED
}

public enum NotificationType
{
    JOB_APPLICATION,
    CONNECTION_REQUEST,
    MESSAGE,
    SYSTEM,
    MODERATION
}
