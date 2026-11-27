# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versionado
[SemVer](https://semver.org/lang/es/).

## [1.0.3] - 2026-11-27

### Mejorado
- La documentación OpenAPI (Scalar) incluye la descripción de cada endpoint.

### Interno
- Comentarios de código simplificados.
- Se elimina `public partial class Program`, innecesario desde .NET 10.

## [1.0.2] - 2026-11-27

### Documentación
- README ampliado: tecnologías y su uso, arquitectura por capas, recorrido de una petición, estructura del
  proyecto, modelo de datos, endpoints, códigos de error, configuración, métricas de calidad y CI/CD.

## [1.0.1] - 2026-11-27

### Corregido
- Pipeline: la propuesta de despliegue a producción fallaba si el repo GitOps ya declaraba la versión;
  ahora termina sin cambios.

## [1.0.0] - 2026-11-27

Primera versión.

### Autorizaciones
- Registro con cálculo de montos por línea de servicio: la ARS cubre el 70 % y el afiliado paga el 30 %
  (porcentaje configurable, redondeo a 2 decimales).
- Idempotencia en la creación mediante el header `Idempotency-Key`.
- Consulta de detalle con `ETag`.
- Búsqueda con filtros combinables y paginación por cursor (keyset).
- Actualización con concurrencia optimista (`If-Match` obligatorio: 428 / 412).
- Inactivación lógica; una autorización inactiva no puede modificarse.
- Validaciones: montos positivos, catálogos existentes, sin duplicados.

### Catálogos
- Afiliados, prestadores, contratantes, intermediarios, tipos de prestador y tipos de servicio, con datos
  iniciales. Catálogos pequeños en caché (HybridCache).

### Integración
- Eventos `authorization.created`, `.updated` y `.deactivated` publicados en RabbitMQ mediante el patrón
  Outbox (entrega garantizada al menos una vez).

### Seguridad y operación
- Autenticación JWT con roles `Operador` (lectura/escritura) y `Consulta` (solo lectura).
- Errores en formato ProblemDetails, correlation id, logs estructurados (Serilog).
- Health checks `/health/live` y `/health/ready`; documentación OpenAPI + Scalar.

### Entrega
- Docker y Docker Compose (API + PostgreSQL + RabbitMQ).
- CI en GitHub Actions: pruebas (unitarias + integración con Testcontainers), cobertura, SonarCloud con
  quality gate, escaneo Trivy y publicación de imagen en GHCR.
- Despliegue GitOps: manifiestos Kubernetes (Kustomize) y Argo CD en
  [ars-humano-gitops](https://github.com/zaizo01/ars-humano-gitops); migraciones como Job `--migrate`.

[1.0.3]: https://github.com/zaizo01/ars-humano-api/releases/tag/v1.0.3
[1.0.2]: https://github.com/zaizo01/ars-humano-api/releases/tag/v1.0.2
[1.0.1]: https://github.com/zaizo01/ars-humano-api/releases/tag/v1.0.1
[1.0.0]: https://github.com/zaizo01/ars-humano-api/releases/tag/v1.0.0
