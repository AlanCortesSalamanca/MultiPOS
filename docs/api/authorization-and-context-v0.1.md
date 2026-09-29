# API Authorization and Context v0.1

Fuente funcional: `especificacion_maestra_pos_multisucursal_v0.5.md`.

Referencia fisica vigente: `docs/database/modelo-fisico-v0.5-db-4.md`.

Contratos API base:

- `docs/api/conventions-v0.1.md`.
- `docs/api/idempotency-and-preconditions-v0.1.md`.
- `docs/api/error-model-v0.1.md`.

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

## 2. Objetivo y alcance

Este documento define reglas conceptuales compartidas para futuros contratos API sobre:

- actor;
- business/tenant;
- branch;
- terminal;
- cash session;
- permisos;
- contexto autenticado;
- input cliente;
- estado persistido autoritativo;
- visibilidad cross-tenant;
- reconciliacion/replay respecto de autorizacion.

Este documento no disena:

- autenticacion concreta;
- JWT;
- OAuth/OIDC;
- cookies;
- bearer tokens;
- headers;
- HTTP status;
- endpoints;
- rutas;
- DTOs concretos;
- API gateway;
- middleware;
- auth provider;
- backend;
- ORM;
- OpenAPI.

## 3. Principio de autoridad

El cliente puede aportar referencias y precondiciones, pero no puede autodeclarar autoridad.

Este documento distingue cuatro fuentes conceptuales:

1. CLIENT INPUT.
2. AUTHENTICATED CONTEXT.
3. DERIVED FROM RESOURCE.
4. AUTHORITATIVE PERSISTED STATE.

Una referencia enviada por el cliente no se vuelve autoridad solo por aparecer en el request.

El estado persistido y los contratos transaccionales frozen determinan finalmente tenant, branch, ownership, status y relaciones.

## 4. Actor

`user_id` conceptual representa el actor actual.

Para nuevas ejecuciones donde corresponda:

- proviene del contexto autenticado;
- no es un selector libre controlado por el cliente;
- debe resolver un `users` valido;
- debe cumplir `users.status = 'ACTIVE'` cuando el contrato lo exige;
- alimenta permisos;
- alimenta auditoria;
- puede alimentar `confirmed_by_user_id` / `actor_user_id` segun command.

Este documento no decide como se representa el actor en JWT, session, cookie, bearer token u otro mecanismo futuro.

Campos persistidos historicos como:

- `confirmed_by_user_id`;
- `received_by_user_id`;
- `created_by_user_id`;

no sustituyen al actor autenticado actual para autorizacion cuando el contrato exige autorizacion presente.

## 5. Modelo de autorizacion

El modelo existente de autorizacion usa:

- `users`;
- `user_roles`;
- `roles`;
- `role_permissions`;
- `permissions`;
- `user_branches`.

Reglas:

- no autorizar por nombre de rol;
- no hay bypass por `ADMIN`, `MANAGER`, `OWNER` ni nombres equivalentes;
- permiso funcional y acceso a sucursal son condiciones distintas;
- el rol debe estar activo cuando el contrato lo exige;
- el permiso debe corresponder al business relevante;
- `user_branches` limita las sucursales operables.

Permisos exactos:

| Command | Permiso |
| --- | --- |
| `CONFIRM_SALE` | `SALES_CONFIRM` |
| `CONFIRM_RETURN` | `RETURNS_CONFIRM` |
| `CONFIRM_ORDER` | `PURCHASE_ORDERS_CONFIRM` |
| `CONFIRM_PURCHASE` | `PURCHASES_CONFIRM` |

Errores compartidos existentes:

- `USER_INACTIVE`;
- `USER_BRANCH_FORBIDDEN`;
- `USER_PERMISSION_DENIED`.

No se inventa un error generico `AUTH_FORBIDDEN`.

## 6. Business / tenant

`business_id` puede existir conceptualmente como contexto, pero no debe asumirse como campo libre de body publico.

Segun el command, puede obtenerse de:

- contexto autenticado;
- branch;
- aggregate persistido.

Siempre se debe distinguir:

- business esperado/autenticado;
- business persistido/derivado.

No se permite usar un `business_id` enviado por cliente para sobreescribir ownership persistido.

Un recurso de otro tenant no visible debe seguir las fronteras seguras definidas por cada contrato. No se universalizan codigos inexistentes.

## 7. Branch authority por command

