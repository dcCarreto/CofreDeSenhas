# Password principale

La password principale è l'unica chiave della cassaforte. Da essa
l'applicazione deriva — in memoria — la chiave che decifra i tuoi dati.
**Non viene conservata da nessuna parte**.

## Senza chiave di recupero, non può essere recuperata

Per impostazione predefinita, non esiste un "ho dimenticato la password".
Se perdi la password principale senza aver attivato la **chiave di
recupero** (vedi sotto), il contenuto della cassaforte diventa
inaccessibile — è esattamente così che la cifratura ti protegge dagli
altri. Perciò:

- scegli una **frase lunga** (più parole), facile da ricordare e
  difficile da indovinare;
- salva il **codice QR di backup** (menu *Sicurezza → Rigenera codice
  QR*) e tienilo lontano dal computer;
- attiva la **chiave di recupero** come seconda rete di sicurezza;
- se vuoi, tieni una copia scritta in un luogo fisico sicuro.

## Cambiare la password principale

In **Impostazioni → Sicurezza → Cambia password principale**. L'intera
cassaforte viene ricifrata con la nuova chiave. Se usi un database o la
[Sincronizzazione cartella](sincronizacao), la modifica riguarda gli
altri dispositivi — leggi la schermata di conferma prima di proseguire.

> Dopo che la modifica segnala l'esito positivo, l'applicazione si riavvia
> da sola. Non toccare la cassaforte durante quell'intervallo.

## Codice QR di backup

È la tua password principale codificata come immagine, da reimportare nel
caso dimentichi quella digitata. Tratta il codice QR con la stessa cura
della password: chi lo possiede può aprire la tua cassaforte.

## Chiave di recupero

In *Impostazioni → Sicurezza → Chiave di recupero*, oppure già alla
creazione della cassaforte. Genera un segreto di 160 bit (in gruppi di 4
caratteri) che permette di reimpostare la password principale senza sapere
quella vecchia — chi ha quel segreto può farlo. Per questo annotalo e
tienilo **lontano dal computer**, con la stessa cura della password
principale stessa: vale quanto lei.

Hai dimenticato la password principale e avevi attivato la chiave? Nella
schermata di accesso, fai clic su "Ho dimenticato la password principale",
digita il segreto annotato e scegli una password nuova — la cassaforte
viene ricifrata con quella. A ogni recupero e a ogni cambio della password
principale viene generato un segreto nuovo; annota sempre il più recente,
quello precedente smette di funzionare. Attivarla, rigenerarla o
disattivarla richiede sempre la password principale attuale, anche a
cassaforte già sbloccata.

## Windows Hello

[Windows Hello](windows-hello) ti permette di sbloccare con la biometria
invece di digitare la password principale — ma la password principale
resta la vera chiave ed è ancora necessaria per le operazioni delicate.
