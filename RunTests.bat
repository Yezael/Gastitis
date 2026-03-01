@echo on

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe"
set PROJECT_PATH=%cd%

%UNITY_PATH% ^
-batchmode ^
-nographics ^
-projectPath "%PROJECT_PATH%" ^
-runTests ^
-testPlatform PlayMode ^
-testResults "%PROJECT_PATH%\playmode-results.xml" ^
-quit

exit /b %ERRORLEVEL%