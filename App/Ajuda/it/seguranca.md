# Rapporto di sicurezza e verifica

L'applicazione valuta lo stato della cassaforte senza che niente lasci il
computer (tranne il controllo violazioni, descritto sotto).

## Rapporto di sicurezza

Fornisce un **punteggio complessivo** ed elenca i problemi per tipo:

- **Password deboli**: corte o prevedibili. Sostituiscile con il
  [Generatore di password](gerador).
- **Password riutilizzate**: la stessa password su servizi diversi. Il
  rischio pratico maggiore — risolvi prima questi.
- **Password vecchie**: non cambiate da molto tempo, quando la cronologia
  d'uso è attiva.

Facendo clic su una voce, l'elenco principale viene filtrato sulle voci
interessate.

## Verifica della cassaforte

Un'analisi più dettagliata, credenziale per credenziale, con il motivo di
ogni segnalazione. Utile per una revisione completa di tanto in tanto.

## Controllo violazioni

Confronta le tue password con database pubblici di violazioni note usando
la **k-anonimità**: viene inviata solo una parte dell'hash della password,
mai la password né il servizio. Se compare una corrispondenza, cambia la
password il prima possibile.

## Cronologia del punteggio

Se tieni attiva la cronologia d'uso, l'applicazione registra come il
punteggio evolve nel tempo, così puoi vedere se la cassaforte sta
migliorando.
