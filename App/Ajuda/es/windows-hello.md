# Windows Hello

Permite **desbloquear la caja con biometría** (huella, rostro o PIN de
Windows) en lugar de escribir la contraseña maestra cada vez.

## Cómo activarlo

En **Configuración → Seguridad → Activar Windows Hello**. Windows pide tu
verificación y la aplicación empieza a ofrecer el desbloqueo por Hello en
la pantalla de apertura.

## Qué cambia y qué no

- La **contraseña maestra sigue siendo la llave real** de la caja. Hello
  solo libera el acceso guardado bajo protección del sistema.
- Las operaciones sensibles (como cambiar la contraseña maestra) siguen
  pidiendo la contraseña maestra.
- Si Hello falla o el dispositivo no lo admite, siempre puedes entrar con
  la contraseña maestra.

## Alcance

La credencial de Hello está vinculada a **tu cuenta de Windows en este
ordenador**. No viaja con el archivo de la caja: en otro ordenador,
vuelves a activar Hello (o usas la contraseña maestra).

## Desactivar

En la misma pantalla, *Desactivar Windows Hello*. Se elimina el acceso
protegido y la caja vuelve a abrirse solo con la contraseña maestra.
