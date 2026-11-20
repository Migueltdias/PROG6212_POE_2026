FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj

COPY src/RaceDay.Web/ src/RaceDay.Web/
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_NOLOGO=true \
    RaceDayApi__BaseUrl=http://host.docker.internal:5088/

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
