# GENGXIN POS — Arquitectura tecnológica y stack v0.1

| Control | Valor |
| --- | --- |
| Estado del documento | **BASELINE DE IMPLEMENTACIÓN PARCIALMENTE FROZEN — DESKTOP PENDING** |
| Versión | v0.1 |
| Fecha de revisión | 2026-09-30 |
| Alcance | Un comercio, inicialmente cuatro sucursales, backend y datos centrales, Web Logística y POS Desktop separados |
| Base de ratificación | Branch `main`, HEAD `9bc2e4e` (`docs(architecture): add POS hardware inventory draft v0.1`), árbol de trabajo inicialmente limpio |
| Implementación | Habilitada para backend/Web; no iniciada por este micro-hito; desktop pendiente |

Este documento es el registro controlado de arquitectura tecnológica y selección de stack. Consolida decisiones existentes, ratifica una baseline implementable para backend/Web/infraestructura e identifica los pendientes reales. Las decisiones marcadas **FROZEN EN TECH-STACK v0.1** quedan congeladas por este hito para iniciar implementación; el stack completo no está cerrado porque `POS_DESKTOP_FRAMEWORK = PENDING`. Ese pendiente independiente no invalida los bloques ya ratificados ni impide comenzar backend/Web. Este documento tampoco sustituye reglas funcionales, contratos transaccionales, contratos API ni el modelo físico vigente.

## 1. Autoridades y trazabilidad

Las referencias siguientes fueron inspeccionadas antes de redactar. Sus estados corresponden al texto presente en el repositorio, no a una inferencia sobre todos los documentos de una carpeta.

| Ref. | Documento / archivo | Estado y autoridad relevante |
| --- | --- | --- |
| A1 | [Especificación maestra v0.5](../../especificacion_maestra_pos_multisucursal_v0.5.md) | Diseño funcional congelado y técnico en consolidación. §§1.2–1.4: dos canales y datos centrales; §§2,11,22,26: terminal/hardware y selección pendiente; §30: arquitectura lógica **congelada como monolito modular**. |
| A2 | [Schema db-4](../../database/schema-v0.5-db-4.sql) | DDL físico vigente congelado por A3. Constraints, tipos, ledgers, idempotencia, secuencias y detalle de fulfillment existentes. |
| A3 | [Modelo físico db-4](../database/modelo-fisico-v0.5-db-4.md) | **VALIDADO / CONGELADO**. Evidencia contra PostgreSQL **17.11**, schema con COMMIT y validation con PASS/ROLLBACK; no selecciona stack de aplicación. |
| T1 | [CONFIRM_SALE transaccional](../transactions/confirmar-venta-v0.1.md) | **VALIDADO / CONGELADO**. Atomicidad, cálculo decimal, idempotencia, advisory lock, orden de locks y reconciliación. Referencia histórica de origen: db-2. |
| T2 | [CONFIRM_RETURN transaccional](../transactions/confirmar-devolucion-v0.1.md) | **VALIDADO / CONGELADO**. Devolución independiente, RESTOCK/DAMAGED, caja condicional, idempotencia y concurrencia. Referencia de origen: db-3. |
| T3 | [CONFIRM_ORDER transaccional](../transactions/confirmar-pedido-proveedor-v0.1.md) | **VALIDADO / CONGELADO**. DRAFT persistido autoritativo, fingerprint, mutex de cabecera, FIFO y locks compatibles. Referencia de origen: db-3. |
| T4 | [CONFIRM_PURCHASE transaccional](../transactions/confirmar-compra-v0.1.md) | **VALIDADO / CONGELADO**, sin gaps conceptuales/físicos pendientes en v0.1. Adopta db-4, Policy A, SAFE, detail, inventario/costo, locks y atomicidad completa. |
| P1 | [API Conventions](../api/conventions-v0.1.md) | **BORRADOR CONTROLADO**, no congelado. Identidad externa, commands/queries y separación entre response pública y replay interno. |
| P2 | [API Authorization and Context](../api/authorization-and-context-v0.1.md) | **BORRADOR CONTROLADO**, no congelado. Contexto, tenant/business/branch, permisos y diferencias entre commands. |
| P3 | [API Error Model](../api/error-model-v0.1.md) | **BORRADOR CONTROLADO**, no congelado. Categorías y separación de errores de dominio/técnicos. |
| P4 | [API Idempotency and Preconditions](../api/idempotency-and-preconditions-v0.1.md) | **BORRADOR CONTROLADO**, no congelado. Matrices de lifecycle, precondiciones y reconciliación por command. |
| P5 | [API Transport](../api/transport-v0.1.md) | **FROZEN**. Decimales wire, timestamps, enums, envelopes, headers, canonicalización y prefijo `/api/v1`. |
| P6 | [API Public Error Codes](../api/public-error-codes-v0.1.md) | **FROZEN**. Catálogo aditivo de fallbacks, con adopción explícita por cada Command API. |
| P7 | [CONFIRM_SALE Command API](../api/commands/confirm-sale-v0.1.md) | **FROZEN**. `POST /api/v1/sales/confirm`, DTOs, referencias, frontera autenticada mínima y precedencia de replay/reconciliación. |
| D1 | [TAX SNAPSHOT v1](../domain/tax-snapshot-v1.md) | Contrato histórico compartido adoptado por T4. Su JSON interno no es un DTO público; P7 no lo adopta como input de venta. |
| I1 | [Compose actual](../../compose.yaml) | Configuración existente: solo servicio `db`, imagen `postgres:17`; no acredita freeze del despliegue completo. |
| I2 | [Variables de ejemplo](../../.env.example) | Solo variables PostgreSQL con password placeholder; no selecciona autenticación de usuarios ni configuración productiva. |

### 1.1 Límites de autoridad

- A1 gobierna el alcance funcional; A2/A3 gobiernan la estructura física vigente; T1–T4 gobiernan la semántica transaccional concreta; P5–P7 gobiernan los aspectos públicos congelados de su alcance.
- Una decisión tecnológica debe satisfacer esas autoridades. Elegir un framework no permite cambiar locks, redondeo, idempotencia, permisos, folios, snapshots ni errores congelados.
- Los documentos API base P1–P4 conservan su estado de borrador. Se usan conforme a su adopción y a las reglas específicas cerradas en P5–P7, sin declarar que toda la API está congelada.
- Los antecedentes db-2/db-3 citados por T1–T3 no obligan a operar distintas bases: A3 declara compatibilidad acumulativa de db-4 para esos dominios y el delta de fulfillment para compras.
- Si aparece una contradicción material no resuelta, debe registrarse y cerrarse documentalmente antes de implementar el punto afectado. Esta baseline no tiene autoridad para corregir silenciosamente un contrato congelado.

## 2. Evidencia de origen y estado previo a implementación

La inspección de origen del borrador encontró documentación y SQL de diseño/validación, sin aplicación implementada:

```text
gengxin/
  especificacion_maestra_pos_multisucursal_v0.5.md
  README.md
  compose.yaml
  .env.example
  .gitignore
  .gitattributes
  database/
    schema-v0.5-db-1.sql ... schema-v0.5-db-4.sql
    validation-v0.5-db-2.sql ... validation-v0.5-db-4.sql
  docs/
    api/commands/
    database/
    domain/
    transactions/
```

Al redactar el borrador original no existía `docs/architecture/`, documento equivalente de stack, `src/`, Dockerfile, configuración adicional Docker, `pyproject.toml`, `requirements.txt`, `uv.lock`, `package.json`, lockfile de dependencias de aplicación, `.sln`, `.csproj` ni configuración de stack equivalente. Desde entonces se agregaron este documento y el inventario POS, pero continúa sin aplicación implementada. El README contiene únicamente el nombre del proyecto. Los patrones `node_modules/` y `dist/` en `.gitignore` no prueban una elección previa de Node/React.

Compose contiene `postgres:17`, contenedor `gengxin-postgres`, volumen persistente, healthcheck y publicación `5432:5432`. `.env.example` contiene `POSTGRES_USER`, `POSTGRES_PASSWORD=change_me` y `POSTGRES_DB`. Existe `.env` local ignorado por Git; sus valores no son evidencia documental del stack. La configuración existente no constituye una topología productiva validada.

