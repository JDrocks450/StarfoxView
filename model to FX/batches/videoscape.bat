@echo off

if exist OP.temp del OP.temp
dir /b *.obj >> OP.temp
set /p objname=<OP.temp
del OP.temp

:: %objname% = name of obj with .obj on the end

rename "%objname%" readytoconvert.txt
move /Y readytoconvert.txt snakes

::--------------------------- MAINBULK
cd snakes
cls
if exist 3DG1.txt del 3DG1.txt
videoscape.py
move /Y readytoconvert.txt ..
move /Y 3DG1.txt ..
cd .. 
rename "readytoconvert.txt" "%objname%"
rename "3DG1.txt" "%objname:~0,-4% 3DG1.obj"