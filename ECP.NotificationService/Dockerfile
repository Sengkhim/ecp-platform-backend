FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ECP.NotificationService/ECP.NotificationService.csproj", "ECP.NotificationService/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]

RUN dotnet restore "ECP.NotificationService/ECP.NotificationService.csproj"

COPY ECP.NotificationService/ ECP.NotificationService/
COPY Contracts/ Contracts/

WORKDIR "/src/ECP.NotificationService"
RUN dotnet build "./ECP.NotificationService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ECP.NotificationService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECP.NotificationService.dll"]