| Command | Fuente de branch | Autoridad | Validaciones principales |
| --- | --- | --- | --- |
| `CONFIRM_SALE` | `branch_id` conceptual de la operacion. | Branch de la venta a confirmar, validada contra contexto y recursos. | Debe existir, estar activa, ser coherente con business, terminal, `user_branches`, caja y recursos relacionados. No afirmar `BRANCH_BUSINESS_MISMATCH` como codigo de SALE si no esta frozen. |
| `CONFIRM_RETURN` | `branch_id` conceptual de la devolucion. | Branch de la devolucion y venta original visible en esa branch. | Venta original visible y de esa branch, terminal, `user_branches`, caja si aplica. No inventar codigo branch/business no congelado. |
| `CONFIRM_ORDER` | `branch_id` esperado. | `purchase_orders.branch_id` persistido debe coincidir. | `PURCHASE_ORDER_BRANCH_MISMATCH`; `BRANCH_BUSINESS_MISMATCH` donde el contrato frozen lo define. |
| `CONFIRM_PURCHASE` | No es autoridad independiente del cliente. | `purchases.branch_id`. | Branch activa/autorizada en nueva ejecucion o recovery. No existe `PURCHASE_BRANCH_MISMATCH`. |

## 8. Terminal

| Command | Regla |
| --- | --- |
| `CONFIRM_SALE` | Terminal funcional requerida. `terminal_id` autenticada, `ACTIVE`, misma branch, se persiste en `sales` y condiciona `cash_session`. |
| `CONFIRM_RETURN` | Terminal es contexto autenticado operacional. Debe estar `ACTIVE` y ser de la misma branch. No se agrega `returns.terminal_id`; queda en audit. Si el reembolso afecta caja, `cash_session` debe corresponder a esa terminal. |
| `CONFIRM_ORDER` | Terminal no es requisito funcional. `audit_log.terminal_id` puede ser nullable/contextual. |
| `CONFIRM_PURCHASE` | Terminal no es requisito funcional. Audit contextual nullable. |

No se inventa terminal POS para commands administrativos.

## 9. Cash session

| Command | Regla | Codigos |
| --- | --- | --- |
| `CONFIRM_SALE` | Requerida como referencia del command. Validar `OPEN`, misma branch y misma terminal. | `CASH_SESSION_REQUIRED`, `CASH_SESSION_CLOSED`, `CASH_SESSION_TERMINAL_MISMATCH`. |
| `CONFIRM_RETURN` | Condicional: solo si `refund_amount > 0` y el payment method `affects_cash`. Validar `OPEN`, misma branch y misma terminal. | `RETURN_CASH_SESSION_REQUIRED`, `RETURN_CASH_SESSION_CLOSED`, `RETURN_CASH_SESSION_MISMATCH`. |
| `CONFIRM_ORDER` | No aplica. | No aplica. |
| `CONFIRM_PURCHASE` | No aplica. | No aplica. |

`cash_session_id` no es identidad autenticada. Es una referencia operacional que debe validarse contra estado persistido.

## 10. Client input vs context vs state

| Dato | Clasificacion conceptual | Observaciones |
| --- | --- | --- |
| `user_id` | AUTHENTICATED CONTEXT | No selector libre del cliente. Alimenta autorizacion y audit. |
| `business_id` | AUTHENTICATED CONTEXT / DERIVED FROM RESOURCE | No body libre autoritativo. Se contrasta contra branch o aggregate segun command. |
| `branch_id` | CLIENT INPUT para `CONFIRM_SALE`, `CONFIRM_RETURN`, `CONFIRM_ORDER`; DERIVED FROM RESOURCE para `CONFIRM_PURCHASE` | En purchase la autoridad final es `purchases.branch_id`. |
| `terminal_id` | AUTHENTICATED CONTEXT para sale/return; audit contextual nullable para order/purchase | No inventar terminal funcional en order/purchase. |
| `cash_session_id` | CLIENT INPUT para sale; CLIENT INPUT condicional para return | No aplica en order/purchase. |
| `sale_id` | CLIENT INPUT | Referencia venta original en return; debe pasar visibilidad/branch. |
| `purchase_order_id` | CLIENT INPUT en order; DERIVED FROM RESOURCE en purchase | En purchase viene de `purchases.purchase_order_id`. |
| `purchase_id` | CLIENT INPUT | Referencia aggregate de purchase; futuro API resolvera identidad externa a PK interna. |
| `client_operation_id` | CLIENT INPUT en sale/return; no identidad en order; persisted nullable en purchase | No es permiso ni tenant boundary. |
| `idempotency_key` | CLIENT INPUT | Frontera idempotente del command. |
| `expected_draft_fingerprint` | CLIENT INPUT | Precondicion de `CONFIRM_ORDER`, comparada contra DRAFT persistido. |
| `expected_purchase_fingerprint` | CLIENT INPUT | Precondicion de `CONFIRM_PURCHASE`, comparada contra DRAFT persistido. |

