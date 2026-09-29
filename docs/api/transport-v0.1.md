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
- la representacion publica canonica usa exactamente el `scale` semantico del campo;
- la parte entera contiene al menos un digito, elimina ceros no significativos a la izquierda y representa cero como `0`;
- para `scale > 0`, la parte fraccionaria contiene exactamente `scale` digitos y el punto decimal no se omite;
- no usar signo `+`; usar `-` solo para valores negativos cuando el dominio lo permita;
- normalizar cero negativo a cero positivo respetando el `scale`;
- el separador decimal es `.`;
- no usar separadores de miles, notacion cientifica ni formatos localizados;
- no usar JSON numbers como representacion publica autoritativa para estos valores exactos.

Ejemplos de representacion canonica:

| Scale | Valor | Representacion |
| --- | --- | --- |
| 2 | 1 | `"1.00"` |
| 2 | 1.5 | `"1.50"` |
| 2 | 1250.50 | `"1250.50"` |
| 4 | 3 | `"3.0000"` |
| 4 | 3.25 | `"3.2500"` |
| 6 | 18.123 | `"18.123000"` |
| 2 | -0.00 | `"0.00"` |
| 4 | -0.0000 | `"0.0000"` |

Por ejemplo, `"00012.50"` no es canonico; `"12.50"` si. Para un campo con scale 2, `"10"` y `"10.0"` no son representaciones publicas canonicas; la forma canonica es `"10.00"`.

El contrato publico normativo es la forma canonica. No se decide aqui si una implementacion futura tolerara input no canonico antes de validarlo.

Toda respuesta publica debe emitir los decimales exactos en la forma canonica correspondiente al `scale` del campo.

Transport no redondea ni trunca silenciosamente. Si un valor tiene mas precision que la permitida, rechazarlo, redondearlo o truncarlo sigue siendo responsabilidad del dominio o Command API correspondiente.

La representacion publica canonica es compatible con las reglas de canonicalizacion de `request_hash` definidas en su seccion especifica; este micro-hito no cambia esas reglas.

Este micro-hito no define todavia:

- por si sola, la canonicalizacion para `request_hash`, cuya autoridad queda en la seccion especifica de `Canonicalizacion wire para request_hash`;
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

- por si solo, el HTTP mapping, cuya autoridad queda en la seccion especifica de `HTTP mapping compartido`;
- otros headers;
- `Retry-After`;
- por si solo, correlation/request ID, cuya autoridad queda en la seccion especifica de `Correlation/request ID compartido`;
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

### Presencia

Para Command APIs que requieran idempotencia, `Idempotency-Key` es obligatorio.

La decision de que commands requieren el header proviene del Command API/contrato correspondiente.

No se convierte esto en una regla para futuros endpoints que no sean commands idempotentes.

### Formato publico

El valor debe:

- ser una unica string con caracteres ASCII desde `0x21` hasta `0x7E`, inclusive;
- tener longitud entre 1 y 128 caracteres;
- no contener espacios;
- no contener caracteres de control;
- no contener CR/LF;
- tratarse como valor opaco y case-sensitive.

Las prohibiciones de espacios, caracteres de control y CR/LF son consistentes con el rango permitido `0x21`-`0x7E`.

No interpretar prefijos ni estructura interna.

No se exige UUID ni ULID. UUID v4, ULID u otros identificadores suficientemente unicos pueden ser utilizados por clientes siempre que cumplan el contrato publico.

### Generacion

La generacion corresponde al cliente que inicia la operacion logica.

Una operacion logica nueva debe usar una nueva `Idempotency-Key`.

Un retry/reintento de la misma operacion logica que deba aprovechar la semantica idempotente debe reutilizar la misma key segun las reglas del command.

Cambiar solamente la key no cambia `request_hash`.

No usar `X-Request-Id` como sustituto de `Idempotency-Key`.

### Seguridad de la key

La key:

- no contiene secretos;
- no debe contener tokens;
- no debe codificar passwords;
- no debe usarse para autorizacion;
- no determina tenant, business, branch ni user;
- debe validarse antes de incluirse en logs para evitar log injection.

No se define aqui una politica completa de logging.

### Valor invalido o ausente

