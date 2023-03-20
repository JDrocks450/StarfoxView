@echo off

if exist OP.temp del OP.temp
dir /b *.obj >> OP.temp
set /p objname=<OP.temp
del OP.temp

:: %objname% = name of obj with .obj on the end

rename "%objname%" readytoconvert.txt
move /Y readytoconvert.txt snakes

::------------------------- MAINBULK
cd snakes
vertamount2.py
move /Y readytoconvert.txt ..
move /Y OUTPUT.txt ..
del FACES.bat
del VERTS.bat
cd ..
rename "readytoconvert.txt" "%objname%"



