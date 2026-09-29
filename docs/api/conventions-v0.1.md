# API Boundary Conventions v0.1

Fuente funcional: `especificacion_maestra_pos_multisucursal_v0.5.md`.

Referencia fisica vigente: `docs/database/modelo-fisico-v0.5-db-4.md`.

Contratos transaccionales frozen relevantes:

- `docs/transactions/confirmar-venta-v0.1.md`.
- `docs/transactions/confirmar-devolucion-v0.1.md`.
- `docs/transactions/confirmar-pedido-proveedor-v0.1.md`.
- `docs/transactions/confirmar-compra-v0.1.md`.

Contrato de dominio compartido relevante:

- `docs/domain/tax-snapshot-v1.md`.

## Estado del documento

- Estado: BORRADOR CONTROLADO
- Version: v0.1
- Implementacion: no iniciada

Este documento todavia NO queda congelado.

## 1. Objetivo

Este documento define convenciones compartidas de frontera para futuros contratos API de GENGXIN POS.

No disena endpoints concretos, rutas, HTTP methods, HTTP status codes, headers exactos, DTOs concretos, OpenAPI, framework, backend, ORM, JWT, frontend, desktop, deployment ni SQL de implementacion.

Su objetivo es cerrar reglas minimas compartidas sobre:

- identidad externa vs interna;
- tipos de identificadores;
- payload del caller vs contexto derivado;
- autoridad del estado persistido;
- separacion command vs query;
- response publica vs replay interno;
- exposicion de estructuras internas;
- representacion conceptual de dinero, cantidades, fechas y enums;
- versionado documental API.

## 2. Principio general de frontera

La API no refleja mecanicamente el esquema fisico.

La existencia de una tabla o columna no implica:

- recurso API;
- endpoint CRUD;
- campo aceptado por cliente;
- identificador externo.

El modelo fisico permanece interno. Los contratos API deben exponer capacidades de negocio, no tablas.

Las mutaciones publicas deben formularse como commands de negocio formales. Las lecturas deben formularse como queries/proyecciones utiles para operacion, administracion o diagnostico autorizado.

## 3. Identidad interna BIGINT

Los PK/FK `BIGINT` internos de db-4 son identidad fisica interna.

Por defecto:

- no se exponen como identificadores publicos;
- no se aceptan desde clientes externos como autoridad;
- no forman parte de URLs ni payloads publicos por costumbre;
- se resuelven internamente desde identidades externas.

Una excepcion futura solo puede existir si un contrato API concreto la declara explicitamente y justifica por que corresponde cruzar la frontera. Este documento no crea excepciones.

## 4. public_id

Cuando una entidad expuesta posea `public_id` fisico:

- `public_id` es el identificador externo preferido;
- cruza la frontera API;
- el backend resuelve `public_id -> id BIGINT`;
- el BIGINT permanece interno;
- no asumir que conocer un `public_id` autoriza acceso;
- tenant, branch y permission siguen validandose.

`public_id` no sustituye authorization.

No afirmar que toda tabla tiene `public_id`. Cada contrato API concreto debe verificar que la entidad realmente lo tenga en db-4 antes de usarlo como identidad externa.

## 5. Folio

Los folios son identidad humana/operativa.

Ejemplos existentes soportados por el repo:

- `VEN` para ventas;
- `DEV` para devoluciones;
- `PED` para pedidos;
- `COM` para compras;
- `COT` para cotizaciones.

El folio:

- puede mostrarse;
- puede buscarse;
- puede imprimirse;
- puede aparecer en responses;
- no sustituye la identidad tecnica externa;
- no debe usarse como PK fisica;
- puede requerir scope de branch y document type.

No inventar unicidad global si db-4 no la garantiza. El modelo documenta folios operativos por sucursal/tipo y unicidad fisica por alcance documental correspondiente.

## 6. client_operation_id

`client_operation_id` es una identidad logica de operacion originada por el cliente cuando el contrato de dominio lo requiere.

No es:

- `public_id` del recurso;
- PK;
- `idempotency_key`;
- fingerprint.

Su obligatoriedad depende del command concreto.

Ejemplos frozen existentes:

- `CONFIRM_SALE`: requerido segun contrato.
- `CONFIRM_RETURN`: requerido segun contrato.
- `CONFIRM_ORDER`: no es identidad del command.
- `CONFIRM_PURCHASE`: nullable y no requerido como autoridad de confirmacion.

Este documento no modifica esas reglas.

## 7. idempotency_key

`idempotency_key` identifica/reconoce reintentos de un command sensible.

Conceptualmente:

- pertenece a la frontera command;
- no identifica la entidad de negocio;
- no reemplaza `public_id`;
- no reemplaza `client_operation_id`;
- no reemplaza fingerprint.

