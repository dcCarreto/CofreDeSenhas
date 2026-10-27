# Importar y exportar

## Importar de otro gestor

En **Configuración → Copia y sincronización → Importar desde CSV**. La
mayoría de gestores exporta un archivo **CSV**; la aplicación reconoce los
formatos más comunes y te deja comprobar el mapeo de columnas antes de
terminar.

> El CSV exportado por otros programas está en **texto plano**, sin
> cifrar. Después de importar, borra ese archivo de forma segura.

## Exportar tu caja

La exportación genera un archivo **cifrado**, protegido por una contraseña
que defines en el momento. Sirve para migrar a otro ordenador o guardar
una copia a largo plazo fuera del formato de [Copia](backup).

Para reimportar, abre la aplicación en el destino y usa la opción de
importar sobre el archivo exportado, indicando la contraseña usada en la
exportación.

## Llevar a otro gestor

En **Configuración → Copia y sincronización → Exportar sin cifrar
(CSV / Bitwarden)**. Después de confirmar la contraseña maestra, elige el
formato:

- **CSV**: lo aceptan Bitwarden y la mayoría de los gestores.
- **JSON de Bitwarden**: incluye también el historial de contraseñas y
  los campos extra de cada tipo de credencial.

Los elementos de la papelera y los adjuntos quedan fuera.

> Este archivo queda **en texto plano**: cualquier persona o programa con
> acceso a él puede leer todas tus contraseñas. Bórralo en cuanto termines
> de importarlo en el otro gestor.

## Llevar a otro ordenador

1. Exporta la caja (o copia una [Copia de seguridad](backup)).
2. Instala la aplicación en el otro ordenador.
3. Importa el archivo e indica la contraseña.
4. Si quieres mantener ambos al día, configura
   [Sincronización por carpeta](sincronizacao).
