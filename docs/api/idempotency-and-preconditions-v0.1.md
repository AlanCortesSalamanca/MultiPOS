# API Idempotency and Preconditions v0.1

Fuente funcional: `especificacion_maestra_pos_multisucursal_v0.5.md`.

Referencia fisica vigente: `docs/database/modelo-fisico-v0.5-db-4.md`.

Contrato API base: `docs/api/conventions-v0.1.md`.

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

Este documento define reglas conceptuales compartidas para idempotencia y precondiciones en futuros contratos API de commands criticos de GENGXIN POS.

No disena endpoints concretos, rutas, HTTP methods, HTTP status codes, headers exactos, DTOs concretos, OpenAPI, framework, backend, ORM, JWT, frontend, desktop, deployment ni SQL de implementacion.

Su objetivo es cerrar reglas minimas compartidas sobre:

- `idempotency_key`;
- `request_hash`;
- estados `IN_PROGRESS`, `COMPLETED` y `FAILED`;
- lease temporal con `locked_until`;
- retencion con `expires_at`;
- diferencia entre fallos deterministas y fallos tecnicos;
- precondiciones optimistas por fingerprint cuando apliquen;
- relacion entre idempotencia, `client_operation_id` y reconciliacion historica.

## 2. Fuente de verdad y alcance

La semantica de negocio sigue viviendo en los contratos transaccionales frozen. Este documento no puede cambiar:

- invariantes;
- atomicidad;
- errores de dominio;
- orden de locks;
- efectos;
- lifecycle idempotente propio de cada command.

Este documento normaliza la frontera conceptual para contratos API futuros. Cuando una regla difiere por command, se expresa como matriz y no como regla universal.

## 3. Base fisica vigente

db-4 define la infraestructura fisica de idempotencia mediante `idempotency_keys` y el enum:

```text
idempotency_status = IN_PROGRESS | COMPLETED | FAILED
```

El alcance unico fisico de key es:

```text
(business_id, operation_type, idempotency_key)
```

Campos fisicos relevantes:

- `business_id`;
- `branch_id`;
- `operation_type`;
- `idempotency_key`;
- `request_hash`;
- `status`;
- `result_entity_type`;
- `result_entity_id`;
- `response_body`;
- `error_code`;
- `error_message`;
- `locked_until`;
- `expires_at`.

Este documento no agrega columnas, constraints, enums ni tablas.

## 4. Principio de command critico

Un command critico es una mutacion que puede producir efectos persistentes, folios, movimientos, cierre de estados, auditoria, caja, inventario o reposicion.

Los commands criticos cubiertos por este borrador son:

- `CONFIRM_SALE`;
- `CONFIRM_RETURN`;
- `CONFIRM_ORDER`;
- `CONFIRM_PURCHASE`.

Para estos commands, la idempotencia no es un detalle de transporte. Es parte de la semantica operacional del command.

## 5. idempotency_key

`idempotency_key` identifica una solicitud critica para reconocer retries seguros.

No es:

- identidad publica del recurso;
- `public_id`;
- PK interna;
- `client_operation_id`;
- fingerprint del `DRAFT`;
- permiso;
- payload completo.

La misma `idempotency_key` solo es comparable dentro de su scope fisico:

```text
business_id + operation_type + idempotency_key
```

No asumir unicidad global fuera de ese scope.

## 6. request_hash

`request_hash` representa conceptualmente el command canonico asociado a la `idempotency_key`.

Regla compartida para los cuatro commands cubiertos:

- si existe la misma key con `request_hash` diferente, se devuelve error de key reutilizada;
- esto aplica aunque la fila este `IN_PROGRESS`, `COMPLETED` o `FAILED`;
- la comprobacion de hash ocurre antes de aplicar el tratamiento especifico por estado.

Este documento no define algoritmo criptografico, canonicalizacion wire-format ni transporte exacto.

## 7. operation_type por command

| Command | `operation_type` |
| --- | --- |
| Confirmar venta | `CONFIRM_SALE` |
| Confirmar devolucion | `CONFIRM_RETURN` |
| Confirmar pedido proveedor | `CONFIRM_ORDER` |
| Confirmar compra | `CONFIRM_PURCHASE` |

