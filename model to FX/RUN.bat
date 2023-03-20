:RESTART
@echo off
color 0e
title SUPER FX MODEL HELPER
::MADE BY EUCLIDIUM

if exist "batches\pythoncheck.bat" call "batches\pythoncheck.bat"

if not exist *.obj (
	cls
	echo MISSING OBJ FILE
	echo OBJ FILE MUST BE IN THE SAME DIRECTORY AS RUN.BAT
	pause
	exit
)



cls
echo WHAT TYPE OF FILE ARE YOU USING CURRENTLY
echo.
echo 1.NORMAL
echo 2.3DG1
choice /c 12
IF %errorlevel%==1 set type=NORMAL
IF %errorlevel%==2 set type=3DG1
	
if exist OP.temp del OP.temp
dir /b *.obj >> OP.temp
set /p objname=<OP.temp
del OP.temp
	
::set PV=3
	
set PV=1
if exist "C:\Program Files\POV-Ray\v3.7\bin\pvengine64.exe" set /a PV=%PV%+1
if exist OUTPUT.txt set /a PV=%PV%+1
	
goto mainmenu



::NORMAL ERROR CHECKS
set file=yes
if not exist batches goto ERRROR1
if not exist snakes goto ERROR1
if not exist *.obj (
	set file=no
	set objname=OUTPUT.txt
	set type=POV-RAY
	if not exist OUTPUT.txt goto ERROR2
)

::POVRAY CHECK
set PV=1
if exist "C:\Program Files\POV-Ray\v3.7\bin\pvengine64.exe" set /a PV=%PV%+1
if exist OUTPUT.txt set /a PV=%PV%+1

::SELECT
if %file%==no goto mainmenu

if exist OP.temp del OP.temp
dir /b *.obj >> OP.temp
set /p objname=<OP.temp
del OP.temp

set type=NORMAL
set /p type=<"%objname%"

if not "%type:~0,4%"=="3DG1" set type=NORMAL


:mainmenu
cls
VER: 2.0a
echo %objname%
echo TYPE:%type%
if %type%==3DG1 echo (1) ROUND AND COLOUR
if %type%==NORMAL echo (1) CONVERT, ROUND AND COLOUR        
if %type%==3DG1 echo (2) ROUND 3DG1 INTERGERS 
if %type%==3DG1 echo (3) COLOUR ONLY
if not %type%==POV-RAY echo (4) POV-RAY EXPORT (must be default colour and use only tris)
if %type%==NORMAL echo (5) WAVEFRONT TO VIDEOSCAPE (REGULAR TO 3DG1) (DOESN'T CONVERT WITH COLOUR)
if %PV%==3 echo (R) RENDER
echo (H) HELP
echo.
choice /c HR12345
IF %errorlevel%==1 goto HELP
IF %errorlevel%==2 goto PRR
IF %errorlevel%==3 goto EVERYTHING
IF %errorlevel%==4 goto ROUNDINT
IF %errorlevel%==5 goto COLOUR
IF %errorlevel%==6 goto POVRAY
IF %errorlevel%==7 goto 3DG1


:ROUNDINT
cls
call batches\roundeverything.bat
goto done

::-------------------------------

:3DG1
cls
call batches\videoscape.bat
cls
type "%objname:~0,-4% 3DG1.obj"
echo.
echo COMPLETE
echo.
pause

exit



::----------------------------

:povray
cls
::call batches\colourandround.bat
if %type%==NORMAL call batches\videoscape.bat
call batches\colouronly.bat
call batches\povrayexport.bat
cls
type OUTPUT.txt
echo.
echo COMPLETE, SAVED IN OUTPUT.txt
echo.
echo 1.EXIT 2.RENDER
choice /c 12R
if %errorlevel%==1 exit
cls
if exist "C:\Program Files\POV-Ray\v3.7\bin\pvengine64.exe" set PV=3
:PRR
if not %PV%==3 goto mainmenu
"C:\Program Files\POV-Ray\v3.7\bin\pvengine64.exe" /RENDER "%cd%\old tools\SCENE.pov" "%cd%\old tools\INI.ini" /EXIT
cd "old tools"
start OUTPUT.png
exit
::----------------------------

:EVERYTHING
if %type%==NORMAL call batches\videoscape.bat
if %type%==NORAML call batches\roundeverything.bat
if %type%==3DG1 call batches\colourandround.bat
cls
goto done

:COLOUR
cls
call batches\colouronly.bat
goto done

:HELP
cls
::echo read README.txt
type README.txt
echo.
pause
goto mainmenu

:ERROR1
cls
echo ERROR1 (missing files)
pause
exit

:ERROR2
cls
echo ERROR2 (missing obj file)
echo place an .obj (must be 3DG1) in the same directory/folder as this program
echo.
pause
exit

:ERROR3
cls
echo ERROR3 (fatal error)
echo cause unknown, check requirements for running program
echo.
pause
exit

:done
set MSGN=%random:~-1%
set MSGN2=%random:~-1%
if %MSGN%==1 set MSG=alrighty done
if %MSGN%==2 set MSG=worked this time
if %MSGN%==3 set MSG=done
if %MSGN%==4 set MSG=ight done
if %MSGN%==5 set MSG=worked, cool and good
if %MSGN%==6 set MSG=completed
if %MSGN%==7 set MSG=conversion completed
if %MSGN%==8 set MSG=3DG1 correct
if %MSGN%==9 set MSG=job done
if %MSGN%==0 set MSG=changes made
if %MSGN2%%MSGN%==18 set MSG=alighty done
if %MSGN2%%MSGN%==98 set MSG=ERROR     just kidding

cls
if not exist "%objname:~0,-4% 3DG1.obj" goto ERROR3
type "%objname:~0,-4% 3DG1.obj"
echo.
echo %MSG%
echo.
pause >NUL
goto RESART