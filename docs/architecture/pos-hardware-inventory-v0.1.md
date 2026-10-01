# GENGXIN POS — Inventario de hardware y entorno Windows v0.1

| Control | Valor |
| --- | --- |
| Estado del documento | **BORRADOR CONTROLADO — NO FROZEN** |
| Versión | v0.1 |
| Fecha de revisión | 2026-09-30 |
| Alcance | PCs, Windows, periféricos y red de las cuatro sucursales para el futuro POC desktop |
| Documento complementado | [Arquitectura tecnológica y stack v0.1](tech-stack-v0.1.md) §§10, 11 y 24 |
| Decisión desktop | `POS_DESKTOP_FRAMEWORK = PENDING` |
| Implementación / pruebas | No iniciadas por este micro-hito |

Este documento registra las configuraciones reales que deberán soportar WinUI 3 o WPF. Existe específicamente para cerrar el blocker **inventario real Windows/hardware y combinaciones soportadas** de `tech-stack-v0.1.md`; llenar una fila no declara soporte y completar el inventario no selecciona framework.

No se implementa POC, aplicación, solución o proyecto; tampoco se instalan SDKs/drivers, se cambia configuración de PCs ni se realizan pruebas de hardware en este micro-hito. Todos los datos técnicos todavía no recopilados permanecen como **PENDING INVENTORY**.

## 1. Autoridad, propósito y límites

Este inventario complementa `docs/architecture/tech-stack-v0.1.md`. No lo sustituye ni sustituye:

- La especificación maestra.
- Los contratos transaccionales.
- El modelo físico db-4.
- Los contratos API.
- Las decisiones y propuestas registradas en `tech-stack-v0.1.md`.

Ningún PC, Windows, driver, SDK, periférico, mecanismo de conexión o combinación se declarará soportado hasta contar con evidencia real suficiente. Una identificación visual, una familia comercial o una afirmación genérica como “Windows 10/11” no acredita compatibilidad.

### 1.1 Estados de datos

| Estado | Significado y uso |
| --- | --- |
| `CONFIRMED` | Dato verificado mediante inspección o evidencia identificable. Debe registrar fuente/método y fecha; no significa por sí solo que toda la configuración esté soportada. |
| `PENDING INVENTORY` | Dato aún no recopilado. Es el estado inicial de los campos de este documento. |
| `N/A JUSTIFICADO` | El campo no aplica a la configuración y existe una razón escrita; no usar como sustituto de un dato faltante. |
| `NEEDS POC` | El dato de inventario se conoce, pero la compatibilidad o el comportamiento requiere prueba sobre hardware real. |
| `UNSUPPORTED` | Evidencia concreta demuestra que la combinación evaluada no está soportada. Debe citarse la restricción o resultado que lo demuestra. |
| `UNKNOWN` | Se intentó determinar el dato o resultado y la evidencia fue insuficiente o inconclusa. No equivale a `UNSUPPORTED`. |

Reglas de registro:

- Usar `PENDING INVENTORY` antes de inspeccionar; usar `UNKNOWN` solo cuando una revisión efectivamente realizada no resuelva el dato.
- No cambiar a `CONFIRMED` sin registrar evidencia, fecha y método o fuente.
- Separar el dato observado de la interpretación de soporte y del resultado futuro del POC.
- No convertir `NEEDS POC` en `CONFIRMED` hasta ejecutar y documentar el caso correspondiente.

## 2. Principio de inventario y configuración única

No se asume que las cuatro sucursales tengan equipos iguales. La unidad de análisis es una **configuración real distinta**, no solo un modelo comercial de PC o periférico.

- Registrar una fila por configuración distinta, las sucursales afectadas y la cantidad de PCs equivalentes.
- Asignar un ID estable con formato `POS-HW-NN` después de identificar la combinación real.
- Dos PCs aparentemente iguales son configuraciones distintas si difieren en edición/build de Windows, arquitectura, driver/SDK, interfaz, permisos u otro factor capaz de afectar instalación u operación.
- El mismo modelo de impresora con drivers, conexiones, arquitecturas o Windows distintos puede requerir IDs de configuración diferentes.
- Agrupar equipos solo después de comparar todos los campos relevantes y conservar identificadores operativos que permitan rastrearlos.
- No usar números de serie como ID principal; preferir IDs internos del proyecto.

## 3. Matriz general de PCs POS

Crear una fila por configuración real identificada. No conservar la fila plantilla cuando existan datos reales: reemplazarla por las filas `POS-HW-NN` necesarias.

