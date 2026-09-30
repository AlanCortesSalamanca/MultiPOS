# CONFIRM_SALE Command API v0.1

- Estado: BORRADOR CONTROLADO
- Version: v0.1
- Implementacion: no iniciada

Este documento **todavia NO esta FROZEN**. Define la frontera HTTP publica del command `CONFIRM_SALE`; las decisiones marcadas `PENDIENTE ANTES DE FREEZE` no se consideran cerradas.

## 1. Objetivo y autoridad

Este contrato adapta a una interfaz HTTP publica concreta el command transaccional frozen de confirmacion de venta. No redefine invariantes, efectos, locks, persistencia, lifecycle de idempotencia, autorizacion frozen ni reconciliacion por `client_operation_id`.

La autoridad de negocio es `docs/transactions/confirmar-venta-v0.1.md`. Las autoridades compartidas de identidad/frontera, authorization/context, error model, idempotencia y transporte se aplican conforme a los documentos referenciados abajo. El modelo fisico vigente es db-4.

Este documento no modifica ni sustituye ninguna autoridad frozen.

## 2. Fuentes autoritativas

- `especificacion_maestra_pos_multisucursal_v0.5.md`
- `database/schema-v0.5-db-4.sql`
- `docs/database/modelo-fisico-v0.5-db-4.md`
- `docs/domain/tax-snapshot-v1.md`
- `docs/transactions/confirmar-venta-v0.1.md`
- `docs/api/conventions-v0.1.md`
- `docs/api/authorization-and-context-v0.1.md`
- `docs/api/error-model-v0.1.md`
- `docs/api/idempotency-and-preconditions-v0.1.md`
- `docs/api/transport-v0.1.md`

`TAX SNAPSHOT v1` tuvo primera adopcion contractual en `CONFIRM_PURCHASE`; este contrato no lo adopta como input de SALE.

## 3. Operacion HTTP

| Propiedad | Valor |
|---|---|
| Metodo | `POST` |
| Ruta | `/api/v1/sales/confirm` |
| Exito | `200 OK` |
| Success envelope | `{ "data": { ... } }` |
| Error envelope | `{ "error": { "code": "...", "message": "...", "category": "...", "retryable": false } }` |

El envelope de exito y error sigue Transport v0.1. El HTTP `200` se utiliza para confirmacion, replay y reconciliacion exitosa.

### 3.1 Headers

#### `Idempotency-Key`

- Obligatorio.
- Se transporta exclusivamente como header y no aparece en el JSON body.
- Formato frozen Transport: un valor opaco ASCII visible (`0x21`–`0x7E`), longitud 1–128 caracteres, sin espacios ni controles, case-sensitive.
- No forma parte de `request_hash`.
- Ausente o invalido: categoria `VALIDATION`, HTTP `422`; Transport no congela un `error.code` especifico. No se inventa uno en este borrador; ver pendientes antes de freeze.

#### `X-Request-Id`

- Opcional en request.
- Si falta o es invalido, el backend genera uno.
- Se devuelve en toda respuesta que la capa HTTP pueda construir.
- No forma parte del command ni de `request_hash`.

No se define header de autenticacion ni otro header command-specific.

## 4. Contexto, referencias y modos del request

El request distingue dos formas mutuamente excluyentes. No se envia un campo `mode`: la presencia de `quotation_public_id` selecciona `QUOTATION SALE`; su ausencia selecciona `DIRECT SALE`.

Campos comunes del body en ambos modos:

- `branch_public_id`: sucursal objetivo.
- `cash_session_public_id`: sesion de caja requerida.
- `client_operation_id`: identidad logica local de la venta.
- `lines`: lineas del command.
- `payments`: pagos aplicados.

No se aceptan `business_id`, `user_id`, `terminal_id` ni PK/FK `BIGINT` internos en el body o path.

Contexto derivado/autenticado:

