# Infrastructure Adapters

Este proyecto contiene adapters externos que implementan los puertos definidos por `HairyPaws.Application`.

## Adapters

- `Persistence`: adapter PostgreSQL/EF Core.
- `Auth`: adapter JWT.
- `Services`: adapters de hashing, usuario actual, reloj, archivos y auditoria.

## Regla

Infrastructure puede depender de `Application` y `Domain`, pero el core no debe depender de Infrastructure. Esta regla esta cubierta por pruebas de arquitectura.

