# API Transport v0.1

Fuente funcional: `especificacion_maestra_pos_multisucursal_v0.5.md`.

Referencia fisica vigente: `docs/database/modelo-fisico-v0.5-db-4.md`.

Contratos API base:

- `docs/api/conventions-v0.1.md`.
- `docs/api/idempotency-and-preconditions-v0.1.md`.
- `docs/api/error-model-v0.1.md`.
- `docs/api/authorization-and-context-v0.1.md`.

Contratos transaccionales frozen relevantes:

- `docs/transactions/confirmar-venta-v0.1.md`.
- `docs/transactions/confirmar-devolucion-v0.1.md`.
- `docs/transactions/confirmar-pedido-proveedor-v0.1.md`.
- `docs/transactions/confirmar-compra-v0.1.md`.

## 1. Estado del documento

- Estado: BORRADOR CONTROLADO
- Version: v0.1
- Implementacion: no iniciada

Este documento todavia NO queda congelado.

## 2. Objetivo y alcance conceptual

Este documento servira para definir, en micro-hitos posteriores, convenciones compartidas de wire-format y transporte para futuros contratos API de GENGXIN POS.

El objetivo es adaptar a una frontera publica la representacion y transporte de commands, responses y errores sin modificar la semantica de dominio congelada en los contratos transaccionales.

Este primer micro-hito solo establece la frontera del documento. No resuelve todavia wire-format, endpoints, DTOs, HTTP mapping, headers, canonicalizacion, algoritmos criptograficos ni OpenAPI.

## 3. Principio de autoridad

Transport adapta representacion y transporte.

Transport no cambia:

- invariantes;
- efectos;
- lifecycle de idempotencia;
- errores frozen;
- locks;
- reglas de autorizacion;
- reglas de reconciliacion.

Los contratos transaccionales frozen siguen siendo la autoridad de negocio. Los contratos API base siguen siendo la autoridad conceptual compartida para identidad, idempotencia, errores y autorizacion/contexto.

## 4. Separacion de responsabilidades

### TRANSPORT

Este documento podra cerrar posteriormente:

- representacion wire de dinero, cantidades, costos y factores;
- timestamps;
- enums;
- representacion de referencias publicas compartidas;
- success envelope compartido si se adopta;
- error envelope compartido;
- transporte de `idempotency_key`;
- transporte de fingerprints/preconditions;
- canonicalizacion wire necesaria para `request_hash`;
- reglas publicas compartidas de replay donde los contratos frozen dejen margen;
- mapping HTTP;
- correlation/request identifier si se adopta.

### COMMAND API

Cada Command API concreto debe seguir cerrando:

- endpoint/ruta concreta;
- HTTP method concreto si depende del command;
- request DTO concreto;
- response DTO concreto;
- que `public_id`/reference usa cada aggregate;
- que branch/reference pertenece al payload;
- que campos especificos devuelve cada command;
- excepciones command-specific;
- cualquier regla que no pueda universalizarse sin contradecir contratos frozen.

## 5. Restricciones heredadas

Los contratos actuales ya soportan estas restricciones, que este documento no debe contradecir:

- `BIGINT` PK/FK internos no se exponen por costumbre.
- `public_id` es preferido externamente donde exista.
- `public_id` no autoriza.
- `business_id` no debe ser selector libre de tenant.
- `response_body` interno de idempotency no equivale al DTO publico.
- `client_operation_id != idempotency_key`.
- fingerprints son precondiciones, no identidad.
- dinero y cantidades deben preservar semantica decimal exacta.
- timestamps publicos deben ser inequivocos y compatibles con timezone/offset.
- enums deben conservar vocabulario de dominio frozen.
- errores publicos no deben filtrar SQL, stack, hashes completos, PK internas ni datos cross-tenant.
- SAME TERMINAL KEY y reconciliation deben seguir semantica command-specific frozen; no inventar regla transversal.

## 6. Decisiones todavia no cerradas

Este documento inicial no cierra:

- JSON media type exacto si corresponde;
- decimal como string vs number;
- formato decimal canonico;
- representacion final de timestamp;
- representacion enum;
- success envelope;
- error envelope JSON definitivo;
- transporte exacto de `idempotency_key`;
- formato, longitud y generacion de `idempotency_key`;
- ubicacion de fingerprints;
- canonicalizacion de `request_hash`;
- algoritmo criptografico de `request_hash`;
- mapping por categoria/codigo a HTTP status;
- headers exactos;
- politica de replay publica adicional donde exista margen;
- correlation/request ID;
- versionado de rutas;
- OpenAPI.

## 7. Fuera de alcance

Queda fuera de alcance:

- backend/framework;
- ORM;
- JWT/provider;
- OAuth/OIDC;
- cookies/bearer;
- frontend;
- desktop;
- deployment;
- SQL;
- db-5;
- implementacion de middleware;
- implementacion de logging/telemetry.

## 8. Pendientes siguientes por micro-hitos

Secuencia recomendada para micro-hitos posteriores:

1. Representacion decimal wire.
2. Timestamps.
3. Enums.
4. References publicas compartidas.
5. Success/error envelopes.
6. Idempotency/preconditions transport.
7. Canonicalizacion wire para `request_hash`.
8. Replay publico compartido donde proceda.
9. HTTP mapping.
10. Correlation/request ID si se adopta.
11. Command API contracts uno por uno.

No se desarrolla ninguna de esas decisiones en este documento inicial.
