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

## 8. Representacion wire de enums

Los valores enum que crucen la frontera publica deben representarse como JSON strings usando exactamente el vocabulario de dominio frozen correspondiente.

Wire type:

- JSON string.

Reglas:

- el valor publico debe usar exactamente el simbolo de dominio congelado;
- la comparacion de enum values es case-sensitive;
- la API no debe traducir enum values;
- la API no debe cambiar mayusculas/minusculas;
- la API no debe reemplazar simbolos por labels de UI;
- la API no debe inventar aliases silenciosos;
- la API no debe aceptar valores desconocidos fuera del vocabulario permitido por el contrato correspondiente.

Ejemplos conceptuales validos cuando pertenezcan al vocabulario frozen:

- `"CONFIRMED"`;
- `"CASH"`;
- `"RESTOCK"`.

Ejemplos conceptuales que no deben usarse como sustitutos del valor de dominio:

- `"Confirmed"`;
- `"confirmado"`;
- `"cash"`;
- labels localizados de interfaz.

La localizacion/traduccion visible pertenece al frontend/UI, no al valor transportado.

Este micro-hito no define todavia:

- un catalogo global nuevo de enums;
- copia de todos los enums de db-4;
- que enums aparecen en cada Command API;
- como documentarlos en OpenAPI;
- fallback para valores futuros;
- versionado de enums;
- cambios a vocabularios frozen existentes.

Cada Command API define que campos enum aparecen. El vocabulario autoritativo proviene del contrato/modelo correspondiente. Transport solo define su representacion compartida.

## 9. Referencias publicas compartidas

Los PK/FK `BIGINT` internos siguen siendo identidad fisica interna y no deben exponerse ni aceptarse por costumbre en la API publica.

Cuando una entidad tenga `public_id`, `public_id` sera la referencia externa preferida.

Wire type de `public_id`:

- JSON string.

`public_id` no autoriza acceso. Resolver un `public_id` a una PK interna no sustituye:

- validacion de tenant/business;
- validacion de branch;
- permisos;
- reglas de visibilidad;
- validaciones especificas del command.

Transport no debe inventar `public_id` para tablas o entidades que no lo poseen.

`folio`:

- es identificador operativo/humano cuando aplique;
- no sustituye automaticamente `public_id`;
- no debe asumirse globalmente unico salvo que el contrato correspondiente lo garantice;
- el Command API concreto decide si se expone.

Referencias de lineas/details como:

- `sale_items`;
- `return_items`;
- `purchase_order_items`;
- `purchase_items`;
- `quotation_items`;

no se convierten en recursos CRUD independientes por esta convencion. Si un Command API necesita referenciar una linea, ese contrato debe definir su identidad externa y validar pertenencia al aggregate.

Transport solo define la representacion compartida. Cada Command API seguira decidiendo:

- que aggregate/reference recibe;
- que `public_id` concreto usa;
- si una referencia va en path, query o body;
- que referencias devuelve;
- que referencias son opcionales u obligatorias.

Este micro-hito no define todavia:

- UUID version concreta;
- generacion de `public_id`;
- formato interno de PK;
- endpoints/rutas;
- path parameters;
- naming definitivo de todos los campos ID;
- IDs para entidades que hoy no tengan `public_id`;
- exposicion global de folios;
- OpenAPI.

No se introduce `business_id` como selector libre de tenant.

## 10. Success/error envelopes

### Success envelope compartido

Las respuestas publicas exitosas deben usar esta forma conceptual:

```json
{
  "data": {}
}
```

Reglas:

- `data` contiene el payload publico especifico del Command API;
- Transport define solo el envelope;
- cada Command API define la estructura concreta de `data`;
- no asumir que `data` equivale a `idempotency_keys.response_body`;
- no introducir automaticamente metadata adicional en este micro-hito;
- no definir pagination, collections ni recursos CRUD genericos.

Ejemplo minimo conceptual:

```json
{
  "data": {
    "example": "command-specific payload"
  }
}
```

### Error envelope compartido

Las respuestas publicas de error deben usar esta forma conceptual:

```json
{
  "error": {
    "code": "...",
    "message": "...",
    "category": "...",
    "retryable": false,
    "details": {}
  }
}
```

Campos:

- `code`: JSON string. Identificador estable de maquina. Usa codigo exacto frozen cuando exista. No se traduce. No contiene IDs, hashes ni texto variable.
- `message`: JSON string. Mensaje publico seguro y breve. No es autoridad programatica. No congela copy localizado definitivo.
- `category`: JSON string. Usa las categorias conceptuales ya definidas por `docs/api/error-model-v0.1.md`. No se inventan nuevas categorias aqui.
- `retryable`: JSON boolean. Expresa retryability conceptual. No implica por si solo retry inmediato, misma key o nueva key.
- `details`: JSON object opcional. Debe contener unicamente metadata publica segura permitida por el contrato concreto. Puede omitirse cuando no exista metadata segura relevante. Transport no define todavia un catalogo global de campos `details`.

No exponer mediante error/details:

- PK/FK `BIGINT` internos;
- SQL;
- stack traces;
- exceptions raw;
- constraints internas;
- locks internos;
- `request_hash` completo;
- fingerprints completos salvo representacion segura explicitamente permitida;
- `idempotency_key` completa cuando el contrato no lo permita;
- secretos/tokens;
- datos cross-tenant;
- detalles de infraestructura innecesarios.

