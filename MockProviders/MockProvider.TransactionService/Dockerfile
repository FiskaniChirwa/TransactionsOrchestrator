# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1. Copy the project file
COPY ["MockProviders/MockProvider.TransactionService/MockProvider.TransactionService.csproj", "MockProviders/MockProvider.TransactionService/"]

# 2. Restore
RUN dotnet restore "MockProviders/MockProvider.TransactionService/MockProvider.TransactionService.csproj"

# 3. Copy everything
COPY . .

# 4. Build
WORKDIR "/app/MockProviders/MockProvider.TransactionService"
RUN dotnet publish "MockProvider.TransactionService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "MockProvider.TransactionService.dll"]