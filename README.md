# Health Authorizations API

API REST para gestionar autorizaciones de servicios de salud: registro, consulta, modificación e
inactivación de autorizaciones, con cálculo de montos cubiertos por la ARS y copago del afiliado.

**Stack:** .NET 10 · ASP.NET Core · EF Core + PostgreSQL · MediatR (CQRS) · FluentValidation · Mapster ·
JWT · OpenAPI + Scalar · Serilog · NUnit + Testcontainers · Docker.

## Ejecución rápida (Docker Desktop)

```bash
docker compose up -d --build
```

- API: http://localhost:8080
- Documentación interactiva (Scalar): http://localhost:8080/scalar
- OpenAPI JSON: http://localhost:8080/openapi/v1.json
- Salud: `/health/live` y `/health/ready`

Las migraciones y los datos iniciales de catálogos se aplican al arrancar.

## Ejecución local sin Docker para la API

Requisitos: .NET SDK 10 y un PostgreSQL accesible (puede ser solo el de compose: `docker compose up -d db`).

```bash
dotnet tool restore
dotnet run --project src/HealthAuthorizations.Api
```

## Autenticación

Obtener un token con un usuario de demostración (definidos en `appsettings.Development.json`):

| Usuario    | Contraseña     | Rol      | Permisos                |
|------------|----------------|----------|-------------------------|
| `operador` | `Operador123!` | Operador | Lectura y escritura     |
| `consulta` | `Consulta123!` | Consulta | Solo lectura            |

```bash
curl -X POST http://localhost:8080/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"userName":"operador","password":"Operador123!"}'
```

Usar el `accessToken` como `Authorization: Bearer <token>`.

## Ejemplo: crear autorización

```bash
curl -X POST http://localhost:8080/api/v1/authorizations \
  -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: 6f1c2a9e-0000-4000-8000-000000000001" \
  -H "Content-Type: application/json" \
  -d '{
        "affiliateId": 1, "providerId": 3, "contractorId": 1, "intermediaryId": 1,
        "serviceDate": "2026-11-24",
        "services": [
          { "serviceTypeId": 1, "description": "Consulta", "claimedAmount": 1000.00 },
          { "serviceTypeId": 4, "description": "Hemograma", "claimedAmount": 500.00 }
        ]
      }'
```

## Ejemplo: actualizar una autorización

1. Consultarla y tomar el header `ETag` de la respuesta (ej. `"812"`).
2. Enviar el `PUT` con ese valor en `If-Match`. El body es igual al de creación y reemplaza los servicios.

```bash
curl -X PUT http://localhost:8080/api/v1/authorizations/{id} \
  -H "Authorization: Bearer $TOKEN" \
  -H 'If-Match: "812"' \
  -H "Content-Type: application/json" \
  -d '{ "affiliateId": 1, "providerId": 3, "contractorId": 1, "serviceDate": "2026-11-24",
        "services": [ { "serviceTypeId": 1, "claimedAmount": 1200.00 } ] }'
```

Si otro usuario la modificó después de consultarla, la API responde `412` y hay que volver al paso 1.

## Ejemplo: inactivar una autorización

```bash
curl -X DELETE http://localhost:8080/api/v1/authorizations/{id} -H "Authorization: Bearer $TOKEN"
```

Responde `204`. La autorización se conserva con estado `Inactive` y ya no puede modificarse.

## Ejemplo: buscar autorizaciones

Filtros opcionales y combinables: `number`, `affiliateId`, `providerId`, `contractorId`, `intermediaryId`,
`serviceTypeId`, `status` (`Active`/`Inactive`), `fromDate`, `toDate`. Paginación: `pageSize` (1–100, por
defecto 20) y `cursor`.

```bash
curl "http://localhost:8080/api/v1/authorizations?affiliateId=1&status=Active&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{ "items": [ { "number": 42, "affiliateName": "María Pérez", "providerName": "Clínica Santa Lucía", "...": "..." } ],
  "nextCursor": 23, "hasMore": true }
```

Para la página siguiente se repite la consulta agregando `&cursor=23`.

## Pruebas

```bash
dotnet test                                   # unitarias + integración (requiere Docker)
dotnet test --collect:"XPlat Code Coverage"   # con cobertura
```

## CI/CD

GitHub Actions (`.github/workflows/ci.yml`) en cada push y pull request:
build → vulnerabilidades de paquetes → pruebas con cobertura → imagen Docker → escaneo Trivy →
publicación en `ghcr.io` (solo `develop`, `main` y tags `v*`).

## Estructura

```
src/
  HealthAuthorizations.Domain          Entidades, reglas de negocio, política de distribución de montos
  HealthAuthorizations.Application     Casos de uso CQRS (comandos/consultas MediatR), validaciones, DTOs
  HealthAuthorizations.Infrastructure  EF Core/PostgreSQL, migraciones, seed, JWT
  HealthAuthorizations.Api             Controllers, middleware, manejo de errores, OpenAPI, health checks
tests/
  HealthAuthorizations.UnitTests         Dominio y aplicación
  HealthAuthorizations.IntegrationTests  API completa contra PostgreSQL en contenedor
docs/
  DECISIONES.md                        Decisiones técnicas y su justificación
```

## Migraciones

```bash
dotnet ef migrations add <Nombre> -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api -o Persistence/Migrations
dotnet ef migrations script --idempotent -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api -o db/schema.sql
```

## Estado

| Funcionalidad                                   | Estado      |
|-------------------------------------------------|-------------|
| Crear autorización (idempotente)                | Hecho       |
| Obtener detalle                                 | Hecho       |
| Búsqueda paginada con filtros                   | Hecho       |
| Actualizar (concurrencia optimista / If-Match)  | Hecho       |
| Inactivar (borrado lógico)                      | Hecho       |
| Endpoints de catálogos                          | Pendiente   |
| Eventos a otros sistemas (Outbox)               | Pendiente   |
| Pipeline CI (GitHub Actions)                    | Hecho       |
| SonarQube, manifiestos K8s, GitOps              | Pendiente   |
