# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy project files first
COPY ECP.Saga.Orchestrator/ECP.Saga.Orchestrator.csproj ECP.Saga.Orchestrator/
COPY Contracts/Contracts.csproj Contracts/
COPY ECP.OrderService/ECP.OrderService.csproj ECP.OrderService/
COPY ECP.PaymentService/ECP.PaymentService.csproj ECP.PaymentService/

# Restore dependencies
RUN dotnet restore ECP.Saga.Orchestrator/ECP.Saga.Orchestrator.csproj

# Copy source code
COPY . .

WORKDIR /src/ECP.Saga.Orchestrator

# Publish directly (no build step needed)
RUN dotnet publish \
    ECP.Saga.Orchestrator.csproj \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:ErrorOnDuplicatePublishOutputFiles=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ARG ASPNETCORE_ENVIRONMENT=Production
#ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ECP.Saga.Orchestrator.dll"]