# Introduction

The **Password Vault** keeps your credentials encrypted on your own
computer. There is no account, no server and no mandatory cloud: the vault
is a file on your disk, opened only by your master password.

## How your data is protected

- Everything is encrypted with **AES-256-GCM**. The key is never written
  to disk: it is derived from your master password every time you open the
  vault.
- Without the master password, the vault file is unreadable — including to
  anyone with access to your computer.
- Nothing leaves your computer on its own. The few features that use the
  network are optional and described in
  [Privacy and network](privacidade-rede).

## Where the vault lives

The vault and preferences live in the application's data folder, inside
your system user profile. To move everything to another computer, see
[Import and export](importar-exportar) and [Backup and restore](backup).

## Where to start

If this is your first time, follow [Getting started](primeiros-passos).
The central piece is the [Master password](senha-mestra) — pick a good one
and don't lose it.
