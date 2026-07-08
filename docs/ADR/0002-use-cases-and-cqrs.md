# ADR 0002: Casos de uso con CQRS simple

## Estado

Aceptado.

## Contexto

El backend tiene operaciones de escritura con reglas de negocio y consultas paginadas o filtradas. Mezclar todo dentro de controllers haria dificil revisar permisos, validaciones, auditoria y transiciones de estado.

## Decision

Modelar cada operacion de aplicacion como comando o query:

- Commands para acciones que cambian estado.
- Queries para lecturas.
- Handlers para orquestar permisos, validaciones, dominio, persistencia y respuestas.

No se usa MediatR para mantener menos dependencias y hacer explicita la inyeccion de handlers.

## Consecuencias positivas

- Los controllers quedan delgados.
- Cada caso de uso tiene una clase clara para revisar.
- Se facilita la ubicacion de reglas, auditoria y transiciones.
- La arquitectura es comprensible para evaluacion academica.

## Trade-offs

- El registro manual de handlers en `Application.DependencyInjection` puede crecer.
- Si el sistema aumenta, podria automatizarse el registro por convencion.

