@echo off
title round numbers of obj
color 0e

if exist readytoconvert.txt (
	rename readytoconvert.txt readytoconvert.obj
	exit
)

if exist OP.temp del OP.temp
dir /b *.obj >> OP.temp
set /p objname=<OP.temp
del OP.temp


rename "%objname%" readytoconvert.txt
move /Y readytoconvert.txt snakes
cd snakes
roundnumbers.py
move /Y readytoconvert.txt ..
cd ..
rename "readytoconvert.txt" "%objname%"
cls
echo alright all done
::pause
::exit


