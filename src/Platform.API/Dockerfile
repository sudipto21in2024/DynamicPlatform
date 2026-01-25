# Use ASP.NET Core SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DynamicPlatform.sln ./
COPY src/Platform.API/Platform.API.csproj src/Platform.API/
COPY src/Platform.Core/Platform.Core.csproj src/Platform.Core/
COPY src/Platform.Infrastructure/Platform.Infrastructure.csproj src/Platform.Infrastructure/
COPY src/Platform.Engine/Platform.Engine.csproj src/Platform.Engine/
COPY src/Platform.Runtime/Platform.Runtime.csproj src/Platform.Runtime/


# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and Publish
WORKDIR /src/src/Platform.API
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (default 8080)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Platform.API.dll"]
