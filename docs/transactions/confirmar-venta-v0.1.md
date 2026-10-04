# CONFIRMAR VENTA v0.1

Fuente funcional: `especificacion_maestra_pos_multisucursal_v0.5.md`.

Referencia física: `docs/database/modelo-fisico-v0.5-db-4.md` y `database/schema-v0.5-db-4.sql` validado en PostgreSQL 17.11.

Este documento diseña conceptualmente la transacción `CONFIRMAR VENTA`. No define framework, API, DTOs, servicios, repositorios ni código.

## Estado del diseño

- Estado: VALIDADO / CONGELADO
- Versión: v0.1
- Compatible con especificación maestra v0.5
- Compatible con modelo físico PostgreSQL v0.5-db-4
- Decisiones pendientes: 0
- Implementación: parcial — FASE A1/A2 implementadas; FASE A3 y posteriores pendientes

Cualquier cambio funcional posterior a este contrato requiere una nueva revisión/versionado del diseño transaccional.

### Aclaración contractual autorizada para FASE A3 — 2026-10-04

Esta revisión documental deliberada congela dentro de v0.1 el advisory-key mapping de `CONFIRM_SALE`, el punto de renovación de la lease de recovery y el formato interno `response_body` v1. Sustituye la indefinición anterior del mapping y la ubicación ambigua de la renovación entre FASE A y FASE B. Estas decisiones se contrastan con el modelo físico vigente db-4 y `docs/api/commands/confirm-sale-v0.1.md`.

No cambia el orden de locks, la precedencia de hash/estado/lease, la reconciliación anterior a autorización, la API pública, los errores, la canonicalización ni el `request_hash`. Se conservan la lease de 30 segundos, la retención de 30 días y db-4 VALIDATED / FROZEN. Este cambio es exclusivamente documental y no implementa FASE A3.

## 1. Objetivo

Confirmar una venta de forma atómica, trazable e idempotente, garantizando que:

- un retry, doble clic o pérdida de respuesta no cree dos ventas;
- terminal, sucursal, usuario, caja, cliente, lista de precios, productos, unidades y pagos sean válidos al momento de confirmar;
- el stock se revalide dentro de la transacción, no con base en una pantalla previa;
- inventario, caja, reposición, folio, cotización e idempotencia queden consistentes;
- ante cualquier fallo antes de `COMMIT`, no quede una venta parcial ni movimientos aislados.

## 2. Entradas conceptuales

La operación requiere, como mínimo:

- `business_id` derivado de la sucursal o contexto autenticado.
- `branch_id` objetivo de la venta.
- `terminal_id` autenticada.
- `user_id` actor.
- `cash_session_id` abierta.
- `client_operation_id` generado por el cliente POS para deduplicación local.
- `idempotency_key` de operación crítica.
- `request_hash` canónico del payload de confirmación.
- `customer_id` opcional.
- `price_list_id` efectiva.
- líneas con `product_id`, `product_unit_id`, cantidad, precio esperado, descuentos e impuestos calculados.
- pagos con `payment_method_id`, importe y referencia opcional.
- `quotation_id` opcional si la venta viene de cotización.

## 3. Validaciones

### Idempotencia

- Validar que `idempotency_key` exista en la solicitud y se registre con `operation_type = 'CONFIRM_SALE'`.
- Validar que `request_hash` corresponda exactamente al payload canónico esperado.
- Validar `client_operation_id` no vacío.
- Si ya existe `sales(branch_id, client_operation_id)`, devolver esa venta existente como defensa secundaria de deduplicación local. `sales` no guarda `request_hash`, por lo que no debe afirmarse que desde esa tabla se detecta payload distinto.

### Terminal, sucursal y usuario

- Validar que `branches.id = branch_id` exista, esté activa y pertenezca al `business_id` esperado.
- Validar que `terminals.id = terminal_id` exista, esté `ACTIVE` y pertenezca a la misma `branch_id`.
- Validar que `users.id = user_id` exista y esté `ACTIVE`.
- Validar que el usuario pueda operar la sucursal mediante `user_branches(user_id, branch_id)`.
- Validar que el usuario posea el permiso funcional `SALES_CONFIRM` mediante roles/permisos.

### Autorización

El permiso definitivo para confirmar venta en el MVP es `SALES_CONFIRM`.

La convención conceptual de permisos es `<MODULE>_<ACTION>`. Ejemplos futuros, no creados en este paso: `SALES_VIEW`, `SALES_DISCOUNT`, `RETURNS_CONFIRM`, `CASH_OPEN`, `CASH_CLOSE`, `INVENTORY_ADJUST`.

La autorización usa el modelo existente: `users`, `user_roles`, `roles`, `role_permissions`, `permissions` y `user_branches`.

Un usuario puede confirmar venta solamente si se cumplen todas estas condiciones:

- `users.status = 'ACTIVE'`;
- existe `user_branches(user_id, branch_id)`;
- posee al menos un `roles.active = TRUE` del `business_id` correspondiente;
- ese rol tiene asociado un permiso con `permissions.code = 'SALES_CONFIRM'`.

La relación conceptual es `user -> user_roles -> roles -> role_permissions -> permissions` y, adicionalmente, `user -> user_branches -> branch`.

No se debe autorizar por nombre de rol, por ejemplo `CASHIER`, `ADMIN` o `MANAGER`. Los roles agrupan permisos; la decisión autoritativa depende de `permissions.code = 'SALES_CONFIRM'`.

No hay bypass especial de administrador. Incluso un rol administrador debe tener `SALES_CONFIRM` asignado mediante `role_permissions` si debe confirmar ventas.

`SALES_CONFIRM` por sí solo no permite operar cualquier sucursal. Se requieren ambas condiciones: permiso funcional y acceso explícito a la sucursal. Si tiene `SALES_CONFIRM` pero no pertenece a la sucursal, devolver `USER_BRANCH_FORBIDDEN`. Si pertenece a la sucursal pero no tiene `SALES_CONFIRM`, devolver `USER_PERMISSION_DENIED`.

Esta validación debe ejecutarse antes de cualquier escritura operativa: reservar folio, insertar `sales`, descontar inventario, registrar pagos, generar `cash_movements` o generar reposición. Puede ocurrir después de resolver idempotencia y adquirir la barrera de `client_operation_id`.

No se insertan registros en `permissions`, no se crean seeds y no se modifica el schema en este diseño. La carga inicial de roles y permisos se definirá en el bootstrap/seed de la aplicación.

### Caja

- Validar que `cash_sessions.id = cash_session_id` exista, esté `OPEN` y pertenezca a la misma `branch_id` y `terminal_id`.
- La FK compuesta de `sales(cash_session_id, branch_id, terminal_id)` protege esta relación, pero debe fallar como error de dominio antes de llegar a error SQL.

