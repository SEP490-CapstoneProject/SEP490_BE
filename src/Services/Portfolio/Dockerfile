FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Portfolio/Portfolio.API/Portfolio.API.csproj", "Services/Portfolio/Portfolio.API/"]
COPY ["src/Services/Portfolio/Portfolio.Application/Portfolio.Application.csproj", "Services/Portfolio/Portfolio.Application/"]
COPY ["src/Services/Portfolio/Portfolio.Domain/Portfolio.Domain.csproj", "Services/Portfolio/Portfolio.Domain/"]
COPY ["src/Services/Portfolio/Portfolio.Infrastructure/Portfolio.Infrastructure.csproj", "Services/Portfolio/Portfolio.Infrastructure/"]

RUN dotnet restore "Services/Portfolio/Portfolio.API/Portfolio.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Portfolio/ Services/Portfolio/

WORKDIR "/src/Services/Portfolio/Portfolio.API"
RUN dotnet build "Portfolio.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portfolio.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.API.dll"]
