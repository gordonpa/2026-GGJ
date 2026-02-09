set WORKSPACE=.\\
set LUBAN_DLL=%WORKSPACE%Tools\\Luban\\Luban.dll
set CONF_ROOT=.

set OUTDIRCODE=..\\Assets\\Scripts\\Table
set OUTDIRDATA=..\\Assets\\Resources\\Table

dotnet %LUBAN_DLL% ^
    -t all ^
    -c cs-bin ^
    -d bin ^
    --conf %CONF_ROOT%\\luban.conf ^
    -x outputCodeDir=%OUTDIRCODE% ^
    -x outputDataDir=%OUTDIRDATA%\\%%a ^
    -x pathValidator.rootDir=%WORKSPACE%\Projects\Csharp_Unity_bin
pause