- `business`: del contexto autenticado/branch segun el flujo frozen; nunca selector libre del body.
- `user`: actor autenticado.
- `terminal`: terminal autenticada, no caller input.

### 4.1 Referencias publicas

- Branch: `branch_public_id`, resuelto desde `branches.public_id`.
- Cash session: `cash_session_public_id`, resuelto desde `cash_sessions.public_id`; se valida abierta y perteneciente a branch y terminal autenticada.
- Customer: `customer_public_id`, cuando aplique, resuelto desde `customers.public_id`.
- Price list: `price_list_public_id` en venta directa, resuelto desde `price_lists.public_id`.
- Product: `product_public_id`, resuelto desde `products.public_id`.
- Product unit/presentation: `unit_code + unit_context` junto con el `product_public_id` de esa linea. db-4 define `units.code` como unico global y `product_units` como unica por `(product_id, unit_id, context)`; no se expone `product_units.id`.
- Payment method: `payment_method_code`, scoped al `business_id` derivado. db-4 define `UNIQUE(business_id, code)`; no se expone `payment_methods.id`.
- Quotation: `quotation_public_id`, cuando aplique, resuelto desde `quotations.public_id`.

`unit_context` usa el vocabulario exacto frozen de `product_unit_context`, pero CONFIRM_SALE acepta solo `SALE` y `BOTH`. Es explicito porque db-4 permite asociaciones distintas para el mismo producto/unidad cuando cambia `context`.

El contrato trata `payment_methods.code` como identificador externo contractual dentro del business derivado. Mientras se publique como referencia API, no debe reutilizarse para un metodo con semantica diferente. Cambiar `name` no cambia `code`; cambiar `code` es cambio contractual, no edicion visual.

Estas referencias no autorizan acceso. Persisten las validaciones de tenant, branch, estado y command.

## 5. Request DTO

Los siguientes JSON son formas conceptuales normativas para este borrador. Todos los importes y cantidades decimales son strings canonicos segun Transport; no JSON numbers.

### 5.1 DIRECT SALE

`quotation_public_id` debe estar ausente. `price_list_public_id` es obligatorio. `customer_public_id` es opcional.

```json
{
  "branch_public_id": "<branch-public-uuid>",
  "cash_session_public_id": "<cash-session-public-uuid>",
  "client_operation_id": "<opaque-client-operation-id>",
  "customer_public_id": "<customer-public-uuid>",
  "price_list_public_id": "<price-list-public-uuid>",
  "lines": [
    {
      "product_public_id": "<product-public-uuid>",
      "unit_code": "<unit-code>",
      "unit_context": "SALE",
      "quantity": "1.0000",
      "expected_unit_price": "10.00",
      "expected_discount_amount": "0.00",
      "expected_tax_total": "1.60"
    }
  ],
  "payments": [
    {
      "payment_method_code": "<business-scoped-code>",
      "amount": "11.60",
      "reference": "<optional-payment-reference>"
    }
  ]
}
```

Reglas:

- `customer_public_id` puede omitirse. Si no se manda, no se asigna cliente. `null` es invalido.
- `price_list_public_id` es obligatorio y representa la lista efectiva enviada por el caller; validar business, estado activo, moneda y compatibilidad con la politica del cliente.
- `quotation_public_id` no debe aparecer.
- `lines` debe contener una o mas lineas. Cada una incluye las referencias publicas, cantidad y valores esperados indicados.
- `expected_unit_price` permite comparar la expectativa POS con el precio activo autoritativo de `product_prices`. El servidor decide el precio final y aplica `PRICE_CHANGED` segun el contrato transaccional.
- `expected_discount_amount` es el descuento monetario esperado/solicitado para la linea. El servidor valida la regla de descuento y calcula los importes/totales autoritativos; el valor del caller no sustituye esas validaciones.
- `expected_tax_total` es el impuesto monetario total esperado de la linea. El servidor calcula/valida impuestos desde las fuentes autoritativas y produce los snapshots persistidos; el valor del caller no sustituye el calculo ni el snapshot.
- No se aceptan tasa, componentes, `tax_profile`, `tax_snapshot`, subtotal, total ni snapshots autoritativos suministrados por el caller.
- `payments` debe contener uno o mas pagos. Cada `amount` representa el importe aplicado a la venta, es mayor que cero y se suma con aritmetica decimal exacta. La suma debe igualar exactamente el total calculado.
- Todos los metodos de pago de la venta deben resolver al mismo `replenishment_channel` (`CASH` o `TRANSFER`).