Este documento no define:

- header exacto;
- nombre JSON;
- longitud;
- formato;
- generacion;
- almacenamiento de raw key;
- algoritmo exacto de `request_hash`.

Eso pertenece al futuro contrato `API Idempotency and Preconditions v0.1`.

## 8. request_hash

`request_hash` representa conceptualmente el command canonico asociado a una `idempotency_key`.

No es:

- identidad de negocio;
- `public_id`;
- `client_operation_id`;
- fingerprint del DRAFT persistido;
- permiso.

Los contratos transaccionales frozen definen que una misma key con hash diferente produce error de reutilizacion de key. Este documento no define algoritmo, canonicalizacion wire-format ni transporte exacto.

## 9. Fingerprints

Un fingerprint esperado es una precondicion optimista de un DRAFT persistido cuando el contrato de dominio lo requiere.

Ejemplos frozen:

- `CONFIRM_ORDER`: `expected_draft_fingerprint`.
- `CONFIRM_PURCHASE`: `expected_purchase_fingerprint`.

No es:

- identidad;
- `idempotency_key`;
- estado enviado para reemplazar al DRAFT;
- permiso.

El fingerprint autoritativo lo calcula o reconstruye el backend segun el contrato de dominio. Este documento no define algoritmo.

## 10. Command vs Query

COMMAND solicita cambio de estado o efecto de negocio.

QUERY lee/proyecta estado sin cambiarlo.

Reglas:

- commands pueden requerir idempotencia y preconditions;
- queries no deben producir efectos de negocio;
- una query no debe confirmar, cancelar, ajustar, timbrar ni corregir silenciosamente;
- no disenar commands como CRUD de tablas internas.

Esta separacion no impone un framework CQRS ni una arquitectura especifica.

## 11. Caller payload vs contexto vs estado persistido

### A. Input del caller

El input del caller debe limitarse a informacion que el usuario/cliente realmente controla o referencia.

Ejemplos conceptuales:

- `public_id` del aggregate objetivo cuando exista y el contrato lo adopte;
- `client_operation_id` cuando aplique;
- expected fingerprint cuando aplique;
- datos propios del command.

### B. Contexto derivado de autenticacion/sesion

No debe aceptarse como autoridad libre desde payload:

- `actor_user_id`;
- `business_id` / tenant;
- permisos;
- roles;
- terminal autenticada cuando aplique;
- IP/user-agent cuando aplique.

Estos datos se derivan de autenticacion, sesion, terminal, infraestructura de request o contexto operacional confiable que el contrato API futuro defina.

### C. Estado autoritativo persistido

El backend debe leerlo y revalidarlo desde la base/aggregate correspondiente:

- `status`;
- branch del aggregate;
- supplier/product relacionados;
- totales persistidos;
- `inventory_balances`;
- allocations;
- movimientos;
- `confirmed_at`;
- `confirmed_by_user_id`;
- estados relacionados.

El cliente no puede volver a enviar estado derivado o persistido y hacerlo autoridad solo porque coincida con su UI.

## 12. Branch y business

`branch_id` y `business_id` no deben aceptarse ciegamente como autoridad si se pueden derivar del aggregate o del contexto autenticado.

Cada command debe declarar explicitamente:

- cual es la branch autoritativa;
- de donde se deriva;
- como se valida tenant/business;
- si terminal/contexto debe coincidir.

No se fija una unica fuente para todos los commands porque `CONFIRM_SALE`, `CONFIRM_RETURN`, `CONFIRM_ORDER` y `CONFIRM_PURCHASE` tienen contratos distintos.

No exponer `business_id` como selector libre de tenant.

## 13. Identidades de details

Details como:

- `sale_items`;
- `return_items`;
- `purchase_order_items`;
- `purchase_items`;
- `quotation_items`;

no son recursos CRUD independientes por defecto.

Si un futuro command necesita referenciar una linea:

- el contrato concreto debe definir su identidad externa;
- debe validar pertenencia al aggregate;
- debe resolver internamente sus BIGINT.

Este documento no decide la forma exacta para todas las lineas.

## 14. Money y numeric

Valores monetarios y cantidades exactas de dominio no deben depender de floating point binario.

El contrato API debe preservar precision decimal.

Hasta elegir representacion concreta:

- no asumir IEEE float/double;
- mantener semantica decimal exacta compatible con `NUMERIC` de db-4;
- respetar las precisiones conceptuales vigentes: dinero `NUMERIC(18,2)`, cantidades base `NUMERIC(18,4)`, costos/factores `NUMERIC(18,6)`.

