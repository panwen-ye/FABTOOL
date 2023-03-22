@echo off
setlocal enabledelayedexpansion

REM 获取当前目录的卷标
for /f "tokens=2 delims=:" %%i in ('vol %cd%') do set label=%%i

REM 检查卷标是否为 "mount"，如果是，则表示当前目录为挂载目录
if "%label%"==" mount" (
    echo 当前脚本无法在挂载目录下执行。
    pause
    exit /b
) else (
    echo 当前目录不是挂载目录，可以执行脚本。
)
pause

set "start_datetime=20230115000000"
set "end_datetime=20230318235959"

set "excluded_folders=C:\Windows;C:\Program Files;C:\Program Files (x86)"
set "excluded_drives=C:"

echo "scan start , wait..."
echo "start date: %start_datetime%"
echo "end date: %end_datetime%"
rem 免扫描目录
echo "skip folders:%excluded_folders%"
rem 免扫描盘
echo "skip drivers:%excluded_drives%"

set result=pass

REM 获取系统语言
for /f "tokens=3 delims=: " %%a in ('reg query "HKCU\Control Panel\International" /v "sLanguage"') do set lang=%%a

if %lang%==409 (
    REM 英文格式时间
	echo "en"
    set "timestamp=%date:~10,4%%date:~4,2%%date:~7,2%%time:~0,2%%time:~3,2%%time:~6,2%"
) else (
    REM 中文格式时间
	echo "ch"
    set "timestamp=%date:~0,4%%date:~5,2%%date:~8,2%%time:~0,2%%time:~3,2%%time:~6,2%"
	echo %date:~0,4%
	echo %date:~0,4%%date:~5,2%%date:~8,2%%time:~0,2%%time:~3,2%%time:~6,2%
)


set "dir_name=%COMPUTERNAME%_!date:~5,2!!date:~8,2!!time:~0,2!!time:~3,2!!time:~6,2!"
echo "!dir_name!"

echo "get local drivers..."
for /f "skip=1 tokens=1,2" %%a in ('wmic logicaldisk get deviceid^,drivetype') do (
    if "%%b"=="3" (
        set "flag=Y"
        for %%c in (%excluded_drives%) do (
            if /i "%%a"=="%%c" (
                echo "skip %%a"
                set "flag=N"
            )
        )
        if "!flag!"=="Y" (
            echo "Enables a scan of new processes : %%a"			
			
			start "child_%%a.bat"  cmd /c child.bat  %%a !start_datetime! !end_datetime! "!excluded_folders!" !dir_name! 
			rem start /b cmd /c child.bat %%a !start_datetime! !end_datetime! "!excluded_folders!" !dir_name!
			echo %%a "!start_datetime!" "!end_datetime!" "!excluded_folders!" "!excluded_drives!"  !dir_name!
        )
    )
)

:check_process
set "process_count=0"


for /f "skip=1 tokens=1,2" %%a in ('wmic logicaldisk get deviceid^,drivetype') do (
	if "%%b"=="3" (
		set "window_title=child_%%a.bat"
		set "window_title=!window_title:'=""!"
		for /f "skip=1 tokens=1" %%c in ('tasklist /fi "imagename eq cmd.exe" /fi "windowtitle eq !window_title!" /v /fo csv') do (
			echo %%c
			set /a "process_count+=1"
		)
	)
)

echo Total number of child command prompt processes running: %process_count%


if %process_count% GTR 0 (
    timeout /t 30 /nobreak > nul
	echo "scan task is running"
    goto check_process
)

echo "all scan process finish"
echo "Start reading the output of child.bat and write into result.txt"


rem 创建结果存放目录
md "!dir_name!"
rem 扫描结果写入txt
(for %%f in (*.txt) do (
    echo "driver:%%~nf"
    type "%%~f"
    echo.
)) > "!dir_name!\result.txt"

echo " result.txt finish"
echo "delete tmp txt..."
del *_temp.txt
