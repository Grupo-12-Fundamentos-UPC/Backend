# Guia de revision academica

Esta guia muestra donde encontrar evidencia concreta de fundamentos de arquitectura de software profesional.

## 1. Separacion de capas

Revisar:

- `src/HairyPaws.Api`
- `src/HairyPaws.Application`
- `src/HairyPaws.Domain`
- `src/HairyPaws.Infrastructure`
- `src/HairyPaws.Contracts`

Evidencia esperada:

- API recibe HTTP y no contiene reglas complejas.
- Application contiene casos de uso.
- Domain contiene reglas de negocio.
- Infrastructure contiene detalles externos.

## 2. Arquitectura hexagonal

Revisar:

- `docs/ARCHITECTURE.md`
- `src/HairyPaws.Application/Common/Ports`
- `src/HairyPaws.Infrastructure/DependencyInjection.cs`

Evidencia esperada:

- Los casos de uso dependen de puertos.
- Infrastructure implementa los puertos.
- Api compone las dependencias.

## 3. DDD tactico

Revisar entidades:

- `src/HairyPaws.Domain/Pets/Entities/Pet.cs`
- `src/HairyPaws.Domain/Adoption/Entities/AdoptionRequest.cs`
- `src/HairyPaws.Domain/Visits/Entities/Visit.cs`

Evidencia esperada:

- Las entidades tienen comportamiento.
- Hay fabricas estaticas.
- Hay transiciones de estado.
- Hay reglas como `CanReceiveAdoptionRequests`, `CanApprove`, `CanCreateVisit` y `GetPublishValidationErrors`.

## 4. CQRS y casos de uso

Revisar:

- `src/HairyPaws.Application/Common/CQRS`
- `src/HairyPaws.Application/**/Commands`
- `src/HairyPaws.Application/**/Queries`

Evidencia esperada:

- Escrituras separadas en commands.
- Lecturas separadas en queries.
- Controllers delegan a handlers.

## 5. Seguridad

Revisar:

- `src/HairyPaws.Api/Common/Extensions/AuthenticationExtensions.cs`
- `src/HairyPaws.Application/Common/Security`
- `src/HairyPaws.Infrastructure/Auth`

Evidencia esperada:

- JWT Bearer.
- Politicas por roles.
- Validacion de issuer, audience, lifetime y signing key.

## 6. Persistencia profesional

Revisar:

- `src/HairyPaws.Infrastructure/Persistence`
- `src/HairyPaws.Infrastructure/Persistence/Configurations`
- `src/HairyPaws.Infrastructure/Persistence/Migrations`

Evidencia esperada:

- PostgreSQL con EF Core/Npgsql.
- Configuraciones por entidad.
- Indices, constraints e indices filtrados.
- Migraciones versionadas.

## 7. Calidad y pruebas

Ejecutar:

```powershell
dotnet build
dotnet test tests\HairyPaws.Tests.Unit\HairyPaws.Tests.Unit.csproj
```

Revisar:

- `tests/HairyPaws.Tests.Unit/Domain`
- `tests/HairyPaws.Tests.Unit/Architecture`
- `tests/HairyPaws.Tests.Integration`

Evidencia esperada:

- Pruebas de dominio.
- Pruebas de arquitectura que protegen dependencias.
- Pruebas de integracion con PostgreSQL real via Testcontainers.

## 8. Operabilidad

Revisar:

- `Dockerfile`
- `docker-entrypoint.sh`
- `docker-compose.yml`
- `deploy/google/cloudrun.service.yaml`
- `deploy/azure/containerapp.yaml`
- `deploy/aws/apprunner-service.yaml`
- `.github/workflows/ci.yml`
- `LOCAL_DOCKER.md`
- `DEPLOYMENT_RENDER.md`

Evidencia esperada:

- Ejecucion local reproducible.
- Health checks.
- Swagger.
- Configuracion separada para entorno academico/local.
- Ruta de uploads configurable por variable `Storage__UploadsPath`.
- Plantillas cloud-ready para contenedores gestionados.
- CI basico con build, tests y Docker build.
