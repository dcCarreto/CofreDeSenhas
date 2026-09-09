# Base de datos compartida

Una función avanzada para quien quiere mantener la caja en una **base de
datos que tú alojas** (SQLite en red, PostgreSQL o MySQL) en lugar de un
archivo local.

## Para quién es

- Equipos o familias que comparten un conjunto de credenciales.
- Quien ya tiene un servidor de base de datos y prefiere centralizar ahí.

Para sincronizar solo **tus propios** dispositivos,
[Sincronización por carpeta](sincronizacao) suele ser más sencilla.

## Cómo funciona

- Tú indicas la dirección y las credenciales de la base de datos. La
  contraseña del servidor se guarda cifrada en la caja local.
- Los datos siguen cifrados con tu contraseña maestra **antes** de ir a la
  base de datos: el servidor nunca ve contraseñas en claro.
- El mismo motor de fusión que la sincronización (gana la edición más
  reciente, los conflictos van a una pantalla de decisión) mantiene los
  dispositivos alineados.

## Conectar y desconectar

En **Configuración → Copia y sincronización**. Al desconectar, la caja
vuelve a operar solo con la copia local.

> Proteger el servidor de la base de datos (acceso, red, copias) es tu
> responsabilidad. La aplicación se encarga del cifrado del contenido, no
> de la seguridad de tu infraestructura.
