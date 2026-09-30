# API Public Error Codes v0.1

- Estado: FROZEN
- Version: v0.1
- Implementacion: no iniciada

Este documento **queda FROZEN en v0.1**. Define el catalogo compartido **ADITIVO** de codigos publicos adoptado inicialmente por `CONFIRM_SALE`.

Las cuatro semanticas publicas quedan estabilizadas en v0.1. Un cambio semantico incompatible requiere evolucion/versionado documental; una implementacion posterior no puede reinterpretar silenciosamente estos codigos.

## 1. Objetivo y alcance

Definir unicamente los codigos publicos compartidos que las autoridades base permiten pero no nombran concretamente. Los Command APIs adoptan estos codigos explicitamente como fallbacks cuando no existe un codigo especifico adecuado.

Este documento no redefine categorias, HTTP mapping, error envelope, errores frozen de dominio, lifecycle idempotente, authorization ni transport. Las categorias provienen de Error Model v0.1 y los HTTP status de Transport v0.1; la tabla siguiente aplica esas autoridades a los cuatro codigos aditivos.

## 2. Autoridades de referencia

- `docs/api/error-model-v0.1.md`
- `docs/api/transport-v0.1.md`
- `docs/api/authorization-and-context-v0.1.md`

No se modifica ni sustituye ninguna de estas autoridades ni ningun contrato frozen existente.

## 3. Catalogo compartido

| `error.code` | `category` | HTTP | `retryable` |
|---|---|---:|---|
| `REFERENCE_NOT_FOUND` | `NOT_FOUND_VISIBILITY` | 404 | false |
| `REQUEST_VALIDATION_FAILED` | `VALIDATION` | 422 | false |
| `AUTHENTICATION_REQUIRED` | `AUTHENTICATION` | 401 | false |
| `INTERNAL_ERROR` | `INTERNAL_TECHNICAL` | 500 por defecto | false |

`retryable` expresa la propiedad publica del codigo; no determina por si mismo misma key, nueva key ni retry inmediato. Su uso no altera la recuperacion idempotente del command que lo adopta.

### 3.1 `REFERENCE_NOT_FOUND`

Referencia publica sintacticamente valida que no puede resolverse dentro del scope visible permitido, solo cuando no existe ya un `error.code` especifico adecuado.

Debe cubrir indistinguiblemente:

- recurso inexistente;
- recurso existente fuera del scope visible.

No realizar un lookup global posterior para distinguir ambos casos. La respuesta publica de no-resolucion debe ser la misma; no revelar tenant, business, branch ni recurso oculto mediante `message`, `details`, status u otro side channel contractual.

No reemplazar codigos frozen especificos cuando correspondan a un recurso visible/resoluble. Tampoco reemplazar un codigo especifico adecuado de no-encontrado/no-visible. Este codigo no representa un fallo tecnico al ejecutar el lookup.

### 3.2 `REQUEST_VALIDATION_FAILED`

Request/header semanticamente invalido cuando no existe un `error.code` domain-specific o command-specific mas concreto.

Primera adopcion: header `Idempotency-Key` obligatorio ausente o con formato publico invalido. En ese caso, conforme a Transport, no iniciar el command ni crear una fila idempotente usando una key invalida.

No usar este fallback si ya aplica `INVALID_QUANTITY`, `INVALID_DISCOUNT`, `PAYMENT_TOTAL_MISMATCH` u otro codigo frozen/especifico correcto.

### 3.3 `AUTHENTICATION_REQUIRED`

No existe identidad/sesion autenticada utilizable para procesar la solicitud.

No sustituye `USER_INACTIVE`: ese codigo significa que existe una identidad conocida, pero esta inactiva, y conserva su tratamiento especifico de autorizacion.

Este catalogo no define provider, JWT, bearer, cookie, OAuth/OIDC ni `WWW-Authenticate`.

### 3.4 `INTERNAL_ERROR`

Fallo tecnico interno generico cuando la capa HTTP puede construir una respuesta segura.

No revelar mediante la respuesta:

- SQL;
- stack traces;
- exception raw;
- constraints;
- locks;
- PK/FK internas;
- secretos;
- detalles de infraestructura.

No crear codigos separados para deadlock, timeout, SQL error, lock contention ni crash especifico. No convertir el fallo tecnico en un codigo frozen de dominio.

`retryable=false` significa que este codigo generico no promete que se reconocio una condicion transitoria. No altera los mecanismos de recuperacion idempotente y no se convierte automaticamente en `FAILED` de dominio.

## 4. Prioridad de codigos

Aplicar esta prioridad:

1. Codigo frozen/domain-specific aplicable.
2. Codigo publico especifico del Command API.
3. Fallback shared de este catalogo.

Nunca reemplazar un codigo especifico correcto por uno generico. Los codigos son identificadores estables de maquina; no se renombran ni se fabrican a partir de diagnosticos internos.

No crear `IDEMPOTENCY_FAILED` generico. Un `FAILED` historico reproduce el codigo original cuando corresponda conforme al contrato del command; este catalogo no cambia el lifecycle ni autoriza otra persistencia.

## 5. Principio transversal de visibilidad

Una referencia publica debe resolverse dentro del scope visible determinado por contexto confiable. No buscar globalmente el recurso para luego decidir si revelar un mismatch.

Distinguir:

- **A. No resoluble dentro del scope visible:** `REFERENCE_NOT_FOUND`, `NOT_FOUND_VISIBILITY`, HTTP `404`, cuando no existe un codigo especifico adecuado. Inexistencia y existencia fuera del scope visible reciben la misma respuesta publica de no-resolucion.
- **B. Visible/resoluble pero incompatible estructuralmente:** corresponde al error especifico del Command API/domain, si existe.

Este catalogo no decide el alcance visible de cada recurso ni que mismatches especificos usa cada command. Eso corresponde al Command API conforme a sus autoridades. La resolucion scoped no redefine authorization ni altera el orden frozen de replay/reconciliacion del command.

## 6. Adopcion y fuera de alcance

`CONFIRM_SALE` adopta este catalogo mediante `docs/api/commands/confirm-sale-v0.1.md`. Otros commands podran adoptarlo explicitamente en sus propios contratos; no se sustituyen automaticamente sus codigos frozen.

Quedan fuera de alcance backend, framework, middleware, provider de autenticacion, OpenAPI y cambios al modelo fisico/db-4.
