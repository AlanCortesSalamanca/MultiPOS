# API Error Model v0.1

Fuente funcional: `especificacion_maestra_pos_multisucursal_v0.5.md`.

Referencia fisica vigente: `docs/database/modelo-fisico-v0.5-db-4.md`.

Contratos API base:

- `docs/api/conventions-v0.1.md`.
- `docs/api/idempotency-and-preconditions-v0.1.md`.

Contratos transaccionales frozen relevantes:

- `docs/transactions/confirmar-venta-v0.1.md`.
- `docs/transactions/confirmar-devolucion-v0.1.md`.
- `docs/transactions/confirmar-pedido-proveedor-v0.1.md`.
- `docs/transactions/confirmar-compra-v0.1.md`.

## Estado del documento

- Estado: BORRADOR CONTROLADO
- Version: v0.1
- Implementacion: no iniciada

Este documento todavia NO queda congelado.

## 1. Objetivo

Este documento define el modelo conceptual compartido de errores para futuros contratos API de GENGXIN POS.

No asigna HTTP status codes, no define endpoints, no define response DTO definitivo, no define OpenAPI, no define framework, no modifica contratos transaccionales frozen, no modifica db-4, no crea db-5 y no define SQL de implementacion.

Su objetivo es establecer reglas comunes para:

- `error_code`;
- `error_message` / mensaje publico seguro;
- categoria conceptual;
- retryability;
- metadata segura opcional;
- separacion entre error publico y diagnostico interno;
- tratamiento de idempotencia, precondiciones, errores deterministas y fallos tecnicos.

## 2. Principio general

Un error API conceptual debe expresar una condicion estable de negocio o frontera sin filtrar detalles internos de implementacion.

Separar claramente:

- codigo estable;
- mensaje seguro;
- clasificacion;
- retryability;
- metadata segura opcional;
- detalle tecnico interno no publico.

No asumir que toda excepcion interna es un error API de dominio.

El contrato transaccional frozen correspondiente sigue siendo la autoridad exhaustiva para cada command. Este documento no renombra codigos frozen ni inventa errores para unificar artificialmente commands distintos.

## 3. Categorias conceptuales

Las categorias son clasificacion conceptual. No sustituyen `error_code` y no renombran codigos frozen.

| Categoria | Significado | Ejemplos respaldados |
| --- | --- | --- |
| `VALIDATION` | Input semanticamente invalido o requisito de negocio no satisfecho. | `INVALID_QUANTITY`, `INVALID_DISCOUNT`, `RETURN_QUANTITY_INVALID`, `RETURN_REFUND_METHOD_REQUIRED`, `PURCHASE_QUANTITY_INVALID`, `PURCHASE_TOTALS_INVALID`, `PURCHASE_TAX_INVALID`. |
| `AUTHENTICATION` | Identidad/sesion no autenticada cuando exista soporte documental suficiente. | No se congelan codigos concretos en este documento porque los contratos revisados no cierran uno especifico. |
| `AUTHORIZATION` | Actor, sucursal o permiso insuficiente para ejecutar el command. | `USER_INACTIVE`, `USER_BRANCH_FORBIDDEN`, `USER_PERMISSION_DENIED`. |
| `NOT_FOUND_VISIBILITY` | Recurso inexistente o no visible segun frontera segura. | `PURCHASE_ORDER_NOT_FOUND`, `PURCHASE_NOT_FOUND`, `SALE_NOT_FOUND`, `PRICE_NOT_FOUND`. |
| `STATE_CONFLICT` | Estado persistido incompatible con la operacion. | `PURCHASE_ORDER_STATUS_INVALID`, `PURCHASE_STATUS_INVALID`, `SALE_NOT_RETURNABLE`, `CASH_SESSION_CLOSED`, `QUOTATION_ALREADY_CONVERTED`, `QUOTATION_STATUS_INVALID`. |
| `PRECONDITION` | Fingerprint, DRAFT o precondicion autoritativa stale cuando aplica. | `ORDER_DRAFT_STALE`, `ORDER_REPLENISHMENT_STALE`, `PURCHASE_DRAFT_STALE`. |
| `IDEMPOTENCY` | Conflicto o estado de idempotencia. | `SALE_IDEMPOTENCY_KEY_REUSED`, `SALE_IDEMPOTENCY_IN_PROGRESS`, `RETURN_IDEMPOTENCY_KEY_REUSED`, `RETURN_IDEMPOTENCY_IN_PROGRESS`, `ORDER_IDEMPOTENCY_KEY_REUSED`, `ORDER_IDEMPOTENCY_IN_PROGRESS`, `PURCHASE_IDEMPOTENCY_KEY_REUSED`, `PURCHASE_IDEMPOTENCY_IN_PROGRESS`. |
| `TEMPORARY_RETRYABLE` | Condicion temporal no terminal definida por dominio. | `PURCHASE_REPLENISHMENT_PREDECESSOR_PENDING`. |
| `INTERNAL_TECHNICAL` | Crash, conexion, timeout tecnico, deadlock, lock timeout, commit outcome unknown, bug o invariante interna. | No convertir automaticamente a codigo de dominio. |

