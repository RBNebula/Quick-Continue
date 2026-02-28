# Quick-Continue

Adds a `Continue` button to the main menu between `New Game` and `Load Game`.

## Behavior

- Clicking `Continue` loads your last-used save immediately.
- No load menu opens first.
- If the remembered save is missing/corrupt, it falls back to the most recent valid save.

## Install

1. Build `QuickContinue.csproj` (or use `QuickContinue/bin/Release/net472/QuickContinue.dll`).
2. Copy `QuickContinue.dll` into `BepInEx/plugins`.

## Config

Generated config: `BepInEx/config/QuickContinue/com.rbplex.quickcontinue.cfg`

- `Continue.LastSaveName`: tracked save used by Continue.
- `Continue.FallbackToMostRecentSave`: fallback when tracked save cannot be used.
