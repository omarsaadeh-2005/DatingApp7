# -------------------------------------------------------------
# 1. Build Stage
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy csproj and restore dependencies as distinct layers for caching
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and publish the Release binary
COPY . ./
RUN dotnet publish -c Release -o out

# -------------------------------------------------------------
# 2. Runtime Stage
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Ensure ASP.NET Core listens on port 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy compiled app from the build stage
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "API.dll"]