### 5.2 QUOTATION SALE

`quotation_public_id` es obligatorio. `customer_public_id` y `price_list_public_id` deben estar ausentes.

```json
{
  "branch_public_id": "<branch-public-uuid>",
  "cash_session_public_id": "<cash-session-public-uuid>",
  "client_operation_id": "<opaque-client-operation-id>",
  "quotation_public_id": "<quotation-public-uuid>",
  "lines": [
    {
      "product_public_id": "<product-public-uuid>",
      "unit_code": "<unit-code>",
      "unit_context": "SALE",
      "quantity": "1.0000"
    }
  ],
  "payments": [
    {
      "payment_method_code": "<business-scoped-code>",
      "amount": "<amount-applied>",
      "reference": "<optional-payment-reference>"
    }
  ]
}
```

No se envian `customer_public_id`, `price_list_public_id`, `expected_unit_price`, `expected_discount_amount` ni `expected_tax_total`. El customer, price list y los snapshots de precio, descuento e impuestos provienen de la quotation vigente. El pago/canal se determina al confirmar la venta.

El contrato transaccional frozen exige validar que la cotizacion sea convertible y vigente, conservar sus snapshots de precio, descuento e impuestos, y revalidar producto/unidad, stock, cliente, pagos, canal, permisos y estado/vigencia segun sus reglas. Separadamente, **este Command API adopta como decision command-specific v0.1** que la conversion sea completa y que las lineas del request correspondan una a una, en el mismo orden, con todas las lineas de la cotizacion; producto, presentacion/unidad y quantity deben coincidir. No se pueden agregar, omitir ni cambiar lineas. El matching exacto y la regla publica de conversion completa pertenecen a este Command API; no se atribuyen como texto literalmente congelado en el contrato transaccional.

La cotizacion no reserva stock ni caja. CONFIRM_SALE revalida stock dentro de su transaccion. Cambios posteriores en `product_prices` no producen `PRICE_CHANGED` para una cotizacion vigente; la cotizacion vencida no se recalcula silenciosamente.

## 6. Representacion wire de cantidades, importes y opcionales

Aplican las reglas frozen de Transport:

| Campo | Representacion |
|---|---|
| `quantity` | JSON string decimal canonico, scale 4 |
| `expected_unit_price` | JSON string decimal canonico, scale 2 |
| `expected_discount_amount` | JSON string decimal canonico, scale 2 |
| `expected_tax_total` | JSON string decimal canonico, scale 2 |
| `payments[].amount` | JSON string decimal canonico, scale 2 |

No usar JSON numbers, notacion cientifica, separadores de miles ni formatos localizados. No redondear ni truncar silenciosamente. El servidor es autoridad del calculo monetario y compara pagos con total usando igualdad decimal exacta.

Regla general: opcionales no proporcionados se representan mediante propiedad ausente. `null` es invalido salvo regla explicita; este contrato no define ninguna excepcion.

| Campo | Regla |
|---|---|
| `customer_public_id` | En DIRECT SALE puede omitirse; `null` invalido. En QUOTATION SALE debe omitirse; cliente proviene de quotation. |
| `quotation_public_id` | En DIRECT SALE debe omitirse. En QUOTATION SALE es obligatorio y no nulo. |
| `price_list_public_id` | En DIRECT SALE obligatorio. En QUOTATION SALE debe omitirse; lista proviene de quotation. |
| Campos expected de linea | En DIRECT SALE obligatorios. En QUOTATION SALE deben omitirse. |
| `payments[].reference` | Omitir si no hay referencia; `null` invalido. Si esta presente, es string semantico exacto. |

