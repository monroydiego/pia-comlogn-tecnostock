#!/usr/bin/env bash
# Ejecuta las pruebas unitarias y de integración con cobertura, une los resultados y falla si la
# cobertura de líneas queda por debajo del umbral (80% por defecto, requisito del PIA).
# Las pruebas de integración necesitan SQL Server: DB_HOST, DB_PORT y MSSQL_SA_PASSWORD.
# Lo usan Docker Compose (servicio "tests") y GitHub Actions.
set -euo pipefail

UMBRAL="${UMBRAL_COBERTURA:-80}"
SALIDA="${DIRECTORIO_COBERTURA:-coverage}"
export PATH="$PATH:$HOME/.dotnet/tools"

command -v reportgenerator >/dev/null 2>&1 || dotnet tool install --global dotnet-reportgenerator-globaltool >/dev/null

rm -rf tests/*/TestResults "$SALIDA"
dotnet test TecnoStock.sln --configuration Release --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal"

reportgenerator "-reports:tests/*/TestResults/*/coverage.cobertura.xml" "-targetdir:$SALIDA" \
  "-reporttypes:TextSummary;Cobertura;Html" "-assemblyfilters:+TecnoStock.*;-TecnoStock.Pruebas.*" >/dev/null
cat "$SALIDA/Summary.txt" | sed -n '1,20p'

LINEAS="$(grep -oP 'Line coverage: \K[0-9.]+' "$SALIDA/Summary.txt")"
if awk -v l="$LINEAS" -v u="$UMBRAL" 'BEGIN { exit !(l + 0 >= u + 0) }'; then
  echo "Cobertura de líneas: ${LINEAS}% (umbral ${UMBRAL}%): CUMPLE"
else
  echo "Cobertura de líneas: ${LINEAS}% es menor al umbral de ${UMBRAL}%" >&2
  exit 1
fi
