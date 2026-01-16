FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared projects
COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]

# Copy UserProfile service projects
COPY ["src/Services/UserProfile/UserProfile.API/UserProfile.API.csproj", "Services/UserProfile/UserProfile.API/"]
COPY ["src/Services/UserProfile/UserProfile.Application/UserProfile.Application.csproj", "Services/UserProfile/UserProfile.Application/"]
COPY ["src/Services/UserProfile/UserProfile.Domain/UserProfile.Domain.csproj", "Services/UserProfile/UserProfile.Domain/"]
COPY ["src/Services/UserProfile/UserProfile.Infrastructure/UserProfile.Infrastructure.csproj", "Services/UserProfile/UserProfile.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Services/UserProfile/UserProfile.API/UserProfile.API.csproj"

# Copy everything else
COPY src/Shared/ Shared/
COPY src/Services/UserProfile/ Services/UserProfile/

# Build
WORKDIR "/src/Services/UserProfile/UserProfile.API"
RUN dotnet build "UserProfile.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserProfile.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserProfile.API.dll"]
