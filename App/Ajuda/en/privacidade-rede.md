# Privacy and network

By default, the app **does not access the internet**. The features below
are optional and you turn each one on knowing what it does.

## Online service icons

When on, it fetches each site's real icon from a public icon service,
sending **only the domain** (for example `github.com`). No password,
username or note leaves the computer. Downloaded icons are cached on disk.
When off, the vault shows initials only and never touches the network.

## Check for updates

When on, it queries the project's releases page to let you know if there's
a newer version. Nothing is downloaded automatically, and nothing beyond
that query is sent. Off by default.

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