Mantener semantica de errores frozen:

- SAME KEY + SAME HASH + `FAILED` reproduce el error historico correspondiente segun contrato;
- no inventar `IDEMPOTENCY_FAILED` generico;
- KEY_REUSED mantiene codigo especifico por command;
- IN_PROGRESS mantiene codigo especifico por command;
- errores tecnicos no se convierten automaticamente en errores de dominio.

Ejemplo minimo conceptual:

```json
{
  "error": {
    "code": "PURCHASE_DRAFT_STALE",
    "message": "The resource no longer matches the expected state.",
    "category": "PRECONDITION",
    "retryable": false
  }
}
```

Este micro-hito no define todavia:

- HTTP status codes;
- otros headers;
- `Retry-After`;
- correlation/request ID;
- tracing;
- localization definitiva;
- estructura concreta de `data` por command;
- estructura exhaustiva de `details`;
- OpenAPI;
- framework serializer.

## 11. Idempotency/preconditions transport

### Idempotency-Key

`idempotency_key` se transporta mediante el header HTTP:

```text
Idempotency-Key: <opaque-value>
```

Reglas:

- `idempotency_key` es concern de transporte/idempotencia, no dato de dominio del payload;
- no incluir `idempotency_key` como campo del JSON body del command;
- cambiar unicamente `idempotency_key` no implica por si mismo un `request_hash` distinto;
- `idempotency_key` sigue estando sujeta al scope fisico ya definido por los contratos: `(business_id, operation_type, idempotency_key)`;
- la key es opaca para cliente y servidor a nivel semantico;
- no derivar permisos, tenant, branch ni identidad desde la key;
- no confundir `idempotency_key` con `client_operation_id`.

Este micro-hito cierra solamente el nombre del header: `Idempotency-Key`.

No define todavia:

- longitud minima/maxima;
- charset exacto;
- UUID obligatorio;
- ULID obligatorio;
- generacion del lado cliente;
- expiracion publica;
- `Retry-After`;
- otros headers de replay;
- representacion publica de estado de idempotencia.

### client_operation_id

`client_operation_id`:

- es dato conceptual del command donde aplique;
- no es `idempotency_key`;
- no se mueve al header `Idempotency-Key`;
- su ubicacion publica concreta se define en el Command API correspondiente;
- su semantica frozen de `CONFIRM_SALE` / `CONFIRM_RETURN` no cambia.

### Fingerprints/preconditions

Los fingerprints esperados son datos semanticos del command, no infraestructura generica de transporte.

Reglas:

- `expected_draft_fingerprint` permanece dentro del payload del futuro `CONFIRM_ORDER`;
- `expected_purchase_fingerprint` permanece dentro del payload del futuro `CONFIRM_PURCHASE`;
- no usar un header generico para moverlos;
- no aplicar fingerprints a `CONFIRM_SALE` / `CONFIRM_RETURN` si sus contratos frozen no los definen.

Wire type conceptual para fingerprints:

- JSON string.

El valor es opaco desde la perspectiva del cliente. El cliente no interpreta su contenido. El backend compara contra el fingerprint autoritativo segun el contrato transaccional.

No se define todavia:

- encoding interno exacto;
- algoritmo de generacion;
- tamano exacto;
- canonicalizacion;
- hash algorithm.

### Seguridad

No exponer automaticamente:

- `request_hash` completo;
- fingerprints completos en errores salvo representacion segura permitida;
- `idempotency_key` completa en logs/errores cuando la politica concreta no lo permita.

### Semantica frozen

Este micro-hito no cambia:

- SAME KEY + SAME HASH;
- SAME KEY + DIFFERENT HASH;
- `IN_PROGRESS`;
- `COMPLETED`;
- `FAILED`;
- leases;
- reconciliacion por `client_operation_id`;
- diferencias entre `CONFIRM_SALE` / `CONFIRM_RETURN` y `CONFIRM_ORDER` / `CONFIRM_PURCHASE`.

Este micro-hito no define:

- canonicalizacion de `request_hash`;
- algoritmo hash;
- replay publico adicional;
- HTTP status;
- `Retry-After`;
- correlation/request ID;
- auth headers;
- JWT/bearer/cookies;
- OpenAPI;
- framework/middleware.

## 12. Decisiones todavia no cerradas

Este documento inicial no cierra:

- JSON media type exacto si corresponde;
- formato decimal canonico;
- formato, longitud y generacion de `idempotency_key`;
- canonicalizacion de `request_hash`;
- algoritmo criptografico de `request_hash`;
- mapping por categoria/codigo a HTTP status;
- otros headers exactos de transporte;
- politica de replay publica adicional donde exista margen;
- correlation/request ID;
- versionado de rutas;
- OpenAPI.

## 13. Fuera de alcance

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

## 14. Pendientes siguientes por micro-hitos

Secuencia recomendada para micro-hitos posteriores:

1. Canonicalizacion wire para `request_hash`.
2. Replay publico compartido donde proceda.
3. HTTP mapping.
4. Correlation/request ID si se adopta.
5. Command API contracts uno por uno.

No se desarrolla ninguna de esas decisiones en este documento inicial.