El único archivo modificado por este micro-hito de ratificación es este documento. No se crean proyectos, código, tests, migrations, SQL, Dockerfiles ni CI/CD; tampoco se mueven archivos o se modifican autoridades existentes.

## 3. Dos aplicaciones cliente con un backend común

La separación **Web Logística / Administración** y **POS Desktop de sucursal** materializa los canales de A1 §1.4. La existencia de ambos canales es **FROZEN EXISTENTE**; su separación tecnológica, la Web responsive y la frontera de hardware quedan **FROZEN EN TECH-STACK v0.1**. No habrá una única aplicación frontend encargada de todo. Ambas consumen la misma lógica central/API y la misma fuente de datos.

### 3.1 Web Logística / Administración

Diseñada desde el inicio para navegador **Desktop/laptop, Tablet y Mobile**, con UI responsive. El celular accede por navegador a la misma Web Logística; no se crea app móvil nativa para el MVP.

Responsabilidades conceptuales, sujetas a permisos y a contratos de cada operación:

- Dashboard, reportes y consulta de ventas/devoluciones.
- Inventario, existencias por sucursal, movimientos, ajustes/traspasos autorizados y reposición.
- Proveedores, pedidos a proveedor y compras/recepción, incluyendo DRAFT y confirmación cuando sus APIs estén definidas.
- Productos, unidades/presentaciones, listas de precios y clientes.
- Cotizaciones administrativas cuando corresponda, sin reserva de stock ni efectos operativos por emitirlas.
- Facturación/CFDI administrativa, consulta/entrega de XML/PDF y operaciones fiscales según el futuro contrato fiscal.
- Usuarios, permisos y configuración administrativa.

**Frontera de hardware FROZEN EN TECH-STACK v0.1:** la Web no integra directamente impresoras térmicas, scanners como hardware POS, cajones de dinero, puertos COM, USB, drivers, hardware local de caja ni impresión POS directa. No se introduce un puente local de hardware para convertirla en caja POS. Un código capturado manualmente es input, no una integración POS del navegador. PDF/descarga o impresión documental convencional no equivalen a impresión directa de tickets.

T3 y T4 permiten operación administrativa sin terminal POS funcional: `audit_log.terminal_id` puede ser contextual o `NULL`. No se inventa una terminal para habilitar pedidos/compras desde la Web.

### 3.2 POS Desktop de sucursal

Aplicación operacional instalada **nativamente en cada PC Windows de sucursal**, ligada a una terminal/sucursal según A1 §2. No se promete una versión móvil del POS.

Responsabilidades conceptuales:

- Login operativo, contexto de terminal/sucursal, apertura y cierre de caja.
- Búsqueda rápida de producto, lectura de códigos, carrito, ventas y cobro mediante `CONFIRM_SALE`.
- Devoluciones operativas mediante el backend, cotizaciones de mostrador cuando corresponda y consulta rápida de stock.
- Tickets, reimpresión e integración con hardware.
- Scanner/lector, impresora térmica, cajón, filesystem local necesario, puertos/USB/COM y APIs nativas de Windows cuando el equipo lo requiera.

La UI y los adaptadores locales son responsabilidad desktop. Los efectos de negocio son responsabilidad del backend. Una falla de impresión posterior no debe crear otra venta ni repetir el cobro: ticket/reimpresión trabajan sobre la operación ya confirmada. Imprimir o abrir un cajón no forma parte de la atomicidad PostgreSQL; no se promete rollback de un efecto físico.

## 4. ONLINE-FIRST CENTRALIZADO

**Estado: FROZEN EN TECH-STACK v0.1.** Compatible con A1 §§1.2–1.4,21,22,27: base lógica central, servidor accesible por sucursales y modo offline fuera del alcance inicial. Este hito ratifica la decisión tecnológica completa sin convertirla en un freeze previo.

```text
POS Desktop (cada PC de las cuatro sucursales) -- HTTPS --\
                                                        Backend API central --> PostgreSQL 17 central
Web Logística (navegador Desktop/Tablet/Mobile) -- HTTPS --/
```

Consecuencias operativas de la baseline:

- Una confirmación requiere conectividad con la API central; el cliente no puede declarar localmente un éxito autoritativo.
- Ante pérdida de respuesta/COMMIT desconocido, conservar la identidad de la operación y recuperar/reintentar conforme al contrato idempotente. No generar una venta nueva solo por timeout ni interpretar pérdida de respuesta como rollback confirmado.
- Ante indisponibilidad central, informar el estado y no continuar nuevas confirmaciones offline. La reanudación usa las defensas existentes de T1–T4/P5/P7.
- No incorporar base operativa offline completa por sucursal, sincronización bidireccional, conflictos distribuidos, folios offline, stock offline autoritativo ni ventas offline sincronizadas posteriormente.
- Puede evaluarse después cache/configuración local **no autoritativa**. Un cache, carrito o dato técnico de recuperación no es una venta confirmada ni una fuente de stock. Este hito no selecciona motor local, cola persistente ni mecanismo de sincronización.

La dependencia de Internet/API es un efecto aceptado por la baseline MVP. Latencia, conectividad y procedimiento ante caída se validarán con la sucursal piloto; no se presupone operación offline para compensarlos.

## 5. Backend como MODULAR MONOLITH

**Estado: FROZEN EXISTENTE para la arquitectura lógica**, por A1 §30; A1 §17.4 y Anexo C también la requieren. No se presenta como una elección recién congelada aquí.

Un backend desplegable como unidad, con módulos internos claros:

| Módulo conceptual | Responsabilidad |
| --- | --- |
| Identity/Auth | Usuarios, permisos, business/branches, activación y contexto de terminales |
| Sales | Ventas, pagos, snapshots y `CONFIRM_SALE` |
| Returns | Devoluciones operativas y `CONFIRM_RETURN` |
| Cash | Sesiones, apertura/cierre y ledger de caja |
| Inventory | Existencias, ledger, ajustes y traspasos |
| Replenishment | Demanda/compromiso por canal y trazabilidad FIFO |
| Purchasing | Proveedores, pedidos, recepción, `CONFIRM_ORDER` y `CONFIRM_PURCHASE` |
| Quotations | DRAFT, emisión, vigencia y conversión a venta |
| Catalog | Productos, unidades, impuestos operativos y pricing/listas |
| Customers | Datos comerciales y perfiles fiscales |
| Invoicing | CFDI, adaptador PAC y referencias de XML/PDF |
| Audit | Auditoría y consultas autorizadas; reporting puede organizarse internamente |

Los módulos colaboran dentro del proceso y de la transacción que corresponda. `CONFIRM_SALE`, por ejemplo, no se divide en llamadas remotas independientes a servicios de caja e inventario. No hay microservices, Kubernetes, broker ni transacciones distribuidas obligatorios para este MVP. El adaptador PAC conserva la separación fiscal existente; una devolución operativa no depende de la respuesta del PAC (T2 §13).

Esta lista no congela nombres de assemblies, clases, capas, handlers ni carpetas internas. No se crean ahora.

## 6. Backend: ASP.NET Core 10 + C# + .NET 10 LTS

**Estado: FROZEN EN TECH-STACK v0.1.** Es el stack backend de la primera implementación; no hay blocker documental real que obligue a otro backend.

Encaje específico para GENGXIN:

- API REST tipada para distinguir referencias públicas, contexto autenticado, precondiciones y estado persistido, evitando DTOs que expongan mecánicamente tablas.
- C# permite modelar estados/resultados y usar aritmética decimal exacta; el código deberá respetar rangos, precisión intermedia y redondeo compatibles con PostgreSQL, no los defaults de un serializador o `Math.Round`.
- Npgsql permite SQL PostgreSQL explícito, transacciones, locks y recovery visibles. El framework no sustituye los protocolos T1–T4.
- Soporte de OpenAPI, autenticación/autorización, logging, configuración e inyección de dependencias dentro de un ecosistema empresarial maduro.
- Ejecución del backend en contenedor Linux y despliegue central Compose, sin exigir Windows Server porque el cliente POS sea Windows.
- Ecosistema C# compartible con ambos candidatos desktop para contratos públicos y cliente API, sin mover reglas críticas al cliente.
- Testing unitario/integración compatible con PostgreSQL real y escenarios concurrentes.

