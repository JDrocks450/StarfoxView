:start
@echo off
color 0e

::blender 2.4 location
set blender24=%userprofile%\Desktop\programs\old blenders\blender-2.40-windows


::notes for programing
::dir /b *.* >> OP.temp
set OGP=%cd%

:objto3dg1

if not exist *.obj (
	echo OBJ FILE ABSENT FROM FOLDER,
	pause
	goto start
)

if not exist "%blender24%\blender.exe" (
	echo BLENDER 2.4 MISSING
	pause
	goto start
)

::finding the name of the obj file
if exist OP.temp del OP.temp
dir /b *.obj >> OP.temp
set /p objname=<OP.temp
del OP.temp

::creation of python script to tell blender 2.4 what to do 
cd snakes
if exist python.py del python.py
echo import bpy >> python.py
echo bpy.ops.object.select_all(action='SELECT') >> python.py
echo bpy.ops.object.delete(use_global=False) >> python.py
:: formating the file path to pythons format
set pyobj=%OGP:\=\\%\\%objname%
set objname=%objname:.obj=%
set py3dg1=%OGP:\=\\%\\%objname%2

echo bpy.ops.import_scene.obj(filepath='%pyobj%') >> python.py
echo bpy.ops.export_scene.videoscape(filepath="%py3dg1%.obj" >> python.py
echo bpy.ops.wm.quit_blender()>> python.py

cd ..

::getting blender to convert to videoscape
"%blender24%\blender" -b -p "%OGP%\python.py"
cd snakes
del python.py
cd ..
pause
echo what happened