Esta matriz no define DTO publico.

## 11. Public IDs vs BIGINT internos

Los PK/FK `BIGINT` internos son identidad fisica interna.

Reglas:

- `BIGINT` no se expone ni acepta por costumbre;
- `public_id` es preferido externamente donde exista;
- `public_id` no autoriza acceso;
- una referencia externa futura debe resolverse a PK interna antes de ejecutar el command;
- resolver `public_id` no evita validar tenant, branch y permiso;
- no afirmar `public_id` para tablas que no lo poseen.

Este documento no cierra que identificador exacto aparece en cada ruta o DTO.

## 12. Authorization order por command

No existe una regla global que pueda cambiar los contratos frozen.

### CONFIRM_SALE

FASE A:

- idempotencia.

FASE B:

1. lock idempotency;
2. advisory lock `(branch_id, client_operation_id)`;
3. buscar `sales(branch_id, client_operation_id)`;
4. si existe:
   - no repetir efectos;
   - reconciliar key actual a `COMPLETED`;
   - devolver venta existente;
   - esto ocurre antes de validaciones posteriores de branch/terminal/user/permiso;
5. si no existe:
   - validar branch;
   - validar terminal;
   - validar user;
   - validar `user_branches`;
   - validar `SALES_CONFIRM`;
   - continuar efectos.

No se impone auth-before-reconciliation para esta barrera.

### CONFIRM_RETURN

FASE A:

- idempotencia.

FASE B:

1. lock idempotency;
2. advisory lock `(branch_id, client_operation_id)`;
3. buscar `returns(branch_id, client_operation_id)`;
4. si existe:
   - no repetir efectos;
   - reconciliar key actual a `COMPLETED`;
   - devolver devolucion existente;
   - esto ocurre antes de validaciones posteriores de branch/terminal/user/permiso;
5. si no existe:
   - validar contexto actual antes de efectos, incluyendo branch, terminal, user, `user_branches` y `RETURNS_CONFIRM`.

No se impone la regla cross-key de `CONFIRM_ORDER` / `CONFIRM_PURCHASE` sobre este flujo.

### CONFIRM_ORDER

Para key nueva o `IN_PROGRESS` recuperable:

- validar scope business/branch;
- validar user;
- validar `user_branches`;
- validar `PURCHASE_ORDERS_CONFIRM`;

antes de reconciliacion historica permitida o nueva confirmacion.

### CONFIRM_PURCHASE

Para key nueva o `IN_PROGRESS` recuperable:

- validar tenant visible;
- validar branch;
- validar user activo;
- validar `user_branches`;
- validar `PURCHASES_CONFIRM`;

antes de nueva ejecucion o reconciliacion historica.

## 13. SAME TERMINAL KEY

SAME TERMINAL KEY significa misma `idempotency_key`, mismo `request_hash` y estado terminal `COMPLETED` o `FAILED` dentro del scope fisico correspondiente.

Mantener semantica frozen:

| Command | `COMPLETED` | `FAILED` |
| --- | --- | --- |
| `CONFIRM_SALE` | Replay/reconstruccion; no crear otra venta. No agregar reauthorization transversal no congelada. | Error almacenado; no reintentar automaticamente. |
| `CONFIRM_RETURN` | Replay/reconstruccion; no crear otra devolucion. No agregar reauthorization transversal no congelada. | Error almacenado; no reintentar automaticamente. |
| `CONFIRM_ORDER` | Replay/reconstruccion segun contrato. No inventar revalidation transversal. | Error historico; no se convierte a `COMPLETED` porque otra key luego tuvo exito. |
| `CONFIRM_PURCHASE` | Replay historico exacto; no reautoriza; no revalida user; no revalida branch; no revalida catalogos; no repite efectos. | Replay error historico; no reejecuta; no reevalua autorizacion; no se vuelve `COMPLETED` porque otra key luego tuvo exito. |

## 14. NEW KEY / IN_PROGRESS recoverable

| Command | Regla |
| --- | --- |
| `CONFIRM_SALE` | No universalizar authorization-before-reconciliation. La barrera `client_operation_id` ocurre primero: si `sales(branch_id, client_operation_id)` ya existe, la entidad existente prevalece, se reconcilia y se devuelve. Si no existe, se valida auth/context actual antes de efectos. |
| `CONFIRM_RETURN` | Mismo principio con `returns(branch_id, client_operation_id)`. Si existe, la entidad existente prevalece, se reconcilia y se devuelve. Si no existe, se valida auth/context actual antes de efectos. |
| `CONFIRM_ORDER` | Current business/branch/auth antes de reconciliar historia o ejecutar. |
| `CONFIRM_PURCHASE` | Current tenant/branch/user/`user_branches`/`PURCHASES_CONFIRM` antes de reconciliar historia o ejecutar. |