### Cliente y lista de precios

- Si hay `customer_id`, validar que el cliente exista, esté activo y pertenezca al mismo `business_id`.
- Validar que `price_list_id` exista, esté activa, pertenezca al mismo `business_id` y use la moneda de la operación.
- Si el cliente define una lista propia, validar que la lista de la venta sea compatible con la política comercial vigente.

### Productos, unidades y precios

- Validar que cada producto exista, esté activo y pertenezca al `business_id`.
- Validar que cada `product_unit_id` exista, esté activo y pertenezca al mismo `product_id`.
- Validar `factor_to_base > 0` y convertir `quantity_base = quantity * factor_to_base`.
- Validar cantidades positivas.
- Validar que exista precio activo en `product_prices(price_list_id, product_id, product_unit_id)`.
- Validar precio no negativo.
- Para venta directa, usar precio activo vigente de `product_prices`. Si el POS envía un precio distinto al vigente, aplicar la política de `PRICE_CHANGED`.
- Para venta desde cotización `ISSUED` y vigente, conservar precio, descuento e impuestos snapshot de la cotización. Un cambio posterior en `product_prices` no provoca `PRICE_CHANGED`.
- Validar descuentos no negativos y que no excedan subtotal de línea ni total de documento.
- Validar impuestos con el `tax_profile` del producto y snapshots calculados.

### Pagos y canal de reposición

- Validar que cada `payment_method_id` exista, esté activo y pertenezca al `business_id`.
- Resolver `replenishment_channel` desde `payment_methods.replenishment_channel`.
- En MVP, impedir mezcla de canales `CASH` y `TRANSFER` dentro de la misma venta.
- Validar que `SUM(sale_payments.amount) = sales.total` con comparación decimal exacta.
- Registrar todos los pagos en `sale_payments`, aunque no todos afecten caja física.

### Política monetaria y redondeo

Para el MVP, la moneda operativa del POS es `MXN`.

Los importes monetarios finales se almacenan como `NUMERIC(18,2)`. Los cálculos monetarios autoritativos no deben usar `FLOAT`, `DOUBLE` ni tipos binarios equivalentes como `number` flotante. El backend futuro deberá usar aritmética decimal exacta y comportarse de forma consistente con PostgreSQL `NUMERIC`.

Se mantienen las precisiones del modelo físico:

- dinero: `NUMERIC(18,2)`;
- cantidades: `NUMERIC(18,4)`;
- costos y factores: `NUMERIC(18,6)`.

Las cantidades y factores pueden tener más precisión que el importe monetario final.

El cálculo conceptual de línea es:

- `importe_bruto_linea = quantity * unit_price`;
- el cálculo intermedio conserva precisión decimal suficiente;
- `subtotal_linea` se redondea a 2 decimales;
- los descuentos monetarios finales de línea se expresan a 2 decimales;
- los impuestos pueden calcularse internamente con mayor precisión cuando sea necesario;
- `tax_total` de línea y `total` de línea terminan almacenados a 2 decimales.

Para importes operativos del POS se usa redondeo decimal estándar: `ROUND(valor, 2)`. No se permite truncamiento silencioso ni tolerancias tipo `ABS(a - b) < 0.01` para decidir igualdad entre montos monetarios almacenados.

El servidor futuro será la autoridad de cálculo. El POS puede enviar montos esperados para UX o validación, pero no es la autoridad definitiva. Los totales del documento se calculan y almacenan a 2 decimales:

- `subtotal = SUM(subtotal de líneas)`;
- `discount_total = SUM(descuentos finales)`;
- `tax_total = SUM(impuestos finales)`;
- `total = resultado monetario final del documento`.

Cada `sale_payments.amount` debe tener como máximo 2 decimales y ser mayor a cero. La regla definitiva para confirmar venta es `SUM(sale_payments.amount) = sales.total` con comparación decimal exacta. Si no coincide, devolver `PAYMENT_TOTAL_MISMATCH`.

Para el MVP no se permite sobrepago, pago incompleto ni generación automática de cambio como parte de una diferencia matemática. Si posteriormente se desea manejar un caso como "recibí $500 y devuelve $73.50", deberá modelarse como monto recibido/cambio en la capa POS correspondiente, sin alterar que `sale_payments.amount` representa exactamente el monto aplicado a la venta.

Las cotizaciones siguen la misma regla monetaria. Cuando una cotización vigente se convierte en venta, se conservan sus snapshots monetarios permitidos y el total final de pagos debe coincidir exactamente con el total de la venta resultante.

Estas reglas definen los totales operativos del POS. La generación CFDI puede requerir precisión fiscal adicional en bases, tasas, impuestos e importes de concepto según reglas SAT vigentes. Los cálculos fiscales detallados se definirán al diseñar el flujo CFDI; no se cambia el modelo fiscal en esta transacción.

### Stock

- Bloquear `inventory_balances` de todos los productos vendidos para la sucursal.
- Revalidar stock dentro de la transacción.
- Rechazar si `quantity_base` solicitada por producto excede `inventory_balances.quantity_base`.
- Impedir stock negativo. La constraint `ck_inventory_balances_quantity_non_negative` queda como última defensa, no como validación principal.

### Cotización opcional

- Si existe `quotation_id`, bloquear la cotización.
- Validar misma `branch_id`.
- Validar estado apropiado para conversión, típicamente `ISSUED`.
- Validar que `converted_sale_id IS NULL` y que no esté `CONVERTED`, `EXPIRED` ni `CANCELLED`.
- Validar vigencia con `valid_until`.
- Si la cotización está vencida, no recalcularla silenciosamente; devolver `QUOTATION_EXPIRED`. La reemisión o revalidación será otro flujo.
- Conservar precio, descuento e impuestos snapshot de la cotización emitida y vigente.
- Revalidar producto/unidad válidos, stock, cliente, pagos, canal, permisos, estado y vigencia; una cotización no reserva inventario ni caja.

## 4. Estrategia de idempotencia

Se usan dos defensas complementarias:

- `idempotency_keys` + `request_hash` es la defensa primaria de idempotencia y validación del payload.
- `sales.client_operation_id` con `UNIQUE(branch_id, client_operation_id)` es una defensa secundaria de identidad y deduplicación de una venta lógica dentro de una sucursal.
- Un transaction advisory lock determinista por `(branch_id, client_operation_id)` cierra la ventana en la que todavía no existe fila en `sales`.

`idempotency_key` y `client_operation_id` no tienen exactamente la misma responsabilidad. La primera protege reintentos de una solicitud concreta y detecta payload distinto mediante `request_hash`. La segunda identifica la venta lógica creada por el POS.

