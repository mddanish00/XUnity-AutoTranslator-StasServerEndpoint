# XUnity-AutoTranslator-StasServerEndpoint

Translation endpoint to support translating using [stas-server](https://github.com/mddanish00/stas-server).

Mainly tested on [PriconneRe-TL](https://github.com/ImaterialC/PriconneRe-TL).

## Requirements

- XUnity-AutoTranslator v5.0.0 or higher
- Working stas-server installation. Please check stas-server [README.md](https://github.com/mddanish00/stas-server?tab=readme-ov-file#user-guide) on how to setup stas-server.


## Installation

1. Install XUnity-AutoTranslator if you not done yet. Please check the project [README.md](https://github.com/bbepis/XUnity.AutoTranslator?tab=readme-ov-file#installation) for instructions.

2. Download pre-compiled dll from Releases and install into `XUnity.AutoTranslator\Translators` folder. (The folder location may different based on loader you are using to use for XUAT)

> DLL Release is compiled with the pinned version of XUnity-AutoTranslator. (Usually, whatever is the latest version at the time of commit.)

3. Run your game once to generate/update the XUAT configuration file. Once the game has run and initialized properly, exit the game.

4. Backup your XUAT configuration file (`AutoTranslatorConfig.ini`). After you have a backup copy, edit the configuration and change the `Endpoint` setting to `StasServer`.  Your `[Service]` section should look like this:
```
[Service]
Endpoint=StasServer
FallbackEndpoint=
```

## Configuration

Example config in `AutoTranslatorConfig.ini`.

```
[StasServer]
StasServerExePath=C:\Users\User\.local\bin\stas-server.exe
ServerPort=14467
ModelsFolderPath=D:\SugoiToolkit\models
EnableCuda=False
DisableCache=False
EnablePreventRetranslation=True
PlayerJPName=ユウキ
PlayerTranslatedName=Yuuki
MaxBatchSize=10
EnableShortDelay=True
DisableSpamChecks=True
LogServerMessages=False
```

- StasServerExePath: Path to stas-server.exe
- ServerPort: Port used to launch the server also communicate with the server.
- ModelsFolderPath: Path to Sugoi Offline Translator models folder.
- EnableCuda: `True` to use CUDA when launching stas-server (Need NVDIA GPU)
- DisableCache: `True` to disable caching translation. (Highly recommended to enable caching)
- EnablePreventRetranslation: `True` to prevent retranslation of translated content when included Player Name in Japanese.
- PlayerJPName: Player Name in Japanese. (Only in Use when EnablePreventRetranslation is `True`)
- PlayerTranslatedName: Translated PlayerJPName. (Only in Use when EnablePreventRetranslation is `True`)
- MaxBatchSize: Number of lines in a translation request.
- EnableShortDelay: Enable the translation delay.
- DisableSpamChecks: Disables the spam checks for this endpoint.
- LogServerMessages: Log stas-server message in XUAT console.


## Building

For developers, it is recommended to use your project XUnity-AutoTranslator DLL to build this project as long it meet the requirements for maximum compatibility.

- Make sure .NET 6 SDK is installed.
- XUnity-AutoTranslator v5.0.0 or higher. 
- Download XUnity.AutoTranslator-Developer-X.X.X.zip. 
- If you using IL2CPP, download XUnity.AutoTranslator-Developer-IL2CPP-X.X.X.zip instead.
- If you want to use your project DLLs, put DLLs in libs folder or libs-il2cpp if using il2cpp.


### Normal Version

1. Unzip XUnity.AutoTranslator-Developer-X.X.X.zip in this repo. Rename `Developer` folder to `libs`.
2. Run the build command.
```
dotnet build -c Release -v n -f net35
```
3. Copy the builded DLL, `build\Release\net35\StasServer.dll` to your project `XUnity.AutoTranslator\Translators` folder.

### IL2CPP Version


1. Unzip XUnity.AutoTranslator-Developer-IL2CPP-X.X.X.zip in this repo. Rename `Developer` folder to `libs-il2cpp`.
2. Run the build command.
```
dotnet build -c Release -v n -f net6.0
```
3. Copy the builded DLL, `build\Release\net6.0\StasServer.dll` to your project `XUnity.AutoTranslator\Translators` folder.

## License

This project is licensed under the [MIT license](./LICENSE).

Copyright for portions of this project are held by [Vin-meido](https://github.com/Vin-meido), 2021 as part of project [XUnity-AutoTranslator-SugoiOfflineTranslatorEndpoint](https://github.com/Vin-meido/XUnity-AutoTranslator-SugoiOfflineTranslatorEndpoint). 

All other copyright for this are held by [mddanish00](https://github.com/mddanish00), 2025.

## Acknowledgement

- Thanks to [MingShiba](https://www.patreon.com/mingshiba) for creating the Sugoi Japanese Toolkit and making high-quality (still machine translation) available to enjoy many untranslated Japanese works.
- Thanks to [Vin-meido](https://github.com/Vin-meido) for the original [XUnity-AutoTranslator-SugoiOfflineTranslatorEndpoint](https://github.com/Vin-meido/XUnity-AutoTranslator-SugoiOfflineTranslatorEndpoint) support. This project is based on that project.