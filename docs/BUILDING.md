# Building from source

The published [Release ZIP](https://github.com/BreezeDelegate/can-em-all-trainer/releases/tag/v7.2.0) is the easiest way to install the trainer. This repository also contains the C# source under `src/` for inspection and local builds.

The project targets **.NET 6** with MelonLoader 0.7.3 and Unity IL2CPP wrappers from the **full** installation of Can 'Em All.

## Dependencies

The `src/CanEmAllFullMod.csproj` file expects the following DLLs **one directory above `src/`**, which means the root of this repository when cloning:

| Local filename | Get it from the user's own installed game |
| --- | --- |
| `Main-Assembly-CSharp.dll` | `MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll` |
| `canem-ml.dll` | `MelonLoader/net6/MelonLoader.dll` |
| `canem-harmony.dll` | `MelonLoader/net6/0Harmony.dll` |
| `canem-runtime.dll` | `MelonLoader/net6/Il2CppInterop.Runtime.dll` |
| `canem-audio.dll` | `MelonLoader/Il2CppAssemblies/UnityEngine.AudioModule.dll` |
| `canem-core.dll` | `MelonLoader/Il2CppAssemblies/UnityEngine.CoreModule.dll` |
| `canem-input.dll` | `MelonLoader/Il2CppAssemblies/UnityEngine.InputLegacyModule.dll` |
| `canem-systemcore.dll` | `MelonLoader/Il2CppAssemblies/Il2CppSystem.Core.dll` |
| `canem-mscorlib.dll` | `MelonLoader/Il2CppAssemblies/Il2Cppmscorlib.dll` |
| `canem-tm.dll` | `MelonLoader/Il2CppAssemblies/UnityEngine.TextRenderingModule.dll` |
| `canem-ui.dll` | `MelonLoader/Il2CppAssemblies/UnityEngine.UI.dll` |
| `canem-uimodule.dll` | `MelonLoader/Il2CppAssemblies/UnityEngine.UIModule.dll` |
| `canem-tmp.dll` | `MelonLoader/Il2CppAssemblies/Unity.TextMeshPro.dll` |
| `canem-physics.dll` | `MelonLoader/Il2CppAssemblies/UnityEngine.PhysicsModule.dll` |

**Do not commit or redistribute** these game/Unity/MelonLoader dependencies. They are intentionally excluded from the repository and from the binary release ZIP.

After gathering the matching assemblies from your own installed game, run:

```shell
dotnet build src/CanEmAllFullMod.csproj -c Release
```

The resulting file is `src/bin/Release/net6.0/CanEmAllFullMod.dll`. This project is for the full game only; the separate demo is not a supported installation target.

The release was compiled and checked using game-specific wrappers. Local builds require compatible wrappers and can stop working after game updates.

## Verification notes

v7.2.0 was tested with 24 focused static regression checks, 15 managed persistence tests and a native headless Unity run with 180 tab changes. It was also installed on a Windows PC and passed a subsequent headless startup check with no logged mod errors. **A headless check does not validate every weapon animation, mouse interaction or progression state.**

We intentionally do not configure GitHub Actions for builds: the original game dependencies cannot be redistributed to hosted CI runners, and no extra hosted runner usage is necessary for this community mod.

## Licensing

Source is published for review. There is currently no explicit open-source license. All third-party game assets and code belong to their respective owners; this repository does not redistribute them.