### Contrato de `client_operation_id`

`client_operation_id` identifica una única venta lógica dentro de una sucursal. Debe ser:

- generado por el POS;
- opaco para el servidor;
- estable durante los retries de esa misma venta lógica;
- distinto para una nueva venta lógica.

La base protege `UNIQUE(branch_id, client_operation_id)`, por lo que un `client_operation_id` confirmado queda consumido permanentemente para esa sucursal.

Si una operación falla antes de crear una venta, por ejemplo por `PRICE_CHANGED`, `INSUFFICIENT_STOCK`, `PAYMENT_TOTAL_MISMATCH` u otro error de dominio, y el usuario corrige la misma venta lógica, puede mantenerse el mismo `client_operation_id`. En ese caso debe utilizarse una nueva `idempotency_key` y un nuevo `request_hash` correspondiente al payload corregido. Esto es válido porque todavía no existe una fila `sales` asociada a ese `client_operation_id`.

Una vez que existe `sales(branch_id, client_operation_id)`, ese `client_operation_id` no puede crear otra venta. Si llega otro request con el mismo `branch_id` y `client_operation_id`, aunque use otra `idempotency_key`, no se crea otra venta. La venta existente es la autoridad para esa identidad lógica.

Para el MVP no se persistirá un fingerprint adicional en `sales`. No se agregan `request_fingerprint`, `payload_hash` ni `confirmation_hash`. Si alguien reutiliza accidentalmente un `client_operation_id` confirmado con otra `idempotency_key` y otro payload, el servidor no intentará inferir una segunda venta: la identidad existente prevalece y se devuelve la venta ya confirmada. Reutilizar un `client_operation_id` confirmado para una nueva venta es un error del cliente POS; la defensa segura del servidor consiste en no crear jamás una segunda venta. Un fingerprint futuro sería una posible mejora fuera del MVP, no una decisión pendiente de `CONFIRMAR VENTA v0.1`.

La regla de `idempotency_key` no cambia: si se reutiliza la misma `idempotency_key` con `request_hash` diferente, se devuelve `SALE_IDEMPOTENCY_KEY_REUSED`.

Regla conceptual para la futura aplicación POS:

- Venta A usa `client_operation_id = X`.
- Retries de Venta A mantienen `X`.
- Venta A falla antes de confirmar y el cajero corrige el carrito: puede mantener `X`, pero usa nueva `idempotency_key` y nuevo `request_hash`.
- Venta A confirma: `X` queda consumido.
- Venta B debe generar `client_operation_id = Y`.
- Nunca reutilizar `X` para Venta B.

### FASE A - Reserva idempotente

La reserva idempotente ocurre antes del `BEGIN` de la transacción operativa principal. Es una transacción corta dedicada exclusivamente a `idempotency_keys`.

Flujo conceptual:

### BEGIN

1. Buscar o adquirir la clave única `(business_id, operation_type='CONFIRM_SALE', idempotency_key)`.
2. Si no existe, crearla con `status='IN_PROGRESS'`, `request_hash` canónico, `locked_until = now() + 30 segundos` y `expires_at = NULL`.
3. Si existe, resolver su estado según las reglas `IN_PROGRESS`, `COMPLETED` o `FAILED` descritas abajo.

### COMMIT

Esta fase no contiene escrituras de negocio: no crea `sales`, `sale_items`, inventario, pagos, caja, folios, reposición, cotización ni `audit_log`.

La constraint única `(business_id, operation_type, idempotency_key)` sigue siendo la última defensa. La implementación futura puede usar semántica equivalente a `INSERT ... ON CONFLICT` seguida de lectura y bloqueo de la fila, sin definir SQL definitivo en este documento.

Separar esta reserva evita que un `ROLLBACK` de venta elimine también la fila `IN_PROGRESS`. Así `locked_until` funciona como lease persistente para detectar y recuperar procesos interrumpidos.

FASE A (A1) reserva una key nueva, pero no renueva la lease de una fila existente. Con mismo hash y lease vencida solamente devuelve `RecoveryCheckRequired`: es un candidato a recovery, no permiso para reejecutar. FASE A hace `COMMIT` antes de iniciar la transacción operacional y la renovación elegible ocurre después de A2, según la regla de abajo.

### Barrera por `client_operation_id`

La función frozen para `CONFIRM_SALE` es `pg_advisory_xact_lock(bigint)`. La barrera conceptual sigue siendo `(branch_id, client_operation_id)` y utiliza una sola clave PostgreSQL de 64 bits, derivada exactamente como se define a continuación.

#### Advisory-key mapping v1

Concatenar, en este orden y sin bytes adicionales:

| Componente | Representación exacta |
| --- | --- |
| Domain separator | Bytes ASCII exactos de `POS:CONFIRM_SALE:CLIENT_OPERATION:v1`, sin BOM. |
| Terminador del separator | Un único byte NUL `0x00`. |
| `branch_id` | Signed Int64, 8 bytes, big-endian, two's-complement. Debe ser positivo; es el ID interno de la branch resuelta dentro del business confiable. |
| Longitud de `client_operation_id` | UInt32, 4 bytes, big-endian, con la longitud **en bytes UTF-8** del valor. No contar chars ni runes. |
| `client_operation_id` | Bytes UTF-8 exactos del valor recibido, sin BOM, trim, uppercase/lowercase, Unicode normalization ni otra transformación. |

Preimagen completa:

```text
ASCII("POS:CONFIRM_SALE:CLIENT_OPERATION:v1")
0x00
Int64BE(branch_id)
UInt32BE(utf8_byte_length(client_operation_id))
UTF8(client_operation_id)
```

Las líneas representan concatenación binaria, no separadores de texto ni saltos de línea. Calcular `SHA-256(preimage)` y tomar `digest[0..8]`: los primeros 8 bytes, índices 0 a 7; el extremo 8 es exclusivo. Interpretar esos bytes como signed Int64 big-endian, two's-complement, y pasar el valor como parámetro `@lock_key BIGINT`:

```sql
SELECT pg_advisory_xact_lock(@lock_key);
```

No convertir la clave a decimal string ni aplicar `Math.Abs`. Los valores negativos, cero y `long.MinValue` son advisory lock keys válidas. Esta derivación es independiente del `request_hash` y no modifica su preimagen, canonicalización ni representación almacenada.

La convención es estable, portable, reproducible e independiente del runtime y de una versión específica del hash interno de PostgreSQL. No se utilizan `GetHashCode()`, hashes process-local, `hashtext()` ni `hashtextextended()`, porque no definen esa convención binaria portable. Tampoco se utiliza MD5: la decisión frozen es SHA-256 con esta preimagen y conversión exactas.