Para un Command API que exige idempotencia:

- ausencia del header o formato invalido debe tratarse como error publico de `VALIDATION`;
- no debe iniciarse la ejecucion del command;
- no crear una fila idempotente usando un valor invalido.

No se inventa todavia un `error.code` especifico compartido si los contratos actuales no lo definen. El Command API concreto cierra el `error.code` si hace falta.

El HTTP mapping sigue la regla existente de `VALIDATION` -> `422`.

### Persistencia

Transport no cambia:

- scope fisico;
- leases;
- expiracion interna;
- lifecycle;
- constraints de DB.

No se define aqui como se almacena la key internamente.

Este micro-hito no define:

- algoritmo de `request_hash`;
- expiracion publica de keys;
- `Retry-After`;
- replay headers adicionales;
- backend/framework;
- middleware concreto;
- OpenAPI.

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

- algoritmo hash;
- por si solo, el replay publico adicional, cuya autoridad queda en la seccion especifica de `Replay publico compartido`;
- por si solo, el HTTP mapping, cuya autoridad queda en la seccion especifica de `HTTP mapping compartido`;
- `Retry-After`;
- por si solo, correlation/request ID, cuya autoridad queda en la seccion especifica de `Correlation/request ID compartido`;
- auth headers;
- JWT/bearer/cookies;
- OpenAPI;
- framework/middleware.

## 12. Canonicalizacion wire para request_hash

`request_hash` representa el contenido semantico canonico del command, no la identidad de transporte de la solicitud.

Por tanto:

- `Idempotency-Key` no forma parte del contenido canonicalizado;
- cambiar unicamente `Idempotency-Key` no cambia `request_hash`;
- headers de autenticacion, tracing o transporte tampoco forman parte del command canonico salvo que un contrato futuro lo declare explicitamente por una razon semantica.

### Base de canonicalizacion

La canonicalizacion parte de la representacion logica del command definida por su Command API concreto.

No canonicalizar:

- SQL;
- filas persistidas completas;
- estado derivado no enviado por el cliente;
- valores generados por backend;
- headers de transporte no semanticos.

### Objetos JSON

- las propiedades se ordenan lexicograficamente por nombre para obtener representacion determinista;
- el orden original de propiedades recibido no tiene significado;
- propiedades ausentes y propiedades presentes con `null` no deben asumirse equivalentes salvo que el Command API lo declare explicitamente;
- no introducir propiedades default silenciosamente durante canonicalizacion salvo que el Command API defina esa normalizacion.

### Arrays

- preservar el orden recibido cuando el array sea semanticamente ordenado;
- Transport no puede ordenar arrays arbitrariamente;
- si un Command API declara explicitamente un conjunto no ordenado, ese contrato debe definir su regla de normalizacion antes del hash;
- no universalizar sorting de line items.

### Strings

- tratar strings como valores semanticos exactos segun el contrato;
- no aplicar trim, lowercase, uppercase, Unicode folding ni localizacion automaticamente;
- enums conservan exactamente el simbolo frozen;
- IDs/references conservan su representacion publica definida.

### Decimales

Los decimales siguen siendo JSON strings.

Reglas para hashing:

- usar `.` como separador decimal;
- no usar separadores de miles;
- no usar notacion cientifica;
- eliminar ceros no significativos a la izquierda;
- normalizar cero negativo a cero positivo;
- usar una representacion decimal determinista compatible con la precision/scale del campo.

No se inventa una unica cantidad universal de decimales para todos los tipos. La normalizacion concreta debe respetar el scale semantico del campo definido por dominio/Command API.

Si dos representaciones son semanticamente equivalentes segun el scale del campo, deben canonicalizar al mismo valor. Transport no debe permitir que diferencias meramente textuales de padding produzcan hashes distintos.

Esta regla no altera reglas de rounding del dominio.

### Timestamps

Para valores timestamp que legitimamente formen parte del command:

- canonicalizar a RFC 3339 UTC con `Z`;
- offsets equivalentes que representen el mismo instante deben canonicalizar al mismo instante UTC;
- no inventar precision temporal inexistente;
- la precision fraccionaria utilizada debe ser determinista segun el valor/contrato correspondiente.

