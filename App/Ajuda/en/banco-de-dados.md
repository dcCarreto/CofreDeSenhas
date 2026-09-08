# Shared database

An advanced feature for those who want to keep the vault in a **database
you host** (networked SQLite, PostgreSQL or MySQL) instead of a local
file.

## Who it's for

- Teams or families sharing a set of credentials.
- Those who already run a database server and prefer to centralize there.

To sync only **your own** devices, [Folder sync](sincronizacao) is usually
simpler.

## How it works

- You provide the database address and credentials. The server password is
  stored encrypted in the local vault.
- Data stays encrypted with your master password **before** it goes to the
  database — the database server never sees plaintext passwords.
- The same merge engine as sync (most recent edit wins, conflicts go to a
  decision screen) keeps the devices aligned.

## Connect and disconnect

In **Settings → Backup & sync**. On disconnect, the vault goes back to
operating with the local copy only.

> Protecting the database server (access, network, backups) is your
> responsibility. The app handles the encryption of the content, not the
> security of your infrastructure.
