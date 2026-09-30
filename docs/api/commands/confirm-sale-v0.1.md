# CONFIRM_SALE Command API v0.1

- Estado: BORRADOR CONTROLADO
- Version: v0.1
- Implementacion: no iniciada

Este documento **todavia NO esta FROZEN**. Define la frontera HTTP publica del command `CONFIRM_SALE`; las decisiones pendientes de la seccion 13 no se consideran cerradas.

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
- `docs/api/public-error-codes-v0.1.md`

`public-error-codes-v0.1.md` es la autoridad aditiva compartida de los cuatro fallbacks publicos adoptados aqui. No sustituye codigos frozen especificos ni redefine las autoridades base.

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
- Ausente o invalido: `REQUEST_VALIDATION_FAILED`, categoria `VALIDATION`, HTTP `422`, `retryable=false`, conforme al catalogo aditivo compartido.
- No iniciar la ejecucion del command ni crear una fila idempotente usando una key invalida.

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

### 4.2 Resolucion scoped y referencias no resolubles

Resolver referencias publicas exclusivamente dentro del scope visible determinado por contexto confiable. La referencia del caller no amplia tenant/business/branch ni permite derivar el scope desde un recurso encontrado globalmente.

Una referencia inexistente y una referencia fuera del scope visible producen la misma respuesta publica de no-resolucion. No hacer lookup global posterior para distinguirlas. No revelar el recurso oculto ni su tenant/business/branch mediante `message`, `details`, status u otro side channel contractual.