No convertir automaticamente `INTERNAL_TECHNICAL` en codigo de dominio.

## 4. error_code

`error_code` es el identificador estable de maquina.

Reglas:

- usa el codigo exacto del contrato transaccional cuando ya existe;
- no debe traducirse;
- no debe incluir datos variables;
- no debe contener IDs, hashes ni mensajes;
- no debe inventarse un codigo generico para reemplazar un codigo frozen;
- no debe fabricarse para exponer una constraint interna que el contrato frozen trata como invariante tecnica.

Una key `FAILED` con mismo hash devuelve el `error_code` original persistido cuando asi lo define `docs/api/idempotency-and-preconditions-v0.1.md` y el contrato transaccional correspondiente.

No crear un error generico `IDEMPOTENCY_FAILED`. Los contratos de venta y devolucion mencionan categorias internas especificas para `SALE_IDEMPOTENCY_FAILED` y `RETURN_IDEMPOTENCY_FAILED`, pero ambos indican preferir devolver el error de dominio original almacenado para un `FAILED` deterministico.

## 5. error_message

`message` / `error_message` publico conceptual debe ser:

- seguro;
- breve;
- comprensible;
- sin SQL;
- sin stack trace;
- sin secretos;
- sin credenciales;
- sin tokens;
- sin hashes completos;
- sin informacion cross-tenant;
- sin datos internos innecesarios.

El codigo es la autoridad programatica. El mensaje es para presentacion o diagnostico seguro.

Este documento no congela textos localizados definitivos ni copy de UI.

## 6. Metadata segura

Un error conceptual puede tener metadata estructurada opcional si el contrato concreto lo permite.

La metadata solo puede incluir informacion util y segura, por ejemplo:

- `field` conceptual;
- `resource_type` conceptual;
- `operation_type`;
- `retryable`;
- `current_status` cuando sea seguro exponerlo;
- version o fingerprint esperado/actual solo si el contrato permite exponer una representacion segura.

No se define shape JSON definitivo.

No exponer por defecto:

- PK `BIGINT` internos;
- `request_hash` completo;
- fingerprint completo;
- `idempotency_key` completa en audit/error si el contrato lo prohibe;
- SQL;
- stack;
- locks internos;
- tablas internas;
- constraints internas;
- datos que revelen recursos de otro tenant.

## 7. retryable

`retryable` es una propiedad conceptual, no una instruccion automatica de transporte.

Distinguir:

- retryable por dominio;
- retryable por infraestructura;
- terminal/deterministico.

Un error retryable no significa necesariamente:

- repetir inmediatamente;
- repetir con misma key;
- crear nueva key;
- cambiar payload;
- conservar payload.

Eso depende del contrato concreto.

Ejemplo cerrado:

- `PURCHASE_REPLENISHMENT_PREDECESSOR_PENDING` es retryable por dominio.
- No se persiste como `FAILED`.
- La misma key/hash puede reevaluarse segun lifecycle frozen.

Errores tecnicos pueden ser retryable operacionalmente, pero no deben convertirse automaticamente en error de dominio persistido.