.NET 10 es la línea base de implementación inicial y es LTS; la política oficial consultada incluye ASP.NET Core y fija fin de soporte el **2028-11-14**. La vida útil de GENGXIN excederá una release: SDK/runtime deberán mantenerse actualizados dentro de versiones soportadas y deberá planearse la evolución correspondiente. LTS y este freeze no significan fijar indefinidamente un patch. SDK, patch e imágenes concretas se fijarán al materializar el stack, sin previews como base productiva.

### 6.1 Comparación acotada al núcleo GENGXIN

| Criterio | ASP.NET Core / C# | FastAPI / Python | NestJS / TypeScript |
| --- | --- | --- | --- |
| Transacciones y PostgreSQL | Npgsql, tipos fuertes y SQL explícito encajan con locks, ledgers y fases A/B/C. | Viable con driver PostgreSQL y SQL explícito; requiere disciplina en tipos, `Decimal`, validación y límites transaccionales. | Viable con driver PostgreSQL y SQL explícito; requiere tratamiento deliberado de NUMERIC/BIGINT y no usar `number` como autoridad monetaria. |
| Mantenimiento y skills | C#, SQL PostgreSQL, async, ASP.NET y Windows/XAML para desktop; concentra backend/POS en un ecosistema. | Python, SQL, typing/validación y operación ASGI; añade un ecosistema distinto del POS C#. | TypeScript/Node/SQL; comparte lenguaje con Web, pero no con POS C#, y exige controles runtime pese al tipado TS. |
| Docker y seguridad | Contenedor Linux; primitivas integradas de auth/config/DI/logging. Proveedor y políticas concretas siguen pendientes. | Docker y seguridad viables; ensamblar/adoptar los componentes de auth/config/observabilidad correspondientes. | Docker y seguridad viables; guards/validation/auth y dependencias deben configurarse conforme al contrato. |
| Integración desktop | Cliente/contratos C# y tooling compartibles con WinUI/WPF. | OpenAPI sigue permitiendo cliente C#, sin compartir ecosistema backend. | OpenAPI sigue permitiendo cliente C#, sin compartir ecosistema backend. |
| Testing y vida útil | xUnit/integración; ciclo LTS explícito y ruta de actualización .NET. | pytest/integración son viables; mantener framework, Python y dependencias soportados. | Ecosistema de tests viable; mantener Node/framework/dependencias soportados. |

Los tres pueden implementar la semántica congelada; no se declara superioridad transaccional automática de un framework. Se ratifica ASP.NET Core por control tipado del núcleo y coherencia con el POS C#, además de su ciclo de mantenimiento. El repositorio no acredita experiencia real del equipo en ninguno: asignar capacidad de mantenimiento es trabajo de planificación, no evidencia inventada de un blocker técnico.

## 7. PostgreSQL como motor y PostgreSQL 17 como runtime del MVP

### 7.1 FROZEN EXISTENTE: motor y modelo físico

- **Motor objetivo:** PostgreSQL.
- **Modelo físico vigente:** db-4, **VALIDADO / CONGELADO** por A2/A3.
- **Alcance físico congelado:** tipos, constraints, ledgers, precisión e invariantes físicas de db-4, incluidos BIGINT IDENTITY internos, UUID públicos donde existen, TIMESTAMPTZ, NUMERIC(18,2)/(18,4)/(18,6) y saldos reconciliables.

Este freeze no fija una major runtime para siempre. No se modifica db-4 ni se crea db-5.

### 7.2 FROZEN EN TECH-STACK v0.1: major runtime

**PostgreSQL 17 como major runtime del MVP.** Se ratifica esta línea porque db-4 fue validado realmente sobre PostgreSQL **17.11**, I1 utiliza `postgres:17`, no existe una necesidad funcional o técnica actual de saltar a PostgreSQL 18 y hacerlo introduciría una migración de major antes de iniciar la implementación.

PostgreSQL 17.11 es evidencia de validación, no un patch congelado para siempre. Los upgrades compatibles dentro de la línea 17 deben mantenerse y validarse. Una futura evolución de major PostgreSQL no crea automáticamente db-5, y cambiar la major runtime no autoriza modificar silenciosamente el modelo físico congelado.

La base lógica central contiene existencias por sucursal y reposición por `branch/product/channel`; no se particiona la autoridad por una base local de cada tienda. Solo el backend y la administración técnica autorizada acceden a PostgreSQL. Web/POS no reciben credenciales ni conectan directamente.

## 8. Acceso a datos: Npgsql y SQL transaccional explícito

**Npgsql + SQL PostgreSQL explícito: FROZEN EN TECH-STACK v0.1** para el núcleo transaccional. Para `CONFIRM_SALE`, `CONFIRM_RETURN`, `CONFIRM_ORDER` y `CONFIRM_PURCHASE`, usar SQL parametrizado y control visible de conexión, transacción y secuencia de operaciones.

El diseño futuro debe mostrar claramente:

- `BEGIN/COMMIT/ROLLBACK` equivalentes mediante la API transaccional de Npgsql; cada fase tiene la frontera definida por su contrato, con una misma conexión/transacción para sus escrituras atómicas.
- `READ COMMITTED` y modos exactos de locks: `SELECT ... FOR UPDATE`, `FOR KEY SHARE` y advisory locks **solo donde el contrato los exige**.
- Orden determinista entre recursos y dentro de cada conjunto; distinción entre discovery y lectura/recomputación autoritativa.
- `idempotency_keys`, lease/status/hash, replay y reconciliation; un retry técnico genérico no puede repetir efectos ignorando el lifecycle.
- Escrituras de documentos, ledgers, posiciones/saldos y auditoría atómicas; generación de folios con `document_sequences` exclusivamente en el flujo al que pertenece.
- Cálculo decimal, snapshots históricos y reconciliación de inventario/reposición, incluido `SUM(detail)=fulfilled` en T4.

| Command | Diferencia contractual que el acceso a datos debe conservar |
| --- | --- |
| `CONFIRM_SALE` | Advisory lock por branch/operación lógica, búsqueda/reconciliación de venta existente, inventario/caja/reposición y folio VEN en su transacción. |
| `CONFIRM_RETURN` | Advisory lock de devolución, venta/líneas originales bloqueadas, RESTOCK y caja condicional; folio DEV. |
| `CONFIRM_ORDER` | Mutex del DRAFT persistido; `sale_items FOR KEY SHARE` antes de positions. No crear folio ni tocar secuencias al confirmar. |
| `CONFIRM_PURCHASE` | Pedido → compra → sale_items → inventario → positions → allocations según T4. SAFE/detail/costo/release atómicos. Sin nuevo advisory lock ni secuencia COM en confirmación. |

Esta tabla es orientación de implementación, no copia exhaustiva ni sustituto del orden exacto de T1–T4.

**EF Core NO ES REQUISITO PARA EL NÚCLEO TRANSACCIONAL.** Puede evaluarse después para CRUD/queries administrativas; no será autoridad sobre transacciones críticas, locks ni evolución automática del schema congelado. Dapper puede evaluarse como ayuda de mapping. **EF Core/Dapper = PENDING**, sin selección definitiva ni dependencia obligatoria en este hito. Cualquier helper futuro debe respetar la transacción explícita y los mutexes del agregado incluso al editar un DRAFT administrativo.

## 9. Web frontend

**Estado: FROZEN EN TECH-STACK v0.1.** SPA React para operación logística/administrativa autenticada; no existe necesidad concreta de SSR en el alcance actual.

| Tecnología | Papel concreto |
| --- | --- |
| React 19 | UI de dashboard, formularios, consultas y operaciones administrativas |
| TypeScript | Tipado frontend y de contratos públicos; no reemplaza validación de servidor |
| Vite 8 | Dev server y build de assets; no servidor productivo de negocio |
| Tailwind CSS 4 | Layout responsive, utilidades y design tokens del proyecto |
| shadcn/ui | Componentes incorporados/controlados por el proyecto, adaptados a sus flujos |
| TanStack Query v5 | Server state, cache, mutations, invalidación/refetch; el cache no es stock ni dominio autoritativo |
| React Router | Routing de la SPA; no añade un backend separado |

