--HOW TO USE--
Put your 3DG1 obj in this folder and run "run.bat" (only works on windows computers, python must be installed)
Make sure there is only 1 object in your .obj file, no lights, no cameras, no second mesh.


--COLOURS FOR CONVERTING OBJ--
Instead of changing the colours of materials you now must rename the material to what colour you want to use.
each material name must start with "FX" then the following number for the colour you want to use, for example to use the colour blue you would type FX7, an example of each colour is provided in COLOURS.txt

For colouring edges you must create a new material and apply it to at least one face and use FE instead of FX. This will colour all the stray edges to the colour you choose.




--COLOURS FOR 3DG1--
Use the provided hex colours in "COLOURS.txt" that correspond to the colour on id_0_c palette.

--ROUND NUMBERS--
The "roundnumbers.py" can be openned in blenders scripting tab on the top row of tabs.
just edit the 'meshname' part to your meshes name and click the play button,

However this isn't spot on, once you get the videoscape obj after blender 2.4 put the file in this folder and
run the program "roundeverything" that will round everything perfectly, though do check that it did!

--POV-RAY EXPORT--
Converts 3DG1 to a POV-RAY mesh, all faces MUST be tris (no quads etc) and all faces must be white. Once done OUTPUT.txt will have your model in pov-ray format

Have fun modelling!