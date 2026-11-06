# syntax=docker/dockerfile:1
# Etapas: restaurar (dependencias en caché) -> publicacion -> pruebas -> final (API, última para que sea la predeterminada).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restaurar
WORKDIR /src
COPY Directory.Build.props TecnoStock.sln ./
COPY src/domain/*.csproj src/domain/
COPY src/application/*.csproj src/application/
COPY src/infrastructure/*.csproj src/infrastructure/
COPY src/api/*.csproj src/api/
COPY tests/unitarias/*.csproj tests/unitarias/
COPY tests/integracion/*.csproj tests/integracion/
RUN dotnet restore TecnoStock.sln
COPY . .

FROM restaurar AS publicacion
RUN dotnet publish src/api/TecnoStock.Api.csproj --configuration Release --no-restore --output /app/publicar

# Servicio "tests" de Docker Compose: pruebas unitarias e integración con cobertura mínima de 80%.
FROM restaurar AS pruebas
RUN dotnet tool install --global dotnet-reportgenerator-globaltool
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["bash", "scripts/pruebas-con-cobertura.sh"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publicacion /app/publicar .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TecnoStock.Api.dll"]
