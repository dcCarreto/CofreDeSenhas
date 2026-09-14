# Preguntas frecuentes

## He olvidado la contraseña maestra. ¿Y ahora?

Depende de si activaste la **clave de recuperación** (opcional, desactivada
por defecto). Si la activaste: en la pantalla de inicio, haz clic en "He
olvidado la contraseña maestra", escribe el secreto que anotaste y elige
una contraseña nueva. Si no la activaste: no hay forma de recuperarla, ni
por nuestra parte ni por nadie. Si guardaste el **código QR de copia de
seguridad**, úsalo para reimportar la contraseña. Sin la contraseña
maestra, la clave de recuperación ni el QR, el contenido de la caja queda
inaccesible. Consulta [Contraseña maestra](senha-mestra).

## ¿Dónde están mis datos?

En un archivo cifrado en la carpeta de datos de la aplicación, dentro de
tu perfil de usuario del sistema. No se envía nada a servidores nuestros.
Detalles en [Introducción](introducao).

## ¿Cómo llevo la caja a otro ordenador?

Exporta la caja (archivo cifrado) o copia una copia de seguridad, instala
la aplicación en el destino e importa. Paso a paso en
[Importar y exportar](importar-exportar).

## ¿La caja se sincroniza sola en la nube?

No. No hay ninguna nube nuestra. Puedes configurar
[Sincronización por carpeta](sincronizacao) o una
[Base de datos](banco-de-dados) que **tú** controles: entonces la
sincronización pasa por tu infraestructura.

## ¿Es seguro? ¿Qué cifrado se usa?

AES-256-GCM, con la clave derivada de tu contraseña maestra en cada
apertura y nunca guardada. La fuerza práctica depende sobre todo de que
elijas una buena contraseña maestra.

## He perdido el móvil con el código QR. ¿Y Windows Hello?

[Windows Hello](windows-hello) está vinculado a tu cuenta de Windows en
este ordenador; el QR es independiente. Si aún puedes abrir la caja (por
contraseña o por Hello), genera un **nuevo código QR** en *Configuración →
Seguridad* y guárdalo.

## ¿Puedo usarla sin instalar nada más que la aplicación?

Sí. La caja local no necesita base de datos, servidor ni cuenta. Las
funciones de red son todas opcionales: consulta
[Privacidad y red](privacidade-rede).
