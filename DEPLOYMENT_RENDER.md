# Despliegue en Render

Este documento prepara el backend Hairy Paws para un despliegue academico en Render usando Docker, Render Blueprint y PostgreSQL administrado. No es una configuracion recomendada para produccion.

## Resumen tecnico del proyecto

- Solucion: `HairyPaws.sln`.
- SDK .NET: `10.0.202`, definido en `global.json`.
- Target framework: `net10.0` en API, Application, Contracts, Domain, Infrastructure y tests.
- C#: `14.0`, inferido por MSBuild para `net10.0`; no hay `LangVersion` explicito.
- Framework principal: ASP.NET Core Web API con controladores.
- Estructura:
  - `src/HairyPaws.Api`: controllers, middleware, Swagger, health checks y arranque HTTP.
  - `src/HairyPaws.Application`: casos de uso, CQRS simple, validaciones, interfaces y mapeos.
  - `src/HairyPaws.Domain`: entidades, enums y reglas de dominio.
  - `src/HairyPaws.Infrastructure`: EF Core, PostgreSQL, migraciones, JWT, hashing, storage local y servicios.
  - `src/HairyPaws.Contracts`: DTOs de requests/responses.
  - `tests/HairyPaws.Tests.Unit`: tests unitarios con xUnit.
  - `tests/HairyPaws.Tests.Integration`: tests de integracion con Testcontainers PostgreSQL.
- Librerias principales:
  - ASP.NET Core / Controllers.
  - Entity Framework Core `10.0.x`.
  - `Npgsql.EntityFrameworkCore.PostgreSQL`.
  - `EFCore.NamingConventions`.
  - FluentValidation.
  - Swashbuckle/OpenAPI.
  - JWT Bearer.
  - xUnit, FluentAssertions, Respawn y Testcontainers.

## Base de datos

El proyecto usa Entity Framework Core con PostgreSQL mediante Npgsql. Las migraciones estan en:

`src/HairyPaws.Infrastructure/Persistence/Migrations`

La configuracion local sigue leyendo:

`ConnectionStrings:DefaultConnection`

Para Render se agrego soporte a:

`DATABASE_URL`

Render entrega PostgreSQL como URL del tipo `postgresql://user:password@host:port/database`. El backend ahora la convierte a una connection string compatible con Npgsql y mantiene esta prioridad:

1. `DATABASE_URL`
2. `ConnectionStrings:DefaultConnection`

EF/Npgsql queda configurado con reintentos transitorios para tolerar el arranque inicial de PostgreSQL en Render.

## Swagger y health checks

Swagger se configura en:

`src/HairyPaws.Api/Common/Extensions/SwaggerExtensions.cs`

Antes solo se habilitaba en `Development` o `Testing`. Para la entrega academica se agrego el entorno `Academic`, usado por Render, para exponer Swagger sin recomendarlo para produccion.

Endpoints esperados:

- `/`: redirige a `/swagger` en `Development`, `Testing` y `Academic`.
- `/swagger`: Swagger UI publico para evidencias.
- `/swagger/v1/swagger.json`: documento OpenAPI.
- `/health`: liveness basico.
- `/health/ready`: readiness con validacion de conexion a PostgreSQL.

## Archivos agregados o modificados

Agregados:

- `Dockerfile`
- `.dockerignore`
- `render.yaml`
- `DEPLOYMENT_RENDER.md`
- `src/HairyPaws.Infrastructure/Persistence/PostgresConnectionString.cs`

Modificados:

- `src/HairyPaws.Api/Program.cs`
- `src/HairyPaws.Infrastructure/DependencyInjection.cs`
- `src/HairyPaws.Infrastructure/Persistence/ApplicationDbContextFactory.cs`

## Variables de entorno en Render

Definidas por `render.yaml`:

- `ASPNETCORE_URLS=http://0.0.0.0:10000`
- `ASPNETCORE_ENVIRONMENT=Academic`
- `DATABASE_URL`: viene de `hairypaws-db` usando `fromDatabase`.
- `Jwt__Issuer=HairyPaws.Api.Academic`
- `Jwt__Audience=HairyPaws.Clients.Academic`
- `Jwt__Secret`: generado automaticamente por Render.
- `Jwt__AccessTokenLifetimeMinutes=30`
- `Jwt__RefreshTokenLifetimeDays=14`
- `Seed__AdminUser__Email=admin@hairypaws.academic`
- `Seed__AdminUser__Password`: `sync: false`; Render pedira ingresarlo al crear el Blueprint.
- `Seed__AdminUser__FirstName=Academic`
- `Seed__AdminUser__LastName=Administrator`

