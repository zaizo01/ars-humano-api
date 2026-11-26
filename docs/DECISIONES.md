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

Concurrencia optimista con la columna de sistema `xmin` de PostgreSQL, expuesta en HTTP como `ETag`
(`GET`, `POST` y `PUT` lo devuelven). `PUT /{id}` **exige** `If-Match`:

| Situación                                                        | Respuesta |
|------------------------------------------------------------------|-----------|
| Sin `If-Match`                                                   | `428 Precondition Required` |
| `If-Match` con formato inválido                                  | `400` |
| La versión enviada ya no es la vigente (otro usuario modificó)   | `412 Precondition Failed` |
| Otro proceso modifica la fila entre la lectura y el guardado     | `409 Conflict` (detectado por `xmin`) |

Exigir `If-Match` (en vez de dejarlo opcional) evita la "actualización perdida": un cliente no puede
sobrescribir sin saberlo cambios que no vio. Probado con 5 `PUT` simultáneos con el mismo ETag: solo uno
se aplica.

`PUT` reemplaza los datos y la lista completa de servicios (semántica de reemplazo) y recalcula los montos
con la política vigente. El número de autorización no cambia. Una autorización inactiva no puede
modificarse (`422`).

Las reglas de validación comunes a crear y actualizar viven en `AuthorizationDataValidator<T>` para no
duplicarlas.

## 8.1 Búsqueda: paginación por cursor (keyset)

`GET /api/v1/authorizations` ordena por `Number` descendente (más recientes primero) y pagina con
`WHERE "Number" < @cursor ORDER BY "Number" DESC LIMIT n+1`:

- **Costo constante por página:** usa el índice único de `Number`; con `OFFSET` la página 10.000 obliga a
  leer y descartar todas las anteriores.
- **Estable ante inserciones:** si se registran autorizaciones mientras el cliente pagina, no se repiten ni
  se saltan filas.
- **Sin `COUNT(*)` total:** en tablas grandes contar todo cuesta más que la propia página. Se pide `n+1`
  filas solo para saber si hay página siguiente (`hasMore`).
- El listado devuelve una vista **resumida** (sin detalle de servicios, con nombre de afiliado y prestador
  resueltos en el mismo SQL); el detalle completo está en `GET /{id}`.
- Índices de apoyo: `(AffiliateId, ServiceDate)`, `(ProviderId, ServiceDate)`, `(Status, ServiceDate)`,
  `ContractorId`, `IntermediaryId` y, para el filtro por tipo de servicio (`EXISTS`),
  `AutorizacionesDetalle(ServiceTypeId, AuthorizationId)`.

Trade-off: no permite saltar directo a la página N; para una API consumida por sistemas y pantallas con
"cargar más" es la opción adecuada.

## 8.2 Inactivación lógica

`DELETE /api/v1/authorizations/{id}` no borra filas: cambia `Status` a `Inactive` y registra
`DeactivatedAt` (y, por auditoría, `UpdatedAt`/`UpdatedBy`). La autorización y su detalle siguen
consultables por `GET /{id}` y por la búsqueda con `status=Inactive`; no pueden volver a modificarse (`422`).

Es **idempotente**: inactivar una autorización ya inactiva responde `204` sin cambiar la fecha original,
de modo que un reintento por timeout no produce un error. No exige `If-Match` porque inactivar es válido
sea cual sea la versión vigente; la escritura concurrente igual queda protegida por `xmin` (`409`).

## 8.3 Catálogos

`/api/v1/catalogs/*`, solo lectura, accesible para ambos roles. Se tratan distinto según su tamaño:

| Catálogo | Volumen | Estrategia |
|---|---|---|
| Tipos de prestador, tipos de servicio, intermediarios, contratantes | Pequeño, cambia poco | Lista completa **cacheada** 10 min |
| Afiliados, prestadores | Grande, crece | **Búsqueda paginada** por cursor (`Id`), sin caché |

- Caché con `HybridCache` (.NET 9+): protege contra la "estampida" (si llegan muchas solicitudes con la
  caché vacía, solo una consulta la base) y, registrando un `IDistributedCache` (Redis), pasa a tener un
  segundo nivel compartido entre réplicas **sin cambiar los handlers**. En este alcance los catálogos solo
  cambian por migraciones, así que la expiración por tiempo es suficiente; si se agrega mantenimiento de
  catálogos, se invalida por la etiqueta `catalogs`.
- Búsqueda de afiliados: coincidencia exacta por número de afiliado o documento, parcial por nombre.
  Prestadores: exacta por código o RNC, parcial por nombre; filtro por tipo de prestador.
- La paginación usa las mismas reglas y el mismo armado de página (`PaginationRules`, `CursorPage.From`)
  que la búsqueda de autorizaciones.

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

Convenciones: **NUnit**, patrón **AAA** (`// Arrange`, `// Act`, `// Assert`), un escenario por prueba y
nombres `Should_<Resultado>_When_<Condición>` (ej. `Should_ReturnNotFound_When_AuthorizationDoesNotExist`),
de modo que el reporte de pruebas se lee como especificación del comportamiento.

- Unitarias: dominio (distribución, invariantes del agregado).
- Integración: API completa con `WebApplicationFactory` + PostgreSQL real vía Testcontainers
  (autenticación, autorización por rol, validaciones, idempotencia concurrente, 404).
  Se prefiere PostgreSQL real a InMemory para que índices, CHECK y concurrencia se prueben de verdad.
