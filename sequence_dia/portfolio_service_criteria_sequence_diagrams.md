# Portfolio Service Criteria Sequence Diagrams

## 1) Create Criteria

```mermaid
sequenceDiagram
    autonumber
    actor A as Admin
    participant C as CriterionController
    participant S as ICriterionService / CriterionService
    participant R as ICriterionRepository
    participant DB as PortfolioDbContext

    A->>C: POST /api/admin/criteria (CreateCriterionRequest)
    C->>C: Validate authorization (ADMIN role)
    alt not authorized
        C-->>A: 403 Forbidden
    else authorized
        C->>S: CreateAsync(request)
        S->>S: Validate Name (required)
        S->>S: Validate Kind (required)
        alt validation fails
            S-->>C: ArgumentException
            C-->>A: 400 Bad Request
        else validation passes
            S->>S: Build Criterion entity
            S->>R: CreateAsync(criterion)
            R->>DB: Criteria.Add(criterion)
            R->>DB: SaveChangesAsync()
            DB-->>R: persisted with Id
            R-->>S: Criterion
            S->>S: MapToDto(criterion)
            S-->>C: CriterionDto
            C-->>A: 201 Created
        end
    end
```

## 2) Update Criteria

```mermaid
sequenceDiagram
    autonumber
    actor A as Admin
    participant C as CriterionController
    participant S as ICriterionService / CriterionService
    participant R as ICriterionRepository
    participant DB as PortfolioDbContext

    A->>C: PUT /api/admin/criteria/{id} (UpdateCriterionRequest)
    C->>C: Validate authorization (ADMIN role)
    alt not authorized
        C-->>A: 403 Forbidden
    else authorized
        C->>S: UpdateAsync(id, request)
        S->>S: Validate Name (required)
        S->>S: Validate Kind (required)
        alt validation fails
            S-->>C: ArgumentException
            C-->>A: 400 Bad Request
        else validation passes
            S->>R: GetByIdAsync(id)
            R->>DB: Criteria.FindAsync(id)
            DB-->>R: Criterion / null

            alt criterion not found
                R-->>S: null
                S-->>C: KeyNotFoundException
                C-->>A: 404 Not Found
            else criterion found
                S->>S: Update Name
                S->>S: Update Kind
                S->>R: UpdateAsync(criterion)
                R->>DB: Criteria.Update(criterion)
                R->>DB: SaveChangesAsync()
                DB-->>R: updated
                R-->>S: Criterion
                S->>S: MapToDto(criterion)
                S-->>C: CriterionDto
                C-->>A: 200 OK
            end
        end
    end
```

## Key Points

- **Authorization**: Only ADMIN role can create/update criteria
- **Validation**: Name and Kind are required fields
- **Persistence**: Uses PortfolioDbContext.Criteria DbSet
- **Response**: Returns CriterionDto with Id, Name, Kind, IsActive
- **Error Handling**: 
  - 400 Bad Request for validation errors
  - 403 Forbidden for authorization errors
  - 404 Not Found for missing criteria
  - 500 Internal Server Error for unexpected failures
