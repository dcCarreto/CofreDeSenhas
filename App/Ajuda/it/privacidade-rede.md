# Privacy e rete

Per impostazione predefinita, l'applicazione **non accede a internet**. Le
funzioni qui sotto sono facoltative e attivi ciascuna sapendo cosa fa.

## Icone dei servizi online

Quando è attiva, recupera l'icona reale di ogni sito da un servizio
pubblico di icone, inviando **solo il dominio** (per esempio
`github.com`). Nessuna password, nome utente o nota lascia il computer. Le
icone scaricate vengono messe in cache su disco. Quando è disattivata, la
cassaforte mostra solo le iniziali e non tocca mai la rete.

## Controllo aggiornamenti

Quando è attivo, interroga la pagina delle release del progetto per
avvisarti se c'è una versione più recente. Niente viene scaricato
automaticamente, e non viene inviato nulla oltre a quella richiesta.
Disattivato per impostazione predefinita.

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
