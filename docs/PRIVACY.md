# Privacy Policy — Journal

_Last updated: 2026-08-06_

Journal is a personal journaling app. This policy explains what data the app
handles and how.

## Data the app stores

- **Journal entries** (text, mood tags, and any photos you attach) are stored
  **locally on your device only**, in an encrypted database (SQLCipher). They
  are never sent to the developer or any third party.
- **Your account password** is never stored in plain text. It is used to
  derive an encryption key (PBKDF2) for the local database and is not
  transmitted anywhere.
- **Biometric unlock**, if you enable it, stores your password in your
  device's secure hardware-backed storage (Android Keystore / iOS Keychain)
  so Face/Fingerprint unlock can retrieve it locally. It never leaves your
  device.

## Google Drive backup (optional)

If you choose to enable backup, Journal asks for Google sign-in with the
restricted `drive.file` scope. This scope only allows the app to see and
manage files **it created itself** — it cannot read any other file in your
Google Drive.

- Your journal database and settings are uploaded, encrypted, to a folder
  named `JournalApp` in your own Google Drive.
- This data is only ever sent between your device and your own Google
  account. The developer has no access to it.
- You can revoke access at any time from
  [Google Account permissions](https://myaccount.google.com/permissions), or
  by deleting the `JournalApp` folder from your Drive.

## Camera

The app requests camera access only to let you attach a photo to a journal
entry. Photos are stored the same way as journal entries (see above).

## Data we do not collect

Journal contains no analytics, ads, or crash-reporting SDKs. The developer
does not receive any usage data, journal content, or personal information
from the app.

## Deleting your data

- Delete all local data any time via in-app settings ("Delete all local
  data"), which erases the local database and any saved biometric
  credential.
- Delete your Drive backup by removing the `JournalApp` folder from your
  Google Drive, or by revoking the app's Google account access.

## Contact

Questions about this policy: open an issue at
https://github.com/bobtheman/Journal/issues
