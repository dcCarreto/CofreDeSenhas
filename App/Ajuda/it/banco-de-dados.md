# Database condiviso

Una funzione avanzata per chi vuole tenere la cassaforte in un **database
gestito da te** (SQLite in rete, PostgreSQL o MySQL) invece che in un file
locale.

## A chi è rivolta

- Team o famiglie che condividono un insieme di credenziali.
- Chi gestisce già un server di database e preferisce centralizzare lì.

Per sincronizzare solo i **tuoi** dispositivi, la
[Sincronizzazione cartella](sincronizacao) è di solito più semplice.

## Come funziona

- Fornisci l'indirizzo e le credenziali del database. La password del
  server viene salvata cifrata nella cassaforte locale.
- I dati restano cifrati con la tua password principale **prima** di
  arrivare al database — il server del database non vede mai le password
  in chiaro.
- Lo stesso motore di merge della sincronizzazione (vince la modifica più
  recente, i conflitti vanno a una schermata di decisione) tiene allineati
  i dispositivi.

## Connettere e disconnettere

In **Impostazioni → Backup e sincronizzazione**. Alla disconnessione, la
cassaforte torna a operare solo con la copia locale.

> Proteggere il server del database (accesso, rete, backup) è tua
> responsabilità. L'applicazione si occupa della cifratura del contenuto,
> non della sicurezza della tua infrastruttura.