En particular, propiedad ausente y `null` no son equivalentes en canonicalizacion; `null` en los casos anteriores se rechaza.

## 7. `client_operation_id`

- JSON string obligatorio dentro del body.
- Longitud wire: 1..128 Unicode code points / Unicode scalar values.
- Opaque, generado por el POS, case-sensitive y no vacio.
- No exige UUID ni ULID.
- Se rechazan caracteres de control. Se permiten caracteres Unicode representables deterministamente por Transport; no restringir a ASCII.
- No aplicar trim, case folding ni normalizacion Unicode. Las strings se interpretan exactamente segun Transport.
- Debe permanecer estable durante retries de la misma venta logica y ser distinto para una nueva venta logica.
- Scope logico: `(branch, client_operation_id)`, respaldado en db-4 por `UNIQUE(branch_id, client_operation_id)`.
- Forma parte de `request_hash` y no es `Idempotency-Key`.

El limite publico no cambia `sales.client_operation_id TEXT` en db-4.

## 8. Orden de arrays y canonicalizacion de `request_hash`

`request_hash` representa el payload semantico canonico. Aplican reglas frozen Transport para UTF-8, orden determinista de propiedades de objetos, arrays, strings, decimales, tipos y serializacion. `Idempotency-Key`, `X-Request-Id`, auth headers, PK internas y valores generados por backend nunca son parte del hash.

### 8.1 `lines`

`lines` es semantico y ordenado. Se preserva el orden recibido y no se ordena para el hash. Ese orden determina la representacion historica `line_number` de `sale_items`; en QUOTATION SALE el matching exacto conserva el orden de lineas definido por este Command API.

### 8.2 `payments`

`payments` es una coleccion semantica no ordenada. Reordenar pagos equivalentes no cambia la operacion economica ni el `request_hash`.

Canonicalizacion command-specific:

1. Canonicalizar cada objeto payment completo aplicando Transport: propiedades ordenadas, strings/decimales canonicos, ausencia conservada y sin `null` para opcionales.
2. Serializar cada objeto canonicalizado a su representacion JSON UTF-8 determinista segun Transport.
3. Ordenar para `request_hash` los objetos por comparacion lexicografica ordinal de su representacion JSON canonicalizada completa, en bytes UTF-8.
4. Mantener todas las ocurrencias, incluidos duplicados legitimos. No consolidar pagos ni alterar sus valores.
5. Hashear el array resultante con el mecanismo interno que se implemente. Esta regla no obliga a persistir los pagos en ese orden.

### 8.3 Payload canonico DIRECT SALE

```json
{
  "branch_public_id": "...",
  "cash_session_public_id": "...",
  "client_operation_id": "...",
  "customer_public_id": "...",
  "price_list_public_id": "...",
  "lines": [
    {
      "product_public_id": "...",
      "unit_code": "...",
      "unit_context": "SALE",
      "quantity": "1.0000",
      "expected_unit_price": "10.00",
      "expected_discount_amount": "0.00",
      "expected_tax_total": "1.60"
    }
  ],
  "payments": [
    {
      "payment_method_code": "...",
      "amount": "11.60",
      "reference": "..."
    }
  ]
}
```

`customer_public_id` solo se incluye si se proporciono. El orden de `payments` en este ejemplo representa el orden despues de canonicalizar y ordenar la coleccion.

### 8.4 Payload canonico QUOTATION SALE

```json
{
  "branch_public_id": "...",
  "cash_session_public_id": "...",
  "client_operation_id": "...",
  "quotation_public_id": "...",
  "lines": [
    {
      "product_public_id": "...",
      "unit_code": "...",
      "unit_context": "SALE",
      "quantity": "1.0000"
    }
  ],
  "payments": [
    {
      "payment_method_code": "...",
      "amount": "...",
      "reference": "..."
    }
  ]
}
```

