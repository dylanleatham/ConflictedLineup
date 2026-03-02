# Stage 1: Build React frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
ENV VITE_SPOTIFY_CLIENT_ID=c24c01e307fc439e9148244da86142be
RUN npm run build

# Stage 2: Build ASP.NET Core backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /app
COPY backend/src/ConflictedLineup.Api/ConflictedLineup.Api.csproj ./backend/src/ConflictedLineup.Api/
RUN dotnet restore ./backend/src/ConflictedLineup.Api/ConflictedLineup.Api.csproj
COPY backend/ ./backend/
RUN dotnet publish ./backend/src/ConflictedLineup.Api/ConflictedLineup.Api.csproj -c Release -o /out

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend-build /out ./
COPY --from=frontend-build /app/frontend/dist ./wwwroot/
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl --fail http://localhost:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "ConflictedLineup.Api.dll"]
