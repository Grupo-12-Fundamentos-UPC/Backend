# ADR 0001: Clean Architecture con estilo hexagonal

## Estado

Aceptado.

## Contexto

El backend necesita ser entendible, testeable y facil de evolucionar. Tambien debe mostrar de forma clara los fundamentos de arquitectura de software: separacion de responsabilidades, inversion de dependencias, dominio independiente y adapters reemplazables.

## Decision

Usar un monolito modular con Clean Architecture y estilo hexagonal:

- `Domain` contiene reglas de negocio y no depende de capas externas.
- `Application` contiene casos de uso y define puertos.
- `Infrastructure` implementa los puertos con tecnologias concretas.
- `Api` actua como adapter HTTP y composition root.
- `Contracts` mantiene los DTOs publicos de la API.

## Consecuencias positivas

- El dominio queda protegido de frameworks.
- Los casos de uso se pueden probar y razonar de forma aislada.
- La infraestructura se puede reemplazar mediante adapters.
- La estructura del repositorio comunica intencion arquitectonica.

## Trade-offs

- Hay mas proyectos y archivos que en una API CRUD simple.
- `IApplicationDbContext` mantiene una abstraccion EF-aware para acelerar el desarrollo academico con LINQ y migraciones.
- La separacion exige disciplina para no mover reglas de negocio hacia controllers o adapters.