| ID configuración | Sucursal | PC/terminal | Cantidad | Fabricante/modelo PC | CPU | RAM | Arquitectura | Disco libre | Windows edición | Windows versión/build | Estado soporte | Usuario admin disponible | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

Registrar CPU por modelo cuando sea posible, RAM en GB, arquitectura como x64/x86/ARM64 según evidencia y disco libre con unidad/fecha de medición. Windows requiere edición, release y build exacto; no escribir únicamente “Windows 10” o “Windows 11”. Formatos como `Windows 10 Pro 22H2 build ...` o `Windows 11 Pro 24H2 build ...` solo son válidos cuando sus valores reales hayan sido comprobados; no completar el build por inferencia.

## 4. Soporte de Windows por configuración

Esta matriz separa el Windows observado de la evaluación posterior de soporte. No marcar “compatible” solo por pertenecer a Windows 10 u 11.

| Configuración | Edición | Release | Build exacto | Arquitectura | Fecha/ciclo de soporte verificado | Compatibilidad esperada .NET 10 | Compatibilidad esperada Windows App SDK | Restricciones encontradas | Evidencia/estado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

La fecha/ciclo de soporte y las compatibilidades esperadas se registrarán posteriormente contra fuentes vigentes y el dato exacto del PC. Una expectativa documental seguirá como `NEEDS POC` cuando dependa de driver, SDK, packaging, permisos o hardware real.

## 5. Scanner / lector de código

| Configuración | Marca | Modelo | Interfaz | Modo | Sufijo | Driver/SDK | Arquitectura driver | Resultado futuro POC | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | NEEDS POC | PENDING INVENTORY |

Para cada lector registrar:

- Conexión real: USB HID, keyboard wedge, COM, USB virtual COM, SDK propietario u otra.
- Cómo entrega el código al sistema; “USB” no implica automáticamente HID.
- Nombre/versión de driver o SDK, configuración necesaria y arquitectura.
- Prefijo y sufijo, incluido Enter/Tab, o `N/A JUSTIFICADO` si se comprueba que no existen.
- Velocidad/repetición y comportamiento de foco, reservando su resultado para el POC.

## 6. Impresora térmica

| Configuración | Marca | Modelo | Ancho | Conexión | Driver | Protocolo | ESC/POS | Corte | Cajón | Método impresión esperado | Resultado futuro POC | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | NEEDS POC | PENDING INVENTORY |

Registrar posteriormente:

- Ancho real: 58 mm, 80 mm u otro.
- Conexión efectiva: USB, Ethernet, serial, cola Windows u otra.
- Nombre y versión del driver, indicando si es genérico o del fabricante.
- Protocolo o SDK; no asumir ESC/POS sin verificar el modelo y su configuración.
- Método previsto: Windows printer, raw spooler, ESC/POS, SDK u otro; el POC determinará el método funcional.
- Corte automático, code page/encoding, caracteres y acentos, resolución y nombre exacto de la cola Windows.
- Puerto de cajón, si existe, sin inferir que se usa para abrirlo.

## 7. Cajón de dinero

| Configuración | Marca/modelo | Conectado a | Interfaz | Método apertura esperado | Dependencia driver/SDK | Resultado futuro POC | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | NEEDS POC | PENDING INVENTORY |

Clasificar el mecanismo observado como puerto de impresora, pulso ESC/POS, USB, serial, SDK u otro solo después de verificar hardware y cableado. La lista representa opciones de clasificación, no el mecanismo actual de GENGXIN.

## 8. COM / serial / USB específico

| Configuración | Dispositivo | Tipo conexión | VID/PID si aplica | Puerto COM | Baud rate | Driver | Arquitectura | Reconexión | Resultado futuro POC | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | NEEDS POC | PENDING INVENTORY |

VID/PID, puerto COM y baud rate pueden registrarse como `N/A JUSTIFICADO` cuando la conexión comprobada no los utilice. Mientras no se haya revisado el dispositivo deben permanecer `PENDING INVENTORY`. “Reconexión” documentará posteriormente qué ocurre al desconectar/conectar y si cambian puerto o identidad; no se prueba ahora.

## 9. Drivers y SDK del fabricante

| Dispositivo | Driver/SDK | Versión | Fuente | x86 | x64 | ARM64 | .NET compatibility | Restricciones | Licencia | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

Por cada dependencia registrar nombre/versión exactos, fuente oficial o medio existente, arquitecturas, dependencia de .NET Framework antiguo, COM interop, DLL nativa, permisos, servicios Windows, instalador requerido y condiciones de licencia. Estos datos son críticos para comparar WinUI 3 y WPF, pero no determinan ganador sin POC.

