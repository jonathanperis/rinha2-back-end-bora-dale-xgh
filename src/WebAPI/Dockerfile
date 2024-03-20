FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

LABEL maintainer "Jonathan Peris"

USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

ARG AOT=true
ARG Trim=false
ARG ExtraOptimize=true

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["src/WebAPI/WebAPI.csproj", "src/WebAPI/"]

RUN dotnet restore -r linux-x64 "./src/WebAPI/./WebAPI.csproj" -p:Configuration=Release -p:AOT=${AOT} -p:Trim=${Trim}

COPY . .

WORKDIR "/src/src/WebAPI"

FROM build AS publish

RUN dotnet publish -r linux-x64 "./WebAPI.csproj" --no-restore -c $BUILD_CONFIGURATION -p:AOT=${AOT} -p:Trim=${Trim} -p:ExtraOptimize=${ExtraOptimize} -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS final

WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["./WebAPI"]