`operation_type` es `TEXT` en db-4. Este documento no introduce enum nuevo.

## 8. Matriz de entradas conceptuales

| Command | `idempotency_key` | `request_hash` conceptual | `client_operation_id` | Fingerprint esperado |
| --- | --- | --- | --- | --- |
| `CONFIRM_SALE` | Requerida | Payload conceptual canonico de venta | Requerido; barrera logica adicional | No definido en contrato frozen |
| `CONFIRM_RETURN` | Requerida | Payload conceptual canonico de devolucion | Requerido; barrera logica adicional | No definido en contrato frozen |
| `CONFIRM_ORDER` | Requerida | Incluye `operation_type`, `purchase_order_id`, `expected_draft_fingerprint` y entradas reales que alteren el command | No es identidad del command | `expected_draft_fingerprint` requerido |
| `CONFIRM_PURCHASE` | Requerida | Incluye `operation_type`, `purchase_id`, `expected_purchase_fingerprint` y precondiciones explicitas que alteren el command | Nullable; no sustituye idempotencia | `expected_purchase_fingerprint` requerido |

No reenviar lineas persistidas como autoridad del command cuando el contrato frozen dice que el `DRAFT` persistido es la autoridad.

## 9. Lifecycle temporal compartido

Los cuatro commands cubiertos usan la misma politica temporal MVP:

| Estado | `locked_until` | `expires_at` |
| --- | --- | --- |
| `IN_PROGRESS` | `now() + 30 segundos` al reservar o recuperar | `NULL` |
| `COMPLETED` | `NULL` | `now() + 30 dias` |
| `FAILED` | `NULL` | `now() + 30 dias` |

`locked_until` controla exclusivamente el lease de ejecucion. `expires_at` no es mecanismo de locking.

No introducir otros tiempos en contratos API futuros para estos commands sin evolucion documental explicita.

## 10. Semantica de IN_PROGRESS

Si existe una key `IN_PROGRESS` con mismo `request_hash` y `locked_until > now()`, el command debe responder conceptualmente como operacion en curso y no debe esperar bloqueado durante los 30 segundos.

Si existe una key `IN_PROGRESS` con mismo `request_hash` y `locked_until <= now()`, la expiracion del lease no autoriza por si sola una segunda ejecucion. La recuperacion solo puede continuar si el contrato transaccional permite adquirir la fila sin competir con una ejecucion activa y despues verificar el estado real del aggregate correspondiente.

Matriz de recuperacion:

| Command | Verificacion antes de repetir efectos o reconciliar |
| --- | --- |
| `CONFIRM_SALE` | Verificar `request_hash`, que `result_entity_id IS NULL`, que no exista `sales(branch_id, client_operation_id)` y que la fila pueda adquirirse sin ejecucion activa. Si ya existe venta, reconciliar a `COMPLETED` sin repetir efectos. |
| `CONFIRM_RETURN` | Verificar `request_hash`, que `result_entity_id IS NULL`, que no exista `returns(branch_id, client_operation_id)` y que la fila pueda adquirirse sin ejecucion activa. Si ya existe devolucion, reconciliar a `COMPLETED` sin repetir efectos. |
| `CONFIRM_ORDER` | Bloquear `purchase_orders(id)`, validar ambito/autorizacion de la solicitud actual, leer estado real del pedido, recalcular y comparar fingerprint cuando siga `DRAFT`; reconciliar solo estados historicos autorizados. |
| `CONFIRM_PURCHASE` | Bloquear recursos segun contrato, validar ambito/autorizacion de la solicitud actual, leer estado real de compra/pedido, recalcular y comparar fingerprint cuando proceda; reconciliar solo estados historicos autorizados. |

No repetir efectos operativos solo porque hayan pasado 30 segundos.

## 11. Semantica de COMPLETED

Si existe una key `COMPLETED` con mismo `request_hash`:

- no crear otra entidad;
- no repetir efectos;
- devolver/reproducir el resultado almacenado o reconstruirlo desde `result_entity_type` y `result_entity_id`;
- preservar `locked_until = NULL` y retencion con `expires_at`.