Desktop/laptop, tablet y celular son targets del mismo diseño Web Logistics: navegación adaptable, formularios utilizables con touch/teclado y proyecciones legibles de tablas/reportes. La matriz concreta de navegadores y versiones mínimas se cerrará antes del piloto; no se promete compatibilidad con cualquier navegador antiguo.

Los tipos generados deben conservar los **strings decimales** de P5/P7. No convertir dinero/cantidades wire en `number` autoritativo. Una previsualización local no reemplaza pricing, impuestos, permisos ni confirmación del backend. TanStack Query no autoriza retries automáticos ni cola offline de commands críticos: sus mutations deben adoptar la política específica de key/precondiciones.

No seleccionar Next.js sin una necesidad posterior documentada de SSR; no introducir Redux por defecto. Estado UI local y server state tienen responsabilidades distintas. El freeze establece React 19, TypeScript, Vite 8, Tailwind CSS 4, shadcn/ui, TanStack Query v5 y React Router, no un patch eterno de cada paquete. Node.js será tooling de desarrollo/build, no runtime obligatorio del servidor Web productivo ni del POS. Vite 8 requiere Node compatible (mínimos publicados 20.19+/22.12+); seleccionar una línea Node **todavía soportada** y fijar package manager/lockfile es una decisión de implementación posterior.

## 10. POS Desktop: WinUI 3 vs WPF

```text
POS_DESKTOP_FRAMEWORK = PENDING
```

Candidatos primarios: **WinUI 3 + C# + .NET** y **WPF + C# + .NET moderno**. Evaluar .NET 10 donde la combinación SO/SDK/driver lo soporte; no se selecciona .NET Framework clásico por costumbre ni se congela aún runtime/Windows App SDK desktop concreto.

El repo contiene [el borrador controlado de inventario POS](pos-hardware-inventory-v0.1.md), pero sus datos reales de PCs, edición/build de Windows, impresora/scanner/cajón, drivers, SDKs y arquitecturas permanecen pendientes; tampoco existen mediciones/POC. La plantilla documental basta para organizar evidencia, **no para declarar ganador**.

| Criterio de este POS | WinUI 3 | WPF |
| --- | --- | --- |
| Windows 10/11 | WinUI 3 declara compatibilidad desde Windows 10 1809; soporte efectivo exige intersección de Windows, Windows App SDK, .NET y dependencias. | Windows-only en .NET moderno; verificar matriz .NET/SO y drivers. No asumir que toda edición de Windows 10 sigue soportada. |
| Estabilidad y madurez | Canal Stable apto para producción; probar regresiones concretas de UI/hardware/packaging. Menor trayectoria que WPF, no implica inviabilidad. | Trayectoria amplia en aplicaciones desktop empresariales, XAML, input/binding y tooling. Madurez no sustituye prueba del equipo objetivo. |
| Ciclo de soporte/mantenimiento por años | Añade lifecycle de Windows App SDK independiente de .NET LTS; prever actualizaciones del SDK y SO. | WPF moderno forma parte del stack .NET Desktop; mantener runtime/SO/dependencias sin Windows App SDK obligatorio. Ninguna opción evita actualizaciones. |
| C#/.NET y API central | Cliente HTTP/DTOs C#, lógica de presentación aislable y adaptadores nativos. | Mismo encaje con C#/HTTP/DTOs; mismo límite de reglas críticas en backend. |
| Impresora térmica/raw printing/ESC/POS | Usar adaptador de fabricante o interop Win32/spooler si aplica. API/UI de impresión no garantiza silent/raw ni compatibilidad ESC/POS. | Impresión documental WPF disponible; para ticket raw/ESC/POS sigue requiriendo adaptador/spooler/SDK real. No confundir PrintDialog con impresión directa. |
| USB y serial/COM | Acceso mediante APIs/bibliotecas nativas/.NET compatibles, sujeto a device, driver y modalidad de despliegue. | Misma necesidad; interop convencional disponible. USB puede ser cola de impresión, HID, puerto virtual o SDK, no un protocolo uniforme. |
| Lector y cajón | HID/keyboard wedge, COM o SDK según scanner; cajón mediante pulso de impresora/ESC/POS o interfaz real. Validar ambos. | Igual dependencia de mecanismos reales. El framework visual no determina el protocolo del cajón. |
| Drivers y DLLs de fabricante | Validar bitness x86/x64/ARM, ABI, threading y restricciones en el paquete elegido. | Validar lo mismo; SDK legacy puede condicionar arquitectura/runtime, no asumir integración resuelta por madurez. |
| Foco/teclado | Probar captura rápida, sufijo del scanner, atajos, modal de cobro y recuperación de foco. | Sistema de input/commands maduro; ejecutar los mismos casos para demostrar ausencia de lecturas perdidas/duplicadas. |
| Rendimiento y arranque | Medir tiempo hasta primera lectura, memoria y latencia con packaging/runtime objetivo; no inferir velocidad por modernidad. | Medir la misma carga y PC; no afirmar automáticamente menor consumo/arranque. |
| Distribución | Packaged/unpackaged y runtime compartido/self-contained; Windows App SDK añade dependencias/inicialización que validar en PC limpio. | Framework-dependent/self-contained y opciones de instalación/paquete; normalmente menos dependencias de framework UI si no se añade App SDK. |
| Actualización en cuatro sucursales | Mecanismo no elegido. Evaluar instalación remota, permisos, firma, conservación de contexto y actualización del App SDK. | Mecanismo no elegido. Evaluar los mismos puntos para runtime/app y operación cotidiana. |
| Debugging y experiencia de desarrollo | XAML/C#, ciclo de ventanas y tooling WinUI/App SDK; medir fricción de diagnóstico/despliegue con el equipo. | XAML/C#, bindings y tooling con experiencia acumulada; requiere skills WPF y disciplina de separación UI/lógica. |
| Testing | xUnit para lógica desacoplada; UI/automatización Windows y hardware real se evalúan en spike. | Igual separación; disponibilidad de tooling no demuestra robustez E2E de foco/hardware. |
| Apariencia moderna | Fluent y controles modernos de origen; ventaja de UX visual, no de integridad del cobro. | Puede modernizarse mediante styles/templates; implica trabajo visual adicional, no obliga a cambiar de framework. |
| Coste de soporte operativo | Evaluar incidentes posibles de App SDK, instalación y herramientas en todas las configuraciones reales. | Evaluar instalación/diagnóstico y mantenimiento de estilos/adaptadores en las mismas configuraciones. |

**Resultado del análisis:** WPF ofrece una hipótesis razonable de menor complejidad operacional por madurez y ausencia de Windows App SDK obligatorio; WinUI 3 ofrece UI moderna nativa y sigue siendo candidato válido. Son hipótesis a contrastar, no una selección implícita. La recomendación general de Microsoft para nuevas apps WinUI no reemplaza la evidencia del POS GENGXIN.

En septiembre de 2026, la documentación .NET consultada limita soporte Windows 10 a ediciones LTSC/Enterprise según su ciclo; Windows App SDK también condiciona soporte a un Windows soportado y su propio servicing. Registrar edición, build, canal y arquitectura de cada caja es necesario para decidir, no declarar Windows 10 genérico como plataforma soportada por años. Una limitación de PCs/driver afecta la decisión desktop; no bloquea por sí misma ASP.NET central en Linux.

Tauri/Electron solo quedan como comparación secundaria si ambos candidatos nativos encuentran un impedimento comprobado: Tauri añade Rust/WebView/interop y Electron añade Chromium/Node/native modules y su servicing. Reutilizar UI web no prueba ventaja para foco, drivers, impresión o soporte de cuatro sucursales. Electron no es primera opción; ninguna alternativa reintroduce hardware en la Web Logística.

## 11. POC/spike obligatorio antes del freeze desktop

**Se documenta el hito; no se implementa ahora.** Ejecutar un spike comparativo mínimo en WinUI 3 y WPF con el mismo equipo/hardware real, usando ticket de prueba y una pantalla mínima de captura. No necesita ventas reales, backend productivo ni SQL nuevo.

### 11.1 Insumos

Inventariar las configuraciones realmente presentes en las cuatro sucursales: Windows edición/build/ciclo, CPU/RAM/arquitectura, permisos de usuario, scanner/modelo/modo/sufijo, impresora/modelo/ancho/protocolo/driver/SDK, cajón y conexión efectiva, COM/USB/puerto virtual si aplica. Probar al menos una máquina representativa por configuración distinta; no asumir equipos homogéneos.

