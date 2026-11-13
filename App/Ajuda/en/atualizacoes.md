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

## How to turn it off

Under *Settings → Behavior → Update automatically*. When off, the app checks
nothing, and an update that was already downloaded is discarded. To update
manually, download the new version from the project's releases page.

## Depending on how you installed

- **Installer or winget**: as described above. If you installed "for all
  users" (in Program Files), you need to click **Install now**, because
  Windows asks for administrator permission; the footer notice tells you
  when that's the case.
- **Portable executable**: the file itself is replaced with the new version.
  It must be in a folder you can write to, such as Downloads or the desktop.
- **Microsoft Store**: the Store handles updates, according to its app update
  setting. The app downloads the update as soon as the Store offers it and
  installs it when the vault is locked. The "Update automatically" option is
  not shown.
- **Linux, AppImage**: same as on Windows; the `.AppImage` file is replaced
  with the new one.
- **Linux, `.tar.gz` package or built from source**: does not update itself.
  Download the new version from the releases page.

The `cofre` command-line tool and the standalone password generator
(`GeradorDeSenhas-*-portatil.exe`) are separate files and **do not** update
themselves. Download the new versions from the releases page whenever you
like.

## Your data during an update

The vault, backups and preferences are not touched: they stay in the app's
data folder, and the new version opens the same vault. Uninstalling the app,
including the Store version, does not delete the vault either. The downloaded
file stays in the system's temporary folder until it is installed and is
deleted the next time the app opens. To be safe, keep your
[backups](backup) up to date.
