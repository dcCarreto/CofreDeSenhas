# Aggiornamenti

L'app **si aggiorna da sola**. Non devi scaricare né cliccare niente.

## Come funziona

1. All'apertura della cassaforte, e poi ogni 6 ore, l'app controlla la
   pagina delle release del progetto.
2. Se c'è una nuova versione, viene scaricata in background. Prima di
   accettarla, l'app verifica la firma digitale e lo SHA256 del file; se
   qualcosa non corrisponde, l'aggiornamento viene scartato.
3. Con l'aggiornamento pronto, il piè di pagina mostra **Aggiornamento
   pronto: verrà installato al blocco della cassaforte**.
4. Al blocco successivo della cassaforte (con *Blocca ora* o per inattività),
   l'app si chiude, installa la nuova versione e si riapre, già sulla
   schermata di sblocco. Basta digitare la password principale, come sempre.

Per installarlo subito, usa **Installa ora** nel piè di pagina: mostra cosa
cambia nella versione e chiede conferma prima di chiudere l'app. La **X**
accanto nasconde solo l'avviso; l'aggiornamento resta programmato per il
blocco successivo.

Nessun dato della cassaforte viene inviato. Il controllo e il download sono
letture pubbliche, senza nulla che identifichi te o il tuo computer. Vedi
anche [Privacy e rete](privacidade-rede).

## Sempre attivo

Non c'è un'opzione per disattivarlo: così le correzioni, comprese quelle di
sicurezza, arrivano a tutti senza dipendere dal ricordarsi di aggiornare.

## In base a come l'hai installata

- **Programma di installazione o winget**: come descritto sopra. Se l'hai
  installata "per tutti gli utenti" (in Programmi), devi cliccare su
  **Installa ora**, perché Windows chiede l'autorizzazione di amministratore;
  l'avviso nel piè di pagina te lo segnala.
- **Eseguibile portatile (non più distribuito)**: al prossimo aggiornamento,
  l'app viene installata solo per il tuo utente, senza chiedere
  l'autorizzazione di amministratore, con la stessa cassaforte e le stesse
  preferenze. Da quel momento il file portatile apre soltanto la versione
  installata e puoi eliminarlo.
- **Microsoft Store**: aggiorna lo Store, secondo la sua impostazione di
  aggiornamento delle app. L'app scarica l'aggiornamento appena lo Store lo
  offre e lo installa al blocco della cassaforte.
- **Linux, AppImage**: come su Windows; il file `.AppImage` viene sostituito
  con quello nuovo.
- **Linux, pacchetto `.deb`**: come su Windows, senza chiedere la password;
  il nuovo pacchetto viene installato sopra quello vecchio.
- **Linux, compilato dal codice sorgente**: non si aggiorna da solo.
  Aggiorna il codice e ricompilalo.

La riga di comando `cofre` e il generatore di password autonomo
(`GeradorDeSenhas-*-win-x64.exe`) sono file separati e **non** si aggiornano
da soli. Scarica le nuove versioni dalla pagina delle release quando vuoi.

## I tuoi dati durante l'aggiornamento

La cassaforte, i backup e le preferenze non vengono toccati: restano nella
cartella dei dati dell'app, e la nuova versione apre la stessa cassaforte.
Disinstallare l'app, inclusa la versione dello Store, non elimina nemmeno la
cassaforte. Il file scaricato resta nella cartella temporanea del sistema
finché non viene installato ed è eliminato alla successiva apertura dell'app.
Per prudenza, tieni aggiornati i [backup](backup).
