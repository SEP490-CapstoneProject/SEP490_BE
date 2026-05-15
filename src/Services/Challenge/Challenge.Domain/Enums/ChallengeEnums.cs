namespace Challenge.Domain.Enums;

public enum SkillCategoryType
{
    Backend = 0,
    Frontend = 1,
    Mobile = 2,
    DevOps = 3,
    AI = 4,
    DataScience = 5,
    CloudComputing = 6,
    Other = 99
}

public enum SkillApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum ChallengeStatus
{
    Draft = 0,
    PendingReview = 1,
    Published = 2,
    Expired = 3,
    Rejected = 4
}

public enum SubmissionStatus
{
    Pending = 0,
    Graded = 1
}

public enum VerificationLevel
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
    Expert = 3
}

public enum SkillRelationType
{
    Prerequisite = 0,
    Related = 1,
    Enables = 2
}