## 8. Terminal / deterministico

Un error de dominio deterministico significa que la misma realidad autoritativa y el mismo command producen la misma invalidacion hasta que cambie alguna precondicion relevante.

Puede persistirse como `FAILED` cuando el contrato frozen lo indique.

No afirmar que todos los validation, authorization o state errors necesariamente persisten `FAILED` sin comprobar cada command.

Ejemplos deterministas respaldados:

- `PAYMENT_TOTAL_MISMATCH` en venta;
- `RETURN_QUANTITY_EXCEEDED` en devolucion;
- `ORDER_DRAFT_STALE` en pedido;
- `ORDER_REPLENISHMENT_STALE` en pedido;
- `PURCHASE_DRAFT_STALE` en compra;
- `PURCHASE_REPLENISHMENT_INCONSISTENT` en compra;
- `PURCHASE_INVENTORY_INCONSISTENT` en compra.

## 9. Idempotency errors

No crear un error generico para `FAILED`. Una key `FAILED` con mismo hash reproduce el error original.

| Command | KEY_REUSED exacto | IN_PROGRESS exacto | Comportamiento FAILED | Condiciones retryable especiales |
| --- | --- | --- | --- | --- |
| `CONFIRM_SALE` | `SALE_IDEMPOTENCY_KEY_REUSED` | `SALE_IDEMPOTENCY_IN_PROGRESS` | Preferir devolver el error de dominio original almacenado; `SALE_IDEMPOTENCY_FAILED` queda como categoria interna si es necesaria. | Ninguna business retryable especial frozen. |
| `CONFIRM_RETURN` | `RETURN_IDEMPOTENCY_KEY_REUSED` | `RETURN_IDEMPOTENCY_IN_PROGRESS` | Preferir devolver el error original almacenado; `RETURN_IDEMPOTENCY_FAILED` queda como categoria interna descrita por el contrato. | Ninguna business retryable especial frozen. |
| `CONFIRM_ORDER` | `ORDER_IDEMPOTENCY_KEY_REUSED` | `ORDER_IDEMPOTENCY_IN_PROGRESS` | Replay del error original almacenado, sin nueva ejecucion y sin convertir despues a `COMPLETED`. | Ninguna business retryable especial frozen. |
| `CONFIRM_PURCHASE` | `PURCHASE_IDEMPOTENCY_KEY_REUSED` | `PURCHASE_IDEMPOTENCY_IN_PROGRESS` | SAME KEY + SAME HASH + `FAILED` reproduce el error historico, no reejecuta y no se convierte despues en `COMPLETED`. | `PURCHASE_REPLENISHMENT_PREDECESSOR_PENDING`: retryable, no `FAILED`, conserva key `IN_PROGRESS` con lease liberado. |

## 10. Precondition errors

Inventario de precondiciones/stale existentes:

| Codigo | Command | Tipo | Regla |
| --- | --- | --- | --- |
| `ORDER_DRAFT_STALE` | `CONFIRM_ORDER` | Stale del propio `DRAFT`. | `expected_draft_fingerprint` no coincide con fingerprint autoritativo del `DRAFT` persistido. |
| `ORDER_REPLENISHMENT_STALE` | `CONFIRM_ORDER` | Stale por estado externo de reposicion. | El `DRAFT` puede seguir igual, pero cambio demanda disponible o trazabilidad FIFO de reposicion. |
| `PURCHASE_DRAFT_STALE` | `CONFIRM_PURCHASE` | Stale del propio `DRAFT`. | Cambio semantico respecto del `DRAFT` esperado, incluyendo fingerprint diferente o identidad/herencia distinta entre pre-read y locks. |

No mezclar stale del propio `DRAFT`, stale por reposicion externa e idempotency conflict bajo un unico codigo.

`KEY_REUSED` pertenece a `IDEMPOTENCY`, no a `PRECONDITION`.

## 11. Authorization errors

Codigos compartidos existentes:

- `USER_INACTIVE`;
- `USER_BRANCH_FORBIDDEN`;
- `USER_PERMISSION_DENIED`.

