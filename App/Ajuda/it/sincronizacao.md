# Sincronizzazione cartella

Mantiene la stessa cassaforte su più tuoi dispositivi tramite un **file
condiviso** in una cartella che scegli tu — di solito una cartella di un
servizio di file che usi già (sincronizzare il file stesso è compito di
quel servizio).

## Come funziona

- L'applicazione scrive un file cifrato nella cartella scelta, protetto da
  una chiave derivata dalla tua password principale.
- Ogni dispositivo legge e scrive quel file periodicamente.
- Quando due dispositivi modificano la stessa credenziale, l'applicazione
  risolve in base alla modifica più recente; i conflitti che richiedono
  una decisione compaiono su una schermata dedicata.

## Configurarla

In **Impostazioni → Backup e sincronizzazione → Sincronizzazione**. Indica
la cartella e imposta la frequenza. Ripeti su ogni dispositivo, usando la
**stessa password principale** e la **stessa cartella**.

## Cosa non è

Questo **non** è un servizio cloud dell'applicazione. Non c'è nessun
nostro server nel mezzo, nessun account, e niente viene inviato a noi. È
la tua infrastruttura che sincronizza il tuo file.

Se l'obiettivo è condividere una cassaforte tra persone o macchine in modo
più robusto, vedi [Database condiviso](banco-de-dados).