## 10. Permisos y operación Windows

Repetir esta checklist por cada `POS-HW-NN`. Registrar estado real, evidencia y restricción; no decidir nuevas políticas en este documento.

| Configuración | Comprobación | Estado real | Evidencia/restricción |
| --- | --- | --- | --- |
| PENDING INVENTORY | Usuario POS estándar | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Usuario administrador disponible | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Instalación requiere admin | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Driver requiere admin | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Acceso a impresoras | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Acceso COM | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Firewall | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Antivirus/EDR | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Políticas corporativas | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Windows Update | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Reinicios automáticos | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Acceso remoto | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Bloqueo de pantalla | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Inicio automático del POS | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Acceso al filesystem local necesario | PENDING INVENTORY | PENDING INVENTORY |

## 11. Red de sucursales

El MVP propuesto es **ONLINE-FIRST CENTRALIZADO**. Esta matriz identificará configuraciones que podrían amenazar la operación POS; no diseña ni habilita modo offline.

| Sucursal/configuración | Tipo enlace | ISP | Ethernet/Wi-Fi | Latencia aproximada | Pérdida observada | IP dinámica/fija | Proxy/firewall | Resultado | Observaciones |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

No registrar métricas estimadas como mediciones. Cuando se recopilen, anotar método, destino, duración, fecha y condiciones; evitar IP pública salvo necesidad justificada.

## 12. Dispositivos no POS que pueden afectar deployment

Registrar hardware presente que no forma parte del flujo POS pero puede afectar instalación, espacio, interacción o continuidad. Su presencia no lo convierte en requisito.

| Configuración | Dispositivo | Marca/modelo | Conexión/característica | Impacto observado o esperado | Estado | Observaciones |
| --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | Monitor/resolución | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Touch screen | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Teclado | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Mouse | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Lector biométrico | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | UPS | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Impresora A4 | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| PENDING INVENTORY | Segundo monitor | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

Usar `N/A JUSTIFICADO` cuando se confirme que una categoría no existe o no aplica; no usarlo antes de revisar.

## 13. Resolución y UI

| Configuración | Resolución | Escala Windows | Monitores | Touch | Orientación | Observaciones |
| --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

Registrar posteriormente valores reales como resolución 1366x768, 1920x1080 u otra; escala 100/125/150 % u otra; monitor único/doble, touch/no touch y orientación. Los ejemplos no son defaults ni requisitos. Esta evidencia alimentará el POC WinUI 3/WPF.

## 14. Inventario por sucursal

Los nombres comerciales no están acreditados en este inventario; se usan identificadores neutrales. Cada equipo deberá vincularse con una configuración `POS-HW-NN` cuando esta exista.

### 14.1 Sucursal A

| Categoría | PC/terminal o equipo | Config ID | Cantidad | Estado | Evidencia/observaciones |
| --- | --- | --- | --- | --- | --- |
| PCs | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Terminales | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Scanner | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Impresora | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Cajón | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Red | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Observaciones | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

### 14.2 Sucursal B

| Categoría | PC/terminal o equipo | Config ID | Cantidad | Estado | Evidencia/observaciones |
| --- | --- | --- | --- | --- | --- |
| PCs | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Terminales | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Scanner | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Impresora | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Cajón | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Red | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Observaciones | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

### 14.3 Sucursal C

| Categoría | PC/terminal o equipo | Config ID | Cantidad | Estado | Evidencia/observaciones |
| --- | --- | --- | --- | --- | --- |
| PCs | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Terminales | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Scanner | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Impresora | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Cajón | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Red | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Observaciones | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

### 14.4 Sucursal D

| Categoría | PC/terminal o equipo | Config ID | Cantidad | Estado | Evidencia/observaciones |
| --- | --- | --- | --- | --- | --- |
| PCs | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Terminales | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Scanner | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Impresora | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Cajón | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Red | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |
| Observaciones | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

## 15. Matriz de configuraciones únicas

Esta tabla determina cuántas combinaciones debe cubrir el POC. Si las cuatro sucursales comparten una combinación realmente equivalente, puede existir un POC técnico principal más verificación de despliegue. Si existen tres combinaciones, el POC deberá cubrir las tres.

| Config ID | Sucursales/PCs | Windows | Arquitectura | Scanner | Impresora | Cajón | COM/USB especial | Requiere POC separado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY | PENDING INVENTORY |

