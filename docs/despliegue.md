# Despliegue de TecnoStock en Render

Render no ofrece SQL Server administrado (solo PostgreSQL), así que el despliegue usa **dos servicios** definidos en [`render.yaml`](../render.yaml):

| Servicio | Tipo | Qué es | Plan |
|---|---|---|---|
| `tecnostock-db` | Servicio privado (`pserv`) | SQL Server 2022 **Express** en una imagen propia (`deploy/sqlserver/Dockerfile`) con un disco persistente de 5 GB en `/var/opt/mssql` | `1c-2g` (1 CPU, 2 GB) |
| `tecnostock-api` | Servicio web | La API con Swagger (Dockerfile de la raíz), pública por HTTPS | `0.5c-512mb` (0.5 CPU, 512 MB) |

La base de datos **no tiene dirección pública**: solo la API la alcanza por la red privada de Render (`DB_HOST` se toma automáticamente del servicio de la base).

## Costo (léalo antes de crear el Blueprint)

- Los **discos persistentes y los servicios privados exigen un plan de pago**; el plan gratuito de Render no admite ninguno de los dos (documentación oficial de Render: «Free web services», «Persistent disks»).
- SQL Server necesita al menos 2 GB de RAM, por eso la base va en `1c-2g`. Con los precios publicados por Render (Starter 7 USD/mes y Standard 25 USD/mes por instancia) el conjunto cuesta **alrededor de 32 USD al mes más el disco**. El precio del disco por GB no pude confirmarlo en la documentación oficial; consúltelo en el panel antes de aplicar. Verifique también el precio vigente de los planes en <https://render.com/pricing>.
- La API en plan gratuito (`free`) bajaría el costo en 7 USD, pero se duerme a los 15 minutos sin tráfico y tarda cerca de un minuto en despertar, lo que no conviene en una defensa en vivo. Además la documentación no aclara si un servicio gratuito puede conectarse a uno privado.

### Alternativa sin costo en la base de datos
La API acepta una cadena de conexión completa en la variable `ConnectionStrings__Default`, por lo que puede usarse una base SQL externa (por ejemplo Azure SQL con su oferta gratuita) y dejar en Render solo la API. En ese caso se elimina `tecnostock-db` del `render.yaml`, se quitan `DB_HOST`, `DB_PORT`, `DB_NAME` y `MSSQL_SA_PASSWORD` de la API y se define `ConnectionStrings__Default` en el panel. No se probó esta variante.

## Pasos

1. **Subir el repositorio a GitHub** (`git push`; el `.env` no se sube, está en `.gitignore`).
2. En el panel de Render: **New → Blueprint**, conectar el repositorio y elegir la rama `main`. Render lee `render.yaml` y propone los dos servicios.
3. Render pide el valor de `MSSQL_SA_PASSWORD` **dos veces** (una por servicio). Escriba **la misma contraseña** en ambos: mínimo 8 caracteres con mayúsculas, minúsculas, números y símbolos. Nunca la guarde en el repositorio.
4. **Apply**. La primera vez Render construye las dos imágenes (varios minutos). La API espera hasta 2 minutos a que SQL Server esté listo; si la base tarda más, Render reinicia la API y reintenta.
5. Al terminar, el servicio web muestra su dirección (por ejemplo `https://tecnostock-api.onrender.com`; si el nombre está tomado, Render agrega un sufijo). Abra `/swagger`.
6. En el primer arranque la API crea las tablas y carga los datos semilla (catálogo, clientes, sucursales y política). Compruebe con `GET /politica/devolucion` (debe responder 200) y siga los recorridos de [`docs/api/guia_de_uso.md`](api/guia_de_uso.md) cambiando `http://localhost:8080` por la dirección pública.

Después del primer despliegue, la API se redespliega sola en cada `push` **solo si pasan las pruebas** de GitHub Actions (`autoDeployTrigger: checksPass`). La base no se redespliega sola (`"off"`): reiniciarla interrumpe el servicio; si cambia `deploy/sqlserver/Dockerfile`, use *Manual Deploy* en `tecnostock-db`.

## Qué se verificó y qué no

**Verificado localmente** (con Docker, simulando la topología de Render): la imagen de la base arranca como root sobre un volumen con permisos de root y SQL Server Express funciona con 2 GB de memoria; la API en 512 MB (usa unos 80 MB) se conecta por una red privada con las mismas variables que define el `render.yaml`; la comprobación `/politica/devolucion` responde 200; los datos **sobreviven al reinicio** de la base; y si la API arranca con la base apagada no se cae: queda sana unos 10 segundos después de que la base responde.

**Verificado contra la documentación oficial de Render:** los nombres de los planes (`free`, `0.5c-512mb`, `1c-2g`, `2c-4g`), que los discos solo existen en servicios de pago con una sola instancia, que el puerto se fija con `PORT`, y el `render.yaml` valida con 0 errores contra el esquema JSON oficial (`https://render.com/schema/render.yaml.json`).

**No se pudo verificar** (requiere una cuenta de Render): que Render detecte el puerto 1433 del servicio privado y que construya la imagen basada en `mcr.microsoft.com`. Si el servicio de la base no queda «Live», revise sus *Logs* y confirme que `PORT` vale `1433`.

## Límites del despliegue

- **Sin autenticación.** La API confía en los encabezados `X-Usuario` y `X-Rol` (decisión del diseño, puerto `IContextoDeUsuario`). En una dirección pública cualquiera puede enviar `X-Rol: Supervisor`. Es aceptable para la demostración del curso, **no** para producción: habría que reemplazar `ContextoDeUsuarioPorEncabezados` por autenticación real.
- **Una sola instancia y breve interrupción:** con disco persistente, Render detiene la instancia anterior antes de iniciar la nueva (sin despliegue sin tiempo muerto).
- **Respaldos:** Render toma una instantánea diaria del disco y la conserva 7 días; no hay restauración puntual.
- **Esquema:** la base se crea con `EnsureCreated` (C-27). Un cambio futuro de esquema exige migraciones o recrear la base (borrar el disco).
- **Express:** hasta 10 GB por base de datos y unos 1.4 GB de memoria para datos; sobra para este proyecto.
- **Datos de ejemplo:** el catálogo, los clientes y las series son sintéticos (BRD 7.3).
