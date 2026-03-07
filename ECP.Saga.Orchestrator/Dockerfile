FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files
COPY ["ECP.Saga.Orchestrator/ECP.Saga.Orchestrator.csproj", "ECP.Saga.Orchestrator/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["ECP.OrderService/ECP.OrderService.csproj", "ECP.OrderService/"]
COPY ["ECP.PaymentService/ECP.PaymentService.csproj", "ECP.PaymentService/"]

RUN dotnet restore "ECP.Saga.Orchestrator/ECP.Saga.Orchestrator.csproj"

# Copy all source code
COPY ECP.Saga.Orchestrator/ ECP.Saga.Orchestrator/
COPY Contracts/ Contracts/
COPY ECP.OrderService/ ECP.OrderService/
COPY ECP.PaymentService/ ECP.PaymentService/

WORKDIR "/src/ECP.Saga.Orchestrator"
RUN dotnet build "./ECP.Saga.Orchestrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# ✅ Fix — suppress duplicate appsettings.json conflict from referenced projects
RUN dotnet publish "./ECP.Saga.Orchestrator.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:ErrorOnDuplicatePublishOutputFiles=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECP.Saga.Orchestrator.dll"]