`response_body` es replay interno. No asumir que equivale al DTO publico del futuro contrato API.

## 12. Semantica de FAILED

Si existe una key `FAILED` con mismo `request_hash`:

- devolver el error de dominio almacenado;
- no reintentar automaticamente la misma key;
- no transformar el error en un codigo generico;
- no reconciliar una key `FAILED` historica hacia `COMPLETED` por el hecho de que otra key haya completado despues.

Una nueva ejecucion posterior debe usar una nueva `idempotency_key`.

El `request_hash` se recalcula desde el command canonico de esa nueva solicitud. Un nuevo `request_hash` solo cambia necesariamente cuando cambian inputs o precondiciones semanticas que forman parte del command canonico. Cambiar la `idempotency_key` por si mismo no implica cambiar `request_hash`.

Cuando un contrato transaccional frozen concreto exige nuevo `request_hash` y/o nuevo fingerprint para corregir un caso especifico, conservar esa regla.

Matriz de correccion despues de `FAILED`:

| Command | Regla |
| --- | --- |
| `CONFIRM_SALE` | Una key `FAILED` con mismo hash conserva su error historico. Una correccion usa nueva `idempotency_key`; el `request_hash` se recalcula desde el payload conceptual canonico de la nueva solicitud. |
| `CONFIRM_RETURN` | Una key `FAILED` con mismo hash conserva su error historico. Si el usuario corrige la devolucion y aun no existe `returns(branch_id, client_operation_id)`, puede conservar `client_operation_id`, pero debe usar nueva `idempotency_key` y nuevo `request_hash` del payload corregido. |
| `CONFIRM_ORDER` | Una key `FAILED` con mismo hash conserva su error historico. Si el usuario corrige o revisa el `DRAFT`, debe usar nueva `idempotency_key`, nuevo `request_hash` y nuevo `expected_draft_fingerprint`. |
| `CONFIRM_PURCHASE` | Una key `FAILED` con mismo hash conserva su error historico. La nueva ejecucion usa nueva `idempotency_key`; `request_hash` y `expected_purchase_fingerprint` se recalculan conforme al command canonico y al `DRAFT` persistido revisado cuando aplique. |

## 13. Fallos deterministas vs tecnicos

Solo los errores de dominio deterministicos entran a persistencia `FAILED` despues de rollback operativo y transaccion corta de FASE C.

FASE C debe guardar:

- `status = 'FAILED'`;
- `error_code` estable;
- `error_message` seguro y breve;
- `locked_until = NULL`;
- `expires_at = now() + 30 dias`.

FASE C no debe guardar stack traces, SQL interno, credenciales, tokens, secretos ni detalles tecnicos internos.

Fallos tecnicos no deterministicos no deben marcar automaticamente `FAILED`. Ejemplos conceptuales:

- crash;
- timeout interno;
- perdida de conexion;
- error inesperado;
- resultado transaccional desconocido;
- lock timeout;
- deadlock;
- fallos transitorios equivalentes.

En esos casos la key puede permanecer `IN_PROGRESS` hasta vencer el lease y despues aplicar recuperacion segura.

## 14. Condiciones temporales retryable no FAILED

`CONFIRM_PURCHASE` tiene una condicion especial:

```text
PURCHASE_REPLENISHMENT_PREDECESSOR_PENDING
```

Esta condicion es temporal, retryable y no terminal. No se persiste como `FAILED`.

Tratamiento conceptual:

1. Rollback completo de FASE B.
2. Transaccion corta sobre `idempotency_keys`.
3. Verificar misma key y mismo `request_hash`.
4. Si la fila sigue `IN_PROGRESS`, mantenerla `IN_PROGRESS`, liberar el lease con `locked_until <= now()` y conservar `expires_at = NULL`.
5. Si la fila ya esta `COMPLETED`, no degradarla.
6. Si la fila ya esta `FAILED`, no degradarla.

Un retry posterior de la misma key/hash entra al flujo de recuperacion segura y reevalua la condicion contra el estado actual.

Esta regla no se generaliza a los otros commands.