No hardcodees secretos reales. Para el admin academico usa una clave temporal solo para la entrega.

## Ejecucion local

Levantar PostgreSQL local:

```powershell
docker compose up -d
```

Si Docker reporta que el contenedor `hairy-paws-postgres` ya existe, revisa su estado con:

```powershell
docker ps -a --filter name=hairy-paws-postgres
```

Si es un contenedor detenido de una ejecucion anterior y no necesitas conservarlo, puedes eliminarlo y volver a ejecutar `docker compose up -d`.

Restaurar, compilar y probar:

```powershell
dotnet restore
dotnet build
dotnet test
```

Ejecutar la API local:

```powershell
dotnet run --project src/HairyPaws.Api/HairyPaws.Api.csproj
```

La API local usa `http://localhost:5184` segun `launchSettings.json`.

## Prueba local con Docker

Construir imagen:

```powershell
docker build -t hairypaws-api:render .
```

Ejecutar contra el PostgreSQL del `docker-compose.yml` desde Windows:

```powershell
docker run --rm -p 10000:10000 `
  -e ASPNETCORE_ENVIRONMENT=Academic `
  -e ASPNETCORE_URLS=http://0.0.0.0:10000 `
  -e "DATABASE_URL=postgresql://hairy_paws:hairy_paws@host.docker.internal:55432/hairy_paws" `
  -e Jwt__Issuer=HairyPaws.Api.Academic `
  -e Jwt__Audience=HairyPaws.Clients.Academic `
  -e Jwt__Secret=LocalDockerJwtSecretThatIsLongEnough123456 `
  -e Jwt__AccessTokenLifetimeMinutes=30 `
  -e Jwt__RefreshTokenLifetimeDays=14 `
  -e Seed__AdminUser__Email=admin@hairypaws.local `
  -e Seed__AdminUser__Password=Admin123! `
  hairypaws-api:render
```

Verificar:

```powershell
curl http://localhost:10000/health
curl http://localhost:10000/health/ready
```

Abrir:

`http://localhost:10000/swagger`

## Despliegue en Render con Blueprint

1. Subir estos cambios a un repositorio Git conectado a Render.
2. En Render, ir a **Blueprints** o **New + > Blueprint**.
3. Seleccionar el repositorio que contiene `render.yaml` en la raiz.
4. Confirmar que Render detecta:
   - Web Service: `hairypaws-api`
   - PostgreSQL: `hairypaws-db`
5. Cuando Render solicite `Seed__AdminUser__Password`, ingresar una clave temporal academica.
6. Crear el Blueprint.
7. Esperar a que la base de datos termine de crearse y luego el Web Service haga build/deploy.

## Verificacion del despliegue

Cuando Render indique que el deploy esta activo, usar la URL publica del Web Service:

- `https://<servicio>.onrender.com/`
- `https://<servicio>.onrender.com/swagger`
- `https://<servicio>.onrender.com/health`
- `https://<servicio>.onrender.com/health/ready`

`/health/ready` debe devolver `Healthy` cuando la API pueda conectarse a PostgreSQL.

## Capturas recomendadas para el informe academico

- Blueprint creado con `hairypaws-api` y `hairypaws-db`.
- Logs de build exitoso del Dockerfile.
- Variables de entorno del Web Service, ocultando valores secretos.
- Swagger UI en `/swagger`.
- Respuesta JSON de `/health`.
- Respuesta JSON de `/health/ready`.
- Tabla de Render Postgres mostrando `hairy_paws`.

## Posibles errores y solucion

- El deploy falla por JWT: revisar que `Jwt__Secret` exista y tenga al menos 32 caracteres.
- `/health/ready` devuelve `Unhealthy`: esperar a que PostgreSQL termine de inicializar o revisar `DATABASE_URL`.
- Swagger no aparece: confirmar `ASPNETCORE_ENVIRONMENT=Academic`.
- Error de migraciones: revisar logs del Web Service; las migraciones se ejecutan al iniciar en `Academic`.
- Render Free puede dormir el servicio por inactividad; la primera peticion puede tardar mas.
- Render Free y PostgreSQL Free son adecuados para evidencias academicas, no para produccion.

## Referencias

- Render Blueprint YAML Reference: https://render.com/docs/blueprint-spec
- Render PostgreSQL connections: https://render.com/docs/postgresql-creating-connecting
- Microsoft ASP.NET Core Docker images: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0
