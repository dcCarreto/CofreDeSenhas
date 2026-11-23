# Updates

The app **updates itself**. You don't need to download or click anything.

## How it works

1. When you open the vault, and then every 6 hours, the app checks the
   project's releases page.
2. If there is a new version, it is downloaded in the background. Before
   accepting it, the app checks the file's digital signature and SHA-256
   hash; if anything doesn't match, the update is discarded.
3. Once the update is ready, the footer shows **Update ready: it will be
   installed when the vault is locked**.
4. The next time the vault is locked (with *Lock now* or after inactivity),
   the app closes, installs the new version and opens again, already on the
   unlock screen. Just type your master password as usual.

To install right away, use **Install now** in the footer: it shows what's
changed in the version and asks for confirmation before closing the app. The
**X** next to it only hides the notice; the update stays scheduled for the
next lock.

No vault data is sent. The check and the download are public reads, with
nothing that identifies you or your computer. See also
[Privacy and network](privacidade-rede).

## Always on

There is no option to turn it off: this is how fixes, including security
fixes, reach everyone without relying on anyone remembering to update.

## Depending on how you installed

- **Installer or winget**: as described above. If you installed "for all
  users" (in Program Files), you need to click **Install now**, because
  Windows asks for administrator permission; the footer notice tells you
  when that's the case.
- **Portable executable (discontinued)**: on the next update, the app is
  installed for your user only, without asking for administrator permission,
  with the same vault and preferences. After that, the portable file just
  opens the installed version and can be deleted.
- **Microsoft Store**: the Store handles updates, according to its app update
  setting. The app downloads the update as soon as the Store offers it and
  installs it when the vault is locked.
- **Linux, AppImage**: same as on Windows; the `.AppImage` file is replaced
  with the new one.
- **Linux, `.deb` package**: same as on Windows, without asking for a
  password; the new package is installed over the old one.
- **Linux, built from source**: does not update itself. Update the source
  code and build it again.

The `cofre` command-line tool and the standalone password generator
(`GeradorDeSenhas-*-win-x64.exe`) are separate files and **do not** update
themselves. Download the new versions from the releases page whenever you
like.

## Your data during an update

The vault, backups and preferences are not touched: they stay in the app's
data folder, and the new version opens the same vault. Uninstalling the app,
including the Store version, does not delete the vault either. The downloaded
file stays in the system's temporary folder until it is installed and is
deleted the next time the app opens. To be safe, keep your
[backups](backup) up to date.
