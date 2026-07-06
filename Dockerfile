# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files to preserve directory structure
COPY ["StoreApi-Architecture.sln", "./"]
COPY ["StoreApi.csproj", "./"]
COPY ["StoreApi.Tests/StoreApi.Tests.csproj", "./StoreApi.Tests/"]
COPY ["Microservices/ApiGateway/ApiGateway.csproj", "./Microservices/ApiGateway/"]
COPY ["Microservices/BffService/BffService.csproj", "./Microservices/BffService/"]
COPY ["Microservices/InventoryService/InventoryService.csproj", "./Microservices/InventoryService/"]
COPY ["Microservices/NotificationService/NotificationService.csproj", "./Microservices/NotificationService/"]
COPY ["Microservices/OrderService/OrderService.csproj", "./Microservices/OrderService/"]
COPY ["Microservices/ProductCatalogService/ProductCatalogService.csproj", "./Microservices/ProductCatalogService/"]

# Restore dependencies for the entire solution
RUN dotnet restore "StoreApi-Architecture.sln"

# Copy the rest of the source code
COPY . .

# Build and publish the main project
RUN dotnet publish "StoreApi.csproj" -c Release -o /app/publish --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080

# Copy published output
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "StoreApi.dll"]