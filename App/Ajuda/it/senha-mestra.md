# Password principale

La password principale è l'unica chiave della cassaforte. Da essa
l'applicazione deriva — in memoria — la chiave che decifra i tuoi dati.
**Non viene conservata da nessuna parte**.

## Non può essere recuperata

Non esiste un "ho dimenticato la password". Se perdi la password
principale, il contenuto della cassaforte diventa inaccessibile — è
esattamente così che la cifratura ti protegge dagli altri. Perciò:

- scegli una **frase lunga** (più parole), facile da ricordare e
  difficile da indovinare;
- salva il **codice QR di backup** (menu *Sicurezza → Rigenera codice
  QR*) e tienilo lontano dal computer;
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

## Windows Hello

[Windows Hello](windows-hello) ti permette di sbloccare con la biometria
invece di digitare la password principale — ma la password principale
resta la vera chiave ed è ancora necessaria per le operazioni delicate.
