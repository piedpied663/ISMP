:: This script creates a symlink to the game binaries to account for different installation directories on different systems.
@Echo off
set ReferencePath=%~dp0Reference\

if not exist %ReferencePath% goto:Refnotexist
if exist %ReferencePath% goto:Refexist

:Refnotexist
mkdir %ReferencePath%
goto:Refexist

:Refexist
set /p path="Please enter the folder location of your SpaceEngineersDedicated.exe: "
cd %ReferencePath%
mklink /J GameBinaries "%path%"
if errorlevel 1 goto Error
echo Done!
goto End
:Error
echo An error occured creating the symlink.
goto EndFinal
:End

set /p path="Please enter the folder location of your Torch.Server.exe: "
cd %ReferencePath%
mklink /J TorchBinaries "%path%"
if errorlevel 1 goto Error
echo Done! You can now open the Solution without issue.
goto EndFinal
:Error2
echo An error occured creating the symlink.
:EndFinal
pause
