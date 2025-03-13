# Schedule 1 Mod

A MelonLoader mod for Schedule 1.

## Prerequisites

1. Install [MelonLoader](https://melonwiki.xyz/#/?id=automated-installation) on your Schedule 1 game
2. Make sure you have Visual Studio 2019/2022 with .NET Framework 4.7.2 development tools installed

## Setup

1. Clone this repository
2. Open the solution in Visual Studio
3. Set the `GamePath` environment variable to your Schedule 1 installation directory (usually `C:\Program Files (x86)\Steam\steamapps\common\Schedule 1`)
4. Build the solution
5. Copy the generated `Schedule1Mod.dll` from the `bin\Debug\net472` folder to your game's `Mods` folder

## Development

The main mod code is in `Schedule1Mod.cs`. Key methods:

- `OnApplicationStart`: Called when the game starts
- `OnSceneWasLoaded`: Called when a new scene is loaded
- `OnUpdate`: Called every frame

## Notes

- Make sure to run the game through Steam after installing MelonLoader
- Check the MelonLoader logs in the game directory if you encounter any issues 