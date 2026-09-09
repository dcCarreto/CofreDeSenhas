# Privacidad y red

Por defecto, la aplicación **no accede a internet**. Las funciones de abajo
son opcionales y activas cada una sabiendo lo que hace.

## Iconos en línea de los servicios

Cuando está activo, obtiene el icono real de cada sitio de un servicio
público de iconos, enviando **solo el dominio** (por ejemplo,
`github.com`). Ninguna contraseña, usuario ni nota sale del ordenador. Los
iconos descargados se guardan en caché en el disco. Cuando está
desactivado, la caja muestra solo iniciales y no toca la red.

## Buscar actualizaciones

Cuando está activo, consulta la página de versiones del proyecto para
avisarte si hay una versión más nueva. No se descarga nada
automáticamente, y no se envía nada más que esa consulta. Desactivado por
defecto.

## Comprobación de filtraciones

Bajo demanda, compara tus contraseñas con bases públicas de filtraciones
usando **k-anonymity**: solo se envía un prefijo del hash de la
contraseña. Consulta [Informe de seguridad](seguranca).

## Sincronización y base de datos

Si configuras [Sincronización por carpeta](sincronizacao) o una
[Base de datos](banco-de-dados), el tráfico va a **tu** carpeta o a **tu**
servidor, nunca a un servicio nuestro.

## Modo privacidad

El botón de ojo en la barra de título difumina los valores sensibles en
pantalla, para que abras la caja cerca de otras personas sin exponer nada.
