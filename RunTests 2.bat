@echo off
setlocal

REM ===== CONFIGURATION =====
set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe
set PROJECT_PATH=%cd%
set RESULTS_PATH=%PROJECT_PATH%\playmode-results.xml
set LOG_PATH=%PROJECT_PATH%\unity-test.log

echo =====================================
echo Unity Headless Test Runner
echo =====================================
echo Unity:   "%UNITY_PATH%"
echo Project: "%PROJECT_PATH%"
echo Results: "%RESULTS_PATH%"
echo Log:     "%LOG_PATH%"
echo =====================================

REM ===== VERIFY UNITY EXISTS =====
if not exist "%UNITY_PATH%" (
    echo ERROR: Unity executable not found.
    exit /b 1
)

REM ===== RUN UNITY TESTS =====
echo Running command:
echo "%UNITY_PATH%" -batchmode -nographics -projectPath "%PROJECT_PATH%" -runTests -testPlatform PlayMode -testResults "%RESULTS_PATH%" -logFile "%LOG_PATH%"

"%UNITY_PATH%" -batchmode -nographics -projectPath "%PROJECT_PATH%" -runTests -testPlatform PlayMode -testResults "%RESULTS_PATH%" -logFile "%LOG_PATH%" 

set EXIT_CODE=%ERRORLEVEL%

echo.
echo =====================================
echo Unity finished with exit code: %EXIT_CODE%
echo =====================================

REM ===== SHOW LOG LOCATION =====
echo Log file saved to:
echo %LOG_PATH%

REM ===== OPTIONAL: PRINT LAST LINES OF LOG =====
echo.
echo ---- Last lines of Unity log ----
powershell -Command "Get-Content '%LOG_PATH%' -Tail 20"

exit /b %EXIT_CODE%