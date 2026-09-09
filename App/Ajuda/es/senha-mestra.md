# Contraseña maestra

La contraseña maestra es la única llave de la caja. A partir de ella, la
aplicación deriva —en memoria— la clave que descifra tus datos. **No se
guarda en ningún sitio.**

## No se puede recuperar

No existe un "he olvidado mi contraseña". Si pierdes la contraseña
maestra, el contenido de la caja queda inaccesible: así es exactamente
como el cifrado te protege de terceros. Por eso:

- elige una **frase larga** (varias palabras), fácil de recordar y difícil
  de adivinar;
- guarda el **código QR de copia de seguridad** (menú *Seguridad →
  Regenerar código QR*) y mantenlo fuera del ordenador;
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

## Windows Hello

[Windows Hello](windows-hello) permite desbloquear con biometría en lugar
de escribir la contraseña maestra, pero la contraseña maestra sigue siendo
la llave real y sigue siendo necesaria para operaciones sensibles.
