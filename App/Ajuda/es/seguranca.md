# Informe de seguridad y auditoría

La aplicación evalúa la salud de la caja sin que nada salga del ordenador
(salvo la comprobación de filtraciones, descrita abajo).

## Informe de seguridad

Da una **puntuación global** y lista los problemas por tipo:

- **Contraseñas débiles**: cortas o predecibles. Sustitúyelas con el
  [Generador de contraseñas](gerador).
- **Contraseñas repetidas**: la misma contraseña en servicios distintos.
  El mayor riesgo práctico: corrige estas primero.
- **Contraseñas antiguas**: sin cambiar desde hace mucho, cuando el
  historial de uso está activo.

Al pulsar un elemento se filtra la lista principal a las entradas
afectadas.

## Auditoría de la caja

Un barrido más detallado, credencial por credencial, con el motivo de cada
señalamiento. Útil para una revisión completa de vez en cuando.

## Comprobación de filtraciones

Compara tus contraseñas con bases públicas de filtraciones conocidas
usando **k-anonymity**: solo se envía una parte del hash de la contraseña,
nunca la contraseña ni el servicio. Si aparece una coincidencia, cambia la
contraseña cuanto antes.

## Historial de puntuación

Si mantienes el historial de uso activo, la aplicación registra cómo
evoluciona la puntuación con el tiempo, para que veas si la caja está
mejorando.
