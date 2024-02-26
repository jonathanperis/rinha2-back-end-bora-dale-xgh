FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

LABEL maintainer "Jonathan Peris"

USER app
WORKDIR /app
# EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["src/WebAPI/WebAPI.csproj", "src/WebAPI/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]

RUN dotnet restore "./src/WebAPI/./WebAPI.csproj"

COPY . .

WORKDIR "/src/src/WebAPI"

RUN dotnet build "./WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish -r linux-x64 "./WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS final

WORKDIR /app
# EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["./WebAPI"]