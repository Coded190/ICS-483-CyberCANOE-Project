# Robotic Museum build and run

Unity **6000.6.7f1** (changeset `b22767f04f04`), the version in `ProjectSettings/ProjectVersion.txt`. Both players use the Mono scripting backend. The project is `RoboticMuseum/`. The test hall is 12 m wide, 22 m long, and 4 m to the underside of the roof.

## Mac, where the project is built

This Mac has Unity Hub 3.22.5 and Unity 6000.6.7f1 at `/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app`. Install Windows Build Support (Mono) for that same editor before building the Windows player. The Apple Silicon editor already includes the Mac Mono player. A Windows IL2CPP player is built on Windows, because that path needs the Windows C++ toolchain.

Sign in to Unity Hub and activate a Personal license before the first editor run. Without that license the editor exits before it can import the project.

Open `RoboticMuseum` in that editor. The scene is `Assets/Scenes/StereoTestHall.unity`.

Play mode and the built player:

| Key | Result |
| --- | --- |
| Click | Lock the mouse and look around |
| Escape | Release the mouse |
| WASD | Walk |
| 1 | Mono, 1280x720, head-center camera |
| 2 | Side-by-side, 2560x720, each eye 1280x720 |
| 3 | Red-cyan anaglyph, 1280x720. Red is the left eye, cyan is the right eye |
| [ ] | Interpupillary distance by 1 mm. Default 0.064 m |
| , . | Convergence distance by 0.25 m. Default 3 m |

IPD is the distance in meters between the eye cameras. Convergence is the distance along the head forward axis to the zero-parallax plane. Disparity in pixels is `leftEyeViewportX - rightEyeViewportX`. It is 0 on the convergence plane, positive when the point is closer, and negative when it is farther.

Editor menus:

- `RoboticMuseum/Build macOS` writes `Builds/macOS/RoboticMuseum.app`
- `RoboticMuseum/Build Windows x64` writes `Builds/Windows/RoboticMuseum.exe` and `RoboticMuseum_Data`
- `RoboticMuseum/Capture Stereo Proof` writes `RoboticMuseum/Artifacts/left-eye.png`, `right-eye.png`, `side-by-side.png`, and `anaglyph.png`

Command-line builds from the Mac editor, after the project has been imported once:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics -quit -projectPath RoboticMuseum -executeMethod RoboticMuseum.EditorTools.MuseumBuild.BuildAll -logFile RoboticMuseum/Artifacts/build.log
```

The Mac player log line to confirm is `[StereoRig] startup`.

## Windows machine

Copy the whole `Builds/Windows` folder. `RoboticMuseum.exe` starts only when `RoboticMuseum_Data` is beside it. The machine needs 64-bit Windows 10 version 21H1 or Windows 11, and a GPU with DirectX 11 or 12. Unity does not need to be installed to run the player.

## Windows IL2CPP, later

Install the same Unity 6000.6.7f1 editor on the Windows machine, plus **Windows Build Support (IL2CPP)** and Visual Studio with the C++ workload. Open this project, set the Windows scripting backend to IL2CPP, and build again. Scripts and the scene stay the same.

## Sharing the project

The repository is `https://github.com/Coded190/ICS-483-CyberCANOE-Project.git`. Riki is a collaborator. Robot meshes go in `Assets/Robots/<Name>/` through Git LFS, with each vendor `LICENSE` in that folder. `Library/`, `Builds/`, and `Artifacts/` stay out of git. When the lab needs a player, upload the Windows folder as a GitHub Release zip.
