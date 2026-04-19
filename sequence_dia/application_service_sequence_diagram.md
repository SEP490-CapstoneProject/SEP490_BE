```mermaid
sequenceDiagram
    actor Candidate
    participant ApplicationController
    participant ApplicationService
    participant SubscriptionService
    participant ApplicationRepository
    participant ApplicationDB

    Candidate->>ApplicationController: 1. POST /api/applications
    activate ApplicationController
    ApplicationController->>ApplicationService: 2. CreateApplicationAsync(request)
    activate ApplicationService
    ApplicationService->>SubscriptionService: 3. Check entitlement/limits
    activate SubscriptionService
    SubscriptionService-->>ApplicationService: 4. Return Allowed/Denied
    deactivate SubscriptionService
    alt Allowed
        ApplicationService->>ApplicationRepository: 5. CreateAsync(application)
        activate ApplicationRepository
        ApplicationRepository->>ApplicationDB: 6. INSERT Application
        activate ApplicationDB
        ApplicationDB-->>ApplicationRepository: 7. Return applicationId
        deactivate ApplicationDB
        ApplicationRepository-->>ApplicationService: 8. Return ApplicationDto
        deactivate ApplicationRepository
        ApplicationService-->>ApplicationController: 9. Return ApplicationDto
        deactivate ApplicationService
        ApplicationController-->>Candidate: 10. 201 Created
        deactivate ApplicationController
    else Denied
        ApplicationService-->>ApplicationController: 5. Return business error
        deactivate ApplicationService
        ApplicationController-->>Candidate: 6. 403/400
        deactivate ApplicationController
    end
```
