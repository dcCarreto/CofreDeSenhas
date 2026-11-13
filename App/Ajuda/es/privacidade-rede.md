# Privacidad y red

Por defecto, la aplicación solo usa internet para buscar y descargar sus
propias actualizaciones, sin enviar ningún dato tuyo (consulta
[Actualizaciones](atualizacoes)); se puede desactivar. Las demás funciones de
abajo son opcionales y activas cada una sabiendo lo que hace.

## Iconos en línea de los servicios

Cuando está activo, obtiene el icono real de cada sitio de un servicio
público de iconos, enviando **solo el dominio** (por ejemplo,
`github.com`). Ninguna contraseña, usuario ni nota sale del ordenador. Los
iconos descargados se guardan en caché en el disco. Cuando está
desactivado, la caja muestra solo iniciales y no toca la red.

## Actualizar automáticamente

Activado por defecto. Consulta la página de versiones del proyecto al abrir
la caja y cada 6 horas; si hay una versión nueva, la descarga en segundo
plano, verifica su firma y su SHA256 y la instala cuando se bloquea la caja.
La aplicación se cierra y vuelve a abrirse ya actualizada, en la pantalla de
desbloqueo. Para instalarla en el momento, usa **Instalar ahora** en el pie.
No se envía ningún dato de la caja. Si se instaló desde Microsoft Store, la
propia tienda se encarga de actualizar y esta opción no aparece.

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
