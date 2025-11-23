FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ECP.Saga.Orchestrator/ECP.Saga.Orchestrator.csproj", "ECP.Saga.Orchestrator/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "ECP.Saga.Orchestrator/ECP.Saga.Orchestrator.csproj"
COPY . .
WORKDIR "/src/ECP.Saga.Orchestrator"
RUN dotnet build "./ECP.Saga.Orchestrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ECP.Saga.Orchestrator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECP.Saga.Orchestrator.dll"]
