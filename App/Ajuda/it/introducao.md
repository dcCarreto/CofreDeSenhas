# Introduzione

La **Cassaforte delle password** conserva le tue credenziali cifrate sul
tuo computer. Non c'è nessun account, nessun server e nessun cloud
obbligatorio: la cassaforte è un file sul tuo disco, aperto solo dalla tua
password principale.

## Come sono protetti i tuoi dati

- Tutto è cifrato con **AES-256-GCM**. La chiave non viene mai scritta su
  disco: viene derivata dalla tua password principale ogni volta che apri
  la cassaforte.
- Senza la password principale, il file della cassaforte è illeggibile —
  anche per chi ha accesso al tuo computer.
- Niente lascia il tuo computer da solo. Le poche funzioni che usano la
  rete sono facoltative e descritte in
  [Privacy e rete](privacidade-rede).

## Dove risiede la cassaforte

La cassaforte e le preferenze risiedono nella cartella dati
dell'applicazione, all'interno del tuo profilo utente di sistema. Per
spostare tutto su un altro computer, vedi
[Importare ed esportare](importar-exportar) e
[Backup e ripristino](backup).

## Da dove cominciare

Se è la prima volta, segui [Primi passi](primeiros-passos). Il fulcro è
la [Password principale](senha-mestra) — scegline una buona e non
perderla.