El mapping reduce un digest de 256 bits a 64 bits; una colisión teórica es posible. Una colisión puede serializar innecesariamente operaciones independientes, pero no autoriza duplicados, no sustituye idempotencia ni `UNIQUE(branch_id, client_operation_id)`, y no produce identidad compartida en `sales`. La constraint física `uq_sales_client_operation` de db-4 sigue siendo la defensa final.

#### Semántica y secuencia de la barrera

La barrera es transaction-scoped y bloqueante; la espera es intencional. No usar `pg_advisory_lock` session-level ni `pg_try_advisory_xact_lock`. El `NOWAIT` de A2 corresponde al row lock de idempotencia, no a esta barrera.

La transacción operacional ya debe mantener el row lock de `idempotency_keys`. A3 conserva ese lock mientras resuelve identidad de branch, deriva la clave y adquiere la barrera; no lo libera entre pasos. Ambos locks pertenecen a la misma transacción. El advisory lock se libera automáticamente por `COMMIT`, `ROLLBACK` o terminación de la conexión/transacción; no hay commit interno ni una transacción separada para la barrera.

El flujo es:

1. reservar/resolver `idempotency_key` en FASE A corta y hacer `COMMIT`;
2. iniciar FASE B `READ COMMITTED`, adquirir/releer la fila y clasificar A2;
3. renovar la lease solamente para recovery elegible, inmediatamente después de A2;
4. resolver identidad de branch scoped al business confiable, sin adelantar validación ACTIVE ni autorización;
5. derivar la clave según advisory-key mapping v1 y adquirir `pg_advisory_xact_lock(bigint)`;
6. consultar la venta histórica exclusivamente por `sales(branch_id, client_operation_id)` con el valor exacto de `client_operation_id`;
7. si existe, reconciliar la key actual y devolver el resultado histórico sin repetir efectos;
8. solamente si no existe y no hay una asociación inconsistente en el camino `ReconciliationRequired`, continuar validaciones actuales y efectos.

La venta histórica prevalece incluso si fue creada con otra `Idempotency-Key`. `sales` no almacena `request_hash`: no inferir igualdad ni diferencia de payload desde esa venta. La consulta histórica y la reconciliación permanecen antes de autorización.

### Si la clave está `IN_PROGRESS`

- Para `CONFIRM_SALE` en MVP, `locked_until = now() + 30 segundos`.
- Si `locked_until > now()`, devolver `SALE_IDEMPOTENCY_IN_PROGRESS`.
- No mantener una petición HTTP esperando 30 segundos por la lease; el POS podrá reintentar posteriormente. Esto no cambia la espera intencional de la barrera advisory entre keys distintas.
- Si otra transacción todavía bloquea la fila, considerarla todavía `IN_PROGRESS`.
- Si `locked_until <= now()`, puede recuperarse únicamente si `request_hash` coincide, `result_entity_id IS NULL`, no existe ya una venta correspondiente a la operación y la fila puede adquirirse sin competir con una transacción activa.
- La renovación de recovery ocurre únicamente en el punto y bajo las condiciones definidos a continuación; no se realiza al clasificar el candidato en FASE A ni dentro de A2.
- Si el `request_hash` no coincide, devolver `SALE_IDEMPOTENCY_KEY_REUSED`.

La expiración de `locked_until` no significa que deba ejecutarse otra venta mientras una transacción original continúa activa. La implementación futura deberá usar locking de PostgreSQL para que solamente una ejecución posea efectivamente la fila.

#### Punto exacto de renovación de recovery lease

Renovar solamente cuando se cumplen simultáneamente:

```text
Phase A decision = RecoveryCheckRequired
AND Phase A2 result = ContinuationOwned
AND RowLockHeld = true
```

A2 relee bajo lock el mismo hash, `status='IN_PROGRESS'`, lease vencida y `result_entity_id IS NULL` antes de producir ese resultado para un candidato a recovery. `ContinuationOwned` permite continuar exclusivamente hacia branch/barrera/historia; la renovación no autoriza crear una venta ni omitir la verificación histórica.

El UPDATE ocurre **inmediatamente después de A2 y antes de branch resolution/advisory barrier**, dentro de la misma transacción operacional que mantiene `idempotency_keys FOR UPDATE`. SQL conceptual parametrizado:

```sql
UPDATE idempotency_keys
SET locked_until = transaction_timestamp() + interval '30 seconds'
WHERE id = @id
  AND business_id = @business_id
  AND operation_type = 'CONFIRM_SALE';
```

La implementación futura debe verificar exactamente una fila afectada. No hace commit interno, no abre una transacción separada y no libera el row lock.

No renovar para `NewKey`, `KeyReused`, `InProgress`, `CompletedReplay`, `FailedReplay` ni `ReconciliationRequired`. La key nueva conserva su lease de reserva, incluso si venció antes de que su caller original adquiriera A2; esa condición no cambia las reglas de ownership de A2.

La lease no representa ownership. Mientras la transacción está activa, el ownership real sigue siendo el PostgreSQL row lock. La actualización de `locked_until` puede no ser visible externamente hasta `COMMIT`: otro recovery puede observar la lease anterior vencida en FASE A, intentar A2 mediante `FOR UPDATE NOWAIT`, encontrar la ejecución activa y recibir `InProgress` sin adquirir ownership.

Si la transacción de recovery hace `ROLLBACK`, la renovación también hace rollback y continúa aplicando la lease anterior. No agregar heartbeat, background renewal ni ownership token.

#### Camino `ReconciliationRequired`

`ReconciliationRequired` significa que A2 encontró `IN_PROGRESS` con `result_entity_id IS NOT NULL` bajo row lock. No autoriza una nueva venta ni la renovación de lease; conserva las reglas de clasificación de A2 y no es sinónimo de recovery libre.

La fase posterior debe verificar `result_entity_type`, `result_entity_id`, la identidad de la venta histórica y su correspondencia con la historia `(branch_id, client_operation_id)` después de la barrera. Para reconciliar, la asociación debe ser coherente con `result_entity_type='sales'` y el ID de esa venta histórica. Una asociación inconsistente o sin venta histórica correspondiente es una invariante interna rota, no un flujo normal de dominio ni permiso para crear una venta. No inventar reparación automática.

### Si la clave está `COMPLETED`

- Validar que el `request_hash` coincida.
- No crear una nueva venta.
- Devolver la respuesta persistida en `response_body` o reconstruir respuesta desde `result_entity_type='sales'` y `result_entity_id`.

### Si la clave está `FAILED`

