# Folder sync

Keeps the same vault across several of your devices through a **shared
file** in a folder you choose — usually a folder of a file service you
already use (syncing the file itself is that service's job).

## How it works

- The app writes an encrypted file to the chosen folder, protected by a
  key derived from your master password.
- Each device reads and writes that file periodically.
- When two devices change the same credential, the app resolves it by the
  most recent edit; conflicts that need a decision show up on their own
  screen.

## Set it up

In **Settings → Backup & sync → Sync**. Point to the folder and set the
frequency. Repeat on each device, using the **same master password** and
the **same folder**.

## What it is not

This is **not** an app cloud service. There is no server of ours in the
middle, no account, and nothing is sent to us. It is your infrastructure
syncing your file.

If the goal is to share a vault between people or machines more robustly,
see [Shared database](banco-de-dados).
