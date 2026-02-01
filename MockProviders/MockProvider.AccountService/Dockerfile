# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1. Copy the project file
COPY ["MockProviders/MockProvider.AccountService/MockProvider.AccountService.csproj", "MockProviders/MockProvider.AccountService/"]

# 2. Restore
RUN dotnet restore "MockProviders/MockProvider.AccountService/MockProvider.AccountService.csproj"

# 3. Copy everything from the root (to include Shared projects if any)
COPY . .

# 4. Build
WORKDIR "/app/MockProviders/MockProvider.AccountService"
RUN dotnet publish "MockProvider.AccountService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "MockProvider.AccountService.dll"]