## 15. client_operation_id

`client_operation_id` no sustituye `idempotency_key` ni `request_hash`.

Matriz vigente:

| Command | Regla |
| --- | --- |
| `CONFIRM_SALE` | Requerido. Identifica una venta logica local dentro de la sucursal y actua como defensa secundaria mediante `sales(branch_id, client_operation_id)`. |
| `CONFIRM_RETURN` | Requerido. Identifica una devolucion logica local dentro de la sucursal y actua como defensa secundaria mediante `returns(branch_id, client_operation_id)`. |
| `CONFIRM_ORDER` | No es identidad del command. |
| `CONFIRM_PURCHASE` | `purchases.client_operation_id` es nullable; no es obligatorio para confirmar y no reemplaza idempotencia ni fingerprint. |

Una key distinta con el mismo `client_operation_id` en venta/devolucion no debe duplicar efectos si la entidad logica ya existe.

## 16. Fingerprints como precondiciones

Un fingerprint esperado es una precondicion optimista sobre un `DRAFT` persistido.

No es:

- identidad del recurso;
- `idempotency_key`;
- `request_hash`;
- permiso;
- payload completo reenviado como autoridad.

Matriz vigente:

| Command | Fingerprint |
| --- | --- |
| `CONFIRM_SALE` | No definido por el contrato frozen. |
| `CONFIRM_RETURN` | No definido por el contrato frozen. |
| `CONFIRM_ORDER` | `expected_draft_fingerprint`. |
| `CONFIRM_PURCHASE` | `expected_purchase_fingerprint`. |

El backend recalcula el fingerprint autoritativo desde el estado persistido bajo las condiciones de locking del contrato transaccional. Si no coincide, el error es deterministico segun el command correspondiente.

## 17. Estado persistido como autoridad

El cliente no puede convertir su vista local en autoridad reenviando estado derivado.

Para `CONFIRM_ORDER`, las lineas persistidas de `purchase_order_items` son la autoridad del `DRAFT`.

Para `CONFIRM_PURCHASE`, `purchases` y `purchase_items` persistidos son la autoridad del `DRAFT` de recepcion.

Para venta y devolucion, el payload conceptual canonico define el `request_hash`, pero los efectos finales siguen validandose contra estado persistido autoritativo: caja, catalogos, inventario, venta original, saldos, reposicion y permisos segun cada contrato.

## 18. Autorizacion y reconciliacion historica

### A. SAME TERMINAL KEY

Una key terminal es una fila `COMPLETED` o `FAILED`.

SAME TERMINAL KEY significa misma `idempotency_key` y mismo `request_hash` dentro del scope fisico `(business_id, operation_type, idempotency_key)`.

Una SAME TERMINAL KEY conserva la semantica historica definida por cada contrato transaccional frozen.

Para `CONFIRM_PURCHASE`:

- SAME KEY + SAME HASH + `COMPLETED`: replay historico exacto; no reautoriza; no revalida estado actual de usuario, branch ni catalogos; no repite efectos.
- SAME KEY + SAME HASH + `FAILED`: replay del error historico; no reejecuta; no se convierte despues en `COMPLETED` por otra key.

Para los demas commands, seguir la semantica especifica del contrato transaccional frozen. Este documento no agrega reautorizacion ni revalidacion transversal para SAME TERMINAL KEY.

### B. NEW KEY / IN_PROGRESS RECOVERABLE

NEW KEY / IN_PROGRESS RECOVERABLE cubre una `idempotency_key` nueva o una key `IN_PROGRESS` cuyo lease no esta vigente y que el contrato permite evaluar como recuperable.

La autorizacion y el contexto actual deben aplicarse solamente conforme al contrato frozen de cada command.

Matriz vigente:

| Command | Regla |
| --- | --- |
| `CONFIRM_SALE` | Seguir la semantica especifica del contrato transaccional frozen; no inventar una regla cross-key adicional que contradiga la barrera existente por `client_operation_id`. |
| `CONFIRM_RETURN` | Seguir la semantica especifica del contrato transaccional frozen; no inventar una regla cross-key adicional que contradiga la barrera existente por `client_operation_id`. |
| `CONFIRM_ORDER` | Para key nueva o `IN_PROGRESS` recuperable, validar ambito `business_id` / `branch_id` y autorizacion de la solicitud actual antes de reconciliar estados historicos autorizados. |
| `CONFIRM_PURCHASE` | Para nueva ejecucion, recuperacion o reconciliacion por nueva key / `IN_PROGRESS` recuperable, aplicar las reglas de autorizacion, tenant y sucursal congeladas para `CONFIRM_PURCHASE`. |

No permitir que una nueva `idempotency_key` use el estado ya confirmado de otro tenant, otra sucursal o un actor no autorizado como canal de divulgacion o acceso salvo que el contrato transaccional frozen del command lo permita expresamente.

### C. Politica publica de replay

La Politica publica de replay adicional de exposicion/autenticacion solo puede cerrarse donde el contrato transaccional deje margen; no puede cambiar la semantica de SAME TERMINAL KEY ya congelada.

No se decide HTTP ni transporte de autenticacion en este documento.

## 19. response_body interno

`idempotency_keys.response_body` es almacenamiento interno minimo para replay.

Reglas compartidas:

- mantenerlo pequeno;
- no guardar payloads completos innecesarios;
- no guardar tickets completos, XML, PDF, imagenes, secretos ni objetos enormes;
- si la respuesta completa supera el limite logico, guardar resumen minimo y reconstruir desde `result_entity_type` + `result_entity_id`.

Los contratos transaccionales usan limite logico de 16 KiB cuando lo declaran. Este documento no convierte `response_body` en DTO publico.

## 20. Limpieza y retencion

Para los commands cubiertos:

- `COMPLETED` se retiene 30 dias;
- `FAILED` se retiene 30 dias;
- `IN_PROGRESS` usa `locked_until` como lease y puede mantener `expires_at = NULL`.

Una tarea futura puede eliminar registros con `expires_at < now()` siempre que no correspondan a operaciones activas. Este documento no disena esa tarea.

## 21. Errores conceptuales de idempotencia

Cada command conserva sus codigos propios.

Matriz de nombres existentes:

| Command | Key reutilizada | En progreso |
| --- | --- | --- |
| `CONFIRM_SALE` | `SALE_IDEMPOTENCY_KEY_REUSED` | `SALE_IDEMPOTENCY_IN_PROGRESS` |
| `CONFIRM_RETURN` | `RETURN_IDEMPOTENCY_KEY_REUSED` | `RETURN_IDEMPOTENCY_IN_PROGRESS` |
| `CONFIRM_ORDER` | `ORDER_IDEMPOTENCY_KEY_REUSED` | `ORDER_IDEMPOTENCY_IN_PROGRESS` |
| `CONFIRM_PURCHASE` | `PURCHASE_IDEMPOTENCY_KEY_REUSED` | `PURCHASE_IDEMPOTENCY_IN_PROGRESS` |

No crear un error publico generico para una key `FAILED`: se devuelve el error de dominio original almacenado cuando el hash coincide.

## 22. Orden conceptual minimo

El orden conceptual para commands cubiertos es:

1. Resolver o reservar `idempotency_key` en transaccion corta.
2. Si la key existente tiene hash distinto, detener por key reutilizada.
3. Si esta `COMPLETED`, replay/reconstruccion.
4. Si esta `FAILED`, replay del error almacenado.
5. Si esta `IN_PROGRESS` con lease activo, responder como en curso.
6. Si es nueva o `IN_PROGRESS` recuperable, entrar al flujo transaccional propio del command.
7. Al completar efectos atomicos, marcar `COMPLETED` dentro de la transaccion operativa cuando el contrato asi lo exige.
8. Si falla por dominio deterministico, rollback operativo y FASE C corta hacia `FAILED`.
9. Si falla tecnicamente o aparece una condicion retryable no terminal, no marcar automaticamente `FAILED`.

El orden exacto de locks internos permanece en cada contrato transaccional frozen.

## 23. Matriz final de commands cubiertos