Propiedades opcionales ausentes permanecen ausentes; no agregar defaults silenciosos. En ambos modos se excluyen siempre:

- `Idempotency-Key`, `X-Request-Id` y auth headers;
- business, user y terminal derivados;
- PK/FK internos;
- folio y `confirmed_at` generados;
- snapshots generados por backend;
- totales calculados autoritativamente;
- valores internos no enviados por cliente.

Transport no fija algoritmo criptografico ni libreria para el digest.

## 9. Idempotencia y reconciliacion

Scope fisico db-4:

```text
(business_id, operation_type, idempotency_key)
operation_type = 'CONFIRM_SALE'
```

La key es obligatoria y distinta de `client_operation_id`.

### 9.1 Resolucion inicial de key

- Misma key y hash distinto, en cualquier estado: `SALE_IDEMPOTENCY_KEY_REUSED`; no ejecutar efectos ni exponer hashes.
- Misma key/hash y `IN_PROGRESS` vigente: `SALE_IDEMPOTENCY_IN_PROGRESS`. No esperar bloqueado por el lease; el caller puede reintentar posteriormente.
- `COMPLETED` con mismo hash: replay exitoso, sin repetir efectos ni crear otra venta.
- `FAILED` con mismo hash: reproducir el error de dominio original almacenado; no reejecutar ni convertir la fila a `COMPLETED`.
- `IN_PROGRESS` recuperable: solo recuperar cuando el hash coincida, `result_entity_id IS NULL`, la fila pueda adquirirse sin ejecucion activa y se aplique la verificacion de venta historica frozen.

Leases y retencion no cambian: `IN_PROGRESS` usa lease de 30 segundos; `COMPLETED` y `FAILED` retienen 30 dias. `expires_at` no es mecanismo de locking.

### 9.2 NEW KEY / `IN_PROGRESS` RECOVERABLE

Preservar exactamente esta precedencia:

1. Resolver idempotencia inicial.
2. Para key nueva o recuperable, verificar propiedad/estado de la key segun contrato frozen.
3. Aplicar la barrera/secuencia frozen por `(branch_id, client_operation_id)`.
4. Buscar `sales(branch_id, client_operation_id)`.
5. Si existe la venta:
   - la venta historica prevalece;
   - reconciliar la key actual a `COMPLETED` y asociarla a esa venta conforme al contrato transaccional;
   - no repetir efectos, movimientos, folio, pagos, caja ni reposicion;
   - devolver success historico de esa venta.
6. Solamente si la venta no existe, continuar validaciones command-specific y efectos conforme al contrato transaccional.

La reconciliacion ocurre antes de las validaciones posteriores de branch, terminal, user, `user_branches` y permiso `SALES_CONFIRM`. **No se aplica authorization-before-reconciliation.**

La resolucion de contexto/referencias necesaria para entrar en la secuencia no equivale a adelantar dichas validaciones. La politica publica adicional para una reconciliacion historica se limita a lo que permiten los contratos frozen; este borrador no impone una reautorizacion que los contradiga.

Una key distinta con el mismo `(branch, client_operation_id)` tambien devuelve la venta ya existente; no se infiere igualdad de payload desde `sales`, que no almacena `request_hash`.

## 10. Secuencia de contexto y autorizacion

Separar explicitamente:

### A. Contexto autenticado/derivado

- Business/tenant: contexto autenticado/branch; no body input.
- User actor: autenticado; alimenta autorizacion y audit.
- Terminal: autenticada; requerida para venta, no body input.
- Branch target: `branch_public_id` del body.
- Cash session: `cash_session_public_id` del body.

Obtener contexto autenticado y resolver las referencias necesarias para la secuencia no es una orden para evaluar primero los permisos command-specific. El orden frozen de idempotencia y reconciliacion prevalece.

### B. Inputs publicos

