# Ejecucion local con Docker

Esta configuracion es solo local. Render sigue usando `render.yaml` y no depende de `docker-compose.yml`.

## Levantar API y PostgreSQL

```powershell
docker compose up -d --build
```

Servicios locales:

- API: http://localhost:10000
- Swagger: http://localhost:10000/swagger
- Health: http://localhost:10000/health
- Readiness con PostgreSQL: http://localhost:10000/health/ready
- PostgreSQL desde el host: `localhost:55432`
- PostgreSQL desde la API en Docker: `postgres:5432`

La API local usa `ConnectionStrings__DefaultConnection` apuntando al servicio `postgres` de Compose. Render no usa esta variable local; en Render la conexion sigue entrando por `DATABASE_URL`.

## Ver logs

```powershell
docker compose logs -f api
docker compose logs -f postgres
```

## Detener

```powershell
docker compose down
```

## Reiniciar desde cero

Esto elimina tambien la data local de PostgreSQL:

```powershell
docker compose down -v
docker compose up -d --build
```

## Contenedor antiguo con nombre fijo

Si existe un contenedor antiguo llamado `hairy-paws-postgres`, ya no deberia bloquear `docker compose up` porque Compose ahora genera nombres propios. Puedes revisarlo con:

```powershell
docker ps -a --filter name=hairy-paws-postgres
```

Si ya no lo necesitas:

```powershell
docker rm hairy-paws-postgres
```
