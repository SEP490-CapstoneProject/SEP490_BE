FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared projects
COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]

# Copy Interview service projects
COPY ["src/Services/Interview/Interview.API/Interview.API.csproj", "Services/Interview/Interview.API/"]
COPY ["src/Services/Interview/Interview.Application/Interview.Application.csproj", "Services/Interview/Interview.Application/"]
COPY ["src/Services/Interview/Interview.Domain/Interview.Domain.csproj", "Services/Interview/Interview.Domain/"]
COPY ["src/Services/Interview/Interview.Infrastructure/Interview.Infrastructure.csproj", "Services/Interview/Interview.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Services/Interview/Interview.API/Interview.API.csproj"

# Copy everything else
COPY src/Shared/ Shared/
COPY src/Services/Interview/ Services/Interview/

# Build
WORKDIR "/src/Services/Interview/Interview.API"
RUN dotnet build "Interview.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Interview.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Interview.API.dll"]
