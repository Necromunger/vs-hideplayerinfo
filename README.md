# HidePlayerInfo

A Vintage Story mod designed for PVP servers. Prevents players from easily locating each other by hiding map pins, nametags, and player data at the server level.

## Features

- **Hide player map pins** — Player positions are never sent to other clients, so pins cannot be revealed even with a modified client.
- **Hide nametags** — Player nametags are hidden unless you are looking directly at them within 20 meters.
- **Hide 3rd-person nametags** — Floating name labels above other players are suppressed.
- **Server-side enforcement** — Map data is blocked at the server before it reaches clients, preventing client-side workarounds.

## Installation

1. Download the latest release.
2. Place the mod `.zip` or folder into your `Mods` directory.
3. The mod is required on **both client and server**.

## How It Works

The mod uses Harmony patches to intercept game behavior at runtime:

| What | How |
|---|---|
| Map pins | A prefix patch on `WorldMapManager.SendMapDataToClient` blocks `PlayerMapLayer` data from being sent to clients. The `mapHideOtherPlayers` world config is also enabled. |
| Nametag rendering | A prefix patch on `EntityBehaviorNameTag.OnRenderFrame` skips rendering for all players except yourself. |
| Show on target only | A prefix patch on `EntityBehaviorNameTag.ShowOnlyWhenTargeted` forces the value to `true`. A JSON asset patch adds `showtagonlywhentargeted` and a 20-meter `renderRange` to the player entity definition. |

## Requirements

- Vintage Story
- Required on both client and server