### 11.2 Validaciones mínimas

1. **Iniciar aplicación:** instalar y arrancar en un PC limpio/representativo bajo el usuario operativo; registrar requisitos y tiempo hasta estar lista para leer.
2. **Capturar scanner:** leer códigos por el mecanismo real; verificar exactitud, sufijos, repetición rápida y foco después de búsqueda/modal/cobro simulado.
3. **Imprimir ticket de prueba:** enviar a la térmica real; validar ancho, texto/accentos, importes, avance/corte y vía de envío.
4. **Impresión directa sin diálogo:** demostrarla si hardware/driver la permite. Identificar spooler raw, ESC/POS o SDK usado; si no es posible, documentar la limitación y resolver el requisito antes de elegir framework.
5. **Abrir cajón:** usar el mecanismo esperado real, incluido pulso vía impresora si corresponde; registrar resultado, dependencias y permisos.
6. **Detectar/usar COM o USB:** cuando el hardware objetivo lo necesite; documentar puerto/driver/arquitectura y reconexión. Si no aplica, justificarlo con inventario, no omitirlo por conveniencia.

Además, comprobar desconexión/reconexión, impresora no disponible, error visible sin congelar UI, reimpresión de prueba sin duplicar operación y distribución de una build de prueba a otro PC. Ensayar al menos actualización/reinstalación de prueba que preserve configuración de terminal; no obliga a elegir todavía el updater definitivo.

### 11.3 Evidencia y criterio de cierre

- Matriz PASS/FAIL/N/A justificado por candidato y configuración, con versiones de SO/runtime/App SDK/driver y mecanismo de cada periférico.
- Medidas comparables de arranque, primera lectura, latencia de impresión, memoria y fricción de instalación/debugging; fijar criterios operativos aceptables antes de ejecutar el spike, sin inventar benchmarks en este documento.
- Dependencias/limitaciones y coste de soporte; no aprobar un candidato que requiera trasladar hardware al navegador.
- Elegir framework con base en hardware funcional, soporte de SO/dependencias y despliegue mantenible; apariencia moderna es un criterio secundario al flujo de caja fiable.
- Si ambos pasan, documentar el motivo de selección. Si ninguno pasa, mantener `POS_DESKTOP_FRAMEWORK = PENDING` y registrar el impedimento concreto antes de ampliar alternativas.

## 12. API común: REST + JSON + OpenAPI

**REST + JSON y OpenAPI: FROZEN EN TECH-STACK v0.1 como dirección tecnológica.** El transporte HTTP/JSON de P5 y la frontera concreta P7 ya son **FROZEN EXISTENTE**; no se vuelven a diseñar aquí.

El backend es autoridad para permisos, pricing, descuentos, impuestos, stock, idempotencia, transacciones, folios, ledgers y reglas de dominio. Los clientes gestionan interacción/input/estado de presentación y envían commands; no duplican reglas críticas para tomar decisiones autoritativas locales.

La implementación y OpenAPI deberán reflejar, entre otras restricciones ya cerradas:

- Prefijo `/api/v1`; `POST /api/v1/sales/confirm` y éxito `200` para confirmación/replay/reconciliación según P7.
- Decimales exactos como **JSON strings canónicos** con scale semántico; timestamps RFC 3339, UTC `Z` para autoritativos; enums con símbolos de dominio exactos.
- Envelopes `{ "data": ... }` / `{ "error": ... }`, mapping HTTP, prioridad de códigos y `X-Request-Id` conforme a P5–P7.
- `Idempotency-Key` en header, distinto de `client_operation_id`; fingerprints en payload solo en commands que los requieran.
- Referencias públicas resueltas dentro de scope confiable; no PK internas ni inferencia de tenant desde un lookup global del selector enviado.
- Replay interno `response_body` distinto de DTO público. No exportar JSONB interno por generar una API automáticamente.

OpenAPI será el contrato **técnico** generado/documentado a partir de la API y revisado contra las autoridades documentales. Generación no autoriza divergencia semántica respecto de P5–P7 ni convierte tablas en endpoints CRUD. No se fija aún generador, versión OpenAPI o librería de documentación.

No existen todavía Command APIs concretas equivalentes para `CONFIRM_RETURN`, `CONFIRM_ORDER` y `CONFIRM_PURCHASE` en el árbol inspeccionado. Sus contratos transaccionales sí están congelados. Sus DTOs/rutas públicas requieren hitos posteriores antes de implementar esas fronteras; no se inventan aquí.

## 13. Contratos compartidos: dirección deseada

```text
Backend C# (API y DTOs públicos conformes a contratos documentales)
    -> OpenAPI
        -> Cliente/tipos TypeScript
            -> React Web

API / Contracts C# (contratos públicos de transporte)
    -> Cliente API C# / POS Desktop C#
```

**Estado: FROZEN EN TECH-STACK v0.1** para la dirección OpenAPI → tipos/cliente TypeScript para Web y contratos/cliente C# para POS; **generación concreta = PENDING**. No elegir NSwag, OpenAPI Generator ni herramienta equivalente en este micro-hito.

Compartir contrato público no significa compartir entidades de persistencia, credenciales, PK, SQL ni servicios de negocio internos. Web y POS consumen los mismos resultados públicos; no se da al POS acceso directo a repositorios .NET del servidor. El cliente C# puede ser generado o mantener contratos públicos compartidos: el mecanismo queda diferido. Decimales wire siguen como strings aunque el consumidor use `decimal` internamente con validación explícita.

## 14. Docker y reverse proxy

**Docker Compose para servidor: FROZEN EN TECH-STACK v0.1.** I1 acredita uso existente para PostgreSQL; este hito ratifica la topología conceptual `postgres` + `api` + `web` + `reverse proxy`, todavía no materializada. Docker es infraestructura central, **no runtime del POS**.

Topología conceptual de la baseline para un servidor/VPS:

```text
Docker Compose (servidor central)
  +-- postgres      PostgreSQL 17 (servicio existente: db)
  +-- api           ASP.NET Core 10
  +-- web           assets React production build, servidos internamente
  +-- reverse proxy Caddy, entrada HTTPS y routing Web/API

Windows en sucursales: POS instalado nativamente, fuera de Docker
```

Los nombres anteriores son conceptuales; no se renombra `db`. El build Web es estático, no `vite dev` en producción. La decisión de servir sus assets en un servicio interno separado o directamente desde Caddy es un detalle no bloqueante; no añade un segundo backend.

**Caddy: FROZEN EN TECH-STACK v0.1** para HTTPS, certificados TLS/renovación, redirect HTTP → HTTPS y reverse proxy/routing Web/API. Su automatización reduce carga operativa de TLS en un único servidor. Requiere dominio/DNS, validación de certificados y almacenamiento persistente de su estado; no basta con elegir la imagen. No se implementa configuración en este hito.

El proxy debe conservar `/api/v1` y sus headers; el fallback de routing SPA no debe absorber rutas API. La futura configuración de errores/proxy no debe sustituir los contratos API con respuestas incompatibles. En producción, PostgreSQL queda en red interna, sin puerto público a clientes; API/web internos se exponen por la entrada HTTPS. I1 publica 5432 al host: **observación para futura configuración de despliegue**, no una instrucción para modificar Compose aquí.

## 15. Deployment MVP

**Estado: FROZEN EN TECH-STACK v0.1.** Un servidor/VPS central para el MVP, con Docker Compose, API monolítica, PostgreSQL 17 central, Web estática y Caddy. No Kubernetes ni microservices distribuidos.

- Cuatro sucursales: `POS Desktop -> HTTPS -> API central`.
- Acceso administrativo: `browser Desktop/Tablet/Mobile -> HTTPS -> infraestructura central`.
- POS: distribución/instalación nativa Windows; estrategia concreta de actualización pendiente.
- Dev/test/prod separados al implementarse; volumen PostgreSQL persistente y configuración/secretos externos a imágenes/código.
- Backups externos al servidor principal y restauración probada, healthchecks, logs estructurados y monitoreo básico conforme a A1 §§17,22,36. Un volumen Docker no es un backup.

