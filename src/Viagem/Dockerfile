FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=1000
USER $APP_UID
WORKDIR /app
EXPOSE 2003

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Viagem/Viagem.csproj", "src/Viagem/"]
COPY ["src/Viagem.Data/Viagem.Data.csproj", "src/Viagem.Data/"]
RUN dotnet restore "./src/Viagem/Viagem.csproj"
COPY . .
WORKDIR "/src/src/Viagem"
RUN dotnet build "./Viagem.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Viagem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Viagem.dll"]