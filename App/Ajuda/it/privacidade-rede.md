# Privacy e rete

Per impostazione predefinita, l'applicazione usa internet solo per cercare e
scaricare i propri aggiornamenti, senza inviare alcun tuo dato (vedi
[Aggiornamenti](atualizacoes)); si può disattivare. Le altre funzioni qui
sotto sono facoltative e attivi ciascuna sapendo cosa fa.

## Icone dei servizi online

Quando è attiva, recupera l'icona reale di ogni sito da un servizio
pubblico di icone, inviando **solo il dominio** (per esempio
`github.com`). Nessuna password, nome utente o nota lascia il computer. Le
icone scaricate vengono messe in cache su disco. Quando è disattivata, la
cassaforte mostra solo le iniziali e non tocca mai la rete.

## Aggiorna automaticamente

Attivo per impostazione predefinita. Interroga la pagina delle release del
progetto all'apertura della cassaforte e ogni 6 ore; se c'è una nuova
versione, la scarica in background, ne verifica la firma e lo SHA256 e la
installa quando la cassaforte viene bloccata. L'app si chiude e si riapre
già aggiornata, sulla schermata di sblocco. Per installarla subito, usa
**Installa ora** nel piè di pagina. Nessun dato della cassaforte viene
inviato. Se installata dal Microsoft Store, gli aggiornamenti li gestisce
lo Store e l'opzione non compare.

## Controllo violazioni

Su richiesta, confronta le tue password con database pubblici di
violazioni usando la **k-anonimità**: viene inviato solo un prefisso
dell'hash della password. Vedi [Rapporto di sicurezza](seguranca).

## Sincronizzazione e database

Se configuri la [Sincronizzazione cartella](sincronizacao) o un
[Database](banco-de-dados), il traffico va alla **tua** cartella o al
**tuo** server — mai a un servizio nostro.

## Modalità privacy

Il pulsante a forma di occhio nella barra del titolo sfoca i valori
sensibili sullo schermo, così puoi aprire la cassaforte vicino ad altre
persone senza esporre nulla.
