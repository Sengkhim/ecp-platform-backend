FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["ECP.PaymentService/ECP.PaymentService.csproj", "ECP.PaymentService/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]

RUN dotnet restore "ECP.PaymentService/ECP.PaymentService.csproj"

COPY ECP.PaymentService/ ECP.PaymentService/
COPY Contracts/ Contracts/

WORKDIR "/src/ECP.PaymentService"
RUN dotnet build "./ECP.PaymentService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ECP.PaymentService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECP.PaymentService.dll"]