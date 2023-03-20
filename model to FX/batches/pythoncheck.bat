cls

::NORMAL
where python >> OP.temp
set /p location=<OP.temp
del OP.temp
if exist "%location%" goto ENDPYLOOP


::WINDOWS INSTALL
python --version 3>NUL
if %errorlevel%==1 goto PYLOOP
goto ENDPYLOOP



::LAST RESORT
set PY=0
set NUMBER=0
:PYLOOP
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%0" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%1" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%2" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%3" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%4" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%5" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%6" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%7" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%8" set /A PY=%PY%+1
if exist "%appdata%\Microsoft\Windows\Start Menu\Programs\Python 3.%NUMBER%9" set /A PY=%PY%+1

if %PY%==1 goto ENDPYLOOP

if %NUMBER%==9 goto ENDPYLOOP2
set /A NUMBER=%NUMBER%+1

goto PYLOOP



:ENDPYLOOP2
cls
echo PYTHON CANNOT BE FOUND
echo.
pause
exit

:ENDPYLOOP
echo.
