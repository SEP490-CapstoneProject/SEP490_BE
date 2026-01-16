# Script to generate remaining microservices
# This script creates all necessary files for each microservice following the Auth Service pattern

$services = @(
    @{
        Name = "UserProfile"
        Database = "UserProfileServiceDb"
        Port = "5002"
        Entities = @("Employee")
    },
    @{
        Name = "Portfolio"
        Database = "PortfolioServiceDb"
        Port = "5003"
        Entities = @("Portfolio", "Skill", "PortfolioSkill", "Education", "Experience", "Project", "Award", "Activity", "Hobby", "Reference")
    },
    @{
        Name = "Company"
        Database = "CompanyServiceDb"
        Port = "5004"
        Entities = @("Company")
    },
    @{
        Name = "JobHiring"
        Database = "JobHiringServiceDb"
        Port = "5005"
        Entities = @("JobPost", "JobApplication")
    },
    @{
        Name = "Connection"
        Database = "ConnectionServiceDb"
        Port = "5006"
        Entities = @("Connection", "MessageRoom", "Message")
    },
    @{
        Name = "Community"
        Database = "CommunityServiceDb"
        Port = "5007"
        Entities = @("CommunityPost")
    },
    @{
        Name = "Subscription"
        Database = "SubscriptionServiceDb"
        Port = "5008"
        Entities = @("Plan", "Subscription", "Payment")
    },
    @{
        Name = "Advertisement"
        Database = "AdvertisementServiceDb"
        Port = "5009"
        Entities = @("Advertisement")
    },
    @{
        Name = "Moderation"
        Database = "ModerationServiceDb"
        Port = "5010"
        Entities = @("Report", "ModerationLog")
    },
    @{
        Name = "Notification"
        Database = "NotificationServiceDb"
        Port = "5011"
        Entities = @("Notification")
    }
)

Write-Host "This script will generate all remaining microservices..." -ForegroundColor Green
Write-Host "Each service will have:" -ForegroundColor Yellow
Write-Host "  - Domain entities" -ForegroundColor Yellow
Write-Host "  - DbContext with configurations" -ForegroundColor Yellow
Write-Host "  - Repository pattern" -ForegroundColor Yellow
Write-Host "  - Service layer" -ForegroundColor Yellow
Write-Host "  - API controllers" -ForegroundColor Yellow
Write-Host "  - Program.cs with JWT auth" -ForegroundColor Yellow
Write-Host "  - appsettings.json" -ForegroundColor Yellow
Write-Host "  - Dockerfile" -ForegroundColor Yellow
Write-Host ""
Write-Host "Note: Due to the complexity, you'll need to implement the detailed entity properties" -ForegroundColor Cyan
Write-Host "and business logic for each service based on the database schema provided." -ForegroundColor Cyan
Write-Host ""
Write-Host "Services to be generated:" -ForegroundColor Green
foreach ($service in $services) {
    Write-Host "  - $($service.Name) Service (Port $($service.Port))" -ForegroundColor White
}
