# Backup and restore

A backup is an encrypted copy of the whole vault, opened by the same
master password. It's there to recover from a disk failure, an accidental
wipe, or a change of computer.

## Automatic backup

In **Settings → Backup & sync → Backup and restore** you set:

- the **frequency** (for example, weekly);
- the **maximum number of backups** kept — the oldest are discarded when
  the limit is reached.

Automatic backups live in the application's data folder. Since they're on
the same computer, they **do not replace** an external copy.

## Manual backup

On the same screen, generate a backup on the spot and save it wherever you
want — preferably on a USB drive or a file service you control.

## Restore

Pick a backup file and confirm. The current contents are **replaced** by
the backup's. If the vault is connected to a database, restoring behaves
differently — read the notice on screen.

## Export

To migrate or store outside the backup format, see
[Import and export](importar-exportar).
