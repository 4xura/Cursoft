<img src="cursoft-logo.png" align="right" width="64" alt="Cursoft logo">

# Cursoft

Cursoft is a portable cursor manager for Windows x64. Save cursor presets, change your pointer size, and add effects that follow the mouse across your desktop.

[Download Cursoft.exe](https://github.com/4xura/Cursoft/releases/latest)

| Day                                                                                 | Night                                                                            |
|:-----------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------:|
| ![Cursoft's daylight library showing the selected Windows cursor](docs/screenshots/gallery-day.webp) | ![The same preset library in Cursoft's night theme](docs/screenshots/gallery-night.webp) |

## Import a cursor pack

A preset saves a set of cursor files, their Windows roles, and a pointer size.

1. Choose **Import** → **Cursor folder…**, open `<cursor pack folder>`, and click **Select Folder**.
2. Select `<preset name>` and click **Import**. If the pack includes `Install.inf`, Cursoft uses it to map the cursor roles.
3. Click the new card when you want to apply the scheme. Importing alone leaves the current pointer active.

![A cursor pack with 15 roles selected in the Import presets dialog](docs/screenshots/import-metro.webp)

You can also drag the folder into the library or import loose `.cur` and `.ani` files. Use **Export** to save a `.cursoft` bundle or create an Xcursor theme for Linux. To resize a pointer, choose a size and click **Apply**. **Windows scaling** keeps the selected cursor files.

## Edit a cursor

Right-click a preset card and choose **Edit preset**. Select a role in the workshop and open **Cursor tools**. The examples below use **Metro Cyan Demo**, a copy of Metro X2 Black with a recolored arrow.

![Preset workshop with the normal pointer role selected and Cursor tools visible](docs/screenshots/preset-workshop.webp)

### Set the click point

For Metro’s crosshair, select **Precision select** → **Cursor tools** → **Hotspot**. Place the marker at the intersection. This is the pixel Windows uses for clicks.

![Hotspot editor with the marker centered on the Metro crosshair](docs/screenshots/hotspot-editor.webp)

### Edit artwork and animation

Choose **Normal select** → **Cursor tools** → **Edit cursor image…**. Use **Brush**, **Eraser**, or **Fill** to edit pixels, and **Move** to shift the artwork.

To slow one step in the demo’s animation, select frame 3, enter **200** in **Duration, ms**, and press **Enter**. Use **Play / Stop** to preview the change. **Add frame** duplicates the selected frame, and the **Frame** menu lets you reorder or remove it.

![Cursor Studio showing frame 3 of the 22-frame cyan arrow animation with a duration of 200 ms](docs/screenshots/cursor-studio.webp)

| Tool | Use |
| --- | --- |
| **Canvas** | Resize the canvas and choose where to anchor the artwork. |
| **Background Ref** | Trace over a reference image that stays out of the saved cursor. |
| **Icon Sizes** | Include more than one resolution so Windows can choose a size for the display. |
| **File** | Load an image or export a PNG or animated GIF preview. |

Click **Save** in the editor, then **Save** in the workshop. Edit pointer trails and click animations in **Effects**.

## Cursor effects

Cursoft uses the cursor you chose in Windows for desktop effects. The **System cursor** card shows that selection, and effects work without a Cursoft preset.

Open **Effects**. On **Pointer**, choose the style that follows movement. On **Clicks**, assign a separate effect to the left and right buttons.

Move and click inside the shared preview to try the combination. Each button remembers the color, radius, opacity, duration, and **Cycle colors** setting for each click style. Choose **Off** to mute a button. **Apply** saves all three choices.

| Pointer | Clicks |
|:---:|:---:|
| ![Night-mode Pointer tab showing a violet Ribbon Trail](docs/screenshots/pointer-effects.webp) | ![Left-click Burst and right-click Snowflake with a shared preview](docs/screenshots/click-effects.webp) |

### Pointer effects

| Effect                                                                                                                                                                           | Motion preview                                                                                                    |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |:-----------------------------------------------------------------------------------------------------------------:|
| **Glow** keeps the pointer easy to spot, even at rest. Choose a soft halo, ring, or crosshair. Adjust the radius for a tight highlight around the tip or a wider glow.           | <img src="docs/previews/glow.webp" width="270" alt="A soft violet halo stays centered on the moving pointer">     |
| **Ribbon Trail** draws thin strands that fan out around turns and settle when you stop. Adjust the trail length for longer arcs and the line width for a finer or bolder stroke. | <img src="docs/previews/ribbon-trail.webp" width="270" alt="Pink ribbon strands fan out as the pointer turns">    |
| **Sparks** scatters fading particles along the pointer’s path. Increase spread for a wider scatter, trail length for a longer wake, or particle size for larger sparks.          | <img src="docs/previews/sparks.webp" width="270" alt="Gold and pink sparks drift away from the pointer and fade"> |
| **Fairy Dust** leaves faceted stars that lift, catch the light, and drift down as they fade. Adjust the emission rate, particle size, and lifetime. | <img src="docs/previews/fairy-dust.webp" width="270" alt="Gold and lavender stars float behind the Windows pointer and fade"> |
| **Follower** moves a dot and a slower ring toward the pointer. Adjust the follow lag, ring radius, and dot size. | <img src="docs/previews/follower.webp" width="270" alt="A cyan dot and violet ring trail the Windows pointer and settle when it stops"> |
| **Snowflake** leaves six-armed crystals that sway and tilt as they fall. Adjust the emission rate, flake size, and lifetime. | <img src="docs/previews/snowflake.webp" width="270" alt="Ice crystals drifting below the moving pointer"> |

Ribbon Trail, Sparks, Fairy Dust, Follower, and Snowflake each have a **Cycle colors** option for a gradual hue shift. Turn it off to use your saved primary and accent colors.

### Click effects

Each click animation stays at the press location. **Ripple** sends a thin crest across the click point, followed by a softer echo. **Burst** throws tapered sparks that break into embers. **Pulse** gathers light at the tip and releases a soft wave.

| Ripple | Burst | Pulse |
|:---:|:---:|:---:|
| ![Two expanding rings](docs/previews/click-ripple.webp) | ![Tapered sparks and trailing embers](docs/previews/click-burst.webp) | ![A bright center releases a soft wave](docs/previews/click-pulse.webp) |

**Fairy Dust** tosses stars along different arcs. Each catches the light before falling away. **Follower** sends a dot around the press, with a ring catching up as the dot settles. **Snowflake** grows from a hexagonal seed, then sheds small crystals from its branches.

| Fairy Dust | Follower | Snowflake |
|:---:|:---:|:---:|
| ![Stars spring up and fall](docs/previews/click-fairy-dust.webp) | ![A dot and ring sweep around the click](docs/previews/click-follower.webp) | ![A branching ice crystal sheds smaller flakes](docs/previews/click-snowflake.webp) |

## Download and update

Download **Cursoft.exe** from the [latest GitHub release](https://github.com/4xura/Cursoft/releases/latest) and run it from a folder you can write to. The standalone executable includes the .NET runtime. Cursoft stores your settings and presets in `%LOCALAPPDATA%\Cursoft`.

Open **Settings** → **Updates** and choose **Check for updates**. When a newer public release is available, **Install update** verifies the download and restarts Cursoft with your settings and presets.

![Settings with Check for updates and Releases buttons](docs/screenshots/settings-updates.webp)

## License

Builds distributed under the Cursoft Software License are free for personal, noncommercial use. Business or commercial use requires a separate written license. [Cursoft Software License](LICENSE)
