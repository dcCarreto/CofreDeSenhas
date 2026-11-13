# Privacy and network

By default, the app **does not access the internet**. The features below
are optional and you turn each one on knowing what it does.

## Online service icons

When on, it fetches each site's real icon from a public icon service,
sending **only the domain** (for example `github.com`). No password,
username or note leaves the computer. Downloaded icons are cached on disk.
When off, the vault shows initials only and never touches the network.

## Update automatically

On by default. Checks the project's releases page when the vault is opened
and every 6 hours; when there is a new version, it downloads it in the
background, checks its signature and SHA-256 hash, and installs it when the
vault is locked. The app closes and reopens already updated, on the unlock
screen. To install right away, use **Install now** in the footer. No vault
data is sent. When installed from the Microsoft Store, the Store itself
handles updates and this option is not shown.

## Breach check

On demand, compares your passwords against public breach databases using
**k-anonymity**: only a prefix of the password hash is sent. See
[Security report](seguranca).

## Sync and database

If you set up [Folder sync](sincronizacao) or a
[Database](banco-de-dados), the traffic goes to **your** folder or
**your** server — never to a service of ours.

## Privacy mode

The eye button in the title bar blurs the sensitive values on screen, so
you can open the vault near other people without exposing anything.