No incluir timestamps generados autoritativamente por backend si no forman parte del command del cliente.

### Enums

- usar exactamente el simbolo frozen;
- case-sensitive;
- no traducir ni normalizar aliases.

### Booleans / null

- boolean canonical: JSON `true` / `false`;
- `null` permanece JSON `null` cuando el Command API permita/defina el campo como presente y nulo;
- ausencia != `null` salvo regla explicita del Command API.

### Public references

- usar la representacion publica definida por el Command API;
- no sustituir una referencia publica por `BIGINT` interno antes del hash;
- resolucion a PK interna ocurre despues y no debe cambiar la identidad semantica del command.

### Serializacion canonica

El backend debe producir una representacion byte/string determinista antes de aplicar el hash.

Reglas minimas:

- UTF-8;
- sin whitespace insignificant;
- propiedades de objetos en orden lexicografico ordinal/determinista e independiente de locale sobre los nombres de las propiedades;
- strings JSON escapadas de forma valida y determinista: escapar obligatoriamente `"` y `\`, escapar caracteres de control requeridos por JSON, usar representacion Unicode directa codificada en UTF-8 para los demas caracteres Unicode y no convertir arbitrariamente caracteres permitidos a escapes `\uXXXX`;
- arrays en orden semantico;
- tipos preservados.

No se adopta todavia formalmente RFC 8785/JCS ni otra especificacion externa. No se define todavia libreria concreta.

### Scope command-specific

Cada Command API debe declarar exactamente que campos pertenecen a su payload canonico para `request_hash`.

Transport define reglas compartidas de canonicalizacion, pero no decide aqui el payload de:

- `CONFIRM_SALE`;
- `CONFIRM_RETURN`;
- `CONFIRM_ORDER`;
- `CONFIRM_PURCHASE`.

### Semantica frozen

Este micro-hito no cambia:

- scope fisico de idempotencia;
- SAME KEY + SAME HASH;
- SAME KEY + DIFFERENT HASH;
- `client_operation_id`;
- fingerprints;
- estados `IN_PROGRESS` / `COMPLETED` / `FAILED`;
- reglas de autorizacion/reconciliacion;
- locks;
- efectos.

Este micro-hito no define:

- algoritmo criptografico final;
- encoding final del hash almacenado;
- longitud del digest;
- salt;
- HMAC;
- por si solo, el HTTP mapping, cuya autoridad queda en la seccion especifica de `HTTP mapping compartido`;
- por si solo, correlation/request ID, cuya autoridad queda en la seccion especifica de `Correlation/request ID compartido`;
- framework serializer;
- OpenAPI.

## 13. Replay publico compartido

Un replay/reconciliation publico nunca debe volver a ejecutar efectos ya materializados.

La response publica puede reconstruirse desde estado persistido/autoritativo cuando el contrato frozen lo permita. `idempotency_keys.response_body` sigue siendo almacenamiento interno y no se convierte automaticamente en DTO publico.

### SAME TERMINAL KEY + SAME HASH + COMPLETED

Mantener semantica frozen command-specific.

Regla publica compartida permitida:

- devolver una response publica de exito equivalente a la operacion historica;
- no repetir efectos;
- no crear una segunda entidad;
- no cambiar el resultado historico por estado actual posterior;
- reconstruir el DTO publico desde estado autoritativo cuando corresponda.

No se impone reauthorization/revalidation transversal.

En particular:

- `CONFIRM_PURCHASE` conserva replay historico exacto sin reautorizar ni revalidar user, branch ni catalogos;
- `CONFIRM_SALE`, `CONFIRM_RETURN` y `CONFIRM_ORDER` siguen su semantica frozen correspondiente.

### SAME TERMINAL KEY + SAME HASH + FAILED

Regla publica:

- reproducir el error historico correspondiente;
- conservar `error.code` original cuando este disponible;
- no reejecutar;
- no transformar `FAILED` a `COMPLETED` por un exito posterior con otra key;
- no generar un error generico `IDEMPOTENCY_FAILED`.

### SAME KEY + DIFFERENT HASH

Reglas:

- devolver el error KEY_REUSED especifico del command;
- no ejecutar efectos;
- no exponer `request_hash` completo;
- no intentar reinterpretar la solicitud como nueva.

Codigos existentes:

- `SALE_IDEMPOTENCY_KEY_REUSED`;
- `RETURN_IDEMPOTENCY_KEY_REUSED`;
- `ORDER_IDEMPOTENCY_KEY_REUSED`;
- `PURCHASE_IDEMPOTENCY_KEY_REUSED`.

### IN_PROGRESS vigente

Reglas:

- no ejecutar una segunda operacion concurrente para la misma key;
- mantener el codigo IN_PROGRESS especifico del command;
- no inventar todavia `Retry-After`;
- no definir todavia estrategia publica de polling.

Codigos existentes:

- `SALE_IDEMPOTENCY_IN_PROGRESS`;
- `RETURN_IDEMPOTENCY_IN_PROGRESS`;
- `ORDER_IDEMPOTENCY_IN_PROGRESS`;
- `PURCHASE_IDEMPOTENCY_IN_PROGRESS`.

### NEW KEY / IN_PROGRESS RECOVERABLE

No universalizar comportamiento.

`CONFIRM_SALE`:

- despues de idempotency lock y advisory lock por `(branch_id, client_operation_id)`, si ya existe `sales(branch_id, client_operation_id)`, prevalece la entidad existente;
- se reconcilia la key actual a `COMPLETED`;
- se devuelve la venta existente;
- esto ocurre antes de validaciones posteriores de branch/terminal/user/permission;
- Transport no agrega authorization-before-reconciliation.

`CONFIRM_RETURN`:

- misma regla conceptual con `returns(branch_id, client_operation_id)`;
- la devolucion existente prevalece;
- reconciliar key y devolverla antes de validaciones posteriores;
- no imponer la regla de `CONFIRM_ORDER` / `CONFIRM_PURCHASE`.

`CONFIRM_ORDER`:

- para key nueva o `IN_PROGRESS` recuperable, current business/branch/auth debe validarse segun contrato frozen antes de reconciliacion historica permitida o nueva ejecucion.

`CONFIRM_PURCHASE`:

- para key nueva o `IN_PROGRESS` recuperable, current tenant/branch/user/`user_branches`/`PURCHASES_CONFIRM` debe validarse segun contrato frozen antes de reconciliacion o ejecucion.

### Reconciliation publica

Cuando una key nueva se reconcilia contra una entidad ya existente de `CONFIRM_SALE` / `CONFIRM_RETURN` por `client_operation_id`:

- la respuesta publica debe representar la entidad historica existente;
- no debe crear efectos adicionales;
- no debe exponer PK internas;
- no debe exponer datos cross-tenant;
- no debe afirmar que fue una nueva venta/devolucion.

No se define todavia un campo publico como:

- `replayed`;
- `reconciled`;
- `idempotent_replay`.

Si se considera util despues, debera ser otro micro-hito o parte del Command API.

### Replay y envelopes

Replay no crea un envelope distinto.

Exito:

```json
{
  "data": {}
}
```

Error:

```json
{
  "error": {}
}
```

### Seguridad

Replay/reconciliation no debe convertirse en canal para:

- descubrir recursos de otro tenant;
- exponer `request_hash`;
- exponer `idempotency_key`;
- exponer PK internas;
- omitir reglas de visibilidad salvo donde el contrato frozen explicitamente ordene reconciliacion antes de validaciones posteriores.

Este micro-hito no define:

- por si solo, el HTTP mapping, cuya autoridad queda en la seccion especifica de `HTTP mapping compartido`;
- `Retry-After`;
- headers adicionales de replay;
- campo publico `replayed`/`reconciled`;
- polling;
- cache headers;
- por si solo, correlation/request ID, cuya autoridad queda en la seccion especifica de `Correlation/request ID compartido`;
- auth provider;
- OpenAPI;
- framework/middleware.

## 14. HTTP mapping compartido

HTTP status describe la semantica de transporte. `error.code` sigue siendo la autoridad programatica especifica.

HTTP status no reemplaza codigos de dominio ni reclasifica errores frozen.

### Success

Para command confirmado exitosamente o replay/reconciliation exitoso:

- HTTP `200 OK` como status publico compartido por defecto para estos Command APIs de confirmacion.

No usar `201 Created` por el hecho de que internamente se haya creado una entidad, porque la operacion publica es un command de confirmacion y un replay/reconciliation debe poder devolver el mismo contrato de exito sin cambiar su semantica HTTP.

No se definen endpoints todavia.

### VALIDATION

Mapping por defecto:

- HTTP `422 Unprocessable Content`.

Aplica a errores donde la solicitud es sintacticamente procesable pero viola reglas de validacion de dominio/input.

No convertir automaticamente errores de estado, precondicion o autorizacion a `422`.

### AUTHENTICATION

Mapping:

- HTTP `401 Unauthorized`.

No se define todavia auth provider, `WWW-Authenticate` ni bearer/JWT.

### AUTHORIZATION

Mapping:

- HTTP `403 Forbidden`.

Debe cubrir errores conceptualmente de autorizacion como:

- `USER_INACTIVE` cuando el error-model lo clasifique como autorizacion;
- `USER_BRANCH_FORBIDDEN`;
- `USER_PERMISSION_DENIED`.

No clasificar automaticamente branch/business structural mismatches como `403`. Respetar `error-model-v0.1.md`: mismatch estructural no equivale automaticamente a `AUTHORIZATION`.

### NOT_FOUND_VISIBILITY

Mapping:

- HTTP `404 Not Found`.

Usar esta categoria cuando el contrato requiere no revelar si el recurso existe fuera de la frontera visible del caller/tenant.

No usar `403` si eso revela existencia que el contrato decidio ocultar.

### STATE_CONFLICT

Mapping por defecto:

- HTTP `409 Conflict`.

Para conflictos con estado actual del recurso cuando la solicitud no puede aplicarse en ese estado.

### PRECONDITION

Mapping:

- HTTP `412 Precondition Failed`.

Esto cubre precondiciones explicitas/fingerprints stale cuando el error-model las clasifique como `PRECONDITION`.

No usar `409` automaticamente para fingerprints si ya existe clasificacion `PRECONDITION`.

### IDEMPOTENCY

Mapping por defecto:

- HTTP `409 Conflict`.

Incluye conceptualmente:

- `*_IDEMPOTENCY_KEY_REUSED`;
- `*_IDEMPOTENCY_IN_PROGRESS`.

No inventar todavia `Retry-After`. No definir polling.

`FAILED` historico no recibe un HTTP status generico de `IDEMPOTENCY`: debe reproducir el error historico y por tanto el HTTP mapping correspondiente al error original cuando sea reconstruible segun contrato.

### TEMPORARY_RETRYABLE

Mapping por defecto:

- HTTP `503 Service Unavailable`.

Excepcion: si un error retryable de dominio tiene codigo/status mas especifico definido por el contrato/API, usar ese mapping especifico.

No convertir automaticamente todo retryable a `503` solo por `retryable=true`.

Para `PURCHASE_REPLENISHMENT_PREDECESSOR_PENDING`, revisar error-model/frozen contract antes de fijar un status especifico; si los documentos actuales no lo cierran de manera suficiente, su status exacto queda pendiente para el Command API.

### INTERNAL_TECHNICAL

Mapping por defecto:

- HTTP `500 Internal Server Error`.

Para indisponibilidad clara de infraestructura, un futuro mapping puede usar `503` si se distingue de forma segura y estable.

No inventar nuevos `error.code` publicos para deadlock, timeout, SQL error, lock contention, etc.

No exponer exception raw.

### Structural/context mismatches

Los codigos estructurales/contextuales se mapean por su categoria conceptual real, no por el nombre del codigo.

Ejemplos:

- `TERMINAL_BRANCH_MISMATCH` no es automaticamente `AUTHORIZATION`;
- `BRANCH_BUSINESS_MISMATCH` no es automaticamente `AUTHORIZATION`;
- `CUSTOMER_BUSINESS_MISMATCH` no es automaticamente `AUTHORIZATION`.

Su status concreto debe derivarse de la categoria que error-model/Command API les asigne. Si el error-model actual no fija categoria exacta suficiente para uno de esos codigos, no inventarla aqui.

### Replay

Replay exitoso usa el mismo HTTP success status compartido del command.

`FAILED` historico reproduce:

- mismo `error.code`;
- misma categoria conceptual;
- HTTP status derivado de ese error historico.

`KEY_REUSED` / `IN_PROGRESS` siguen su mapping de `IDEMPOTENCY`.

No crear status especial de replay.

### Error envelope

El body mantiene:

```json
{
  "error": {
    "code": "...",
    "message": "...",
    "category": "...",
    "retryable": false
  }
}
```

HTTP status no sustituye esos campos.

### Tabla resumen

| Category | Default HTTP |
| --- | --- |
| VALIDATION | 422 |
| AUTHENTICATION | 401 |
| AUTHORIZATION | 403 |
| NOT_FOUND_VISIBILITY | 404 |
| STATE_CONFLICT | 409 |
| PRECONDITION | 412 |
| IDEMPOTENCY | 409 |
| TEMPORARY_RETRYABLE | 503 por defecto, sujeto a excepcion especifica |
| INTERNAL_TECHNICAL | 500 por defecto |

Exito:

- success -> `200`.

La tabla es default conceptual. No borra excepciones command-specific ni reclasifica errores frozen.

Este micro-hito no define:

- `Retry-After`;
- `WWW-Authenticate`;
- cache headers;
- auth headers;
- por si solo, correlation/request ID, cuya autoridad queda en la seccion especifica de `Correlation/request ID compartido`;
- rate limiting;
- 429 policies;
- redirect semantics;
- CORS;
- endpoints/routes;
- OpenAPI;
- framework exception handlers.

## 15. Correlation/request ID compartido

Se adopta `X-Request-Id` como header publico compartido para correlacionar una solicitud HTTP con su respuesta y con observabilidad interna.

`X-Request-Id` no es identidad de dominio.

### Header publico

Header:

```http
X-Request-Id: <opaque-value>
```

### Entrada

El cliente puede enviar `X-Request-Id`.

Si el cliente no lo envia, el backend genera uno.

El backend debe devolver un `X-Request-Id` valido en toda respuesta HTTP que la capa API/HTTP pueda construir.

### Naturaleza

`X-Request-Id`:

- es un identificador de transporte/observabilidad;
- no es `idempotency_key`;
- no es `client_operation_id`;
- no es `public_id`;
- no es `business_id`;
- no forma parte del payload de dominio;
- no forma parte de `request_hash`;
- no autoriza;
- no determina tenant/branch/user.

### Confianza del valor recibido

El valor enviado por cliente debe tratarse como input no confiable.

El backend puede aceptarlo y propagarlo solo si cumple la validacion publica definida por Transport.

Si es ausente o invalido, generar uno nuevo en vez de fallar el command unicamente por ese motivo.

No convertir un request ID invalido en error de dominio.

### Formato publico minimo

Reglas:

- ASCII visible;
- longitud de 1 a 128 caracteres;
- sin espacios;
- sin caracteres de control;
- sin CR/LF;
- tratarlo como valor opaco;
- case-sensitive;
- no imponer UUID/ULID.

No definir semantica interna a partir de su estructura.

### Generacion backend

Cuando el backend genere el valor:

- debe ser suficientemente unico para correlacion operacional;
- no debe contener informacion sensible;
- no debe codificar `user_id`, `business_id`, `branch_id`, PK internas, timestamps sensibles ni datos de dominio.

No se fija todavia una libreria concreta.

UUID v4 o equivalente seria una implementacion valida, pero UUID no es obligatorio en el contrato publico.

### Response

Toda respuesta HTTP que la capa API/HTTP pueda construir debe devolver:

```http
X-Request-Id: <value>
```

Incluye conceptualmente:

- exito;
- replay;
- errores de dominio;
- errores de autorizacion;
- errores de validacion;
- errores tecnicos, siempre que la capa HTTP haya podido construir una respuesta.

La response debe devolver el request ID aceptado del cliente o el generado por backend para esa solicitud.

### Error envelope

No agregar `request_id` dentro del JSON error envelope por ahora.

La autoridad publica para este micro-hito es el header `X-Request-Id`.

Evitar duplicarlo en body y header sin necesidad.

### Logging/observabilidad

Internamente, el backend debe poder asociar el mismo request ID a logs/eventos de esa solicitud.

Este documento no define:

- formato de logs;
- tracing spans;
- `trace_id`/`span_id`;
- OpenTelemetry;
- propagacion entre microservicios;
- storage/retention de logs;
- dashboards.

### Seguridad

Nunca confiar en `X-Request-Id` como prueba de identidad.

No exponer mediante su valor:

- secretos;
- tokens;
- PK internas;
- tenant IDs sensibles;
- user IDs;
- informacion de infraestructura.

Evitar log injection validando el header antes de propagarlo.

### Request hash

`X-Request-Id` no forma parte de `request_hash`.

Dos requests semanticamente identicos con request IDs distintos deben producir el mismo `request_hash` si todos los datos semanticos son iguales.

### Idempotencia

Cambiar `X-Request-Id` no crea una operacion nueva.

No sustituye `Idempotency-Key`.

Un replay puede tener un `X-Request-Id` distinto al request original porque identifica la solicitud HTTP actual, no la operacion historica.

Este micro-hito no define:

- W3C `traceparent`;
- `trace_id`/`span_id`;
- OpenTelemetry;
- distributed tracing;
- correlation entre servicios externos;
- `Retry-After`;
- auth headers;
- CORS;
- rate limiting;
- OpenAPI;
- middleware concreto;
- framework concreto.

## 16. Versionado compartido de rutas HTTP

### Prefijo

Toda ruta publica perteneciente a esta API versionada debe comenzar conceptualmente con:

```text
/api/v1
```

Ejemplo puramente estructural:

```text
/api/v1/...
```

Esta convencion no define la ruta concreta de ningun Command API.

### Significado de v1

`v1` representa la major version del contrato publico HTTP.

Los cambios backward-compatible dentro de la misma version no requieren automaticamente `v2`.

Los cambios breaking del contrato publico que no puedan introducirse de forma compatible requieren una nueva major version, por ejemplo `/api/v2/...`.

Ejemplos conceptuales de cambios potencialmente breaking:

- eliminar o renombrar campos publicos requeridos;
- cambiar tipos publicos;
- cambiar el significado contractual de un campo existente;
- cambiar de forma incompatible una ruta o HTTP method;
- eliminar valores previamente validos cuando eso rompa clientes;
- cambiar envelopes compartidos de forma incompatible.

Ejemplos que no exigen automaticamente una nueva major:

- agregar un campo opcional cuando el contrato permita extension compatible;
- agregar nuevos endpoints;
- agregar nuevos error codes command-specific cuando el contrato permita evolucion compatible;
- cambios internos de DB;
- cambio de framework o refactors internos;
- cambio de algoritmo interno de `request_hash` mientras preserve el contrato publico y la semantica idempotente correspondiente.

Estos ejemplos no forman un catalogo exhaustivo. No se redefinen aqui reglas adicionales de compatibilidad de enums.

### Versionado fuera del path

Para la API HTTP v1, el mecanismo publico compartido de major version es el path. No usar como mecanismo principal:

- Accept vendor media types;
- query parameter `?version=`;
- header custom de version.

Esto no impide que HTTP use media types normales posteriormente.

### Scope

Transport define unicamente el prefijo y versionado compartidos. Cada Command API concreto sigue definiendo:

- path restante;
- HTTP method;
- request DTO;
- response DTO;
- referencias concretas.

Esta convencion no define hostname, base URL de produccion, puerto, HTTPS termination, reverse proxy, API gateway, deployment, CORS, auth provider, OpenAPI, deprecation/sunset headers ni estrategia completa de migracion v1 -> v2.

## 17. Decisiones todavia no cerradas

Este documento inicial no cierra:

- JSON media type exacto si corresponde;
- algoritmo criptografico de `request_hash`;
- otros headers exactos de transporte;
- OpenAPI.

## 18. Fuera de alcance

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

## 19. Pendientes siguientes por micro-hitos

Secuencia recomendada para micro-hitos posteriores:

1. Command API contracts uno por uno.

No se desarrolla ninguna de esas decisiones en este documento inicial.
