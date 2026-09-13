# Contraseña maestra

La contraseña maestra es la única llave de la caja. A partir de ella, la
aplicación deriva —en memoria— la clave que descifra tus datos. **No se
guarda en ningún sitio.**

## Sin clave de recuperación, no se puede recuperar

Por defecto, no existe un "he olvidado mi contraseña". Si pierdes la
contraseña maestra sin haber activado la **clave de recuperación** (ver
más abajo), el contenido de la caja queda inaccesible: así es exactamente
como el cifrado te protege de terceros. Por eso:

- elige una **frase larga** (varias palabras), fácil de recordar y difícil
  de adivinar;
- guarda el **código QR de copia de seguridad** (menú *Seguridad →
  Regenerar código QR*) y mantenlo fuera del ordenador;
- activa la **clave de recuperación** como una segunda red de seguridad;
- opcionalmente, guarda una copia escrita en un lugar físico seguro.

## Cambiar la contraseña maestra

En **Configuración → Seguridad → Cambiar contraseña maestra**. Toda la
caja se vuelve a cifrar con la nueva clave. Si usas una base de datos o
[Sincronización por carpeta](sincronizacao), el cambio afecta a los demás
dispositivos: lee la pantalla de confirmación antes de continuar.

> Cuando el cambio informa de que ha tenido éxito, la aplicación se
> reinicia sola. No toques la caja durante ese intervalo.

## Código QR de copia de seguridad

Es tu contraseña maestra codificada como imagen, para reimportarla si
olvidas la que escribiste. Trata el QR con el mismo cuidado que la
contraseña: quien lo tenga, abre tu caja.

## Clave de recuperación

En *Configuración → Seguridad → Clave de recuperación*, o ya en la
creación de la caja. Genera un secreto de 160 bits (en grupos de 4
caracteres) que permite redefinir la contraseña maestra sin saber la
anterior: quien tenga ese secreto puede hacerlo. Por eso anótalo y
guárdalo **lejos del ordenador**, con el mismo cuidado que la propia
contraseña maestra: vale tanto como ella.

¿Olvidaste la contraseña maestra y tenías la clave activada? En la
pantalla de inicio, haz clic en "He olvidado la contraseña maestra",
escribe el secreto anotado y elige una contraseña nueva: la caja se
vuelve a cifrar con ella. Se genera un secreto nuevo en cada recuperación
y en cada cambio de contraseña maestra; anota siempre el más reciente, el
anterior deja de servir. Activarla, regenerarla o desactivarla siempre
exige la contraseña maestra actual, incluso con la caja ya desbloqueada.

## Windows Hello

[Windows Hello](windows-hello) permite desbloquear con biometría en lugar
de escribir la contraseña maestra, pero la contraseña maestra sigue siendo
la llave real y sigue siendo necesaria para operaciones sensibles.