| Command | idempotency_key | request_hash | client_operation_id | fingerprint/precondition | IN_PROGRESS | COMPLETED replay | FAILED deterministico | recuperacion historica | condicion retryable especial |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `CONFIRM_SALE` | Requerida | Requerido; payload conceptual canonico de venta | Requerido; barrera por `sales(branch_id, client_operation_id)` | Sin fingerprint frozen | Lease 30 segundos; si vigente, `SALE_IDEMPOTENCY_IN_PROGRESS`; si expirado, recovery seguro con `client_operation_id` | No crea otra venta; replay o reconstruccion desde `sales` | Persiste error de dominio deterministico; misma key/hash reproduce error | Recovery con `client_operation_id`; si ya existe venta, reconciliar sin repetir efectos | Ninguna business retryable especial frozen |
| `CONFIRM_RETURN` | Requerida | Requerido; payload conceptual canonico de devolucion | Requerido; barrera por `returns(branch_id, client_operation_id)` | Sin fingerprint frozen | Lease 30 segundos; si vigente, `RETURN_IDEMPOTENCY_IN_PROGRESS`; si expirado, recovery seguro con `client_operation_id` | No crea otra devolucion; replay o reconstruccion desde `returns` | Persiste error de dominio deterministico; misma key/hash reproduce error | Recovery con `client_operation_id`; si ya existe devolucion, reconciliar sin repetir efectos | Ninguna business retryable especial frozen |
| `CONFIRM_ORDER` | Requerida | Requerido; incluye `operation_type`, `purchase_order_id`, `expected_draft_fingerprint` y entradas reales del command | No usa `client_operation_id` como identidad del confirm | `expected_draft_fingerprint` | Lease 30 segundos; si vigente, `ORDER_IDEMPOTENCY_IN_PROGRESS`; si expirado, recovery seguro segun estado real del pedido | Replay/reconstruccion desde `purchase_orders` | Persiste error de dominio deterministico; misma key/hash reproduce error y no se convierte luego en `COMPLETED` | Reconciliacion historica segun `CONFIRMED` o `CLOSED`/`CANCELLED` con evidencia autoritativa de confirmacion previa | Ninguna business retryable especial frozen |
| `CONFIRM_PURCHASE` | Requerida | Requerido; incluye `operation_type`, `purchase_id`, `expected_purchase_fingerprint` y precondiciones explicitas del command | Nullable; no autoridad de confirmacion | `expected_purchase_fingerprint` | Lease 30 segundos; si vigente, `PURCHASE_IDEMPOTENCY_IN_PROGRESS`; si expirado, recovery seguro segun contrato | SAME KEY + SAME HASH + `COMPLETED`: replay historico exacto; no reautoriza ni revalida estado actual; no repite efectos | Persiste error de dominio deterministico; SAME KEY + SAME HASH + `FAILED` reproduce error historico y no se convierte luego en `COMPLETED` | Recuperacion historica `CONFIRMED` + `CLOSED` segun contrato frozen | `PURCHASE_REPLENISHMENT_PREDECESSOR_PENDING`: retryable, no `FAILED`, libera lease y conserva `IN_PROGRESS` |

## 24. Fuera de alcance

Queda explicitamente fuera de este documento:

- endpoint paths;
- HTTP methods;
- HTTP status codes;
- exact request/response DTOs;
- exact headers;
- exact idempotency transport;
- algoritmo criptografico de `request_hash`;
- canonicalizacion wire-format definitiva;
- OpenAPI;
- framework;
- backend language;
- ORM;
- JWT/provider;
- frontend;
- desktop technology;
- SQL de implementacion;
- nueva tarea de limpieza;
- cambios fisicos db-4.

## 25. Pendientes despues de este micro-hito

Pendientes para contratos API posteriores:

1. Modelo conceptual de errores API.
2. Autorizacion y contexto API.
3. Wire-format de dinero, cantidades y timestamps.
4. DTOs concretos por command.
5. Transporte exacto de `idempotency_key`.
6. Canonicalizacion exacta de `request_hash`.
7. Politica publica de replay adicional solo donde el contrato transaccional deje margen; no puede cambiar la semantica de SAME TERMINAL KEY ya congelada.
