@echo off
title create a awesome script 2000
color 0e

:: 000000
goto new

set NUM=000001
:baseround
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000002
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000003
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000004
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000005
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000006
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000007
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000008
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000009
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=000010
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000011
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000012
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000013
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000014
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000015
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000016
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000017
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000018
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000019
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt
set NUM=0000020
echo filedata = filedata.replace('%NUM%', '000000') >>OUTPUT.txt

echo alright first part done
pause

goto new

:old
set NUM=-200
set /a NUM2=%NUM%+1
set nines=999980
set legit=%NUM%.%nines%
goto phase2
:phase1
set /a NUM=%NUM%+1
If /i %NUM% LSS 0 set /a NUM2=%NUM%-1
If /i %NUM% GTR -1 set /a NUM2=%NUM%+1
set nines=999980
echo DONE %NUM%
if %NUM%==201 goto done
:phase2
set legit=%NUM%.%nines%
echo filedata = filedata.replace('%legit%', '%NUM2%.000000') >>OUTPUT.txt
if %nines%==999999 goto phase1
set /a nines=%nines%+1
goto phase2


:done
cls
echo all done
pause
exit



:new
set NUM=250
set nines=999960


set /a NUM2=%NUM%+1
set legit=%NUM%.%nines%
goto part2
:part1
set /a NUM=%NUM%-1
set /a NUM2=%NUM%+1
set nines=999980
if %NUM%==-1 goto done
echo DONE %NUM%
:part2
set legit=%NUM%.%nines%
echo filedata = filedata.replace('%legit%', '%NUM2%.000000') >>OUTPUT.txt
if %nines%==999999 goto part1
set /a nines=%nines%+1
goto part2




