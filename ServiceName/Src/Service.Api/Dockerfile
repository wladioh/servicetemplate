ARG RUNTIME_IMAGE_VERSION
ARG SDK_IMAGE_VERSION
FROM microsoft/dotnet:2.2-${RUNTIME_IMAGE_VERSION} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2-${SDK_IMAGE_VERSION} AS build
WORKDIR /src
COPY Src/Service.Api/Service.Api.csproj Src/Service.Api/
RUN dotnet restore Src/Service.Api/Service.Api.csproj
COPY . .
WORKDIR /src/Src/Service.Api
RUN dotnet build Service.Api.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Service.Api.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Service.Api.dll"]
