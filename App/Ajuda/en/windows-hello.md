# Windows Hello

Lets you **unlock the vault with biometrics** (fingerprint, face or
Windows PIN) instead of typing the master password every time.

## How to enable it

In **Settings → Security → Enable Windows Hello**. Windows asks for your
verification and the app starts offering the Hello unlock on the opening
screen.

## What changes and what doesn't

- The **master password is still the real key** to the vault. Hello only
  releases the access stored under system protection.
- Sensitive operations (like changing the master password) still ask for
  the master password.
- If Hello fails or the device doesn't support it, you can always sign in
  with the master password.

## Scope

The Hello credential is tied to **your Windows account on this
computer**. It doesn't travel with the vault file: on another computer,
you enable Hello again (or use the master password).

## Disable

On the same screen, *Disable Windows Hello*. The protected access is
removed and the vault goes back to opening by master password only.