Un solo servidor no implica alta disponibilidad: la caída central afecta nuevas operaciones de todas las sucursales. La baseline no congela proveedor VPS, región, sizing, dominio, SLA, RPO/RTO, SO de servidor ni imágenes concretas; esos parámetros, alertas y recuperación se cerrarán antes de producción.

CFDI se integra mediante adaptador del backend. PAC, correo, almacenamiento de XML/PDF y protección de CSD/secretos se seleccionarán en sus hitos; no se inventan proveedores aquí ni se hace depender venta/devolución operativa del éxito fiscal posterior.

## 16. Seguridad arquitectónica y contexto

Restricciones heredadas de A1/A3/T1–T4/P5–P7:

- El frontend no autoriza por sí mismo; **ocultar botones NO constituye autorización**. El backend aplica permisos funcionales y acceso a sucursal, no nombres de rol ni bypass implícito ADMIN.
- Actor, terminal cuando aplique y tenant/business provienen del contexto confiable. Branch y pertenencias se resuelven/validan server-side según el command; no son autoridad libre del cliente.
- POS y Web nunca conectan directamente a PostgreSQL. Solo contratos públicos, sin exposición de PK/FK internas, SQL, stack, secretos ni existencia cross-tenant.
- HTTPS obligatorio en producción; secretos fuera del repo y del bundle Web/instalador como credenciales de servidor. Credenciales de dispositivo se provisionan/guardan de forma segura, no se incrustan compartidas en cada instalación.
- Logs/auditoría seguros, contraseñas con hash seguro, expiración/revocación de sesiones y cifrado/control de secretos fiscales conforme a A1; tecnologías concretas pendientes.

**Autenticación != autorización actual del command.** No imponer middleware/policy global que adelante checks de negocio y rompa replay/reconciliación. P7 exige frontera HTTP autenticada mínima y business/terminal conocidos antes de historia, pero conserva las validaciones actuales (`ACTIVE`, `user_branches`, `SALES_CONFIRM`, caja/catálogos) después de buscar venta existente. Para SAME KEY terminal no reautoriza/revalida el command. T3/T4 tienen reglas diferentes para nueva key/recovery; no generalizarlas.

Proveedor/mecanismo de identidad, cookies/bearer/OIDC/JWT, refresh y credencial de terminal siguen **PENDING**. ASP.NET no implica seleccionar ASP.NET Core Identity ni agregar su schema mediante migraciones automáticas: cualquier diseño deberá encajar con `users`, roles/permisos y terminales de db-4 y con la frontera P7.

## 17. Estructura base del repositorio

**Estado: FROZEN EN TECH-STACK v0.1** para la separación de tres aplicaciones; nombres internos finales diferidos.

```text
database/             schemas y validation existentes, preservados/versionados
docs/                 autoridades funcionales, físicas, transaccionales, API y arquitectura
src/                  futuro, al materializar esta baseline
  backend/            único backend desplegable, módulos internos
  pos-desktop/        UI Windows, cliente API y adaptadores de hardware
  web/                SPA logística/administrativa responsive
```

La ubicación concreta de contratos públicos, tests, scripts de operación o infraestructura se decidirá al materializar proyectos. No se crean ahora `src/`, solución .NET, proyectos React/desktop ni carpetas de módulos; no se mueven SQL, documentos o Compose actuales. La estructura final deberá conservar estas fronteras al implementar la baseline.

## 18. Testing base y verificación del núcleo

**Herramientas base: FROZEN EN TECH-STACK v0.1.** No se implementan ni ejecutan nuevos tests en este hito documental; el tooling UI/hardware desktop permanece `PENDING`.

| Área | Baseline |
| --- | --- |
| Backend | xUnit para lógica; integration tests contra PostgreSQL 17 real, preferentemente instancia/contenedor aislado con db-4 |
| Web | Vitest; React Testing Library cuando el comportamiento de componentes lo requiera; Playwright para E2E de operaciones administrativas |
| Desktop | xUnit para lógica/presentación desacoplada; estrategia concreta de UI Windows/hardware después de elegir framework, con hardware real en POC/piloto |