- Si el `request_hash` no coincide, devolver `SALE_IDEMPOTENCY_KEY_REUSED`.
- Si el `request_hash` coincide, devolver el mismo error de dominio almacenado.
- No reintentar automáticamente la operación.
- Si el usuario corrige algo y desea intentar otra vez, el POS debe generar una nueva `idempotency_key`.

### FASE B - CONFIRMAR VENTA transaccional

Después de reservar correctamente la clave, inicia la transacción operativa.

Al inicio de FASE B:

1. Bloquear/releer `idempotency_keys` y clasificar A2 con precedencia hash, estado y lease/propiedad.
2. Si es recovery elegible, renovar la lease inmediatamente después de A2 según la regla anterior.
3. Resolver identidad de branch scoped al business confiable, derivar advisory key y adquirir la barrera transaccional.
4. Buscar `sales(branch_id, client_operation_id)` y reconciliar si existe; solamente si no existe y no hay una asociación inconsistente `ReconciliationRequired`, continuar autorización, cotización, caja, catálogos, inventario, reposición, folio, venta, pagos, caja, auditoría y efectos operativos.

Al final de la misma transacción operativa, actualizar `idempotency_keys`:

- `status = 'COMPLETED'`;
- `result_entity_type = 'sales'`;
- `result_entity_id = sales.id`;
- `response_body = respuesta interna v1` definida abajo;
- `error_code = NULL`;
- `error_message = NULL`;
- `locked_until = NULL`;
- `expires_at = now() + 30 días`.

La venta confirmada y `idempotency_keys.status='COMPLETED'` son atómicos. Nunca debe existir una venta `COMMITTED` cuyo cambio a `COMPLETED` haya quedado fuera de esa misma transacción operativa.

### FASE C - Persistencia de FAILED

Persistir `FAILED` solamente para errores de dominio determinísticos, por ejemplo: `USER_BRANCH_FORBIDDEN`, `USER_PERMISSION_DENIED`, `CASH_SESSION_CLOSED`, `PRODUCT_INACTIVE`, `PRICE_CHANGED`, `INVALID_DISCOUNT`, `INSUFFICIENT_STOCK`, `PAYMENT_TOTAL_MISMATCH`, `QUOTATION_EXPIRED` o `QUOTATION_ALREADY_CONVERTED`.

Si la FASE B ya inició y ocurre un error de dominio determinístico:

1. Hacer `ROLLBACK` completo de la transacción operativa.
2. Abrir una transacción corta separada.
3. Bloquear `idempotency_keys`.
4. Verificar que `request_hash` siga correspondiendo.
5. Actualizar `status='FAILED'`, `error_code`, `error_message` seguro y breve, `locked_until=NULL`, `expires_at=now() + 30 días`.
6. Hacer `COMMIT`.

FASE C no contiene escrituras de negocio. No debe persistir stack traces, SQL interno, credenciales, tokens, datos sensibles ni detalles técnicos internos.

### Fallos técnicos

Para fallos técnicos no determinísticos, como pérdida de conexión, caída del proceso, timeout interno o error inesperado antes de conocer el resultado, no marcar automáticamente `FAILED`.

La clave puede permanecer `IN_PROGRESS` hasta que expire `locked_until`. Después se aplica la recuperación segura descrita para `IN_PROGRESS` expirado. Antes de reejecutar, debe comprobarse si `sales(branch_id, client_operation_id)` ya existe. Si existe, la operación no debe repetirse; debe reconciliarse la `idempotency_key` con la venta existente actualizando la clave hacia `COMPLETED` con `result_entity_type='sales'`, `result_entity_id` y una `response_body` interna v1, dentro de una transacción controlada.

### Retención y `response_body`

Para MVP:

- `COMPLETED`: retener 30 días después de completar.
- `FAILED`: retener 30 días después de fallar.
- `IN_PROGRESS`: `locked_until` controla la lease; el row lock mantiene ownership transaccional y `expires_at` no debe usarse como mecanismo de locking.

Una tarea de limpieza futura podrá eliminar registros con `expires_at < now()`, siempre que no estén asociados a una operación todavía activa. No se crea esa tarea en este diseño.

Mientras una `idempotency_key` está `IN_PROGRESS`, `expires_at` puede permanecer `NULL`; `locked_until` es el mecanismo de lease. Al pasar a `COMPLETED` o `FAILED`, establecer `expires_at = now() + 30 días`. Esto no requiere cambios de schema porque `expires_at` ya admite `NULL`.

#### Internal `response_body` v1 de `CONFIRM_SALE`

El formato interno mínimo persistido en `idempotency_keys.response_body` queda frozen para confirmación y reconciliación histórica. Es un objeto JSON con exactamente estas seis propiedades:

```json
{
  "sale_public_id": "<uuid>",
  "folio": "VEN-000001",
  "status": "CONFIRMED",
  "total": "11.60",
  "currency": "MXN",
  "confirmed_at": "2026-09-29T00:00:00Z"
}
```

| Propiedad | Formato y autoridad |
| --- | --- |
| `sale_public_id` | UUID público persistido en `sales.public_id`, representado como JSON string. |
| `folio` | JSON string con el folio persistido de la venta. |
| `status` | JSON string literal `CONFIRMED`: resultado histórico del command, aunque el estado vivo posterior sea `PARTIALLY_RETURNED`, `RETURNED` o `CANCELLED`. |
| `total` | JSON string decimal canónico scale 2 desde `sales.total`, con punto decimal y dos dígitos fraccionarios: `"0.00"`, `"11.60"`, `"1000.00"`. Sin JSON number, notación científica, formato localizado ni redondeo silencioso. |
| `currency` | JSON string desde el valor persistido `sales.currency`; `MXN` en MVP. |
| `confirmed_at` | JSON string desde `sales.confirmed_at`, RFC 3339 UTC con sufijo `Z`, sin offset local; preservar la precisión temporal autoritativa conforme a Transport v0.1. |

`response_body` es almacenamiento idempotente interno, no por definición el envelope HTTP. El endpoint futuro construirá `{ "data": { ... } }` desde esta información/recurso persistido conforme al Command API, sin cambiar su contrato público. `v1` identifica esta convención documental y no agrega una propiedad de versión al objeto.

Se conserva el máximo lógico de aplicación de **16 KiB**. No incluir `data`, `replayed`, `reconciled`, `idempotency_key`, `request_hash`, ID interno de sale, `business_id`, `branch_id`, `terminal_id`, `user_id`, customer, lines, payments, ticket, XML, PDF, imágenes, secretos ni propiedades adicionales. Una respuesta pública más amplia no debe ampliar este objeto interno; se reconstruye desde el recurso persistido cuando el contrato lo permita.

## 5. Orden definitivo de locks