La politica publica adicional de exposicion/autenticacion para reconciliacion `CONFIRM_SALE` / `CONFIRM_RETURN` queda pendiente solo donde el contrato frozen deje margen. No se resuelve aqui.

## 15. client_operation_id

| Command | Regla |
| --- | --- |
| `CONFIRM_SALE` | Requerido. Scope logico `branch + client_operation_id`. Defensa secundaria. No es permiso ni tenant boundary. Una key nueva/recoverable con venta ya existente devuelve/reconcilia esa entidad segun contrato frozen. |
| `CONFIRM_RETURN` | Requerido. Scope logico `branch + client_operation_id`. Defensa secundaria. No es permiso ni tenant boundary. Una key nueva/recoverable con devolucion ya existente devuelve/reconcilia esa entidad segun contrato frozen. |
| `CONFIRM_ORDER` | No es identidad del confirmation command. |
| `CONFIRM_PURCHASE` | Nullable. No es autoridad. No reemplaza idempotencia, `request_hash` ni `expected_purchase_fingerprint`. |

## 16. Cross-tenant visibility

Fronteras conocidas:

- `PURCHASE_ORDER_NOT_FOUND`: pedido inexistente o no visible cuando el contrato lo usa para frontera segura.
- `PURCHASE_NOT_FOUND`: compra inexistente o de otro tenant/business no visible.
- `SALE_NOT_FOUND`: venta original inexistente o no visible para devolucion.

Distinguir:

1. recurso no visible;
2. recurso visible con mismatch estructural.

No exponer existencia cross-tenant.

No inventar:

- `PURCHASE_BUSINESS_MISMATCH`;
- `PURCHASE_BRANCH_MISMATCH`;
- `BRANCH_BUSINESS_MISMATCH` para `CONFIRM_SALE` / `CONFIRM_RETURN` si no existe frozen.

## 17. Structural mismatches

| Codigo | Commands |
| --- | --- |
| `BRANCH_BUSINESS_MISMATCH` | `CONFIRM_ORDER`, `CONFIRM_PURCHASE` solo donde frozen. |
| `PURCHASE_ORDER_BRANCH_MISMATCH` | `CONFIRM_ORDER`. |
| `TERMINAL_BRANCH_MISMATCH` | `CONFIRM_SALE`, `CONFIRM_RETURN`. |
| `CASH_SESSION_TERMINAL_MISMATCH` | `CONFIRM_SALE`. |
| `RETURN_CASH_SESSION_MISMATCH` | `CONFIRM_RETURN`. |
| `CUSTOMER_BUSINESS_MISMATCH` | `CONFIRM_SALE`. |
| `PRICE_LIST_BUSINESS_MISMATCH` | `CONFIRM_SALE`. |
| `PRODUCT_BUSINESS_MISMATCH` | `CONFIRM_SALE`, `CONFIRM_ORDER`, `CONFIRM_PURCHASE`. |
| `SUPPLIER_BUSINESS_MISMATCH` | `CONFIRM_ORDER`, `CONFIRM_PURCHASE`. |

Estos codigos no son automaticamente authorization errors.

## 18. Authoritative resource state

| Command | Estado autoritativo |
| --- | --- |
| `CONFIRM_SALE` | Branch, terminal, cash, catalogos persistidos, payload canonico, inventario, precios/cotizacion y existing sale por `client_operation_id` para reconciliacion. |
| `CONFIRM_RETURN` | `sales`, `sale_items`, `returns` / `return_items` previos, branch de venta y caja solo si aplica. |
| `CONFIRM_ORDER` | `purchase_orders`, `purchase_order_items`, `replenishment_positions`; el DRAFT persistido es autoridad y `expected_draft_fingerprint` es solo precondicion. |
| `CONFIRM_PURCHASE` | `purchases`, `purchase_items`, `purchases.branch_id`, `purchases.purchase_order_id`, supplier/herencia persistida; el DRAFT persistido es autoridad y `expected_purchase_fingerprint` es solo precondicion. |

El pre-read de `CONFIRM_PURCHASE` es discovery only. Despues de locks se relee autoridad. Si identidad/herencia cambio segun contrato, corresponde `PURCHASE_DRAFT_STALE`.

## 19. Audit context

Separar:

- AUTHORIZATION AUTHORITY;
- AUDIT METADATA.

Audit puede conservar segun contratos:

- `actor_user_id`;
- `branch_id`;
- `terminal_id` nullable/contextual;
- `entity_public_id`;
- business/context;
- `client_operation_id`;
- key segura/truncada/hasheada cuando corresponda;
- fingerprints seguros;
- `ip_address`;
- `user_agent`.

`ip_address` y `user_agent` no autorizan.

Este documento no disena logging framework ni middleware.

## 20. Matriz final cuatro commands

| Command | actor source | business source | branch source | terminal | cash session | permission | client_operation_id | aggregate authority | same-terminal-key behavior | new/recoverable-key behavior | cross-tenant visibility |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `CONFIRM_SALE` | Contexto autenticado para ejecucion nueva. | Contexto/branch; no body libre. | Entrada conceptual validada. | Funcional requerida. | Requerida. | `SALES_CONFIRM`. | Requerido; `sales(branch_id, client_operation_id)`. | Venta existente por `client_operation_id` prevalece; si no existe, payload y estado persistido. | Replay/reconstruccion o error almacenado, sin nuevos efectos. | Existing entity por `client_operation_id` puede reconciliarse antes de auth posterior; si no existe, auth antes de efectos. | No inventar `BRANCH_BUSINESS_MISMATCH`; aplicar fronteras frozen. |
| `CONFIRM_RETURN` | Contexto autenticado para ejecucion nueva. | Contexto/branch/venta. | Entrada conceptual y venta same branch. | Contexto operacional requerido. | Condicional. | `RETURNS_CONFIRM`. | Requerido; `returns(branch_id, client_operation_id)`. | Devolucion existente por `client_operation_id` prevalece; si no existe, venta/lineas/returns previas. | Replay/reconstruccion o error almacenado, sin nuevos efectos. | Existing entity por `client_operation_id` puede reconciliarse antes de auth posterior; si no existe, auth antes de efectos. | `SALE_NOT_FOUND` para venta no visible. |
| `CONFIRM_ORDER` | Contexto autenticado actual. | Contexto/branch/pedido seguro. | Entrada esperada y `purchase_orders.branch_id`. | No funcional; audit nullable. | No aplica. | `PURCHASE_ORDERS_CONFIRM`. | No identidad. | `purchase_orders + purchase_order_items`. | Replay/error historico segun key. | Current authorization antes de historical reconciliation o ejecucion. | `PURCHASE_ORDER_NOT_FOUND` para otro tenant/no visible. |
| `CONFIRM_PURCHASE` | Contexto autenticado actual. | Contexto autenticado + `purchases.branch_id`. | `purchases.branch_id`. | No funcional; audit nullable. | No aplica. | `PURCHASES_CONFIRM`. | Nullable, no autoridad. | `purchases + purchase_items`. | `COMPLETED`/`FAILED` no reautoriza ni revalida. | Current authorization antes de historical reconciliation o ejecucion. | `PURCHASE_NOT_FOUND` para otro tenant/no visible. |

## 21. Documentacion parcial

1. `CONFIRM_SALE` same-terminal-key no detalla todos los "no revalidate" con el nivel exhaustivo de `CONFIRM_PURCHASE`.
2. `CONFIRM_RETURN` same-terminal-key no detalla todos los "no revalidate" con el nivel exhaustivo de `CONFIRM_PURCHASE`.
3. `CONFIRM_ORDER` same-key tampoco tiene el nivel exhaustivo de no-revalidation de `CONFIRM_PURCHASE`.
4. Politica publica adicional de exposicion/auth para reconciliation `CONFIRM_SALE` / `CONFIRM_RETURN` queda pendiente solo donde no contradiga frozen contracts.
5. Mapping exacto `public_id` -> DTO/ruta queda pendiente.
6. Authentication transport/provider queda pendiente.

No cerrar estos gaps por inferencia.

## 22. Fuera de alcance

Queda fuera de alcance:

- JWT claims;
- session provider;
- OAuth/OIDC;
- cookies/bearer;
- auth provider;
- headers exactos;
- API gateway;
- endpoints;
- rutas;
- HTTP methods;
- HTTP status;
- DTO exacto;
- OpenAPI;
- middleware;
- RBAC implementation code;
- backend/framework;
- ORM;
- token refresh;
- caching;
- logging framework;
- db-5;
- SQL de implementacion.

## 23. Pendientes

Despues de este micro-hito:

1. Wire-format / transport conventions.
2. Command API contracts.
3. Mapping publico definitivo de references/DTOs cuando corresponda.

No se desarrollan aqui.
