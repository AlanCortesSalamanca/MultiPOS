# MultiPOS · Web administrativa — FRONT-01

SPA inicial de administración y logística. No es el POS Desktop de Windows y no contiene caja, cobros, ventas operativas ni integraciones de hardware.

## Requisitos y ejecución manual

- Node.js **24 LTS** y npm **11**. El manifiesto declara npm 11.21.0 como versión de referencia.
- Ejecutar los comandos desde `D:\gengxin-frontend\src\frontend` (Windows) o `/mnt/d/gengxin-frontend/src/frontend` (WSL).

```powershell
cd D:\gengxin-frontend\src\frontend
npm install
npm run dev
```

Abrir la URL indicada por Vite (normalmente `http://localhost:5173`). No se requiere backend ni PostgreSQL para esta interfaz inicial.

```powershell
npm run typecheck
npm run build
npm run preview
```

`build` comprueba TypeScript y genera assets estáticos en `dist/`. `preview` permite revisarlos localmente; no es un servidor productivo.

No se ha ejecutado instalación, comprobación de tipos, build ni pruebas durante esta implementación. Las versiones base se consultaron en metadatos públicos de npm; eso no acredita instalación ni compatibilidad probada. El primer `npm install` debe generar `package-lock.json`; no se fabricó un lockfile. Revisarlo y conservarlo para poder usar `npm ci` en instalaciones posteriores.

## Estructura

```text
src/frontend/
  package.json, index.html
  vite.config.ts, tsconfig.json, tsconfig.node.json
  components.json
  src/
    main.tsx                  React, Router y QueryClientProvider
    app/                      Rutas y definición de navegación
    components/
      layout/                 Sidebar, header y layout responsive
      ui/                     Primitivos shadcn/ui incorporados y adaptados
      empty-state.tsx
      page-heading.tsx
    features/
      dashboard/              Resumen administrativo sin métricas inventadas
      products/               Catálogo base y demostración local opcional
      inventory/              Estado vacío y consultas previstas
      reports/                Estado vacío y vistas previstas
    pages/                    Página no encontrada
    lib/                      Utilidad de clases CSS
    styles/                   Tailwind CSS 4 y tokens visuales
```

La ubicación concreta `src/frontend/` fue autorizada para FRONT-01. Mantiene las fronteras entre Web, backend y POS Desktop descritas en el tech-stack; no modifica ese contrato.

## Dependencias y decisiones

- React 19, TypeScript 5.9 y Vite 8 con `@vitejs/plugin-react`.
- Tailwind CSS 4 mediante `@tailwindcss/vite`; sin configuración de Tailwind 3 ni PostCSS adicional.
- Componentes locales siguiendo shadcn/ui: Button, Badge, Card, Input y Sheet, adaptados a este alcance. `components.json` registra estilos y aliases; no se ejecutó el generador ni se añadió un supuesto paquete runtime `shadcn/ui`.
- Radix Dialog proporciona el menú móvil modal: foco atrapado, Escape y cierre por fondo. Radix Slot permite enlaces con estilo de botón sin anidar controles interactivos.
- `class-variance-authority`, `clsx` y `tailwind-merge` gestionan variantes y clases; `lucide-react` proporciona iconos.
- React Router 7, modo declarativo con BrowserRouter; sin framework/SSR.
- TanStack Query 5 tiene un provider inicial, sin queries ni mutations. Los retries de mutations están deshabilitados; cualquier command futuro debe adoptar su contrato específico, no una política genérica de recuperación.
- Fuentes del sistema; sin fuentes remotas, analytics ni servicios externos.

## Qué funciona

| Ruta | Implementación |
| --- | --- |
| `/` | Dashboard con estados sin datos, acceso a módulos y estado de FRONT-01 |
| `/productos` | Estado vacío, demostración opcional y búsqueda por nombre/SKU y categoría sobre cuatro ejemplos ficticios |
| `/inventario` | Pantalla base sin saldos, sucursales ni movimientos inventados |
| `/reportes` | Pantalla base con consultas previstas y exportación explícitamente no disponible |
| Cualquier otra | Página no encontrada con retorno al Dashboard |

Navegación activa, historial del navegador, títulos de página y layout responsive. Sidebar fija desde 1024 px; menú modal en tablet/móvil. Hay enlace para saltar al contenido, etiquetas de campos, foco visible y tabla de demostración desplazable con teclado.

La demostración de Productos está desactivada al entrar. Sus ejemplos están aislados en `demo-products.ts`, con SKU `DEMO-*` y avisos explícitos. Mostrar/ocultar la demostración limpia los filtros; navegar fuera de Productos descarta su estado UI. No hay almacenamiento local ni persistencia. Los guiones del Dashboard significan **dato no disponible**, nunca cero.

## Qué queda pendiente

Contratos de consulta, cliente API, datos reales, autenticación y autorización, contexto de negocio/sucursal, CRUD de productos, inventario y reportes/exportaciones. Los botones deshabilitados comunican funcionalidad no implementada, no decisiones de permisos. No hay endpoints simulados, llamadas de red de negocio, datos de usuarios falsos ni confirmaciones locales.

No se han añadido herramientas ni archivos de tests en FRONT-01. La baseline documental Vitest/React Testing Library/Playwright queda para un hito autorizado de pruebas.

Para despliegue posterior, el servidor estático deberá hacer fallback SPA para las rutas Web, **sin absorber rutas `/api/v1`**. No se creó configuración de despliegue ni proxy en este hito. Esta interfaz sin autenticación no debe presentarse como un panel productivo protegido.

## Verificación manual propuesta — NO_EJECUTADA

1. Instalar y ejecutar `typecheck` y `build`; registrar las salidas reales.
2. Abrir las cuatro rutas mediante sidebar, enlaces del Dashboard y atrás/adelante del navegador. Probar recarga directa y ruta desconocida.
3. Revisar anchos de 320, 768 y 1440 px; confirmar que solo la tabla tiene scroll horizontal en móvil.
4. Usar teclado: Tab, enlace de salto, Enter en enlaces y Escape en menú móvil. Comprobar foco atrapado, retorno al botón al cancelar y foco en contenido al navegar.
5. Con menú abierto, ampliar a desktop: debe cerrarse y liberar el bloqueo modal.
6. Productos: activar demostración, buscar `cable`, buscar `DEMO-002`, combinar con `Electricidad`, buscar un término inexistente, limpiar y ocultar. Deben verse resultados, estado sin coincidencias y regreso al estado vacío, respectivamente.
7. Confirmar avisos de demostración, indicadores no disponibles y controles de negocio deshabilitados. No debe haber solicitudes API ni escrituras en almacenamiento local.
8. Revisar consola y lector de pantalla; comprobar etiquetas, navegación activa, títulos y tabla.

Estado de entrega: **READY_FOR_MANUAL_VALIDATION**, sin evidencia de ejecución ni Git. No se hizo staging, commit o push.
