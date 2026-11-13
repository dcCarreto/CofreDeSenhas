# Actualizaciones

La aplicación **se actualiza sola**. No necesitas descargar ni hacer clic en
nada.

## Cómo funciona

1. Al abrir la caja, y después cada 6 horas, la aplicación consulta la página
   de versiones del proyecto.
2. Si hay una versión nueva, se descarga en segundo plano. Antes de aceptarla,
   la aplicación verifica la firma digital y el SHA256 del archivo; si algo no
   coincide, la actualización se descarta.
3. Con la actualización lista, el pie muestra **Actualización lista: se
   instalará cuando se bloquee la caja**.
4. En el siguiente bloqueo de la caja (con *Bloquear ahora* o por
   inactividad), la aplicación se cierra, instala la versión nueva y vuelve a
   abrirse, ya en la pantalla de desbloqueo. Solo tienes que escribir la
   contraseña maestra, como siempre.

Para instalarla en el momento, usa **Instalar ahora** en el pie: muestra qué
cambia en la versión y pide confirmación antes de cerrar la aplicación. La
**X** de al lado solo oculta el aviso; la actualización sigue programada para
el siguiente bloqueo.

No se envía ningún dato de la caja. La consulta y la descarga son lecturas
públicas, sin nada que te identifique a ti o a tu ordenador. Consulta también
[Privacidad y red](privacidade-rede).

## Cómo desactivarla

En *Configuración → Comportamiento → Actualizar automáticamente*. Desactivada,
la aplicación no consulta nada, y una actualización ya descargada se descarta.
Para actualizar a mano, descarga la versión nueva en la página de versiones del
proyecto.

## Según cómo la instalaste

- **Instalador o winget**: como se describe arriba. Si la instalaste "para
  todos los usuarios" (en Archivos de programa), tienes que hacer clic en
  **Instalar ahora**, porque Windows pide permiso de administrador; el aviso
  del pie te indica cuándo es el caso.
- **Ejecutable portátil**: el propio archivo se sustituye por la versión
  nueva. Tiene que estar en una carpeta en la que puedas escribir, como
  Descargas o el escritorio.
- **Microsoft Store**: actualiza la propia tienda, según su configuración de
  actualización de aplicaciones. La aplicación descarga la actualización en
  cuanto la tienda la ofrece y la instala al bloquear la caja. La opción
  "Actualizar automáticamente" no aparece.
- **Linux, AppImage**: igual que en Windows; el archivo `.AppImage` se
  sustituye por el nuevo.
- **Linux, paquete `.tar.gz` o compilado desde el código fuente**: no se
  actualiza solo. Descarga la versión nueva en la página de versiones.

La línea de comandos `cofre` y el generador de contraseñas independiente
(`GeradorDeSenhas-*-portatil.exe`) son archivos aparte y **no** se actualizan
solos. Descarga las versiones nuevas en la página de versiones cuando quieras.

## Tus datos durante la actualización

La caja, las copias de seguridad y las preferencias no se tocan: siguen en la
carpeta de datos de la aplicación, y la versión nueva abre la misma caja.
Desinstalar la aplicación, incluida la versión de la tienda, tampoco borra la
caja. El archivo descargado se queda en la carpeta temporal del sistema hasta
instalarse y se borra la siguiente vez que se abre la aplicación. Por
precaución, mantén las [copias de seguridad](backup) al día.
