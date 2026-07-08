# Application Ports

Esta carpeta contiene los puertos que el nucleo de aplicacion necesita para hablar con el exterior sin depender directamente de adapters concretos.

## Puertos actuales

- `IApplicationDbContext`: persistencia transaccional y consultas del modelo.
- `IJwtTokenService`: generacion de access tokens y refresh tokens.
- `IPasswordHasher`: hashing y verificacion de passwords.
- `IFileStorageService`: almacenamiento de archivos subidos.
- `ICurrentUserService`: identidad del usuario autenticado.
- `IDateTimeProvider`: reloj del sistema.
- `IAuditService`: escritura de eventos de auditoria.
- `INotificationService`: creacion de notificaciones.

## Regla

Los casos de uso dependen de estos puertos. Los adapters concretos viven en `HairyPaws.Infrastructure`.

