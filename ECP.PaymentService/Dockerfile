# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files first for Docker cache
COPY ECP.PaymentService/ECP.PaymentService.csproj ECP.PaymentService/
COPY Contracts/Contracts.csproj Contracts/

RUN dotnet restore ECP.PaymentService/ECP.PaymentService.csproj

# Copy source code
COPY . .

WORKDIR /src/ECP.PaymentService

RUN dotnet publish \
    ECP.PaymentService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
#ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ECP.PaymentService.dll"]