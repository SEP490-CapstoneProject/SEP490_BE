FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/JobHiring/JobHiring.API/JobHiring.API.csproj", "Services/JobHiring/JobHiring.API/"]
COPY ["src/Services/JobHiring/JobHiring.Application/JobHiring.Application.csproj", "Services/JobHiring/JobHiring.Application/"]
COPY ["src/Services/JobHiring/JobHiring.Domain/JobHiring.Domain.csproj", "Services/JobHiring/JobHiring.Domain/"]
COPY ["src/Services/JobHiring/JobHiring.Infrastructure/JobHiring.Infrastructure.csproj", "Services/JobHiring/JobHiring.Infrastructure/"]

RUN dotnet restore "Services/JobHiring/JobHiring.API/JobHiring.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/JobHiring/ Services/JobHiring/

WORKDIR "/src/Services/JobHiring/JobHiring.API"
RUN dotnet build "JobHiring.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobHiring.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobHiring.API.dll"]
