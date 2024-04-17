# SongPlayHistory

[![release](https://img.shields.io/github/v/release/qe201020335/SongPlayHistory?color=Green&style=for-the-badge)](https://github.com/qe201020335/SongPlayHistory/releases/latest)

Track all your scores and song votes. This is the actively maintained fork of the [fork](https://github.com/Shadnix-was-taken/BeatSaber-SongPlayHistoryContinued) of the currently unmaintained [SongPlayHistory](https://github.com/swift-kim/SongPlayHistory) mod.

![Screenshot](Screenshot.png)

## Features
- Keep detailed track of your plays
- Show how many times you've played a single beatmap
- Interop with BeatSaverVoting and DiTails to track your vote
- Visualize your song preferences (👍/👎) (if you don't have BeatSaverVoting installed)

## Requirements
- BSIPA
- BeatSaberMarkupLanguage
- BS Utils
- SiraUtil

Available in [ModAssistant](https://github.com/Assistant/ModAssistant) or on [BeatMods](https://beatmods.com/#/mods)

## Notes
- Recording play data begins when you first install this plugin. (This doesn't apply to play counts.)
- The data file `SongPlayData.json` is created in Beat Saber's `UserData` directory. You can delete individual records from there if you want.
- The data are not uploaded anywhere so you have to **backup** the file when re-installing the game.
- If you run into any problems, please contact me either via Discord _(Search me in the BSMG discord server)_ or by open an issue.

## For Modders
### Vote Data
- To access the vote data, use `Zenject` and inject `IVoteTracker`.
- `IVoteTracker` is installed into `PCAppInit`, using `SiraUtil`'s `Location.App`.
### Play History Data
- Inject `IRecordManager`
- `IRecordManager` is installed into `PCAppInit`, using `SiraUtil`'s `Location.App`.
### Scoring Data Cache
- Inject `IScoringCacheManager`
- `IScoringCacheManager` is installed into `PCAppInit`, using `SiraUtil`'s `Location.App`.

## Credits
- [swift-kim](https://github.com/swift-kim)
- [Shadnix](https://github.com/Shadnix-was-taken)
- And everyone contributed to this project
