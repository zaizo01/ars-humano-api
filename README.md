# Health Authorizations API

[![CI](https://github.com/zaizo01/ars-humano-api/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/zaizo01/ars-humano-api/actions/workflows/ci.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=zaizo01_ars-humano-api&metric=alert_status)](https://sonarcloud.io/dashboard?id=zaizo01_ars-humano-api)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=zaizo01_ars-humano-api&metric=coverage)](https://sonarcloud.io/component_measures?id=zaizo01_ars-humano-api&metric=coverage)
[![Duplicated Lines](https://sonarcloud.io/api/project_badges/measure?project=zaizo01_ars-humano-api&metric=duplicated_lines_density)](https://sonarcloud.io/component_measures?id=zaizo01_ars-humano-api&metric=duplicated_lines_density)

API REST para gestionar **autorizaciones de servicios de salud**: registro, consulta, búsqueda, modificación
e inactivación, con cálculo automático del monto que cubre la ARS (70 %) y el copago del afiliado (30 %).

## Contenido

1. [Funcionalidades](#funcionalidades)
2. [Inicio rápido](#inicio-rápido)
3. [Tecnologías](#tecnologías)
4. [Arquitectura](#arquitectura)
5. [Estructura del proyecto](#estructura-del-proyecto)
6. [Modelo de datos](#modelo-de-datos)
7. [API](#api)
8. [Eventos](#eventos)
9. [Configuración](#configuración)
10. [Pruebas y calidad](#pruebas-y-calidad)
11. [CI/CD y despliegue](#cicd-y-despliegue)
12. [Flujo de trabajo Git](#flujo-de-trabajo-git)
13. [Documentación relacionada](#documentación-relacionada)

## Funcionalidades

| Funcionalidad | Detalle |
|---|---|
| Registrar autorización | Una o más líneas de servicio; montos calculados por línea. Idempotente con `Idempotency-Key`. |
| Consultar detalle | Devuelve la autorización con sus líneas y un `ETag` con su versión. |
| Buscar | Filtros combinables y paginación por cursor, rápida aunque la tabla crezca. |
| Modificar | Reemplaza datos y servicios y recalcula montos. Concurrencia optimista con `If-Match`. |
| Inactivar | Borrado lógico: la autorización se conserva con estado `Inactive` y ya no se puede modificar. |
| Catálogos | Afiliados, prestadores, contratantes, intermediarios, tipos de prestador y tipos de servicio. |
| Eventos | Publica `created`, `updated` y `deactivated` en RabbitMQ con entrega garantizada (Outbox). |
| Seguridad | JWT con dos roles: `Operador` (lectura y escritura) y `Consulta` (solo lectura). |

**Reglas de negocio**

- La ARS cubre el **70 %** de cada línea y el afiliado paga el **30 %**. El porcentaje es configurable
  (`Coverage:CoveragePercentage`) y la regla está aislada en una política (`IAmountDistributionPolicy`)
  para poder cambiarla sin tocar el resto.
- El monto cubierto se redondea a 2 decimales y el copago es la diferencia, así que siempre suman el
  monto reclamado.
- No se aceptan montos negativos ni en cero; la base de datos también lo impide (`CHECK`).
- No se crean duplicados: reintentar con la misma `Idempotency-Key` devuelve la misma autorización.
- Afiliado, prestador, contratante, intermediario y tipos de servicio deben existir y estar activos.

## Inicio rápido

### Con Docker (recomendado)

Requisito: Docker Desktop.

```bash
docker compose up -d --build
```

Levanta la API, PostgreSQL y RabbitMQ. Las migraciones y los datos iniciales de catálogos se aplican al
arrancar.

| Servicio | URL |
|---|---|
| API | http://localhost:8080 |
| Documentación interactiva (Scalar) | http://localhost:8080/scalar |
| OpenAPI JSON | http://localhost:8080/openapi/v1.json |
| Salud | http://localhost:8080/health/live y `/health/ready` |
| Consola de RabbitMQ | http://localhost:15672 (guest / guest) |

Si algún puerto ya está ocupado se puede cambiar sin editar archivos con `API_PORT`, `DB_PORT`,
`RABBITMQ_PORT` y `RABBITMQ_UI_PORT` (ej. `DB_PORT=55432 docker compose up -d`).

Para detenerlo: `docker compose down` (agregar `-v` para borrar también los datos).

### Sin Docker para la API

Requisitos: .NET SDK 10 y un PostgreSQL accesible (puede ser solo el de compose: `docker compose up -d db`).

```bash
dotnet tool restore
dotnet run --project src/HealthAuthorizations.Api
```

En `Development` la cadena de conexión apunta a `localhost:5432` y las migraciones se aplican al arrancar.
RabbitMQ es opcional: sin él, los eventos se escriben en el log.

### Primer recorrido

1. Abrir http://localhost:8080/scalar.
2. `POST /api/v1/auth/token` con `{"userName":"operador","password":"Operador123!"}`.
3. Usar el `accessToken` como Bearer token en el resto de las llamadas.
4. Consultar `GET /api/v1/catalogs/service-types` y crear una autorización (ver [ejemplos](#ejemplos)).

## Tecnologías

| Área | Tecnología | Uso en el proyecto |
|---|---|---|
| Plataforma | .NET 10, ASP.NET Core (controllers) | API REST |
| Persistencia | EF Core 10 + Npgsql, PostgreSQL 17 | ORM, migraciones, concurrencia con `xmin`, secuencia para el número de autorización |
| CQRS | MediatR 15 | Un comando o consulta por caso de uso, con *pipeline behaviors* de validación y logging |
| Validación | FluentValidation 12 | Reglas de entrada y existencia de catálogos, antes de llegar al handler |
| Mapeo | Mapster 10 | Entidad → DTO de respuesta |
| Caché | HybridCache | Catálogos pequeños en memoria (10 min) |
| Mensajería | RabbitMQ 4 + RabbitMQ.Client 7 | Publicación de eventos de dominio (exchange tipo *topic*) |
| Seguridad | JWT Bearer | Autenticación y políticas por rol |
| Documentación | OpenAPI (Microsoft.AspNetCore.OpenApi) + Scalar | Especificación y UI interactiva con soporte de Bearer |
| Observabilidad | Serilog (JSON compacto), health checks, correlation id | Logs estructurados y sondas para Kubernetes |
| Pruebas | NUnit 5, NSubstitute, Testcontainers, WebApplicationFactory, coverlet | Unitarias e integración contra PostgreSQL y RabbitMQ reales |
| Calidad | SonarCloud, analizadores .NET (`TreatWarningsAsErrors`), Trivy, Dependabot | Quality gate, análisis estático y vulnerabilidades |
| Contenedores | Docker (multi-stage, usuario no root), Docker Compose | Imagen de la API y entorno local completo |
| CI/CD | GitHub Actions, GitHub Container Registry | Build, pruebas, análisis, imagen y actualización de GitOps |
| Despliegue | Kubernetes, Kustomize, Argo CD | Repositorio GitOps separado: [ars-humano-gitops](https://github.com/zaizo01/ars-humano-gitops) |

## Arquitectura

**Clean Architecture** en cuatro proyectos. Las dependencias apuntan hacia adentro: el dominio no conoce
a nadie y la infraestructura implementa lo que la aplicación define.

```
┌───────────────────────────────────────────────────────────────┐
│ Api             Controllers, middleware, errores, OpenAPI     │
│  └─► Infrastructure   EF Core, JWT, Outbox, RabbitMQ          │
│       └─► Application   Casos de uso (MediatR), validaciones  │
│            └─► Domain     Entidades y reglas de negocio       │
└───────────────────────────────────────────────────────────────┘
```

| Capa | Responsabilidad | Depende de |
|---|---|---|
| **Domain** | Agregado `Authorization`, política de distribución de montos, eventos de dominio. Sin paquetes externos. | — |
| **Application** | Un caso de uso por carpeta (comando o consulta + validador + handler). Define las abstracciones (`IApplicationDbContext`, `ICurrentUser`). | Domain |
| **Infrastructure** | Implementaciones: `AppDbContext`, configuración de tablas, migraciones, JWT, Outbox y publicación en RabbitMQ. | Application |
| **Api** | HTTP: controllers delgados, ETags, manejo global de errores (ProblemDetails), OpenAPI, health checks. | Infrastructure |

**Recorrido de una petición** (ejemplo: crear autorización)

```
POST /api/v1/authorizations
  → CorrelationIdMiddleware        agrega X-Correlation-Id al log y a la respuesta
  → JWT + política de escritura    401 / 403
  → AuthorizationsController       arma el comando con el Idempotency-Key
  → MediatR
      → LoggingBehavior            mide y registra la ejecución
      → ValidationBehavior         FluentValidation → 400 con detalle por campo
      → CreateAuthorizationCommandHandler
          → Authorization.Create(...)   reglas de negocio + evento AuthorizationCreated
          → SaveChanges                 autorización + mensaje de outbox en la misma transacción
  → 201 Created + Location + ETag
```

Las decisiones y sus alternativas están explicadas en [docs/DECISIONES.md](docs/DECISIONES.md).

## Estructura del proyecto

```
.
├── src/
│   ├── HealthAuthorizations.Domain/
│   │   ├── Authorizations/     Authorization (agregado), AuthorizationDetail, política 70/30, eventos
│   │   ├── Catalogs/           Afiliado, prestador, contratante, intermediario, tipos
│   │   └── Common/             DomainException, IAuditableEntity, IDomainEvent
│   │
│   ├── HealthAuthorizations.Application/
│   │   ├── Abstractions/       IApplicationDbContext, ICurrentUser, IAuthorizationNumberGenerator
│   │   ├── Authorizations/
│   │   │   ├── Create/         Comando, validador, handler
│   │   │   ├── GetById/        Consulta de detalle
│   │   │   ├── Search/         Búsqueda con filtros y cursor
│   │   │   ├── Update/         Modificación con control de versión
│   │   │   └── Deactivate/     Inactivación lógica
│   │   ├── Catalogs/           Consultas de catálogos (con caché y búsqueda paginada)
│   │   ├── Behaviors/          Pipeline de MediatR: validación y logging
│   │   └── Common/             Excepciones de aplicación, paginación, opciones de cobertura
│   │
│   ├── HealthAuthorizations.Infrastructure/
│   │   ├── Persistence/        AppDbContext, configuraciones de tablas, migraciones, datos iniciales
│   │   ├── Outbox/             OutboxMessage y OutboxProcessor (servicio en segundo plano)
│   │   ├── Messaging/          Publicadores de eventos: RabbitMQ y log
│   │   └── Auth/               Emisión de JWT, roles, políticas, usuario actual
│   │
│   └── HealthAuthorizations.Api/
│       ├── Controllers/        Auth, Authorizations, Catalogs
│       ├── Middleware/         Correlation id y manejo global de excepciones
│       ├── Http/               Utilidades de ETag
│       ├── OpenApi/            Esquema de seguridad Bearer en el documento OpenAPI
│       └── Program.cs          Composición de la app; modo --migrate para Kubernetes
│
├── tests/
│   ├── HealthAuthorizations.UnitTests/         Dominio y validadores de aplicación
│   ├── HealthAuthorizations.IntegrationTests/  API completa contra PostgreSQL y RabbitMQ en contenedores
│   └── coverage.runsettings                    Formatos y exclusiones de cobertura
│
├── docs/DECISIONES.md          Decisiones técnicas y su justificación
├── .github/
│   ├── workflows/ci.yml        Pipeline de CI/CD
│   └── dependabot.yml          Actualizaciones automáticas de dependencias
├── Dockerfile                  Imagen multi-stage de la API
├── docker-compose.yml          API + PostgreSQL + RabbitMQ para desarrollo local
├── Directory.Build.props       Configuración común de compilación (net10.0, nullable, warnings = errores)
├── global.json                 Versión fija del SDK
├── dotnet-tools.json           Herramientas locales (dotnet-ef)
├── CHANGELOG.md                Historial de versiones
└── HealthAuthorizations.sln
```

## Modelo de datos

PostgreSQL, tablas en español.

| Tabla | Contenido |
|---|---|
| `Autorizaciones` | Encabezado: número correlativo, afiliado, prestador, contratante, intermediario, fecha de servicio, estado, totales, auditoría y versión. |
| `AutorizacionesDetalle` | Líneas de servicio: tipo de servicio, descripción, monto reclamado, cubierto y copago. |
| `Afiliados`, `Prestadores`, `Contratantes`, `Intermediarios`, `TiposPrestadores`, `TiposServicios` | Catálogos con datos iniciales. |
| `OutboxMessages` | Eventos pendientes de publicar en RabbitMQ. |

- **Ids:** `Guid` v7 (ordenables por tiempo) para autorizaciones; número correlativo legible desde una secuencia (`AutorizacionesNumeroSeq`).
- **Concurrencia:** la columna de sistema `xmin` de PostgreSQL actúa como versión; es lo que viaja en el `ETag`.
- **Integridad:** `CHECK` de montos, claves foráneas a catálogos e índice único filtrado por `IdempotencyKey`.
- **Índices** pensados para la búsqueda: afiliado + fecha, prestador + fecha, estado + fecha, contratante,
  intermediario y tipo de servicio (en el detalle).

Migraciones:

```bash
dotnet ef migrations add <Nombre> -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api -o Persistence/Migrations
dotnet ef migrations script --idempotent -p src/HealthAuthorizations.Infrastructure -s src/HealthAuthorizations.Api -o db/schema.sql
dotnet run --project src/HealthAuthorizations.Api -- --migrate   # aplica y termina (lo que hace el Job de Kubernetes)
```

## API

Todas las rutas, salvo el token, requieren `Authorization: Bearer <token>`. La referencia completa está en
Scalar (`/scalar`).

### Endpoints

| Método | Ruta | Permiso | Descripción |
|---|---|---|---|
| `POST` | `/api/v1/auth/token` | Público | Obtiene un JWT |
| `POST` | `/api/v1/authorizations` | Escritura | Crea una autorización. Header opcional `Idempotency-Key`. |
| `GET` | `/api/v1/authorizations` | Lectura | Búsqueda con filtros y cursor |
| `GET` | `/api/v1/authorizations/{id}` | Lectura | Detalle, con `ETag` |
| `PUT` | `/api/v1/authorizations/{id}` | Escritura | Modifica. Header **obligatorio** `If-Match`. |
| `DELETE` | `/api/v1/authorizations/{id}` | Escritura | Inactiva (borrado lógico) |
| `GET` | `/api/v1/catalogs/service-types` | Lectura | Tipos de servicio |
| `GET` | `/api/v1/catalogs/provider-types` | Lectura | Tipos de prestador |
| `GET` | `/api/v1/catalogs/intermediaries` | Lectura | Intermediarios |
| `GET` | `/api/v1/catalogs/contractors` | Lectura | Contratantes |
| `GET` | `/api/v1/catalogs/affiliates` | Lectura | Afiliados: búsqueda por número, documento o nombre; paginado |
| `GET` | `/api/v1/catalogs/providers` | Lectura | Prestadores: búsqueda por código, RNC o nombre, filtro por tipo; paginado |

### Usuarios de demostración

Definidos en `appsettings.Development.json`, solo existen en `Development`:

| Usuario | Contraseña | Rol | Permisos |
|---|---|---|---|
| `operador` | `Operador123!` | Operador | Lectura y escritura |
| `consulta` | `Consulta123!` | Consulta | Solo lectura |

### Respuestas de error

Todos los errores usan el formato estándar **ProblemDetails** (RFC 9457). Las reglas de negocio incluyen
además un `code` (ej. `authorization.inactive`). Cada respuesta lleva el header `X-Correlation-Id` para
encontrarla en los logs.

| Código | Cuándo |
|---|---|
| `400` | Datos inválidos (detalle por campo) o `If-Match` mal formado |
| `401` / `403` | Sin token / el rol no tiene permiso |
| `404` | La autorización no existe |
| `409` | Conflicto: duplicado o modificación simultánea |
| `412` | El `If-Match` no coincide con la versión actual: alguien la modificó antes |
| `422` | Regla de negocio violada (ej. modificar una autorización inactiva) |
| `428` | `PUT` sin `If-Match` |
| `500` | Error inesperado; el detalle queda solo en el log |

### Ejemplos

Obtener un token:

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"userName":"operador","password":"Operador123!"}' | jq -r .accessToken)
```

Crear una autorización:

```bash
curl -X POST http://localhost:8080/api/v1/authorizations \
  -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: 6f1c2a9e-0000-4000-8000-000000000001" \
  -H "Content-Type: application/json" \
  -d '{
        "affiliateId": 1, "providerId": 3, "contractorId": 1, "intermediaryId": 1,
        "serviceDate": "2026-11-24",
        "services": [
          { "serviceTypeId": 1, "description": "Consulta",  "claimedAmount": 1000.00 },
          { "serviceTypeId": 4, "description": "Hemograma", "claimedAmount": 500.00 }
        ]
      }'
```

Respuesta `201` (resumida): `totalClaimed: 1500`, `totalCovered: 1050`, `totalCopay: 450`, y por línea
`700 / 300` y `350 / 150`. Repetir la llamada con la misma `Idempotency-Key` devuelve la misma
autorización.

Modificar (concurrencia optimista):

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

Buscar:

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

Para la página siguiente se repite la consulta agregando `&cursor=23`. A diferencia de `OFFSET`, el cursor
mantiene el mismo tiempo de respuesta en la página 1 y en la 10 000.

Inactivar:

```bash
curl -X DELETE http://localhost:8080/api/v1/authorizations/{id} -H "Authorization: Bearer $TOKEN"
```

Responde `204`. La autorización se conserva con estado `Inactive` y ya no puede modificarse.

## Eventos

Cada vez que una autorización se crea, se modifica o se inactiva, la API publica un evento para que otros
sistemas (facturación, notificaciones, auditoría) reaccionen sin consultar esta API.

| Routing key | Cuándo |
|---|---|
| `authorization.created` | Se registró una autorización |
| `authorization.updated` | Se modificó |
| `authorization.deactivated` | Se inactivó |

Exchange `health-authorizations.events` (tipo *topic*, durable). Mensajes persistentes, con confirmación
del broker y `MessageId` único para que el consumidor descarte duplicados.

**Patrón Outbox:** el evento se guarda en la tabla `OutboxMessages` en la **misma transacción** que la
autorización. Un servicio en segundo plano (`OutboxProcessor`) los publica y los marca como procesados.
Así, si RabbitMQ está caído, la operación no falla y el evento no se pierde: se publica cuando vuelva.
Con varias réplicas, `FOR UPDATE SKIP LOCKED` evita que dos procesen el mismo mensaje.

Para verlos con `docker compose`: en la consola de RabbitMQ (http://localhost:15672), crear una cola
durable, enlazarla al exchange con la routing key `authorization.*` y crear una autorización.

Sin RabbitMQ (`RabbitMq:Enabled=false`, valor por defecto fuera de compose) los eventos se escriben en el log.

## Configuración

Se configura con `appsettings*.json` o variables de entorno (`Seccion__Clave`).

| Clave | Por defecto | Descripción |
|---|---|---|
| `ConnectionStrings:Database` | solo en `Development` | Cadena de conexión a PostgreSQL. Sin ella, la API no arranca. |
| `Database:ApplyMigrationsOnStartup` | `false` (`true` en Development) | Aplica migraciones al iniciar |
| `Coverage:CoveragePercentage` | `70` | Porcentaje que cubre la ARS |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes` | ver `appsettings.json` | Parámetros del token |
| `Jwt:SigningKey` | vacío (valor de ejemplo en Development) | Clave de firma; en producción viene de un secreto |
| `RabbitMq:Enabled` | `false` | Publicar en RabbitMQ o solo en el log |
| `RabbitMq:ConnectionString`, `RabbitMq:Exchange` | `amqp://…localhost`, `health-authorizations.events` | Conexión y exchange |
| `Outbox:PollingInterval`, `BatchSize`, `MaxAttempts` | `5 s`, `50`, `10` | Ritmo y reintentos del publicador |
| `MediatR:LicenseKey` | vacío | Licencia de MediatR (sin ella solo muestra un aviso en el log) |

En producción no hay credenciales en archivos: llegan como variables de entorno desde un secreto de
Kubernetes alimentado por Azure Key Vault (ver el repositorio GitOps).

## Pruebas y calidad

```bash
dotnet test                                                              # unitarias + integración (requiere Docker)
dotnet test --settings tests/coverage.runsettings --collect:"XPlat Code Coverage"   # con cobertura
```

| Proyecto | Qué prueba |
|---|---|
| `UnitTests` | Dominio (cálculo 70/30, redondeo, reglas del agregado, eventos) y validadores |
| `IntegrationTests` | La API completa por HTTP contra PostgreSQL y RabbitMQ reales en contenedores (Testcontainers): seguridad, idempotencia, concurrencia, búsqueda, catálogos, outbox |

Convenciones: NUnit, patrón **AAA** (Arrange / Act / Assert) y nombres `Should_<Resultado>_When_<Condición>`.

**Métricas** (SonarCloud, rama `main`):

| Métrica | Valor | Objetivo |
|---|---|---|
| Cobertura | 92.8 % | > 80 % |
| Líneas duplicadas | 0.0 % | < 3 % |
| Bugs / vulnerabilidades | 0 / 0 | sin críticas |
| Calificación de seguridad, confiabilidad y mantenibilidad | A / A / A | sin errores bloqueantes |

El pipeline falla si no se cumple el quality gate de SonarCloud, si hay paquetes con vulnerabilidades altas
o críticas, o si Trivy encuentra vulnerabilidades críticas en la imagen.

## CI/CD y despliegue

GitHub Actions ([.github/workflows/ci.yml](.github/workflows/ci.yml)) en cada push y pull request:

```
build → paquetes vulnerables → pruebas + cobertura → SonarCloud (quality gate)
      → imagen Docker → Trivy → publicación en ghcr.io → actualización del repo GitOps
```

- Las ramas `feature/*` solo validan; `develop`, `main` y los tags `v*` publican imagen.
- **GitOps:** el pipeline nunca toca el clúster. Cambia la versión declarada en
  [ars-humano-gitops](https://github.com/zaizo01/ars-humano-gitops) y Argo CD la despliega:
  - push a `develop` → commit directo en el entorno **dev** (sincronización automática);
  - tag `vX.Y.Z` → **pull request** al entorno **prod**, que requiere aprobación.
- En Kubernetes las migraciones corren como un Job previo al despliegue (`--migrate`), así varias réplicas
  nunca migran a la vez.

Secrets del repositorio: `SONAR_TOKEN` (SonarCloud) y `GITOPS_TOKEN` (escritura en el repo GitOps). Sin
ellos, esos pasos se omiten con una advertencia.

## Flujo de trabajo Git

- **Gitflow:** `feature/*` desde `develop`, `release/*` y `hotfix/*` hacia `main`, merges con `--no-ff`.
- **Conventional Commits** (`feat:`, `fix:`, `ci:`, `docs:`, `chore:`…).
- **Versionado SemVer** con tags `vX.Y.Z` y notas en [CHANGELOG.md](CHANGELOG.md) y en
  [Releases](https://github.com/zaizo01/ars-humano-api/releases).

## Documentación relacionada

- [docs/DECISIONES.md](docs/DECISIONES.md): por qué se eligió cada tecnología y patrón, y qué alternativas se descartaron.
- [CHANGELOG.md](CHANGELOG.md): cambios por versión.
- [ars-humano-gitops](https://github.com/zaizo01/ars-humano-gitops): manifiestos de Kubernetes y Argo CD.