Codigos de branch/business respaldados por contratos frozen:

- `BRANCH_INACTIVE`;
- `BRANCH_BUSINESS_MISMATCH`;
- `PURCHASE_ORDER_BRANCH_MISMATCH`;
- `TERMINAL_BRANCH_MISMATCH`;
- `CASH_SESSION_TERMINAL_MISMATCH`;
- `RETURN_CASH_SESSION_MISMATCH`;
- `CUSTOMER_BUSINESS_MISMATCH`;
- `PRICE_LIST_BUSINESS_MISMATCH`;
- `PRODUCT_BUSINESS_MISMATCH`;
- `SUPPLIER_BUSINESS_MISMATCH`.

Principios:

- no revelar que un recurso existe en otro tenant/sucursal si el contrato usa `NOT_FOUND` / visibility segura;
- no crear un unico `AUTH_FORBIDDEN` si los contratos frozen ya distinguen;
- no confundir `USER_BRANCH_FORBIDDEN` con mismatch estructural de tenant/business;
- no inferir permisos desde nombres de roles si el contrato exige permiso funcional.

Ejemplos de visibilidad segura:

- `PURCHASE_ORDER_NOT_FOUND` no revela pedidos de otros tenants;
- `PURCHASE_NOT_FOUND` es frontera segura cuando `purchase_id` no existe o pertenece a otro tenant/business no visible;
- `SALE_NOT_FOUND` cubre venta original inexistente o no visible para devolucion.

## 12. Domain state errors

No copiar aqui catalogos exhaustivos largos. La autoridad exhaustiva sigue en el contrato frozen correspondiente.

Patrones respaldados:

| Patron | Ejemplos | Fuente autoritativa |
| --- | --- | --- |
| Status invalido | `PURCHASE_ORDER_STATUS_INVALID`, `PURCHASE_STATUS_INVALID`, `QUOTATION_STATUS_INVALID`, `SALE_NOT_RETURNABLE`. | Contrato frozen del command. |
| Recurso vacio | `PURCHASE_ORDER_EMPTY`. | `CONFIRM_ORDER`. |
| Producto / unidad / precio | `PRODUCT_INACTIVE`, `PRODUCT_BUSINESS_MISMATCH`, `PRODUCT_UNIT_INVALID`, `PRICE_NOT_FOUND`, `PRICE_CHANGED`, `PRICE_LIST_INACTIVE`. | `CONFIRM_SALE`, `CONFIRM_ORDER`, `CONFIRM_PURCHASE` segun aplique. |
| Caja | `CASH_SESSION_REQUIRED`, `CASH_SESSION_CLOSED`, `CASH_SESSION_TERMINAL_MISMATCH`, `RETURN_CASH_SESSION_REQUIRED`, `RETURN_CASH_SESSION_CLOSED`, `RETURN_CASH_SESSION_MISMATCH`. | `CONFIRM_SALE`, `CONFIRM_RETURN`. |
| Stock / inventario | `INSUFFICIENT_STOCK`, `PURCHASE_INVENTORY_INCONSISTENT`. | `CONFIRM_SALE`, `CONFIRM_PURCHASE`. |
| Pagos / reembolso | `PAYMENT_METHOD_INACTIVE`, `PAYMENT_TOTAL_MISMATCH`, `RETURN_REFUND_METHOD_REQUIRED`, `RETURN_REFUND_METHOD_INVALID`, `RETURN_REFUND_SPLIT_NOT_SUPPORTED`. | `CONFIRM_SALE`, `CONFIRM_RETURN`. |
| Proveedor | `SUPPLIER_INACTIVE`, `SUPPLIER_BUSINESS_MISMATCH`. | `CONFIRM_ORDER`; `CONFIRM_PURCHASE` no crea `SUPPLIER_INACTIVE`. |
| Purchase/order state | `PURCHASE_ORDER_NOT_FOUND`, `PURCHASE_ORDER_STATUS_INVALID`, `PURCHASE_NOT_FOUND`, `PURCHASE_STATUS_INVALID`. | `CONFIRM_ORDER`, `CONFIRM_PURCHASE`. |
| Return eligibility | `SALE_NOT_FOUND`, `SALE_NOT_RETURNABLE`, `RETURN_QUANTITY_INVALID`, `RETURN_QUANTITY_EXCEEDED`, `RETURN_ITEM_SALE_MISMATCH`, `RETURN_DISPOSITION_INVALID`, `RETURN_DAMAGED_REASON_REQUIRED`. | `CONFIRM_RETURN`. |
| Replenishment consistency | `MIXED_REPLENISHMENT_CHANNELS`, `ORDER_REPLENISHMENT_STALE`, `PURCHASE_REPLENISHMENT_INCONSISTENT`. | `CONFIRM_SALE`, `CONFIRM_ORDER`, `CONFIRM_PURCHASE`. |
| Folios / sequences | `DOCUMENT_SEQUENCE_NOT_FOUND`, `DOCUMENT_SEQUENCE_INACTIVE`. | `CONFIRM_SALE`, `CONFIRM_RETURN`. |