El orden debe ser determinista para reducir deadlocks:

1. `idempotency_keys` por `(business_id, operation_type='CONFIRM_SALE', idempotency_key)`.
2. Transaction advisory lock por `(branch_id, client_operation_id)`.
3. `sales` por `(branch_id, client_operation_id)` si existe una venta previa para la misma operación local.
4. `quotations` por `quotation_id` si aplica.
5. `cash_sessions` por `cash_session_id`.
6. Catálogos validados por lectura consistente: `branches`, `terminals`, `users`, `user_branches`, `customers`, `price_lists`, `products`, `product_units`, `product_prices`, `payment_methods`.
7. `inventory_balances` de la sucursal en orden estable `ORDER BY product_id`.
8. `replenishment_positions` existentes en orden estable `ORDER BY product_id, channel`; si no existen, se crean con UPSERT determinista por producto/canal.
9. `document_sequences` para `business_id`, `branch_id`, `document_type = 'VEN'`.

Nota crítica: el folio se bloquea después de revalidar inventario para no mantener la secuencia bloqueada mientras se resuelven productos. Aun así, el folio se reserva antes de insertar `sales` y dentro de la misma transacción.

## 6. Orden transaccional

### FASE A - Reserva idempotente

### BEGIN

1. Resolver o reservar `idempotency_key`; comparar `request_hash` antes de interpretar estado o lease.
2. Si está `COMPLETED`, devolver la venta existente sin entrar a FASE B.
3. Si está `FAILED` con mismo `request_hash`, devolver el error de dominio almacenado sin entrar a FASE B.
4. Si está `IN_PROGRESS` vigente, devolver `SALE_IDEMPOTENCY_IN_PROGRESS`.
5. Si no existe, reservar `IN_PROGRESS` con lease de 30 segundos y resultado `NewKey`. Si la fila existente tiene mismo hash y lease vencida, devolver `RecoveryCheckRequired` sin renovar ni asumir recovery autorizado.

### COMMIT

### FASE B - CONFIRMAR VENTA transaccional

### BEGIN READ COMMITTED

1. Bloquear/releer la fila `idempotency_keys` reservada como primer lock persistente.
2. Clasificar A2 comprobando nuevamente hash, estado y lease/propiedad en ese orden. Solo `ContinuationOwned` o `ReconciliationRequired` con `RowLockHeld=true` pueden continuar hacia la barrera/historia; no autorizan por sí mismos una venta nueva.
3. Solamente si FASE A devolvió `RecoveryCheckRequired` y A2 devolvió `ContinuationOwned` con row lock retenido, renovar recovery lease inmediatamente, según §4.
4. Resolver identidad de la branch objetivo dentro del business confiable, sin filtrar por ACTIVE ni adelantar autorización u otras validaciones actuales.
5. Derivar la advisory key exacta de `(branch_id, client_operation_id)` conforme a §4.
6. Adquirir `pg_advisory_xact_lock(bigint)` bloqueante en esta misma transacción.
7. Buscar venta histórica por `sales(branch_id, client_operation_id)` después de la barrera.
8. Si existe, reconciliar idempotencia hacia `COMPLETED` y devolver éxito histórico según la regla de abajo, sin ejecutar nuevamente efectos de negocio.
9. Solamente si no existe venta histórica y no hay una asociación inconsistente `ReconciliationRequired`, continuar validando `branch`, `terminal`, `user`, permisos y pertenencia a sucursal. No aplicar authorization-before-reconciliation.
10. Bloquear y validar cotización si aplica.
11. Bloquear y validar `cash_session` abierta de la misma sucursal y terminal.
12. Resolver y validar cliente, lista de precios, productos, unidades, precios, descuentos e impuestos.
13. Resolver métodos de pago y `replenishment_channel`.
14. Rechazar mezcla de canales `CASH` / `TRANSFER` para MVP.
15. Validar `SUM(sale_payments.amount) = sales.total` con comparación decimal exacta.
16. Agregar cantidades por `product_id` para evitar doble descuento si el producto aparece en varias líneas.
17. Bloquear `inventory_balances` por `branch_id` y productos agregados, en orden `product_id`.
18. Revalidar stock suficiente y capturar `average_cost_base` como costo de salida.
19. Bloquear o preparar `replenishment_positions` por `branch_id`, `product_id`, `channel` en orden estable.
20. Bloquear `document_sequences` de `VEN` para la sucursal.
21. Reservar folio incrementando `next_number`.
22. Insertar `sales` con `status='CONFIRMED'`, folio, canal, snapshots y totales.
23. Insertar `sale_items` con snapshots comerciales, `quantity_base` y `unit_cost_snapshot` desde `inventory_balances.average_cost_base`.
24. Actualizar `inventory_balances.quantity_base = quantity_base - vendido_base`, conservar `average_cost_base` e incrementar `version`.
25. Insertar `inventory_movements` tipo `SALE` con `quantity_delta_base` negativo, costo snapshot y `balance_after_base`.
26. Insertar `sale_payments` con snapshot de método y canal.
27. Insertar `cash_movements` solo para pagos cuyo `payment_methods.affects_cash = TRUE`; para efectivo usar `movement_type='SALE_CASH'` y `amount_delta > 0`.
28. Actualizar o crear `replenishment_positions` incrementando `demand_qty_base`; no modificar `committed_qty_base`.
29. Insertar un `replenishment_movements` por cada `sale_item`, con `reference_entity_type='sale_items'` y `reference_entity_id=sale_items.id`.
30. Convertir cotización si aplica: actualizar `quotations.status='CONVERTED'` y `converted_sale_id=sale.id`.
31. Insertar `audit_log` de venta confirmada.
32. Marcar `idempotency_keys.status='COMPLETED'`, `result_entity_type='sales'`, `result_entity_id=sale.id`, `response_body` interna v1, `error_code=NULL`, `error_message=NULL`, `locked_until=NULL` y `expires_at=now()+30 días`.

### COMMIT

### FASE C - Fallo de dominio determinístico

Solo si FASE B falla por error de dominio determinístico:

1. `ROLLBACK` completo de FASE B.
2. `BEGIN` corto sin escrituras de negocio.
3. Bloquear `idempotency_keys`.
4. Verificar `request_hash`.
5. Actualizar `status='FAILED'`, `error_code`, `error_message`, `locked_until=NULL`, `expires_at=now()+30 días`.
6. `COMMIT`.

### Reconciliación por venta existente

Si FASE B encuentra una venta existente para `(branch_id, client_operation_id)`:

La reconciliación actualiza atómicamente la misma fila de idempotencia bloqueada con:

