FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Challenge/Challenge.API/Challenge.API.csproj", "Services/Challenge/Challenge.API/"]
COPY ["src/Services/Challenge/Challenge.Application/Challenge.Application.csproj", "Services/Challenge/Challenge.Application/"]
COPY ["src/Services/Challenge/Challenge.Domain/Challenge.Domain.csproj", "Services/Challenge/Challenge.Domain/"]
COPY ["src/Services/Challenge/Challenge.Infrastructure/Challenge.Infrastructure.csproj", "Services/Challenge/Challenge.Infrastructure/"]

RUN dotnet restore "Services/Challenge/Challenge.API/Challenge.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Challenge/ Services/Challenge/

WORKDIR "/src/Services/Challenge/Challenge.API"
RUN dotnet build "Challenge.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Challenge.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Challenge.API.dll"]