No inventar uniformidad entre commands. Por ejemplo, `CONFIRM_PURCHASE` no crea `PURCHASE_EMPTY`, `PURCHASE_BRANCH_MISMATCH`, `PURCHASE_BUSINESS_MISMATCH`, `SUPPLIER_INACTIVE`, `PRODUCT_INACTIVE` ni `PRODUCT_UNIT_INVALID`.

## 13. Internal inconsistency

Estados imposibles o inconsistentes detectados internamente no deben inventar exito ni auto-reparacion.

Si el contrato frozen no define codigo publico especifico:

- tratar como `INTERNAL_TECHNICAL`;
- registrar diagnostico interno;
- no fabricar un nuevo codigo de dominio en este documento.

Ejemplos:

- `CONFIRM_ORDER` trata constraints/invariantes imposibles como invariante interno/tecnico y no inventa codigo de dominio por constraint.
- `CONFIRM_PURCHASE` no considera exito historico si falta coherencia entre `purchase CONFIRMED` y `purchase_order CLOSED`; no crea ahora un error publico especifico adicional para corrupcion/integridad interna.
- `PURCHASE_REPLENISHMENT_INCONSISTENT` y `PURCHASE_INVENTORY_INCONSISTENT` si son codigos frozen especificos cuando el contrato de compra los define como inconsistencias deterministicas materializables.

## 14. Technical failure

Categoria conceptual no-business para:

- database unavailable;
- connection lost;
- deadlock;
- lock timeout;
- timeout interno;
- crash;
- serialization/unique race tecnica cuando corresponda;
- commit outcome unknown;
- bug/invariant violation.

Reglas:

- no guardar como `FAILED` de dominio automaticamente;
- no mapear a un `error_code` business frozen;
- no exponer exception raw;
- no convertir deadlock, lock contention, serialization failure, unique race tecnica o infraestructura en `PURCHASE_INVENTORY_INCONSISTENT`, `PURCHASE_REPLENISHMENT_INCONSISTENT` ni otro error de dominio;
- recuperacion depende de idempotencia.

No decidir HTTP status todavia.

## 15. Public error vs internal diagnostic

Error publico conceptual puede incluir:

- `error_code` estable cuando exista;
- mensaje seguro;
- categoria conceptual;
- retryability conceptual cuando sea util;
- metadata segura permitida por contrato.

Diagnostico interno puede incluir:

- correlation/request trace futuro;
- exception;
- stack;
- SQL diagnostics;
- lock diagnostics;
- internal IDs;
- datos de infraestructura;
- observabilidad interna.

Los datos internos no deben cruzar automaticamente la API.

Este documento no disena logging framework, telemetry provider ni middleware.

## 16. Error de replay FAILED

Same key + same hash + `FAILED`:

- no genera error nuevo;
- no reevalua command;
- devuelve/reproduce el error original segun contrato;
- no cambia `FAILED` -> `COMPLETED` porque otra key haya tenido exito despues.

Esta regla debe permanecer consistente con `docs/api/idempotency-and-preconditions-v0.1.md`.

## 17. Error de KEY_REUSED

