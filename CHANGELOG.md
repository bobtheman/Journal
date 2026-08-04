# Changelog

All notable changes to Journal are documented here. Each release section is what the
in-app update dialog shows to users when a newer build is available.

## [1.0] (build 10)
- Fixed an "app update" failure that could happen if a previous install prompt was still
  open, and cleaned up the error message shown if it happens again.
- Confirming an update download now takes you to Settings so you can see its progress,
  instead of downloading silently in the background.

## [1.0] (build 9)
- Backups now include your settings and account details, so restoring on a new device
  signs you back in correctly.
- Only the latest backup is kept in Google Drive; older ones are cleaned up automatically.
- Backup and Restore now show a loading indicator while they work.
- Fixed an issue where restoring a backup could fail or corrupt local data.
- Long usernames no longer push the side menu out of shape.
- Entries can no longer be dated or timed in the future.

## [1.0] (build 8)
- Swipe left on an entry in JournalHome to reveal a delete button.
- Multiple entries per day, each with its own time and mood icon.
- Day entries in JournalHome now show a mood colour rolled up from that day's entries.
- Attach an image to an entry; view it full-screen and delete it from the viewer.
- Journal now checks for updates automatically on launch and shows what changed before
  you download.