T1–T4 requieren integration/**concurrency tests**; un mock o base in-memory no reproduce NUMERIC, row/advisory locks, constraints, triggers, fases persistentes ni COMMIT desconocido. Mantener fixtures controlados y aislamiento de bases de prueba, sin modificar db-4 para facilitar tests.

Casos mínimos de alcance posterior, trazables a esos contratos:

- Dos cajas sobre última existencia, folios concurrentes, doble clic y dos keys para la misma venta/devolución lógica.
- Cantidad retornable, RESTOCK/costo y DAMAGED, caja condicional y conversiones concurrentes de cotización.
- Edición vs confirmación de DRAFT, stale de fingerprint/demanda, FIFO reservado completo, pedido sin líneas y ausencia de reserva parcial/reclasificación silenciosa.
- Compra vs venta/devolución/pedido/otra compra; balance inexistente; SAFE/predecessor pending; detalle many-to-many; terminalización y reconciliación de ledgers/positions/costo.
- Fallos antes de COMMIT, resultado desconocido, recovery seguro, replay COMPLETED/FAILED, ausencia de efectos duplicados y respeto de diferencias de autorización por command.
- Contrato HTTP congelado de venta: strings/scale, canonicalización, referencias scoped, envelopes/códigos/headers y resumen histórico sin revalidación actual.

Playwright cubre la Web; no se presupone que automatiza WinUI/WPF o prueba físicamente el cajón. Tooling de contenedores/UI, organización de tests y métricas concretas se seleccionarán posteriormente.

## 19. Tabla controlada de decisiones

Los estados controlados son:

| Estado | Definición |
| --- | --- |
| **FROZEN EXISTENTE** | Autoridad o decisión ya congelada fuera y antes de este documento; requiere la evidencia indicada. |
| **FROZEN EN TECH-STACK v0.1** | Tecnología o decisión formalmente ratificada por este documento para implementación. No puede reinterpretarse silenciosamente. |
| **PENDING** | Decisión todavía no seleccionada; no invalida freezes independientes ni debe tratarse como elegida. |
| **FUERA DE ALCANCE** | Elemento que no forma parte del MVP o del micro-hito indicado. |

El estado parcial del documento permite implementar los bloques ratificados sin afirmar que el stack desktop está cerrado.

| Área | Tecnología / decisión | Estado | Motivo |
| --- | --- | --- | --- |
| Arquitectura lógica | MODULAR MONOLITH, un backend desplegable | FROZEN EXISTENTE | A1 §30 la declara congelada; evita dividir transacciones entre servicios |
| Canales funcionales | Escritorio operacional y panel Web consumen lógica/API central | FROZEN EXISTENTE | A1 §§1.3–1.4 y freeze funcional §30 |
| Database engine / physical model | PostgreSQL + db-4 | FROZEN EXISTENTE | PostgreSQL es el motor objetivo y el modelo físico db-4 está VALIDADO/CONGELADO; no se reabre el schema ni se crea db-5 |
| Database runtime major | PostgreSQL 17 | FROZEN EN TECH-STACK v0.1 | db-4 fue validado sobre 17.11 e I1 usa `postgres:17`; patch mantenible, sin motivo actual para cambiar de major |
| Núcleo de dominio | T1–T4: atomicidad, locks, idempotencia, folios/ledgers | FROZEN EXISTENTE | Contratos transaccionales VALIDADO/CONGELADO, no redefinidos aquí |
| Transporte/API existente | P5 Transport, P6 errores aditivos, P7 CONFIRM_SALE | FROZEN EXISTENTE | Freeze explícito y alcance público específico |
| Operación central | ONLINE-FIRST CENTRALIZADO, sin confirmaciones offline MVP | FROZEN EN TECH-STACK v0.1 | Autoridad central; sin ventas/stock autoritativo offline ni sincronización distribuida MVP |
| Frontera hardware | Hardware/impresión POS solo en desktop; Web logística responsive | FROZEN EN TECH-STACK v0.1 | Separa Web/POS sin puente local de scanner/impresora/cajón/COM/USB en navegador |
| Backend | ASP.NET Core 10 + C# + .NET 10 LTS | FROZEN EN TECH-STACK v0.1 | Primera implementación tipada, API central, Npgsql y contenedor Linux viable |
| Driver/transacciones | Npgsql + SQL PostgreSQL explícito | FROZEN EN TECH-STACK v0.1 | Control visible de fases, locks, idempotencia y reconciliación según T1–T4 |
| ORM/helper administrativo | EF Core o Dapper opcionales | PENDING | No necesarios para núcleo; evaluar valor real en CRUD/mapping |
| Web UI | React 19 + TypeScript | FROZEN EN TECH-STACK v0.1 | UI administrativa tipada con contratos públicos |
| Web build/estilos | Vite 8 + Tailwind CSS 4 + shadcn/ui | FROZEN EN TECH-STACK v0.1 | SPA/build y responsive/componentes controlados |
| Web state/routing | TanStack Query v5 + React Router | FROZEN EN TECH-STACK v0.1 | Server state y routing SPA sin reglas críticas locales |
| Targets Web | Desktop/laptop, Tablet, Mobile por navegador | FROZEN EN TECH-STACK v0.1 | Una Web responsive; no implica POS móvil |
| Desktop framework | WinUI 3 vs WPF; `POS_DESKTOP_FRAMEWORK = PENDING` | PENDING | Faltan SO/hardware real, POC y evidencia de distribución |
| Desktop actualización | Mecanismo concreto de distribución/update | PENDING | Debe cerrar instalación/firma/permisos y recuperación después del framework |
| API tecnológica | REST + JSON, OpenAPI conforme a autoridades | FROZEN EN TECH-STACK v0.1 | API común; automatización no redefine frontera congelada |
| Contratos compartidos | OpenAPI → TS/Web; API/Contracts C# → POS | FROZEN EN TECH-STACK v0.1 | Evitar divergencia de DTOs sin compartir persistencia/reglas internas |
| Generación de clientes | Generador y estrategia concreta C#/TS | PENDING | No seleccionar NSwag/OpenAPI Generator en este hito |
| Contenedores | Docker Compose central, POS nativo Windows | FROZEN EN TECH-STACK v0.1 | Topología conceptual `postgres` + `api` + `web` + `reverse proxy`; no modifica I1 todavía |
| Deployment base | Un servidor/VPS central + API/PostgreSQL/Web/Caddy | FROZEN EN TECH-STACK v0.1 | Baseline MVP central; proveedor, región, dominio, sizing y SLA quedan pendientes |
| Reverse proxy | Caddy | FROZEN EN TECH-STACK v0.1 | HTTPS/TLS/renovación, redirect y routing Web/API |
| Hosting | Proveedor/VPS/ubicación/dominio concretos | PENDING | Detalle operativo sin impedimento encontrado para Compose |
| Autenticación | Proveedor/mecanismo/contexto/credencial de terminal | PENDING | Respetar db-4/P7 sin inventar proveedor ni policies incompatibles |
| Tests backend/desktop | xUnit; integración backend contra PostgreSQL 17 real/aislado | FROZEN EN TECH-STACK v0.1 | Lógica desacoplada y concurrencia PostgreSQL real |
| Tests Web | Vitest, React Testing Library cuando aplique, Playwright | FROZEN EN TECH-STACK v0.1 | Componentes y flujos Web E2E |
| Tests UI/hardware desktop | Tooling concreto | PENDING | Depende de framework/hardware; POC previo requerido |
| Estructura de aplicaciones | `src/backend`, `src/pos-desktop`, `src/web` conceptual | FROZEN EN TECH-STACK v0.1 | Preserva repo documental y separación Web/POS/backend; nombres internos finales diferidos |
| Offline distribuido, móvil nativo, microservices/Kubernetes | No seleccionados para MVP | FUERA DE ALCANCE | A1 y ausencia de necesidad que compense complejidad |
| Código/proyectos/configuración nuevos en este hito | Implementación y materialización del stack | FUERA DE ALCANCE | Esta ratificación modifica solo documentación; el siguiente hito puede iniciar código backend |

## 20. Decisiones descartadas / no seleccionadas para el MVP

No se presentan como tecnologías malas; no corresponden a la dirección actual de GENGXIN:

- **Microservices y Kubernetes:** no necesarios para cuatro sucursales y transacciones centrales; monolito modular ya congelado.
- **App móvil nativa:** celular usa Web Logística responsive; A1 la excluye del MVP.
- **Next.js/SSR:** no hay necesidad concreta actual; SPA Vite suficiente.
- **Flutter como UI principal:** no seleccionado para Web ni desktop; no se busca una UI única multiplataforma.
- **Electron como primera opción:** se prioriza comparación WinUI/WPF y hardware Windows; Tauri queda también como alternativa secundaria no seleccionada.
- **Base operativa offline distribuida:** fuera del MVP; sin sincronización bidireccional, folios/stock autoritativos locales o ventas diferidas offline.
- **PostgreSQL directo desde clientes:** incompatible con frontera central de permisos/dominio.
- **Redux por defecto, EF Core obligatorio o generadores de clientes elegidos por costumbre:** no se introducen como requisitos.

## 21. Consistencia con documentos existentes y observaciones

No se encontró contradicción material entre las decisiones ahora ratificadas para backend/Web/online-first y una autoridad congelada previa. Sí hay diferencias de alcance y metadatos históricos que deben conservarse visibles:

1. **Monolito modular ya congelado:** A1 §30 es más fuerte que una simple recomendación de §17.4. Por eso se registra FROZEN EXISTENTE; la baseline tecnológica compatible se ratifica aquí y solo el framework desktop principal continúa pendiente.
2. **Pendientes físicos históricos:** A1 §§26,33,37 habla de PostgreSQL por evaluar, 50 tablas y schema inicial pendiente. A3 documenta db-4 validado en PostgreSQL 17.11, con 52 tablas. No se reabre db-4 ni se regenera SQL por esas listas históricas.
3. **Pendientes de compra en el documento físico:** A3 §§8,12 conserva como futuro integrar T4, inventory/costo/locks/READ COMMITTED. T4 actual §§1,43 ya los cierra y está congelado. Son desfases de seguimiento documental, no blockers del stack ni gaps reabiertos de compra.
4. **API base vs hitos posteriores:** P1–P4 mencionan wire, HTTP, referencias y política pública de venta pendientes; P5–P7 ya cierran lo correspondiente a su alcance. Los cuatro borradores no se etiquetan FROZEN ni se universaliza la frontera de venta a devolución/pedido/compra.
5. **JSON interno vs público:** D1 usa JSON numbers decimales y un ID interno de trazabilidad en snapshot persistido; P5 exige strings decimales y frontera pública sin PK. Son capas distintas, no autorización para convertir el snapshot interno a DTO. P7 no acepta `tax_snapshot` como input.
6. **Infraestructura existente vs baseline:** I1 solo tiene DB y puerto de host. Docker Compose central, API/Web y Caddy quedan congelados como dirección tecnológica, pero sus servicios, secretos productivos y red interna aún no están materializados. Freeze de decisión no equivale a deployment implementado.
7. **Selección desktop condicionada a hardware:** A1 §§11,22,26 la exige; mantener PENDING y POC respeta esa condición. No se atribuye soporte genérico Windows 10/11 sin validar edición/ciclo/driver.

Una actualización futura de referencias de seguimiento de A1/A3/P1–P4 puede hacerse donde proceda, respetando sus freezes, pero no es requisito para iniciar backend/Web. **No se modifica ningún otro archivo en este micro-hito.** Estas observaciones no permiten alterar silenciosamente semántica congelada.

## 22. Evidencia técnica externa consultada

Consultada el **2026-09-30** para soporte/compatibilidad y capacidades, no como autoridad funcional ni freeze del proyecto. Revalidar matrices de soporte al ejecutar el POC y al materializar versiones:

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core): .NET 10 LTS y ASP.NET Core, servicing y fin de soporte 2028-11-14.
- [.NET on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows): matriz de SO/runtime/arquitectura y requisitos de tooling.
- [WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/), [Windows App SDK support](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/support) y [release channels/lifecycle](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-channels): UI nativa, compatibilidad y servicing independiente.
- [WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/): WPF moderno Windows-only, input/binding y personalización.
- [Windows App SDK deployment](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/deploy-overview): packaged/unpackaged, framework-dependent/self-contained y sus dependencias.
- [Win32 raw printing](https://learn.microsoft.com/en-us/windows/win32/printdocs/sending-data-directly-to-a-printer): posibilidad de envío directo al spooler, sin acreditar compatibilidad de un modelo concreto.
- [Npgsql basic usage](https://www.npgsql.org/doc/basic-usage.html) y [Npgsql 10 release notes](https://www.npgsql.org/doc/release-notes/10.0.html): SQL parametrizado, pooling/transacciones y evolución del driver. No se congela patch del paquete aquí.
- [Vite 8 release](https://vite.dev/blog/announcing-vite8): release estable y requisitos Node para build/dev.
- [Caddy Automatic HTTPS](https://caddyserver.com/docs/automatic-https): certificados/renovación, redirect y requisitos de dominio/almacenamiento.

## 23. Control de evolución de la baseline parcialmente congelada

Toda implementación nueva debe respetar las decisiones **FROZEN EXISTENTE** y **FROZEN EN TECH-STACK v0.1**, además de las autoridades de dominio/API/DB. El estado **BASELINE DE IMPLEMENTACIÓN PARCIALMENTE FROZEN — DESKTOP PENDING** congela los bloques ratificados sin fingir que el framework desktop ya fue elegido.

Una decisión marcada **FROZEN EN TECH-STACK v0.1** no puede reinterpretarse silenciosamente durante implementación. Cualquier cambio incompatible requiere:

1. Documentar la razón y la decisión afectada.
2. Evaluar impacto en contratos, clientes, hardware, despliegue, datos, testing y mantenimiento.
3. Registrar una decisión explícita y evolucionar/versionar el documento o contrato correspondiente, conservando trazabilidad de la decisión anterior.

Este control no fija eternamente patches de SDKs, runtimes, imágenes o paquetes: deben mantenerse dentro de líneas compatibles y soportadas. Versionado de stack, modelo físico/database, major runtime PostgreSQL, transacciones y API son ejes relacionados pero distintos; cambiar la major runtime no crea una db-5 ni autoriza modificar silenciosamente el modelo físico congelado, y cambiar tecnología no autoriza una nueva semántica de command sin su propia evolución. Los `PENDING` conservan su alcance y momento de cierre sin invalidar decisiones congeladas independientes.

## 24. Pendientes y blockers después de esta baseline

### 24.1 BLOCKERS PARA CERRAR EL STACK COMPLETO

| Pendiente | Por qué bloquea | Evidencia necesaria para cierre |
| --- | --- | --- |
| Completar inventario real Windows/hardware y configuraciones únicas | El borrador [pos-hardware-inventory-v0.1.md](pos-hardware-inventory-v0.1.md) existe, pero aún requiere datos reales de las cuatro sucursales | Edición/build/ciclo/arquitectura, PCs, periféricos, drivers/SDK, permisos y combinaciones reales identificadas |
| Ejecutar POC comparativo WinUI 3 vs WPF | Impresión/scanner/cajón/COM/USB y distribución son funciones esenciales POS aún no demostradas | Matriz PASS/FAIL/N/A justificado por candidato y configuración conforme a §11 |
| Seleccionar y documentar framework Desktop | El stack completo no puede cerrarse con dos candidatos principales sin decisión | Hito documental basado en inventario/POC que cambie `POS_DESKTOP_FRAMEWORK = PENDING` por la selección aprobada |

### 24.2 BLOCKERS PARA COMENZAR BACKEND/WEB

**NINGUNO.** La baseline congelada en este documento autoriza iniciar backend y Web sin esperar el inventario, el POC o la selección del framework Desktop. Los pendientes de implementación posteriores no reabren decisiones ratificadas ni son blockers artificiales para el bootstrap.

### 24.3 DECISIONES DE IMPLEMENTACIÓN QUE NO BLOQUEAN BACKEND/WEB

| Pendiente real | Cuándo debe cerrarse / límite que debe respetar |
| --- | --- |
| Estrategia concreta de actualización/distribución POS | Después del framework y antes de piloto; firma, permisos, recuperación/reversión, actualización de dependencias y preservación de terminal/configuración. El POC sí debe demostrar viabilidad básica de instalación/update, sin elegir updater definitivo |
| Runtime/patch/packaging desktop y versión Windows App SDK si aplica | Con materialización posterior al POC, dentro de las combinaciones soportadas; no convertir compatibilidad genérica en soporte garantizado |
| EF Core/Dapper u otro helper opcional para CRUD/mapping | Cuando exista necesidad concreta; Npgsql/SQL explícito basta para empezar el núcleo y los mutexes/contratos siguen mandando |
| Generador OpenAPI y cliente C#/TS concreto | Al implementar la primera API/consumidor; DTOs, strings decimales y semántica congelada deben preservarse |
| Hosting/VPS, ubicación, dominio, SO y sizing | Antes del despliegue piloto; compatibles con Compose central/HTTPS y presupuesto operativo |
| Proveedor/mecanismo concreto de autenticación, sesiones y credencial de terminal | Antes de implementar login/contexto/auth; debe encajar con db-4 y frontera P7 sin adelantar autorización ni agregar schema ajeno automáticamente |
| SDK/patches backend, Npgsql y paquetes Web/tests, Node LTS, package manager/lockfile | Al crear proyectos, con versiones estables soportadas y builds reproducibles; no cambian las líneas de la baseline por sí mismos |
| Tooling UI/hardware desktop, contenedores de tests y automatización | Tras framework y al implementar tests; no sustituye hardware real ni concurrencia PostgreSQL |
| DTOs/rutas Command API de RETURN/ORDER/PURCHASE y demás queries/commands | Antes de implementar cada frontera; son hitos de contrato separados, no reapertura de T1–T4 ni blocker artificial del stack |
| Librerías de hash/canonicalización, parsing/serialización y precisión decimal | Antes de implementar commands; respetar P5/P7, NUMERIC, rangos/intermedios y rounding de dominio |
| Layout interno de proyectos/contratos/tests y servicio estático Web separado o assets en Caddy | Al materializar repo/deployment; preservar separación Web/POS/backend y autoridades actuales |
| Navegadores mínimos, políticas de cache/refetch y timeouts/retries por command | Antes de piloto/consumidores; responsive desde diseño, sin autoridad local ni confirmaciones offline |
| PAC/correo, XML/PDF, cifrado de CSD/secretos y contrato fiscal | Antes de implementar/validar facturación; responsabilidades del backend y límites operativos/fiscales existentes |
| Backups/retención/destino, prueba de restauración, RPO/RTO, alertas y mantenimiento | Antes de producción, conforme a A1; volumen persistente no reemplaza recuperación validada |

No hay pendientes nuevos de reglas funcionales, schema db-4, aislamiento/locks/idempotencia de T1–T4, Transport v0.1 ni CONFIRM_SALE API v0.1 creados por este micro-hito. Estos diferidos tampoco impiden comenzar backend/Web.

## 25. Implementación habilitada por esta baseline

Orden conceptual de implementación:

1. Bootstrap de solución/backend ASP.NET Core.
2. Configuración base.
3. Health endpoint.
4. Npgsql y conexión a PostgreSQL 17.
5. Infraestructura HTTP compartida.
6. Infraestructura de errores y request-id.
7. Idempotencia.
8. Vertical slice de `CONFIRM_SALE`.
9. Tests de integración y concurrencia.
10. Bootstrap de Web Logística con React.
11. Integración Web → API.

Este orden implementa la baseline; no redefine ni reemplaza contratos de dominio, database, transacciones o API. No se requiere abrir más análisis generales de arquitectura antes del primer código salvo que durante la implementación aparezca un blocker técnico real. El siguiente micro-hito previsto es `chore(backend): bootstrap ASP.NET Core application`.

### 25.1 Track Desktop en paralelo

```text
pos-hardware-inventory-v0.1.md
    -> inventario real
    -> POC WinUI 3 vs WPF
    -> selección de framework
    -> actualización posterior de tech-stack-v0.1.md
```

Este track no bloquea backend/Web. Hasta que concluya mediante un hito documental separado:

```text
POS_DESKTOP_FRAMEWORK = PENDING
```

Este documento termina como **BASELINE DE IMPLEMENTACIÓN PARCIALMENTE FROZEN — DESKTOP PENDING**. Este micro-hito solo ratifica decisiones: no crea código/proyectos, no modifica infraestructura, schema ni contratos, y no congela los patches exactos de dependencias.