```text
status = COMPLETED
result_entity_type = 'sales'
result_entity_id = historical sale internal id
response_body = internal response_body v1
error_code = NULL
error_message = NULL
locked_until = NULL
expires_at = transaction_timestamp() + interval '30 days'
```

Todo ocurre dentro de la misma transacción operacional que mantiene el idempotency row lock y el client-operation advisory lock. El caller es dueño de esa transacción: la fase de reconciliación no hace commit interno ni abre una transacción separada; el caller completa FASE B antes de emitir el éxito histórico.

1. No ejecutar nuevamente efectos de negocio.
2. No descontar inventario.
3. No reservar otro folio.
4. No generar nuevos pagos.
5. No generar nueva caja.
6. No generar nueva reposición.
7. Reconciliar la `idempotency_key` actual hacia `COMPLETED`.
8. Asociarla con `result_entity_type='sales'` y `result_entity_id` de la venta existente.
9. Guardar `response_body` interna v1 y limpiar `error_code`, `error_message` y `locked_until`.
10. Establecer `expires_at = now() + 30 días`.
11. El caller hace `COMMIT` de la misma FASE B, sin liberar los locks antes de completar la reconciliación.
12. Devolver la venta existente.

Puede existir más de una `idempotency_key` `COMPLETED` apuntando a la misma venta como resultado de reconciliación. Eso no significa que existan ventas duplicadas.

## 7. Escrituras definitivas

La transacción crea o actualiza:

- `idempotency_keys`: `IN_PROGRESS` en FASE A; `COMPLETED` al final de FASE B si confirma; `FAILED` en FASE C solo para errores de dominio determinísticos.
- `document_sequences`: incremento de `next_number` para `VEN`.
- `sales`: cabecera confirmada.
- `sale_items`: líneas con snapshots y costo unitario snapshot.
- `sale_payments`: todos los pagos recibidos.
- `inventory_balances`: reducción de existencias por producto y sucursal; `average_cost_base` no se recalcula.
- `inventory_movements`: movimientos `SALE` negativos append-only.
- `cash_movements`: solo pagos que afectan caja física.
- `replenishment_positions`: incremento de demanda por `branch_id`, `product_id`, `channel`; `available_to_order_base` lo calcula PostgreSQL.
- `replenishment_movements`: movimientos append-only de demanda, uno por línea de venta, referenciando `sale_items`.
- `quotations`: solo si aplica conversión, queda `CONVERTED` con `converted_sale_id`.
- `audit_log`: evento mínimo de venta confirmada.

## 8. Inventario

Para cada línea:

- convertir cantidad de presentación a unidad base con `product_units.factor_to_base`;
- agrupar por producto para comparar contra saldo real;
- bloquear `inventory_balances(branch_id, product_id)`;
- validar stock suficiente dentro de la transacción;
- descontar `quantity_base`;
- conservar `average_cost_base`;
- guardar `sale_items.unit_cost_snapshot = inventory_balances.average_cost_base`;
- crear `inventory_movements.movement_type='SALE'` con delta negativo.

Las ventas no recalculan costo promedio. El costo promedio cambia en entradas o ajustes que correspondan, no en una salida normal.

## 9. Caja

`sale_payments` registra todos los pagos, incluso tarjeta o transferencia.

`cash_movements` se crea solamente cuando `payment_methods.affects_cash = TRUE`.

Para efectivo:

- `movement_type = 'SALE_CASH'`;
- `amount_delta` positivo;
- `cash_session_id` de la sesión bloqueada;
- referencia a la venta;
- `actor_user_id = user_id`.

No se debe asumir que toda venta genera caja física.

## 10. Reposición

La venta incrementa demanda de reposición por producto y canal.

Para cada producto vendido:

- actualizar `replenishment_positions(branch_id, product_id, channel)`;
- incrementar `demand_qty_base`;
- no modificar `committed_qty_base`;
- dejar que `available_to_order_base` se calcule como `GREATEST(demand_qty_base - committed_qty_base, 0)`;
- insertar `replenishment_movements` con `demand_delta_base > 0`, `committed_delta_base = 0`.

Para demanda originada por venta, la referencia estándar es:

- `reference_entity_type = 'sale_items'`;
- `reference_entity_id = sale_items.id`;
- un `replenishment_movement` por cada línea de venta.

Si el mismo producto aparece en varias líneas, `inventory_balances` y `replenishment_positions` pueden actualizarse con cantidad agregada por `product_id`, pero el ledger `replenishment_movements` conserva granularidad por `sale_item`. Esto facilita devoluciones, `replenishment_allocations`, trazabilidad FIFO y auditoría de qué venta/línea originó la demanda.

Si ya existe `committed_qty_base > demand_qty_base`, la venta puede aumentar demanda y `available_to_order_base` seguirá siendo `0` hasta que la demanda supere lo comprometido.

## 11. Folio

La generación usa `document_sequences`; nunca `MAX(folio) + 1`.

La transacción debe:

- localizar la secuencia activa por `business_id`, `branch_id`, `document_type='VEN'`;
- bloquearla con `SELECT ... FOR UPDATE`;
- formar el folio con `prefix`, `next_number` y `padding`;
- incrementar `next_number`;
- usar el folio reservado en `sales.folio` dentro del mismo `COMMIT`.

Si la transacción hace `ROLLBACK` después de incrementar `next_number`, el incremento se revierte porque vive en la misma transacción. Por tanto no queda folio consumido sin venta. Si en el futuro se decide permitir huecos de folio por auditoría, debe documentarse como cambio explícito.

## 12. Cotización

Cuando la venta proviene de cotización:

- bloquear `quotations` al inicio del flujo, antes de inventario y folio;
- validar `branch_id` igual al de la venta;
- validar estado convertible;
- validar `converted_sale_id IS NULL`;
- validar vigencia;
- conservar precio, descuento e impuestos snapshot si la cotización está `ISSUED` y dentro de `valid_until`;
- devolver `QUOTATION_EXPIRED` si está vencida, sin recalcular silenciosamente;
- revalidar stock;
- crear la venta;
- actualizar `quotations.converted_sale_id = sales.id`;
- actualizar `quotations.status = 'CONVERTED'`.

Dos conversiones simultáneas de la misma cotización compiten por el lock de la fila; solo una puede observarla convertible.

## 13. Auditoría

Evento mínimo en `audit_log`:

- `actor_user_id = user_id`;
- `branch_id`;
- `terminal_id`;
- `action = 'SALE_CONFIRMED'`;
- `entity_type = 'sales'`;
- `entity_id = sales.id`;
- `entity_public_id = sales.public_id`;
- `after_data` con folio, total, moneda, canal, `cash_session_id`, conteo de líneas y conteo de pagos;
- `context` con `client_operation_id`, `idempotency_key` truncada o hasheada, `quotation_id` si aplica, origen POS y versión de flujo;
- `occurred_at` por defecto.