No agrupar por parecido visual ni únicamente por marca/modelo. La agrupación debe considerar Windows/build, arquitectura, drivers, SDKs, mecanismos de periféricos, permisos y restricciones operativas.

## 16. Criterio para iniciar el POC

El POC WinUI 3 vs WPF no debe comenzar formalmente hasta conocer, como mínimo, edición/build de Windows, arquitectura, modelo y mecanismo del scanner, modelo/conexión/driver de la impresora, mecanismo del cajón, COM/USB relevante, restricciones de permisos y configuraciones únicas reales.

- [ ] Windows inventariado
- [ ] PCs inventariadas
- [ ] Scanner inventariado
- [ ] Impresora inventariada
- [ ] Cajón inventariado
- [ ] Drivers identificados
- [ ] COM/USB identificado
- [ ] Permisos identificados
- [ ] Configuraciones únicas agrupadas

Cada elemento se cambia manualmente a completado solo con evidencia registrada. Ninguno está completado al crear este documento.

## 17. Criterio de cierre del inventario

Este inventario puede considerarse completo **para iniciar el POC** únicamente cuando:

- Todas las PCs POS estén cubiertas.
- Todas las configuraciones únicas estén identificadas y vinculadas con sucursales/PCs.
- Los periféricos esenciales tengan marca, modelo e interfaz conocidos.
- Los drivers/SDK relevantes estén identificados con versión y arquitectura cuando aplique.
- La edición, release y build exactos de Windows estén registrados por configuración.
- No existan dispositivos críticos en estado `UNKNOWN`.
- Todo dato no aplicable esté justificado como `N/A JUSTIFICADO`.
- Las diferencias entre sucursales estén documentadas.
- Los datos `CONFIRMED` conserven evidencia, fecha y método/fuente.

Cerrar el inventario no declara hardware soportado, no congela este documento y no selecciona framework. Solo habilita el siguiente micro-hito: POC WinUI 3 vs WPF.

## 18. Checklist física por PC y periféricos

Usar esta guía durante una visita o revisión autorizada. No instalar software, no modificar configuración y no ejecutar acciones destructivas.

### 18.1 Windows y PC

- [ ] Identificar sucursal y PC/terminal mediante ID operativo interno.
- [ ] Abrir `Win + R`, ejecutar `winver` y registrar edición, release y build exactos.
- [ ] Revisar `Settings -> System -> About` para arquitectura, CPU y RAM.
- [ ] Registrar fabricante/modelo del PC y disco libre con fecha.
- [ ] Registrar escala, resolución, monitores, orientación y touch.
- [ ] Identificar usuario operativo, disponibilidad de administrador y restricciones sin recopilar credenciales.

### 18.2 Impresora

- [ ] Registrar marca/modelo de etiqueta física y ancho de papel.
- [ ] Identificar conexión y cableado reales.
- [ ] Revisar Windows Printer Properties y registrar nombre de cola.
- [ ] Registrar nombre/versión del driver cuando sea posible.
- [ ] Identificar protocolo/SDK declarado sin asumir ESC/POS.
- [ ] Registrar corte, resolución, code page/encoding y conexión de cajón cuando puedan determinarse sin cambiar configuración.

### 18.3 Scanner

- [ ] Registrar marca/modelo de la etiqueta y conexión física.
- [ ] Observar su comportamiento al escanear en Notepad sin reconfigurarlo.
- [ ] Registrar prefijo/sufijo, incluido Enter o Tab, y comportamiento básico de foco observado.
- [ ] Revisar Device Manager para identificar HID, COM, USB virtual o driver/SDK.
- [ ] No cambiar modo, velocidad ni programación durante el inventario.

### 18.4 Cajón

- [ ] Registrar marca/modelo si está disponible.
- [ ] Fotografiar o describir cableado sin desconectarlo cuando la política local lo permita.
- [ ] Determinar si está conectado a impresora u otro dispositivo sin afirmar mecanismo de apertura todavía.
- [ ] Registrar interfaz, driver/SDK aparente y restricciones observadas.

### 18.5 Device Manager y red

- [ ] Revisar `Ports (COM & LPT)`.
- [ ] Revisar `Printers`.
- [ ] Revisar `Human Interface Devices`.
- [ ] Revisar dispositivos USB relevantes.
- [ ] Registrar adaptador Ethernet/Wi-Fi, tipo de enlace y restricciones de proxy/firewall conocidas.
- [ ] No desconectar dispositivos, cambiar drivers, alterar firewall ni ejecutar pruebas de red intrusivas.

