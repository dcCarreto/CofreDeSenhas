# Frequently asked questions

## I forgot the master password. Now what?

There is no way to recover it — not by us, not by anyone. It's the price
of the encryption that protects the vault from others. If you saved the
**backup QR code**, use it to re-import the password. Without the password
or the QR, the vault's contents are inaccessible. See
[Master password](senha-mestra).

## Where is my data?

In an encrypted file in the application's data folder, inside your system
user profile. Nothing is sent to servers of ours. Details in
[Introduction](introducao).

## How do I move the vault to another computer?

Export the vault (encrypted file) or copy a backup, install the app on the
target and import. Step by step in
[Import and export](importar-exportar).

## Does the vault sync itself to the cloud?

No. There is no cloud of ours. You can set up
[Folder sync](sincronizacao) or a [Database](banco-de-dados) that **you**
control — then syncing goes through your infrastructure.

## Is it safe? What encryption is used?

AES-256-GCM, with the key derived from your master password on every open
and never stored. The practical strength depends above all on you
choosing a good master password.

## I lost the phone with the QR code. And Windows Hello?

[Windows Hello](windows-hello) is tied to your Windows account on this
computer; the QR is independent. If you can still open the vault (by
password or by Hello), generate a **new QR code** in *Settings → Security*
and keep it.

## Can I use it without installing anything besides the app?

Yes. The local vault needs no database, server or account. The network
features are all optional — see
[Privacy and network](privacidade-rede).