Este documento no decide si JSON publico usara string o number. Esa representacion concreta queda pendiente para un futuro micro-hito de DTO/API wire-format.

## 15. Timestamps

Los timestamps de dominio provienen del backend cuando son autoritativos.

El cliente no determina:

- `confirmed_at`;
- `closed_at`;
- `occurred_at` finales;
- timestamps equivalentes generados por la transaccion.

Responses publicas futuras deben usar una representacion temporal inequivoca con timezone/offset, compatible conceptualmente con `TIMESTAMPTZ`.

ISO-8601/RFC3339 es una convencion wire-format razonable a evaluar/cerrar despues. Este documento no elige libreria, framework ni serializador.

## 16. Enums y status

Valores de dominio como:

- `status`;
- `movement_type`;
- `channel`;
- `disposition`;

deben usar los vocabularios congelados del contrato/modelo cuando crucen la frontera.

El cliente no puede inventar nuevos enum values.

Un contrato API no debe renombrar silenciosamente estados de dominio sin razon explicita y mapeo documentado.

## 17. Response publica vs replay interno

### A. response_body interno de idempotency

`response_body` interno es persistencia/replay interno segun el contrato transaccional.

### B. Response publica API

La response publica API sera definida por cada command API.

No asumir:

```text
response_body interno == DTO HTTP publico
```

`CONFIRM_PURCHASE v0.1` ya declara explicitamente que su `response_body` es replay interno, no DTO/API publico congelado, y esta regla debe respetarse.

Una response publica puede devolver:

- identidad externa;
- folio;
- status;
- timestamps;
- resumen necesario.

El detalle completo puede pertenecer a una query separada.

Este documento no congela shapes concretos.

## 18. Estructuras no expuestas directamente

No crear escritura CRUD publica directa sobre:

- `inventory_movements`: ledger interno de inventario;
- `replenishment_movements`: ledger interno de reposicion;
- `cash_movements`: ledger interno de caja;
- `audit_log`: audit append-only;
- `idempotency_keys`: infraestructura de idempotencia;
- `replenishment_allocations`: detail interno FIFO de reposicion;
- `replenishment_allocation_fulfillments`: detail interno historico db-4;
- `inventory_balances`: estado derivado/resumen reconciliable;
- `replenishment_positions`: estado derivado/resumen de reposicion;
- `invoice_events`: ledger/eventos fiscales;
- `document_sequences`: infraestructura de folios para emision operativa directa.

Queries administrativas pueden existir en el futuro cuando proceda y con autorizacion adecuada.

Las mutaciones deben suceder mediante commands de negocio formales.

## 19. Versionado documental API

Cada contrato API documental tiene version propia.

Ejemplos:

- `conventions-v0.1`;
- `confirm-sale-v0.1`.

No asumir que:

```text
API contract v0.1 == database db-4 == transaction v0.1
```

Son ejes de version relacionados pero independientes.

Un cambio semantico incompatible en un contrato API congelado requiere evolucion/versionado, no edicion silenciosa.

Este documento no define URL `/v1` ni versionado de rutas.

## 20. Contratos transaccionales como autoridad

Para commands respaldados por contratos frozen, el contrato API adapta la frontera.

No puede cambiar:

- invariantes;
- errores de dominio;
- atomicidad;
- idempotency lifecycle;
- locks;
- estados;
- efectos.

La fuente de verdad de semantica business sigue siendo el contrato transaccional frozen correspondiente.

## 21. Operaciones actualmente listas

LISTO PARA API CONTRACT:

- `CONFIRM_SALE`;
- `CONFIRM_RETURN`;
- `CONFIRM_ORDER`;
- `CONFIRM_PURCHASE`.

Otras operaciones con documentacion parcial no deben congelarse solo por existir esta frontera. Cada una requiere su propio contrato de dominio o contrato API suficientemente justificado antes de quedar congelada.

## 22. Fuera de alcance

Queda explicitamente fuera de este documento:

- endpoint paths;
- HTTP methods;
- HTTP status codes;
- exact request/response DTOs;
- exact headers;
- exact idempotency transport;
- `request_hash` algorithm;
- OpenAPI;
- framework;
- backend language;
- ORM;
- JWT/provider;
- deployment;
- frontend;
- desktop technology;
- SQL implementation.

## 23. Pendientes despues de este micro-hito

Pendientes documentales vigentes:

1. `Wire-format / Transport Conventions v0.1`.
2. Command API contracts, uno por uno:
   - `CONFIRM_SALE`.
   - `CONFIRM_RETURN`.
   - `CONFIRM_ORDER`.
   - `CONFIRM_PURCHASE`.
3. Mapping publico definitivo de references/DTOs cuando corresponda.

No se desarrollan aqui.
