    # Stage 1: Build
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /app

    # 1. Copy the project file
COPY ["DocumentService/Presentation/Document.Api/Document.Api.csproj", "DocumentService/Presentation/Document.Api/"]
    # 2. Restore
    RUN dotnet restore "DocumentService/Presentation/Document.Api/Document.Api.csproj"

    # 3. Copy everything
    COPY . .

    # 4. Build
    WORKDIR "/app/DocumentService/Presentation/Document.Api"
    RUN dotnet publish "Document.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

    # Stage 2: Runtime
    FROM mcr.microsoft.com/dotnet/aspnet:8.0
    WORKDIR /app
    COPY --from=build /app/publish .

    EXPOSE 8080
    ENTRYPOINT ["dotnet", "Document.Api.dll"]