| Input publico | Condicion | `error.code` | `category` | HTTP | `retryable` |
|---|---|---|---|---:|---|
| `branch_public_id` | No resoluble dentro del scope visible | `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `cash_session_public_id` | No resoluble dentro del scope visible | `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `customer_public_id` | No resoluble dentro del scope visible | `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `price_list_public_id` | No resoluble dentro del scope visible | `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `product_public_id` | No resoluble dentro del scope visible | `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `payment_method_code` | No resoluble dentro del business/scope visible derivado | `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `quotation_public_id` | No resoluble dentro de su scope visible | `QUOTATION_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `unit_code + unit_context`, junto con el producto | Presentacion no valida/utilizable para el producto | `PRODUCT_UNIT_INVALID` | `VALIDATION` | 422 | false |

Para quotation, `QUOTATION_NOT_FOUND` significa cotizacion no encontrada dentro del scope visible: cubre indistinguiblemente inexistencia y no-visibilidad, sin afirmar inexistencia global. Una cotizacion visible de branch incompatible conserva `QUOTATION_BRANCH_MISMATCH`.

No usar `REFERENCE_NOT_FOUND` para una presentacion invalida ni reemplazar `PRICE_NOT_FOUND`, `DOCUMENT_SEQUENCE_NOT_FOUND` u otro codigo frozen/especifico adecuado. `CASH_SESSION_REQUIRED` expresa que falta la referencia obligatoria; una referencia proporcionada con formato valido pero no resoluble sigue el mapping de esta tabla.

### 4.3 Semantica publica de `PRODUCT_UNIT_INVALID`

`PRODUCT_UNIT_INVALID` significa que la presentacion seleccionada no es valida/utilizable para `CONFIRM_SALE`. Incluye contractualmente:

- combinacion inexistente;
- unit que no corresponde al producto;
- context incompatible;
- asociacion `product_unit` inactiva.

No revelar cual de estas causas internas ocurrio. El codigo mantiene siempre `VALIDATION`, HTTP `422` y `retryable=false`; no bifurcar category/HTTP segun la causa interna.

### 4.4 Mismatch visibility policy

Los codigos mismatch frozen solo se emiten cuando el recurso ya es visible/resoluble dentro del scope permitido. Si la referencia no resuelve dentro de ese scope, no hacer lookup global para comprobar si existe en otro tenant/business/branch: usar `REFERENCE_NOT_FOUND/404` cuando no exista codigo especifico seguro; para quotation, `QUOTATION_NOT_FOUND/404`.

| Codigo frozen | Condicion publica para emitirlo | `category` | HTTP |
|---|---|---|---:|
| `TERMINAL_BRANCH_MISMATCH` | Terminal del contexto autenticado y branch visible con relacion incompatible | `STATE_CONFLICT` | 409 |
| `CASH_SESSION_TERMINAL_MISMATCH` | Sesion visible/resoluble que no corresponde a la terminal requerida | `STATE_CONFLICT` | 409 |
| `CUSTOMER_BUSINESS_MISMATCH` | Cliente visible/resoluble con business incompatible para el command | `STATE_CONFLICT` | 409 |
| `PRICE_LIST_BUSINESS_MISMATCH` | Lista visible/resoluble con business incompatible para el command | `STATE_CONFLICT` | 409 |
| `PRODUCT_BUSINESS_MISMATCH` | Producto visible/resoluble con business incompatible para el command | `STATE_CONFLICT` | 409 |
| `QUOTATION_BRANCH_MISMATCH` | Cotizacion visible/resoluble con branch incompatible para la venta | `STATE_CONFLICT` | 409 |

La terminal proviene del contexto autenticado, no de un selector libre del body. Una branch publica no resoluble no se convierte en `TERMINAL_BRANCH_MISMATCH`. Con scope estricto de un solo business, algunos `*_BUSINESS_MISMATCH` pueden no ser alcanzables por selectores publicos ordinarios; se conservan los codigos frozen y no se amplia visibilidad para hacerlos alcanzables.

La resolucion scoped se aplica en el punto correspondiente de la secuencia frozen; no introduce `authorization-before-reconciliation` ni una validacion anticipada de todos los recursos antes de replay/reconciliacion. Visibilidad no equivale a permiso para ejecutar el command: se preservan las validaciones y el orden de las secciones 9 y 10.

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

Las referencias se resuelven conforme al scope visible de la seccion 4, en el punto que corresponda a la secuencia frozen. No adelantar validaciones de catalogos, caja o cotizacion para sustituir un replay/reconciliacion por un error actual de esos recursos.

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

La response de error sigue el envelope Transport. Los codigos frozen conservan su nombre y significado; los cuatro fallbacks compartidos provienen de `public-error-codes-v0.1.md`. La prioridad es: codigo frozen/domain-specific aplicable, codigo publico especifico del Command API y, por ultimo, fallback shared. Nunca reemplazar un codigo especifico correcto por uno generico.

Las clasificaciones publicas command-specific se cierran en este contrato usando las categorias existentes de Error Model y el HTTP mapping de Transport; no se atribuye al contrato transaccional un mapping HTTP que no define.

`retryable=true` significa que la misma intencion semantica, sin corregir datos de negocio, puede razonablemente volver a intentarse posteriormente por una condicion temporal/transitoria reconocida por el contrato. No determina por si mismo misma key, nueva key ni retry inmediato; la mecanica de key se documenta separadamente en la seccion 9.

Solo `SALE_IDEMPOTENCY_IN_PROGRESS` usa `retryable=true`; todos los demas codigos publicos de este command, incluidos los shared, usan `false`. Una posible reposicion futura de stock, reactivacion de recursos o cambio de precio no convierte por si sola la invalidacion en una condicion transitoria reconocida para reintento. `CONFIRM_SALE` no tiene una business-retryable especial frozen; la necesidad de cambiar key no determina este boolean.

`FAILED persistido` usa `CONDITIONAL` para errores de dominio deterministas solo si ocurren dentro de FASE B conforme al contrato frozen: rollback completo y persistencia de la key `FAILED` mediante FASE C. No afirmar ni crear persistencia `FAILED` fuera de FASE B. `NO` en los conflictos de idempotencia significa que el error no cambia arbitrariamente el estado historico de la key; `IN_PROGRESS` no es `FAILED`. Un fallo tecnico no se convierte automaticamente en `FAILED` de dominio.

| `error.code` | `category` | Retryable | HTTP | FAILED persistido | Autoridad / observacion |
|---|---|---|---:|---|---|
| `SALE_IDEMPOTENCY_KEY_REUSED` | `IDEMPOTENCY` | false | 409 | NO | Transaction §4; Error Model §§9,17; no cambia el estado historico de la key |
| `SALE_IDEMPOTENCY_IN_PROGRESS` | `IDEMPOTENCY` | true | 409 | NO | Transaction §4; Error Model §18; condicion en curso con reintento posterior permitido |
| `USER_INACTIVE` | `AUTHORIZATION` | false | 403 | CONDITIONAL | Error Model §§3,11; Transaction §§3,16; actor conocido inactivo |
| `USER_BRANCH_FORBIDDEN` | `AUTHORIZATION` | false | 403 | CONDITIONAL | Transaction §§3,4; Error Model §§3,11; falta acceso por user_branches |
| `USER_PERMISSION_DENIED` | `AUTHORIZATION` | false | 403 | CONDITIONAL | Transaction §§3,4; Error Model §§3,11; falta SALES_CONFIRM |
| `TERMINAL_INACTIVE` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Error Model §3; estado persistido no operativo |
| `TERMINAL_BRANCH_MISMATCH` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Auth & Context §17; mismatch visible conforme a §4.4 |
| `BRANCH_INACTIVE` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Error Model §3; sucursal existente inactiva |
| `CASH_SESSION_REQUIRED` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §§3,16; Auth & Context §9; falta referencia obligatoria |
| `CASH_SESSION_CLOSED` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,4; Error Model §3; estado persistido CLOSED |
| `CASH_SESSION_TERMINAL_MISMATCH` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Auth & Context §§9,17; mismatch visible conforme a §4.4 |
| `CUSTOMER_INACTIVE` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Error Model §3; cliente existente inactivo |
| `CUSTOMER_BUSINESS_MISMATCH` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Auth & Context §17; mismatch visible conforme a §4.4 |
| `PRICE_LIST_INACTIVE` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Error Model §3; lista existente inactiva |
| `PRICE_LIST_BUSINESS_MISMATCH` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Auth & Context §17; mismatch visible conforme a §4.4 |
| `PRODUCT_INACTIVE` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,4; Error Model §3; producto existente inactivo |
| `PRODUCT_BUSINESS_MISMATCH` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Auth & Context §17; mismatch visible conforme a §4.4 |
| `PRODUCT_UNIT_INVALID` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §§3,16; Error Model §3; semantica publica unica de §4.3 |
| `PRICE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | false | 404 | CONDITIONAL | Transaction §3; Error Model §§3,12; no hay precio activo resoluble para lista/producto/unidad |
| `PRICE_CHANGED` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §§3,4; precio esperado distinto del vigente; no PRECONDITION/fingerprint frozen |
| `INVALID_QUANTITY` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §3; Error Model §3; cantidad semanticamente invalida |
| `INVALID_DISCOUNT` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §§3,4; Error Model §3; descuento fuera de regla |
| `INVALID_TAX_CALCULATION` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §§3,16; invalidacion deterministica de dominio; un fallo tecnico usa INTERNAL_ERROR |
| `INSUFFICIENT_STOCK` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,4,17; saldo autoritativo revalidado dentro de transaccion insuficiente |
| `PAYMENT_METHOD_INACTIVE` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Error Model §3; metodo existente inactivo |
| `PAYMENT_TOTAL_MISMATCH` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §§3,4,16; Error Model §8; pagos no igualan exactamente el total |
| `MIXED_REPLENISHMENT_CHANNELS` | `VALIDATION` | false | 422 | CONDITIONAL | Transaction §§3,16; combinacion de pagos viola el canal unico MVP |
| `DOCUMENT_SEQUENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | false | 404 | CONDITIONAL | Transaction §§11,16; Error Model §12; secuencia requerida no encontrada |
| `DOCUMENT_SEQUENCE_INACTIVE` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§11,16; secuencia existente inactiva |
| `QUOTATION_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | false | 404 | CONDITIONAL | Transaction §16; frontera publica de no-resolucion scoped de §4.2 |
| `QUOTATION_BRANCH_MISMATCH` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; mismatch de cotizacion visible conforme a §4.4 |
| `QUOTATION_EXPIRED` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,4,12; vigencia incompatible; reemision/revalidacion es otro flujo |
| `QUOTATION_ALREADY_CONVERTED` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,4,17; Error Model §3; conversion previa incompatible |
| `QUOTATION_STATUS_INVALID` | `STATE_CONFLICT` | false | 409 | CONDITIONAL | Transaction §§3,16; Error Model §3; estado persistido no convertible |

