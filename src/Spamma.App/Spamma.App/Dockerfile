FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
EXPOSE 25

# Node.js build stage
FROM node:20-alpine AS node-build
WORKDIR /src
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=0.1.0
ARG BUILD_DATE=unknown
ARG VCS_REF=unknown
WORKDIR /src
COPY ["src/Spamma.App/Spamma.App/Spamma.App.csproj", "src/Spamma.App/Spamma.App/"]
COPY ["src/modules/Spamma.Modules.DomainManagement/Spamma.Modules.DomainManagement.csproj", "src/modules/Spamma.Modules.DomainManagement/"]
COPY ["src/modules/Spamma.Modules.Common/Spamma.Modules.Common.csproj", "src/modules/Spamma.Modules.Common/"]
COPY ["shared/Spamma.Shared/Spamma.Shared.shproj", "shared/Spamma.Shared/"]
COPY ["src/modules/Spamma.Modules.DomainManagement.Client/Spamma.Modules.DomainManagement.Client.csproj", "src/modules/Spamma.Modules.DomainManagement.Client/"]
COPY ["src/modules/Spamma.Modules.UserManagement/Spamma.Modules.UserManagement.csproj", "src/modules/Spamma.Modules.UserManagement/"]
COPY ["src/modules/Spamma.Modules.UserManagement.Client/Spamma.Modules.UserManagement.Client.csproj", "src/modules/Spamma.Modules.UserManagement.Client/"]
COPY ["src/modules/Spamma.Modules.Common.Client/Spamma.Modules.Common.Client.csproj", "src/modules/Spamma.Modules.Common.Client/"]
COPY ["src/modules/Spamma.Modules.EmailInbox/Spamma.Modules.EmailInbox.csproj", "src/modules/Spamma.Modules.EmailInbox/"]
COPY ["src/modules/Spamma.Modules.EmailInbox.Client/Spamma.Modules.EmailInbox.Client.csproj", "src/modules/Spamma.Modules.EmailInbox.Client/"]
COPY ["src/Spamma.App/Spamma.App.Client/Spamma.App.Client.csproj", "src/Spamma.App/Spamma.App.Client/"]
RUN dotnet restore "src/Spamma.App/Spamma.App/Spamma.App.csproj"
COPY . .
WORKDIR "/src/src/Spamma.App/Spamma.App"
RUN dotnet build "./Spamma.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Spamma.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG BUILD_VERSION=0.1.0
ARG BUILD_DATE=unknown
ARG VCS_REF=unknown
WORKDIR /app
LABEL org.opencontainers.image.version=$BUILD_VERSION \
      org.opencontainers.image.created=$BUILD_DATE \
      org.opencontainers.image.revision=$VCS_REF
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Spamma.App.dll"]
