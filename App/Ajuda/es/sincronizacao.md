# Sincronización por carpeta

Mantiene la misma caja en varios de tus dispositivos mediante un **archivo
compartido** en una carpeta que tú elijas, normalmente una carpeta de un
servicio de archivos que ya usas (sincronizar el archivo en sí es tarea de
ese servicio).

## Cómo funciona

- La aplicación escribe un archivo cifrado en la carpeta indicada,
  protegido por una clave derivada de tu contraseña maestra.
- Cada dispositivo lee y escribe ese archivo periódicamente.
- Cuando dos dispositivos cambian la misma credencial, la aplicación lo
  resuelve por la edición más reciente; los conflictos que necesitan una
  decisión aparecen en una pantalla propia.

## Configurar

En **Configuración → Copia y sincronización → Sincronización**. Indica la
carpeta y define la frecuencia. Repite en cada dispositivo, usando la
**misma contraseña maestra** y la **misma carpeta**.

## Lo que no es

Esto **no** es un servicio de nube de la aplicación. No hay ningún
servidor nuestro en medio, ni cuenta, y no se nos envía nada. Es tu
infraestructura sincronizando tu archivo.

Si el objetivo es compartir una caja entre personas o máquinas de forma
más robusta, consulta [Base de datos compartida](banco-de-dados).
