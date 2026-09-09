# Domande frequenti

## Ho dimenticato la password principale. E adesso?

Non c'è modo di recuperarla — né da parte nostra, né di nessun altro. È il
prezzo della cifratura che protegge la cassaforte dagli altri. Se hai
salvato il **codice QR di backup**, usalo per reimportare la password.
Senza la password o il QR, il contenuto della cassaforte è inaccessibile.
Vedi [Password principale](senha-mestra).

## Dove sono i miei dati?

In un file cifrato nella cartella dati dell'applicazione, all'interno del
tuo profilo utente di sistema. Niente viene inviato a server nostri.
Dettagli in [Introduzione](introducao).

## Come sposto la cassaforte su un altro computer?

Esporta la cassaforte (file cifrato) o copia un backup, installa
l'applicazione sul computer di destinazione e importa. Passo per passo in
[Importare ed esportare](importar-exportar).

## La cassaforte si sincronizza da sola nel cloud?

No. Non esiste nessun cloud nostro. Puoi configurare la
[Sincronizzazione cartella](sincronizacao) o un
[Database](banco-de-dados) che controlli **tu** — allora la
sincronizzazione passa dalla tua infrastruttura.

## È sicuro? Quale cifratura viene usata?

AES-256-GCM, con la chiave derivata dalla tua password principale a ogni
apertura e mai conservata. La forza pratica dipende soprattutto dal fatto
che tu scelga una buona password principale.

## Ho perso il telefono con il codice QR. E Windows Hello?

[Windows Hello](windows-hello) è legato al tuo account Windows su questo
computer; il QR è indipendente. Se riesci ancora ad aprire la cassaforte
(con la password o con Hello), genera un **nuovo codice QR** in
*Impostazioni → Sicurezza* e conservalo.

## Posso usarla senza installare nient'altro oltre all'applicazione?

Sì. La cassaforte locale non ha bisogno di database, server o account. Le
funzioni di rete sono tutte facoltative — vedi
[Privacy e rete](privacidade-rede).