`SALE_ALREADY_CONFIRMED` esta reservado en el contrato transaccional para otros flujos y **no** se emite al encontrar la venta durante replay/reconciliacion. `SALE_IDEMPOTENCY_FAILED` se menciona como categoria interna; una key `FAILED` reproduce el error original, no un codigo publico generico.

### 12.1 Errores de transporte y tecnicos

Se adoptan explicitamente los cuatro codigos de `public-error-codes-v0.1.md`:

| `error.code` | `category` | `retryable` | HTTP | FAILED persistido | Aplicacion |
|---|---|---|---:|---|---|
| `REQUEST_VALIDATION_FAILED` | `VALIDATION` | false | 422 | NO | Request/header semanticamente invalido sin codigo especifico; incluye Idempotency-Key ausente/invalido |
| `AUTHENTICATION_REQUIRED` | `AUTHENTICATION` | false | 401 | NO | No existe identidad/sesion autenticada utilizable; no sustituye USER_INACTIVE |
| `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | false | 404 | CONDITIONAL | Referencia con formato valido no resoluble dentro del scope visible, segun §4.2 |
| `INTERNAL_ERROR` | `INTERNAL_TECHNICAL` | false | 500 por defecto | NO | Fallo tecnico interno generico si la capa HTTP puede construir una respuesta segura |

`REQUEST_VALIDATION_FAILED` representa validacion generica de la frontera request/header y tiene `FAILED persistido = NO`: no pasa por FASE C ni se persiste como `FAILED`. `REFERENCE_NOT_FOUND` conserva `CONDITIONAL` unicamente cuando la no-resolucion deterministica ocurre dentro de FASE B conforme al contrato frozen; no autoriza persistencia fuera de esa fase. `Idempotency-Key` ausente/invalido siempre se rechaza antes de iniciar la ejecucion del command y sin crear una fila idempotente usando la key invalida.

- No usar `REQUEST_VALIDATION_FAILED` cuando exista un codigo especifico adecuado, como `INVALID_QUANTITY`, `INVALID_DISCOUNT` o `PAYMENT_TOTAL_MISMATCH`.
- `INTERNAL_ERROR` no expone SQL, stack, exception raw, constraints, locks, PK/FK internas, secretos ni infraestructura. No crear codigos separados para deadlock, timeout, SQL error, lock contention o crash especifico, ni persistir automaticamente el fallo como `FAILED` de dominio.
- `INTERNAL_ERROR.retryable=false` no promete que se reconocio una condicion transitoria y no altera la recuperacion idempotente frozen.
- `X-Request-Id` ausente/invalido se sustituye por uno generado y no es error del command.

## 13. Pendientes antes de freeze

Category/HTTP, retryable publico, semantica unica de `PRODUCT_UNIT_INVALID`, codigos genericos, referencias no resolubles y mismatch visibility policy quedan cerrados en este borrador mediante las secciones 4 y 12 y la adopcion del catalogo aditivo compartido.

Permanece pendiente cerrar la politica publica adicional de exposicion/auth en reconciliacion historica, unicamente en los aspectos no cubiertos por la resolucion scoped y donde las autoridades frozen dejan margen. La politica de referencias/mismatches no equivale a cerrar toda esa politica adicional. No puede cambiar el orden frozen, introducir `authorization-before-reconciliation` ni agregar reautorizacion/revalidacion transversal de una key terminal.

El documento sigue `BORRADOR CONTROLADO`; este cierre del error contract no lo declara FROZEN.

Algoritmo criptografico, libreria de canonicalizacion, backend, framework, middleware, auth provider, SQL, logging, deployment y OpenAPI siguen diferidos a implementacion/otros hitos y no son pendientes de freeze de este contrato.

## 14. Fuera de alcance

Este contrato no define backend, lenguaje, framework, ORM, SQL, repositorios, servicios, clases, handlers, implementacion de locks/advisory locks, auth provider, deployment, frontend, aplicacion desktop, API gateway, reverse proxy, OpenAPI ni cambios a db-4/db-5.
