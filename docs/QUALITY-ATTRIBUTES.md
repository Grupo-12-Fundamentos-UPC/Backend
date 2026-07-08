# Atributos de calidad

Este documento resume como el backend cubre preocupaciones tipicas de arquitectura profesional.

## Mantenibilidad

- Separacion por proyectos: `Api`, `Application`, `Domain`, `Infrastructure` y `Contracts`.
- Casos de uso pequenos y localizables mediante comandos y queries.
- Puertos explicitos en `Application/Common/Ports`.
- Pruebas de arquitectura para proteger la regla de dependencias.

## Testabilidad

- Reglas de dominio probadas con unit tests.
- Pruebas de arquitectura en `tests/HairyPaws.Tests.Unit/Architecture`.
- Pruebas de integracion con `WebApplicationFactory`, PostgreSQL real via Testcontainers y limpieza de datos con Respawn.

## Seguridad

- JWT Bearer con validacion de issuer, audience, lifetime y signing key.
- Politicas por roles: Admin, Adopter, Ong, Owner/Ong.
- Manejo centralizado de errores para evitar respuestas inconsistentes.
- Refresh tokens con hash en persistencia.

## Observabilidad y operabilidad

- `/health` para liveness.
- `/health/ready` para readiness con base de datos.
- Swagger/OpenAPI para inspeccionar endpoints.
- Dockerfile y Docker Compose para ejecucion reproducible.
- Storage local configurable con `Storage__UploadsPath` para evitar rutas hardcodeadas entre local, tests y nube.
- Plantillas de despliegue para Google Cloud Run, Azure Container Apps y AWS App Runner.
- GitHub Actions para build, tests y construccion de imagen Docker.

## Integridad de datos

- Migraciones EF Core versionadas.
- Indices para busquedas frecuentes.
- Constraints de base de datos para reglas criticas.
- Indices unicos filtrados para evitar solicitudes de adopcion activas duplicadas.

## Escalabilidad evolutiva

El sistema es un monolito modular, una decision apropiada para el alcance academico. Permite evolucionar sin distribuir prematuramente la complejidad. Si el dominio crece, los modulos candidatos a extraer serian:

- Identity
- Organizations
- Pets
- Adoption
- Donations
- Events
- Notifications