Same key + different `request_hash`:

- usa codigo especifico por command;
- ocurre antes de tratamiento por estado;
- no ejecuta efectos;
- no se convierte a validation generica;
- no expone hashes completos en response.

Codigos exactos:

- `SALE_IDEMPOTENCY_KEY_REUSED`;
- `RETURN_IDEMPOTENCY_KEY_REUSED`;
- `ORDER_IDEMPOTENCY_KEY_REUSED`;
- `PURCHASE_IDEMPOTENCY_KEY_REUSED`.

## 18. IN_PROGRESS

`IN_PROGRESS` no es domain failure terminal.

Representa una operacion actualmente poseida/leased o todavia no recuperable.

Mantener codigos especificos por command:

- `SALE_IDEMPOTENCY_IN_PROGRESS`;
- `RETURN_IDEMPOTENCY_IN_PROGRESS`;
- `ORDER_IDEMPOTENCY_IN_PROGRESS`;
- `PURCHASE_IDEMPOTENCY_IN_PROGRESS`.

Este documento no define polling, retry transport, headers, timers publicos ni HTTP status codes.

## 19. Matriz resumen de los 4 commands

| Command | authorization errors | validation/context/state errors | idempotency errors | precondition/stale | temporary retryable | technical/internal treatment | fuente autoritativa |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `CONFIRM_SALE` | `USER_INACTIVE`, `USER_BRANCH_FORBIDDEN`, `USER_PERMISSION_DENIED`. | Principales: `TERMINAL_INACTIVE`, `TERMINAL_BRANCH_MISMATCH`, `BRANCH_INACTIVE`, `CASH_SESSION_REQUIRED`, `CASH_SESSION_CLOSED`, `CASH_SESSION_TERMINAL_MISMATCH`, `PRODUCT_INACTIVE`, `PRICE_CHANGED`, `INVALID_QUANTITY`, `INVALID_DISCOUNT`, `INVALID_TAX_CALCULATION`, `INSUFFICIENT_STOCK`, `PAYMENT_TOTAL_MISMATCH`, `QUOTATION_EXPIRED`, `QUOTATION_ALREADY_CONVERTED`; catalogo exhaustivo en contrato frozen. | `SALE_IDEMPOTENCY_KEY_REUSED`, `SALE_IDEMPOTENCY_IN_PROGRESS`; `FAILED` devuelve error original almacenado. | Sin fingerprint frozen; quotation state tiene codigos propios, no `DRAFT_STALE`. | Ninguna business retryable especial frozen. | Fallo tecnico no marca `FAILED` automaticamente; recuperacion por `locked_until` y `sales(branch_id, client_operation_id)`. | `docs/transactions/confirmar-venta-v0.1.md`. |
| `CONFIRM_RETURN` | `USER_INACTIVE`, `USER_BRANCH_FORBIDDEN`, `USER_PERMISSION_DENIED`. | Principales: `TERMINAL_INACTIVE`, `TERMINAL_BRANCH_MISMATCH`, `BRANCH_INACTIVE`, `SALE_NOT_FOUND`, `SALE_NOT_RETURNABLE`, `RETURN_QUANTITY_INVALID`, `RETURN_QUANTITY_EXCEEDED`, `RETURN_ITEM_SALE_MISMATCH`, `RETURN_DISPOSITION_INVALID`, `RETURN_DAMAGED_REASON_REQUIRED`, `RETURN_REFUND_METHOD_REQUIRED`, `RETURN_REFUND_METHOD_INVALID`, `RETURN_CASH_SESSION_CLOSED`, `RETURN_CASH_SESSION_MISMATCH`; catalogo exhaustivo en contrato frozen. | `RETURN_IDEMPOTENCY_KEY_REUSED`, `RETURN_IDEMPOTENCY_IN_PROGRESS`; `FAILED` devuelve error original almacenado. | Sin fingerprint frozen. | Ninguna business retryable especial frozen. | Fallo tecnico no marca `FAILED` automaticamente; recuperacion por `locked_until` y `returns(branch_id, client_operation_id)`. | `docs/transactions/confirmar-devolucion-v0.1.md`. |
| `CONFIRM_ORDER` | `USER_INACTIVE`, `USER_BRANCH_FORBIDDEN`, `USER_PERMISSION_DENIED`. | Principales: `BRANCH_INACTIVE`, `BRANCH_BUSINESS_MISMATCH`, `PURCHASE_ORDER_BRANCH_MISMATCH`, `PURCHASE_ORDER_NOT_FOUND`, `PURCHASE_ORDER_STATUS_INVALID`, `PURCHASE_ORDER_EMPTY`, `SUPPLIER_INACTIVE`, `SUPPLIER_BUSINESS_MISMATCH`, `PRODUCT_INACTIVE`, `PRODUCT_BUSINESS_MISMATCH`, `PRODUCT_UNIT_INVALID`; catalogo exhaustivo en contrato frozen. | `ORDER_IDEMPOTENCY_KEY_REUSED`, `ORDER_IDEMPOTENCY_IN_PROGRESS`; `FAILED` con mismo hash replay del error original. | `ORDER_DRAFT_STALE`, `ORDER_REPLENISHMENT_STALE`. | Ninguna business retryable especial frozen. | Constraints/invariantes imposibles se tratan como interno/tecnico salvo codigo frozen especifico; fallos tecnicos no usan FASE C automaticamente. | `docs/transactions/confirmar-pedido-proveedor-v0.1.md`. |
| `CONFIRM_PURCHASE` | `USER_INACTIVE`, `USER_BRANCH_FORBIDDEN`, `USER_PERMISSION_DENIED`. | Principales: `BRANCH_INACTIVE`, `BRANCH_BUSINESS_MISMATCH`, `SUPPLIER_BUSINESS_MISMATCH`, `PRODUCT_BUSINESS_MISMATCH`, `PURCHASE_NOT_FOUND`, `PURCHASE_STATUS_INVALID`, `PURCHASE_ORDER_STATUS_INVALID`, `PURCHASE_QUANTITY_INVALID`, `PURCHASE_DIFFERENCE_REASON_REQUIRED`, `PURCHASE_TAX_INVALID`, `PURCHASE_TOTALS_INVALID`, `PURCHASE_REPLENISHMENT_INCONSISTENT`, `PURCHASE_INVENTORY_INCONSISTENT`; no crea `SUPPLIER_INACTIVE`, `PRODUCT_INACTIVE`, `PRODUCT_UNIT_INVALID` ni `PURCHASE_EMPTY`. | `PURCHASE_IDEMPOTENCY_KEY_REUSED`, `PURCHASE_IDEMPOTENCY_IN_PROGRESS`; SAME KEY + SAME HASH + `FAILED` replay historico. | `PURCHASE_DRAFT_STALE`. | `PURCHASE_REPLENISHMENT_PREDECESSOR_PENDING`: retryable, no `FAILED`. | Fallos tecnicos no se convierten en dominio; estado parcial incompatible sin codigo frozen publico especifico es internal/technical; inconsistencias frozen especificas conservan sus codigos. | `docs/transactions/confirmar-compra-v0.1.md`. |

## 20. Shape conceptual

Modelo abstracto de error publico:

```text
error
  code
  message
  category
  retryable
  details opcional
```

Esto no congela nombres JSON, envelope, DTO, serializacion, localization ni wire-format definitivo.

El wire-format definitivo se cerrara despues.

## 21. No HTTP mapping

Este documento no decide mapeo HTTP.

No incluye recomendaciones provisionales de status code.

Ese mapeo sera un micro-hito posterior de transporte/wire.

## 22. Fuera de alcance

Queda explicitamente fuera de este documento:

- HTTP status;
- endpoint;
- JSON DTO exacto;
- localization final;
- frontend copy;
- logging framework;
- exception classes;
- OpenAPI;
- backend framework;
- middleware;
- telemetry provider;
- SQL;
- db-5.

## 23. Pendientes siguientes

Despues de este documento:

1. API Authorization / Context v0.1.
2. Wire-format / transport conventions.
3. Command API contracts.

No se desarrollan aqui.