Branch, cash session, `client_operation_id`, modo/direct quotation, referencias de cliente/lista/productos/unidades, cantidades, valores esperados de venta directa y pagos, segun el modo.

### C. Validaciones posteriores si no existe venta

Seguir contrato transaccional: branch activa/coherente con business, terminal autenticada ACTIVE y misma branch, user ACTIVE, acceso por `user_branches`, permiso `SALES_CONFIRM`; despues quotation si aplica, cash session abierta/same branch/same terminal, cliente/lista/productos/unidades/precios, descuentos/impuestos, metodos/canal de pagos, igualdad exacta de pagos, stock, secuencia y efectos atomicos.

`SALES_CONFIRM` requiere permiso funcional mediante roles activos del business y acceso explicito a branch mediante `user_branches`. No se autoriza por nombre de rol ni existe bypass especial ADMIN.

## 11. Response de exito

Toda confirmacion nueva, replay `COMPLETED` y reconciliacion exitosa usa HTTP `200 OK` y el mismo DTO publico:

```json
{
  "data": {
    "sale_public_id": "<sale-public-uuid>",
    "folio": "VEN-000001",
    "status": "CONFIRMED",
    "total": "11.60",
    "currency": "MXN",
    "confirmed_at": "2026-09-29T00:00:00Z"
  }
}
```

- `sale_public_id`: `sales.public_id`, UUID no nulo, generado por db-4 y unico.
- `folio`: identidad operativa/humana con unicidad por branch; no sustituye `sale_public_id`.
- `status`: `CONFIRMED` representa el resultado historico de este command, no el estado vivo actual de la venta.
- `total`: string decimal canonico scale 2.
- `currency`: codigo de moneda; MVP `MXN`.
- `confirmed_at`: timestamp RFC 3339 UTC con `Z`.

Si posteriormente el estado actual de `sales` cambia por una devolucion o cancelacion, un replay/reconciliacion conserva `status: "CONFIRMED"` como resultado historico de la confirmacion. No sustituirlo por `PARTIALLY_RETURNED`, `RETURNED`, `CANCELLED` u otro estado posterior.

Este DTO es igual en confirmacion nueva, replay `COMPLETED` y reconciliacion por `client_operation_id`. No se agregan `replayed`, `reconciled` ni `idempotent_replay`. `idempotency_keys.response_body` es almacenamiento interno y no equivale automaticamente al DTO publico.

## 12. Errores

La response de error sigue el envelope Transport. `code` conserva exactamente el codigo frozen; `category` usa las categorias existentes; `HTTP` se deriva de Transport solo cuando la categoria esta suficientemente cerrada.

`FAILED persistido` indica el tratamiento de errores deterministas que ocurren en FASE B: rollback completo y persistencia de la key `FAILED` mediante FASE C segun el contrato transaccional. Un fallo tecnico no se convierte automaticamente en `FAILED`.

