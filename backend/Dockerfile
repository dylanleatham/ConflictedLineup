# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy csproj and restore dependencies
COPY src/ConflictedLineup.Api/ConflictedLineup.Api.csproj src/ConflictedLineup.Api/
RUN dotnet restore src/ConflictedLineup.Api/ConflictedLineup.Api.csproj

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/src/ConflictedLineup.Api
RUN dotnet build ConflictedLineup.Api.csproj -c Release -o /app/build
RUN dotnet publish ConflictedLineup.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set entry point
ENTRYPOINT ["dotnet", "ConflictedLineup.Api.dll"]
