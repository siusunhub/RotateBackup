# RotateBackup

RotateBackup is a simple Windows program for rotating backups.

It copies a file or folder into a backup sequence.

---

## How It Works

When RotateBackup runs:

- The oldest copy is deleted
- Other copies move back one position
- The latest source is copied to the newest position

The newest backup is always kept in the first copy position.

---

## Features

- Backup a folder
- Backup a single file
- Keep multiple rotated copies
- Delete the oldest copy automatically
- Run a command before backup
- Run a command after backup
- Can be used with Windows Task Scheduler

---

## Requirement

- Windows

---

## Real Use Case 1: Weekly FTP Backup

A backup application in another physical location uploads a full backup image by FTP every week.

RotateBackup keeps rotated copies of that FTP backup for safety.

This helps protect against overwrite, failed upload, corrupted backup, or accidental deletion.

---

## Real Use Case 2: Monthly Apache Log Rotation

RotateBackup can be used to backup Apache log files every month.

After the backup, a post-command can stop Apache, delete the active log files, and start Apache again.

This keeps old logs in backup storage and lets Apache continue with fresh log files.

---

## Install / Schedule

1. Open RotateBackup.
2. Set the source file or folder.
3. Set the backup destination.
4. Set the number of copies.
5. Add a pre-command or post-command if needed.
6. Copy the command shown on the setup screen.
7. Add the command to Windows Task Scheduler.
8. Set the task schedule as needed.
