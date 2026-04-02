FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Realtime/Realtime.API/Realtime.API.csproj", "Services/Realtime/Realtime.API/"]
COPY ["src/Services/Realtime/Realtime.Application/Realtime.Application.csproj", "Services/Realtime/Realtime.Application/"]
COPY ["src/Services/Realtime/Realtime.Domain/Realtime.Domain.csproj", "Services/Realtime/Realtime.Domain/"]
COPY ["src/Services/Realtime/Realtime.Infrastructure/Realtime.Infrastructure.csproj", "Services/Realtime/Realtime.Infrastructure/"]

RUN dotnet restore "Services/Realtime/Realtime.API/Realtime.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Realtime/ Services/Realtime/

WORKDIR "/src/Services/Realtime/Realtime.API"
RUN dotnet build "Realtime.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Realtime.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Realtime.API.dll"]