| `error.code` | `category` | Retryable | HTTP | FAILED persistido | Autoridad / observacion |
|---|---|---|---:|---|---|
| `SALE_IDEMPOTENCY_KEY_REUSED` | `IDEMPOTENCY` | PENDIENTE ANTES DE FREEZE | 409 | No cambia el estado historico de la key | Transaction §4; Error Model §§9,17; Transport §14 |
| `SALE_IDEMPOTENCY_IN_PROGRESS` | `IDEMPOTENCY` | true: reintento posterior permitido por lease/lifecycle | 409 | No | Transaction §4; Error Model §18; Transport §14 |
| `USER_INACTIVE` | `AUTHORIZATION` | PENDIENTE ANTES DE FREEZE | 403 | Si es error deterministico en FASE B | Error Model §§3,11; Transaction §§3,16 |
| `USER_BRANCH_FORBIDDEN` | `AUTHORIZATION` | PENDIENTE ANTES DE FREEZE | 403 | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Error Model §§3,11; Transaction §4 |
| `USER_PERMISSION_DENIED` | `AUTHORIZATION` | PENDIENTE ANTES DE FREEZE | 403 | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Error Model §§3,11; Transaction §4 |
| `TERMINAL_INACTIVE` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§11,19 dejan categoria sin cerrar |
| `TERMINAL_BRANCH_MISMATCH` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Auth & Context §17 y Error Model §11: mismatch estructural, no automaticamente AUTHORIZATION |
| `BRANCH_INACTIVE` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§11,19 no fijan categoria exacta |
| `CASH_SESSION_REQUIRED` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Auth & Context §9; Transaction §16; Error Model §12 no asigna categoria |
| `CASH_SESSION_CLOSED` | `STATE_CONFLICT` | PENDIENTE ANTES DE FREEZE | 409 | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Error Model §§3,12; Transaction §4; Transport §14 |
| `CASH_SESSION_TERMINAL_MISMATCH` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Auth & Context §9; Error Model §§11,12 |
| `CUSTOMER_INACTIVE` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§12,19 |
| `CUSTOMER_BUSINESS_MISMATCH` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Auth & Context §17; mismatch estructural, no automaticamente AUTHORIZATION |
| `PRICE_LIST_INACTIVE` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§12,19 |
| `PRICE_LIST_BUSINESS_MISMATCH` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Auth & Context §17; mismatch estructural |
| `PRODUCT_INACTIVE` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Transaction §§4,16; Error Model §§12,19 |
| `PRODUCT_BUSINESS_MISMATCH` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Auth & Context §17; mismatch estructural |
| `PRODUCT_UNIT_INVALID` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§12,19 |
| `PRICE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | PENDIENTE ANTES DE FREEZE | 404 | Error de dominio deterministico sujeto a FASE C | Error Model §§3,12; Transport §14 |
| `PRICE_CHANGED` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Transaction §§3,4; Error Model §§9,12,19 no lo clasifica como PRECONDITION |
| `INVALID_QUANTITY` | `VALIDATION` | PENDIENTE ANTES DE FREEZE | 422 | Error de dominio deterministico sujeto a FASE C | Error Model §§3,12; Transport §14 |
| `INVALID_DISCOUNT` | `VALIDATION` | PENDIENTE ANTES DE FREEZE | 422 | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Error Model §§3,12; Transaction §4 |
| `INVALID_TAX_CALCULATION` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§12,19 no fija categoria especifica |
| `INSUFFICIENT_STOCK` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Transaction §§3,4; Error Model §§12,19 |
| `PAYMENT_METHOD_INACTIVE` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§12,19 |
| `PAYMENT_TOTAL_MISMATCH` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Si es error deterministico en FASE B; determinismo confirmado por Error Model §8 | Transaction §16; Error Model §§8,12 |
| `MIXED_REPLENISHMENT_CHANNELS` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §§12,19 |
| `DOCUMENT_SEQUENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | PENDIENTE ANTES DE FREEZE | 404 | Error de dominio deterministico sujeto a FASE C | Error Model §12; mapping de Transport §14 |
| `DOCUMENT_SEQUENCE_INACTIVE` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model §12 no asigna categoria |
| `QUOTATION_NOT_FOUND` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Transaction §16; Error Model no fija especificamente visibilidad de quotation |
| `QUOTATION_BRANCH_MISMATCH` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Error de dominio deterministico sujeto a FASE C | Auth & Context §17; mismatch estructural, no automaticamente AUTHORIZATION |
| `QUOTATION_EXPIRED` | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | PENDIENTE ANTES DE FREEZE | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Transaction §§3,4; Error Model §§12,19 no fija categoria especifica |
| `QUOTATION_ALREADY_CONVERTED` | `STATE_CONFLICT` | PENDIENTE ANTES DE FREEZE | 409 | Si es error deterministico en FASE B; ejemplo frozen de FASE C | Transaction §§3,4; Error Model §§3,12; Transport §14 |
| `QUOTATION_STATUS_INVALID` | `STATE_CONFLICT` | PENDIENTE ANTES DE FREEZE | 409 | Error de dominio deterministico sujeto a FASE C | Error Model §§3,12; Transport §14 |

