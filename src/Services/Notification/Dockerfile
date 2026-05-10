FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Install required libraries for Alpine (fixes globalization and timezone issues)
RUN apk add --no-cache icu-libs tzdata

# Create non-root user for security
RUN addgroup -g 1001 -S appuser && \
    adduser -u 1001 -S appuser -G appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy only project files first for better caching
COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Notification/Notification.API/Notification.API.csproj", "Services/Notification/Notification.API/"]
COPY ["src/Services/Notification/Notification.Application/Notification.Application.csproj", "Services/Notification/Notification.Application/"]
COPY ["src/Services/Notification/Notification.Domain/Notification.Domain.csproj", "Services/Notification/Notification.Domain/"]
COPY ["src/Services/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj", "Services/Notification/Notification.Infrastructure/"]

# Restore dependencies (separate layer for better caching)
RUN dotnet restore "Services/Notification/Notification.API/Notification.API.csproj"

# Copy source code
COPY src/Shared/ Shared/
COPY src/Services/Notification/ Services/Notification/

# Build
WORKDIR "/src/Services/Notification/Notification.API"
RUN dotnet build "Notification.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notification.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Add labels for metadata
LABEL maintainer="SkillSnap Team" \
      description="FCM Push Notification Service" \
      version="1.0"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Set environment
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0

ENTRYPOINT ["dotnet", "Notification.API.dll"]
