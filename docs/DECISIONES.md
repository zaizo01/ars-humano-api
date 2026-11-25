# Decisiones técnicas

Documento vivo. Cada decisión indica el contexto, la elección y la alternativa descartada.

## 1. Arquitectura: Clean Architecture en 4 proyectos

`Domain ← Application ← Infrastructure ← Api`. El dominio no depende de nada; Application define
puertos (`IApplicationDbContext`, `ICurrentUser`) que Infrastructure implementa.

Dentro de Application el código se organiza **por caso de uso** (`Authorizations/Create`,
`Authorizations/GetById`…): comando/consulta, handler y validador juntos. Cambiar una funcionalidad
toca una sola carpeta.

## 2. CQRS con MediatR, sin repositorios genéricos

- **Comandos** cargan el agregado con tracking, ejecutan la regla en el dominio y guardan.
- **Consultas** proyectan directo a DTO con `ProjectToType` (Mapster) + `AsNoTracking`: el SQL trae solo
  las columnas necesarias.
- Application usa `DbSet` vía `IApplicationDbContext`. Se descarta el repositorio genérico porque
  ocultaría `IQueryable` (necesario para paginar/proyectar en SQL) sin aportar aislamiento real.
- Pipeline behaviors: `LoggingBehavior` (duración, alerta de requests lentos) y `ValidationBehavior`.
- Nota: MediatR ≥ 13 requiere licencia (gratuita para uso comunitario); se configura en `MediatR:LicenseKey`.

## 3. Base de datos: PostgreSQL

Imagen liviana para Docker Desktop, buen soporte en EF Core y funcionalidades útiles aquí: secuencias,
índices filtrados, `xmin` para concurrencia. Las tablas usan los nombres del enunciado
(`Autorizaciones`, `AutorizacionesDetalle`, `Afiliados`, …).

Integridad garantizada **también en la base** (no solo en código):
- Claves foráneas con `RESTRICT` (no se borran catálogos referenciados).
- `CHECK`: montos no negativos, monto reclamado > 0, y `cubierto + copago = reclamado` (cabecera y detalle).
- Únicos: número de autorización, `IdempotencyKey`, códigos de catálogos, documentos.

## 4. Identificadores

- Autorización: `Guid` v7 (ordenable por tiempo → buen comportamiento en índices) generado en el dominio,
  más un `Number` correlativo legible generado por secuencia de PostgreSQL (seguro ante concurrencia).
- Catálogos: `int` identity. Los datos semilla usan IDs fijos; la identidad arranca en 1000 para no colisionar.

## 5. Reglas de negocio en el agregado `Authorization`

- Al menos un servicio; montos > 0; totales recalculados desde el detalle.
- Una autorización inactiva no puede modificarse ni inactivarse otra vez.
- Inactivación lógica (`Status = Inactive`, `DeactivatedAt`): se conserva el histórico.
- La **existencia** de entidades referenciadas se valida en Application (FluentValidation async), porque
  requiere acceso a datos; la FK en la base es la última línea de defensa.

## 6. Distribución de montos configurable

`IAmountDistributionPolicy` (Strategy). Implementación actual: `PercentageDistributionPolicy(70)`, cuyo
porcentaje se lee de `Coverage:CoveragePercentage`. Cambiar a 80/20 = cambiar configuración. Una regla
distinta (por plan, por tipo de servicio, con topes) = nueva implementación de la interfaz, sin tocar el
agregado ni los handlers.

Redondeo: el monto cubierto se redondea a 2 decimales (`AwayFromZero`) y el copago se calcula por
diferencia, garantizando que la suma cuadre al centavo. Los montos calculados se **persisten** por
línea: si la regla cambia, las autorizaciones históricas conservan lo que se autorizó.

## 7. Idempotencia

Header `Idempotency-Key` (opcional) guardado con índice único filtrado. Un reintento con la misma
clave devuelve la autorización existente. Si dos solicitudes simultáneas compiten, la segunda choca con
el índice único, se captura el conflicto y se devuelve la ganadora (probado con 3 requests en paralelo).

## 8. Concurrencia

Concurrencia optimista con la columna de sistema `xmin` de PostgreSQL (`Version` en la respuesta).
Un conflicto se traduce a `409 Conflict`. Pendiente: exponerlo como `ETag` / `If-Match` en `PUT`.

## 9. Manejo de errores

`IExceptionHandler` global → respuestas `ProblemDetails` (RFC 9457) con `traceId` y `correlationId`:

| Excepción              | HTTP |
|------------------------|------|
| `ValidationException`  | 400  |
| `NotFoundException`    | 404  |
| `ConflictException`    | 409  |
| `DomainException`      | 422  |
| Otra                   | 500 (sin detalles internos) |

## 10. Seguridad

- JWT Bearer, firma HMAC-SHA256. Clave mínima de 32 caracteres validada al arrancar.
- Autorización por políticas: `authorizations:read` (Operador, Consulta) y `authorizations:write` (Operador).
- El endpoint `/auth/token` con usuarios de demostración existe solo para facilitar la evaluación; en
  producción el token lo emitiría un proveedor de identidad (Entra ID, Keycloak).
- La clave de desarrollo está en `appsettings.Development.json`; en otros entornos se inyecta como secreto.
- Contenedor ejecutado con usuario sin privilegios.

## 11. Observabilidad y resiliencia

- Serilog con logs estructurados (JSON compacto fuera de desarrollo) y `X-Correlation-Id` propagado.
- Health checks `live` (proceso) y `ready` (incluye PostgreSQL), pensados para probes de Kubernetes.
- `EnableRetryOnFailure` en Npgsql para fallos transitorios de conexión.

## 12. CI/CD con GitHub Actions

Se usa GitHub Actions en lugar de Azure DevOps porque el código vive en GitHub: el pipeline queda junto
al repositorio, sin una organización externa ni service connections, y publica la imagen en GitHub
Container Registry con el `GITHUB_TOKEN` del propio repo. Los conceptos (stages/jobs, artefactos,
quality gates, publicación de imagen) son equivalentes y trasladables a Azure DevOps YAML.

`.github/workflows/ci.yml`:

1. **build-test:** restore, build en Release (warnings = errores), verificación de paquetes vulnerables,
   pruebas unitarias + integración (Testcontainers) con cobertura y resumen en el job.
2. **docker:** construye la imagen, la escanea con Trivy (falla ante vulnerabilidades críticas) y la
   publica en GHCR solo desde `develop`, `main` y tags `v*` (las ramas feature solo validan).

Dependabot propone semanalmente actualizaciones de NuGet, actions e imagen base contra `develop`.

## 13. Pruebas

- Unitarias: dominio (distribución, invariantes del agregado).
- Integración: API completa con `WebApplicationFactory` + PostgreSQL real vía Testcontainers
  (autenticación, autorización por rol, validaciones, idempotencia concurrente, 404).
  Se prefiere PostgreSQL real a InMemory para que índices, CHECK y concurrencia se prueben de verdad.