`SALE_ALREADY_CONFIRMED` esta reservado en el contrato transaccional para otros flujos y **no** se emite al encontrar la venta durante replay/reconciliacion. `SALE_IDEMPOTENCY_FAILED` se menciona como categoria interna; una key `FAILED` reproduce el error original, no un codigo publico generico.

### 12.1 Errores de transporte y tecnicos

- `Idempotency-Key` ausente/invalido: `VALIDATION`, HTTP `422`; no hay `error.code` frozen especifico. No inventar uno en este borrador.
- Auth no autenticada: `AUTHENTICATION`, HTTP `401`; no hay codigo concreto frozen.
- Fallo tecnico: `INTERNAL_TECHNICAL`, HTTP `500` por defecto. No exponer SQL, stack, exceptions raw, locks, PK internas ni detalles de infraestructura; no persistir automaticamente como `FAILED`.
- `X-Request-Id` ausente/invalido se sustituye por uno generado y no es error del command.

## 13. Pendientes antes de freeze

1. Cerrar `category`, HTTP cuando no derivable y `retryable` publico de los errores marcados en la tabla. En especial:
   - `PRICE_CHANGED` no es `PRECONDITION`: CONFIRM_SALE no tiene fingerprint/precondition frozen; su categoria y HTTP quedan pendientes.
   - La tabla de categorias de Error Model §3 da como ejemplos claros de `AUTHORIZATION` `USER_INACTIVE`, `USER_BRANCH_FORBIDDEN` y `USER_PERMISSION_DENIED`. Authorization & Context §17 establece que los mismatches estructurales no son automaticamente `AUTHORIZATION`. Error Model §19 coloca `TERMINAL_INACTIVE`, `TERMINAL_BRANCH_MISMATCH` y `BRANCH_INACTIVE`, junto con `CASH_SESSION_TERMINAL_MISMATCH`, `CUSTOMER_BUSINESS_MISMATCH`, `PRICE_LIST_BUSINESS_MISMATCH`, `PRODUCT_BUSINESS_MISMATCH` y `QUOTATION_BRANCH_MISMATCH`, en la columna general `validation/context/state errors`, no en `authorization errors`. Esa agrupacion amplia no fija la categoria individual exacta. Por ello, category/HTTP permanecen `PENDIENTE ANTES DE FREEZE`; no se escoge una categoria por inferencia.
   - Para los demas codigos con retryability pendiente, el contrato transaccional no fija un boolean publico. No inferir `false` solo por ser deterministico.
2. Definir errores/codigos publicos para referencias con formato valido pero no resolubles cuando el catalogo frozen no contiene un codigo especifico (por ejemplo branch, customer, price list, product o payment method no encontrados). No inventar ni sustituir silenciosamente codigos en este borrador.
3. Definir `error.code` para errores genericos de request validation, ausencia/formato invalido de `Idempotency-Key`, autenticacion y fallo tecnico solo si se requiere un codigo concreto; las autoridades actuales no lo fijan.
4. Cerrar la politica publica adicional de exposicion/auth en reconciliacion solo donde las autoridades frozen dejan margen. No puede cambiar el orden frozen ni introducir authorization-before-reconciliation.

Algoritmo criptografico, libreria de canonicalizacion, backend, framework, middleware, auth provider, SQL, logging, deployment y OpenAPI siguen diferidos a implementacion/otros hitos y no son pendientes de freeze de este contrato.

## 14. Fuera de alcance

Este contrato no define backend, lenguaje, framework, ORM, SQL, repositorios, servicios, clases, handlers, implementacion de locks/advisory locks, auth provider, deployment, frontend, aplicacion desktop, API gateway, reverse proxy, OpenAPI ni cambios a db-4/db-5.