## 19. Comandos Windows opcionales de inventario

Estas son ayudas **no destructivas** para una recopilación futura autorizada. No se ejecutan en este micro-hito y no sustituyen la inspección física.

| Comando | Uso futuro |
| --- | --- |
| `winver` | Edición, release y build de Windows mediante UI. |
| `systeminfo` | Resumen del sistema operativo y equipo; revisar/redactar datos no necesarios antes de registrar evidencia. |
| `Get-ComputerInfo` | Información amplia del equipo y Windows; seleccionar solo campos necesarios. |
| `Get-CimInstance Win32_OperatingSystem` | Versión/build y arquitectura del sistema operativo. |
| `Get-CimInstance Win32_ComputerSystem` | Fabricante/modelo, RAM y arquitectura general. |
| `Get-CimInstance Win32_Processor` | Modelo y arquitectura de CPU. |
| `Get-PnpDevice` | Dispositivos Plug and Play y estados observados. |
| `Get-CimInstance Win32_Printer` | Colas, drivers y puertos de impresora disponibles. |
| `Get-CimInstance Win32_SerialPort` | Puertos seriales detectados cuando existan. |

No guardar la salida completa si contiene identificadores innecesarios. No recopilar claves de producto, secretos o credenciales; copiar únicamente los campos requeridos por este inventario.

## 20. Privacidad y datos sensibles

No registrar:

- Passwords, tokens, credenciales ni secretos.
- Claves de Windows o claves de producto.
- Contraseñas Wi-Fi.
- Certificados privados ni CSD.
- IP pública salvo necesidad documentada; preferir descripción de conectividad.
- Datos personales innecesarios o nombres de usuario personales cuando baste un rol.

Registrar números de serie de hardware solo si son indispensables para distinguir equipos; preferir IDs internos como terminal/configuración. La evidencia adjunta futura debe redactar cualquier dato sensible no necesario.

## 21. Cómo este inventario afecta la decisión WinUI 3 vs WPF

La selección posterior se basará en evidencia de:

- Sistemas operativos realmente presentes y soportados.
- Drivers, SDKs legacy y dependencias de .NET Framework/COM interop/DLL nativa.
- Arquitecturas x86, x64 o ARM64 efectivamente requeridas.
- Impresión térmica, método directo/raw/SDK, caracteres y corte.
- Apertura del cajón mediante el mecanismo real.
- COM, USB, puertos virtuales, HID y reconexión.
- Deployment, instalación, actualización y permisos del usuario operativo.
- Foco/teclado y estabilidad sobre el hardware representativo de cada configuración única.

El inventario permite diseñar pruebas comparables; no demuestra todavía que un candidato funcione ni convierte apariencia o preferencia en criterio técnico. El estado se mantiene:

```text
POS_DESKTOP_FRAMEWORK = PENDING
```

## 22. Relación con el POC futuro

Un siguiente artefacto posible es:

```text
docs/architecture/pos-desktop-poc-v0.1.md
```

No se crea ahora. El futuro POC comparará WinUI 3 y WPF sobre todas las configuraciones únicas identificadas aquí, conforme a `tech-stack-v0.1.md` §11. Sus resultados serán distintos de los datos de inventario y deberán registrarse por candidato/configuración como PASS, FAIL o N/A justificado con evidencia.

## 23. Mantenimiento del inventario

- Conservar una fila por combinación real y actualizar cantidades/sucursales cuando cambien equipos.
- Registrar fecha, fuente y responsable o método no personal de cada verificación.
- Mantener historial mediante Git; no reemplazar un dato confirmado por una suposición.
- Al cambiar Windows, driver, interfaz, arquitectura o periférico, reevaluar si nace una configuración única nueva.
- No declarar “soportado” a partir del inventario sin la validación posterior correspondiente.

## 24. Estado y salida de este micro-hito

Este documento termina como **BORRADOR CONTROLADO — NO FROZEN**. Cambiará conforme se recopilen datos reales y no se convierte automáticamente en FROZEN por llenar sus tablas.

La selección final entre WinUI 3 y WPF deberá registrarse en `tech-stack-v0.1.md` mediante un hito documental separado, después del inventario y del POC. Hasta entonces:

```text
POS_DESKTOP_FRAMEWORK = PENDING
```

Este micro-hito crea únicamente documentación de inventario: no implementa POC, no prueba hardware, no instala software, no modifica db-4, Docker ni contratos, y no cambia ninguna decisión tecnológica.
