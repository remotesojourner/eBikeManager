FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/EBikeManager.Application/EBikeManager.Application.csproj src/EBikeManager.Application/
COPY src/EBikeManager.Web/EBikeManager.Web.csproj src/EBikeManager.Web/

RUN dotnet restore src/EBikeManager.Web/EBikeManager.Web.csproj -a $TARGETARCH

COPY . .
WORKDIR /src/src/EBikeManager.Web
ARG VERSION
RUN dotnet publish -c Release -a $TARGETARCH --no-restore -o /app/publish /p:UseAppHost=false ${VERSION:+/p:Version=$VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/data

ARG PORT=2004
ENV PORT=${PORT}
ENV DOTNET_RUNNING_IN_CONTAINER=true

VOLUME ["/app/data"]
EXPOSE ${PORT}

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 CMD curl -fsS "http://127.0.0.1:${PORT}/healthz" || exit 1

ENTRYPOINT ["dotnet", "EBikeManager.Web.dll"]
