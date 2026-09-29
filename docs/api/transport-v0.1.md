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

## 6. Representacion decimal wire

Los valores decimales exactos de dominio deben cruzar la frontera JSON como strings decimales, no como JSON numbers.

Esta regla aplica conceptualmente a:

- dinero;
- cantidades base;
- costos;
- factores;
- tasas/porcentajes decimales cuando el contrato de dominio los trate como `NUMERIC` exacto.

Motivos:

- evitar dependencia de floating point binario;
- preservar semantica exacta compatible con PostgreSQL `NUMERIC`;
- mantener comportamiento consistente entre clientes JavaScript, Flutter/Dart, .NET, Python u otros;
- permitir representacion determinista para futuros DTOs y canonicalizacion de `request_hash`.

Precisiones conceptuales heredadas:

- dinero: `NUMERIC(18,2)`;
- cantidades base: `NUMERIC(18,4)`;
- costos/factores: `NUMERIC(18,6)`.

Reglas de representacion publica:

- en JSON publico, estos valores viajan como strings;
- ejemplos validos conceptuales: `"1250.50"`, `"3.2500"`, `"18.123456"`;
- no usar JSON number como representacion publica autoritativa para estos valores exactos;
- no usar notacion cientifica en la representacion canonica futura;
- el separador decimal es `.`;
- no usar separadores de miles;
- el signo negativo, cuando el campo de dominio lo permita, se representa con `-` inicial.

Este micro-hito no define todavia:

- reglas exactas de padding/trailing zeros por tipo;
- normalizacion exacta para `request_hash`;
- limites de validacion por campo;
- rounding por operacion;
- localizacion/UI;
- representacion de porcentajes especificos;
- serializador/framework.

La wire representation no cambia precision/scale de dominio. Cada Command API decidira que campos decimales aparecen. Las reglas de redondeo siguen perteneciendo al contrato de dominio/transaccional correspondiente.

## 7. Representacion wire de timestamps

Los timestamps publicos deben representarse como strings RFC 3339 con timezone/offset explicito.

Wire type:

- JSON string.

Formato:

- RFC 3339;
- timezone/offset obligatorio.

Para timestamps autoritativos generados por backend, la representacion publica canonica sera UTC usando sufijo `Z`.

Ejemplo valido:

```json
"2026-09-28T23:41:15Z"
```

Ejemplo valido con fraccion cuando exista precision relevante:

```json
"2026-09-28T23:41:15.123456Z"
```

No usar timestamps ambiguos sin timezone, por ejemplo:

```json
"2026-09-28 23:41:15"
"2026-09-28T23:41:15"
```

Fracciones de segundo:

- pueden incluirse cuando la precision autoritativa las requiera;
- no inventar precision inexistente;
- no definir todavia una cantidad fija universal de digitos fraccionarios.

El wire-format no cambia la semantica temporal persistida. Debe seguir siendo compatible conceptualmente con `TIMESTAMPTZ`. Transport no convierte timestamps autoritativos en hora local de sucursal dentro del contrato de transporte.

La presentacion/localizacion de fechas u horas de UI corresponde al cliente. Transport conserva un instante inequivoco.

Este micro-hito no define todavia:

- timezone de negocio por sucursal;
- reglas de calendario comercial;
- date-only;
- time-only;
- duracion/intervalos;
- parsing framework-specific;
- serializador;
- headers HTTP de fecha;
- request timestamps command-specific;
- canonicalizacion exacta de timestamps para `request_hash`.

Restriccion heredada: `confirmed_at`, `closed_at`, `occurred_at` y timestamps autoritativos equivalentes no son determinados libremente por cliente cuando el contrato de dominio indique que los genera backend.

## 8. Decisiones todavia no cerradas

Este documento inicial no cierra:

- JSON media type exacto si corresponde;
- formato decimal canonico;
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

## 9. Fuera de alcance

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

## 10. Pendientes siguientes por micro-hitos

Secuencia recomendada para micro-hitos posteriores:

1. Enums.
2. References publicas compartidas.
3. Success/error envelopes.
4. Idempotency/preconditions transport.
5. Canonicalizacion wire para `request_hash`.
6. Replay publico compartido donde proceda.
7. HTTP mapping.
8. Correlation/request ID si se adopta.
9. Command API contracts uno por uno.

No se desarrolla ninguna de esas decisiones en este documento inicial.
