# Introducción

La **Caja de contraseñas** guarda tus credenciales cifradas en tu propio
ordenador. No hay cuenta, ni servidor, ni nube obligatoria: la caja es un
archivo en tu disco, abierto solo por tu contraseña maestra.

## Cómo se protegen tus datos

- Todo se cifra con **AES-256-GCM**. La clave nunca se escribe en disco:
  se deriva de tu contraseña maestra cada vez que abres la caja.
- Sin la contraseña maestra, el archivo de la caja es ilegible, incluso
  para quien tenga acceso a tu ordenador.
- Nada sale de tu ordenador por sí solo. Las pocas funciones que usan la
  red son opcionales y están descritas en
  [Privacidad y red](privacidade-rede).

## Dónde vive la caja

La caja y las preferencias viven en la carpeta de datos de la aplicación,
dentro de tu perfil de usuario del sistema. Para llevar todo a otro
ordenador, consulta [Importar y exportar](importar-exportar) y
[Copia y restauración](backup).

## Por dónde empezar

Si es tu primera vez, sigue [Primeros pasos](primeiros-passos). La pieza
central es la [Contraseña maestra](senha-mestra): elige una buena y no la
pierdas.
