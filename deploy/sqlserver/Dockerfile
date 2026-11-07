# SQL Server para el despliegue en Render (servicio privado con disco persistente).
# Se ejecuta como root porque el disco de Render se monta con permisos de root y la imagen oficial corre como el usuario
# "mssql", que entonces no podría escribir en /var/opt/mssql. Microsoft admite ejecutar el contenedor con --user root.
FROM mcr.microsoft.com/mssql/server:2022-latest
USER root
# Express es gratis y apto para producción (límite de 10 GB por base y ~1.4 GB de memoria para datos), así que no hay
# que licenciar Developer. MSSQL_MEMORY_LIMIT_MB deja margen dentro de los 2 GB de la instancia.
ENV ACCEPT_EULA=Y \
    MSSQL_PID=Express \
    MSSQL_MEMORY_LIMIT_MB=1536
EXPOSE 1433