No guardar contraseñas, tokens, secretos, CSD, claves PAC ni datos sensibles innecesarios.

## 14. Nivel de aislamiento

Nivel de aislamiento propuesto para `CONFIRMAR VENTA`: `READ COMMITTED` + locks explícitos.

Justificación:

- `inventory_balances` se bloquea explícitamente;
- `cash_sessions` se bloquea;
- `quotations` se bloquea si aplica;
- `replenishment_positions` se serializa explícitamente;
- `document_sequences` se bloquea;
- idempotencia y `client_operation_id` tienen barreras explícitas mediante `idempotency_keys` y advisory lock transaccional.

No usar `SERIALIZABLE` por defecto en el MVP. Esta decisión podrá revisarse si las pruebas concurrentes encuentran una anomalía real.

## 15. Rollback y fallos

Si falla cualquier paso antes de `COMMIT`, PostgreSQL revierte:

- `sales`;
- `sale_items`;
- `sale_payments`;
- descuentos de `inventory_balances`;
- `inventory_movements`;
- `cash_movements`;
- `replenishment_positions` y `replenishment_movements`;
- conversión de cotización;
- incremento de `document_sequences.next_number`;
- evento de `audit_log`;
- cambio final de `idempotency_keys` a `COMPLETED`.

No queda venta parcial, inventario descontado, movimiento de caja aislado, demanda de reposición aislada, cotización convertida sin venta ni folio confirmado de forma inconsistente.

Consideración de idempotencia: FASE A persiste `IN_PROGRESS` antes de FASE B, por lo que un `ROLLBACK` operativo no elimina el lease. Si FASE B falla por dominio determinístico, FASE C registra `FAILED` en una transacción corta sin escrituras de negocio. Si FASE B falla técnicamente sin resultado conocido, no se marca automáticamente `FAILED`; la recuperación dependerá de `locked_until` y de reconciliar contra `sales(branch_id, client_operation_id)`.

## 16. Errores de dominio

Códigos propuestos:

- `SALE_IDEMPOTENCY_IN_PROGRESS`: existe una ejecución válida todavía activa.
- `SALE_IDEMPOTENCY_KEY_REUSED`: la misma key fue usada con `request_hash` diferente.
- `SALE_IDEMPOTENCY_FAILED`: categoría interna si es necesaria; para un `FAILED` determinístico se debe preferir devolver el error de dominio original almacenado.
- `SALE_ALREADY_CONFIRMED`: reservado para otros flujos futuros que intenten confirmar explícitamente una venta ya confirmada. Encontrar `sales(branch_id, client_operation_id)` durante un retry idempotente válido no debe usar este error; debe tratarse como replay/reconciliación exitosa y devolver la venta.
- `TERMINAL_INACTIVE`
- `TERMINAL_BRANCH_MISMATCH`
- `BRANCH_INACTIVE`
- `USER_INACTIVE`
- `USER_BRANCH_FORBIDDEN`: el usuario no tiene acceso a la sucursal mediante `user_branches(user_id, branch_id)`.
- `USER_PERMISSION_DENIED`: el usuario pertenece a la sucursal, pero no posee `permissions.code = 'SALES_CONFIRM'` mediante roles activos.
- `CASH_SESSION_REQUIRED`
- `CASH_SESSION_CLOSED`
- `CASH_SESSION_TERMINAL_MISMATCH`
- `CUSTOMER_INACTIVE`
- `CUSTOMER_BUSINESS_MISMATCH`
- `PRICE_LIST_INACTIVE`
- `PRICE_LIST_BUSINESS_MISMATCH`
- `PRODUCT_INACTIVE`
- `PRODUCT_BUSINESS_MISMATCH`
- `PRODUCT_UNIT_INVALID`
- `PRICE_NOT_FOUND`
- `PRICE_CHANGED`
- `INVALID_QUANTITY`
- `INVALID_DISCOUNT`
- `INVALID_TAX_CALCULATION`
- `INSUFFICIENT_STOCK`
- `PAYMENT_METHOD_INACTIVE`
- `PAYMENT_TOTAL_MISMATCH`: `SUM(sale_payments.amount) <> sales.total` con comparación decimal exacta.
- `MIXED_REPLENISHMENT_CHANNELS`
- `DOCUMENT_SEQUENCE_NOT_FOUND`
- `DOCUMENT_SEQUENCE_INACTIVE`
- `QUOTATION_NOT_FOUND`
- `QUOTATION_ALREADY_CONVERTED`
- `QUOTATION_BRANCH_MISMATCH`
- `QUOTATION_EXPIRED`
- `QUOTATION_STATUS_INVALID`

No se definen HTTP status codes en este documento.

## 17. Casos de concurrencia

### A. Dos terminales venden simultáneamente la última unidad

Ambas intentan bloquear el mismo `inventory_balances(branch_id, product_id)`. Una descuenta y confirma. La otra, al adquirir el lock después, revalida el saldo actualizado y falla con `INSUFFICIENT_STOCK`.

Resultado esperado: solo una confirma.

### B. Doble clic en una misma terminal

Ambos intentos usan la misma `idempotency_key` y `client_operation_id`. El primero adquiere la clave y confirma. El segundo recibe `SALE_IDEMPOTENCY_IN_PROGRESS` si llega durante la ejecución, o la venta ya creada si llega después del `COMMIT`.

Resultado esperado: una sola venta.

### C. Mismo `client_operation_id` enviado después de `COMMIT`

La unicidad `sales(branch_id, client_operation_id)` ya contiene la venta. Devolver la venta existente aunque llegue con otra `idempotency_key`. Como `sales` no almacena `request_hash`, no se detecta payload distinto solo con esta tabla. Una validación futura con fingerprint sería una mejora fuera del MVP, no una decisión pendiente de `CONFIRMAR VENTA v0.1`.

Resultado esperado: devolver la venta ya creada, no crear otra.

### D. Dos intentos simultáneos de convertir la misma cotización

Ambos intentan bloquear `quotations(id)`. Solo uno observa estado convertible y asigna `converted_sale_id`. El otro, al continuar, ve `CONVERTED` o `converted_sale_id` no nulo y falla con `QUOTATION_ALREADY_CONVERTED`.

Resultado esperado: solo uno puede convertirla.

### E. Dos ventas simultáneas generan folio

Ambas bloquean la misma fila de `document_sequences` de forma serializada. Cada una lee un `next_number` distinto después del incremento confirmado de la anterior.

Resultado esperado: folios distintos sin colisión.

## 18. Decisiones pendientes

Ninguna.
