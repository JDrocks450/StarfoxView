# StarFox (SNES) Source Code IDE
SFEdit is an all-in-one interactive development environment for viewing original code, assets, graphics, and levels. 
While more supported file types are planned, for now only certain file types are compatible and implemented.  

See *Compatibility* for details.

## Special Mentions
Thank you very much to the following projects for being Open-Source and helping to make this Editor possible!
* NAudio: https://github.com/naudio/NAudio
    > WAV Audio Player for the Editor
* Special thanks to Everything! Maker of sf_decrunch: https://www.romhacking.net/utilities/1543/
    > His work helped me create Decrunch.cs - a C# translation of decrunch.c for interlaced 2D imagery.
* Special thanks to Matthew Callis! Maker of SF2 FontTools: https://www.romhacking.net/utilities/346/
    > His work helped me create FX.cs - a C# translation of FX.cpp for compressed CGX imagery.
* Special thanks to LUIGIBLOOD! Maker of hcgcad: https://github.com/LuigiBlood/hcgcad
    > His work helped me create CAD.cs - an adaptation of CAD.cs from his project for 2D imagery.
* Special thanks to Euclidium, Sunlit, segaretro92, Random and kando from the Starfox EX discord for helping with various curiousities.

You are all appreciated.

## Setup
Instructions for setup I have split into two user-groups.

**Everyone!** You need to have a copy of the StarFox source code. 

> Check out:
> https://github.com/Sunlitspace542/ultrastarfox/tree/master/SF

### Developers
Before cloning the repository, allow me to draw your attention to the following:

* Requires Visual Studio 2022
* Requires .NET Standard 5.0 / 6.0
* Requires Windows Presentation Foundation package included

After cloning the repository, there are some additional things to note:

* This project has NuGet packages, ensure they are installed and updated.
* Project references should all be valid, you shouldn't need to edit project references.

### Enthusiasts 
People who just want to explore the StarFox source code.

Refer to the *Packages* section for the latest build, if one is available.

Once the latest build package has been downloaded, place it somewhere you won't lose it. (Shorter paths are generally safer)

1. Place the StarFox Source Code along with the Editor in the same folder. 
    > (This is a recommendation to keep everything together)
2. Open the editor (*.exe) program you downloaded from the package.
3. You are greeted with the Homepage, press *Import Source Code* and choose the directory containing the source code.
    > Generally, this folder is called "SF"
4. You have now set up your workspace.

## General Information
For information regarding *how* the original asset files are handled and processed, please visit *Starfox.Interop*

There are a few components to know about when using this editor. More detailed information can be found in the wiki.

### SHAPE Viewer
![image](https://user-images.githubusercontent.com/16988651/230552729-dfd3fc0f-a5a5-4627-9b63-336095d7ac71.png)

The Shape Viewer will render original Starfox shapes (models) in a 3D space to view and study the composition of the model. 
From this screen, you can also export the Shape data to other usable formats. Currently, *.sfshape is the only supported exported file type.

**.sfshape** is a serialized object-graph of the composition of a BSPShape instance, visit *Starfox.Interop* for more information.

### MAP Viewer
![image](https://cdn.discordapp.com/attachments/1002355565881725108/1093397753381539870/explorer_Tc5yqWvkfD.gif)

The MAP Viewer is a powerful component with multiple functions.
After selecting an *.ASM file, you are greeted with a dialog asking how to interpret the file, pressing *Map Script* will bring you here.

The initial MAP Viewer view is set to be the *Node Tree* view. You can press the *View 3D* button "pop-out" a second window that will act as your 3D world viewer.
Both the *Node Tree* view and the *3D Map View* will work simultaneously, try clicking on a Node to observe the 3D viewer respond to your inputs.

### GFX Viewer
![image](https://user-images.githubusercontent.com/16988651/230554672-cb3f9c49-8eed-4f21-8306-8448a1325bbd.png)

The GFX is a graphics tile viewer. It should be compatible with most SNES content, StarFox included. There are two types of files to notice:

1. ***.CGX** files are sprite-based graphics, for all intents and purposes.
2. ***.SCR** files map the sprite tiles of a *.CGX to form a larger image. 

These are implementation-specific, and crafty developers can use these to build different, unexpected imagery. Not all implementations will appear correctly.

You can view these graphics with any palettes you want. Palettes are ***.COL** files, you *need* to right-click -> Include all COL files you want to use as palettes.
