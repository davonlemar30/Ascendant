# Local build hard stop

The carried goal01 brief says: "If one problem has eaten three attempts or about 90 minutes, stop on it, write up what you tried, and move on." No local Unity build is resumed without an answer to this question.

1. `corridor-web.log`: disk full while Bee writes build DAG/cache files; failed build, exit1. No usable artifact.
2. Removed ONLY this run's completed baseline Library, preserving its successful Web artifact;1.7GiB free. `corridor-web-2.log`: incomplete binlog left by the disk-full event, failed build, exit1.
3. Removed ONLY the Corridor worktree's generated Bee cache;1.9GiB free. `corridor-web-3.log`: mechanical1,203PASS, compilation proceeds, then IL2CPP/cache writing fills disk again. Failed build, exit1. Incomplete data file is not accepted as a player.

All game source compiles; no C# compile diagnostic is present. The build system's generic "scripts had compiler errors" in the first two failures follows its cache write failures. The third attempt explicitly reports `IOException: Disk full` and "No space left on device". Logs above are preserved.

Question for Davon: Once more disk space is available (about8GB of headroom for Web and Android builds), may I resume the stopped local builds, phone/frozen browser checks and final combined fixture/APK work?

The owner checkout and other existing worktrees were untouched. The failed generated Bee cache in this run's Corridor worktree was removed after the third process exited, to leave room for source/art and CI artifacts. No fourth local build was attempted. PR code/art and required CI checks can proceed independently.
