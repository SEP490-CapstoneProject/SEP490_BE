FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Company/Company.API/Company.API.csproj", "Services/Company/Company.API/"]
COPY ["src/Services/Company/Company.Application/Company.Application.csproj", "Services/Company/Company.Application/"]
COPY ["src/Services/Company/Company.Domain/Company.Domain.csproj", "Services/Company/Company.Domain/"]
COPY ["src/Services/Company/Company.Infrastructure/Company.Infrastructure.csproj", "Services/Company/Company.Infrastructure/"]

RUN dotnet restore "Services/Company/Company.API/Company.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Company/ Services/Company/

WORKDIR "/src/Services/Company/Company.API"
RUN dotnet build "Company.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Company.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Company.API.dll"]
