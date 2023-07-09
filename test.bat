@echo off



setlocal enabledelayedexpansion
echo "%CD:~0,2%"
echo "%SystemDrive%"
if not "%CD:~0,2%"=="%SystemDrive%" (
    echo "Please execute the script on a local drive."
    exit /B
)

REM 获取当前日期
for /F "tokens=2 delims==" %%G in ('wmic OS Get localdatetime /value') do set "datetime=%%G"
set "date=!datetime:~0,4!/!datetime:~4,2!/!datetime:~6,2!"

REM 获取当前时间
set "time=%TIME:~0,8%"
set "current_time=!date! !time!"
set "dir_name_t=%COMPUTERNAME%_1"
REM 检查目录是否存在
set "start_datetime=" 

if not exist "!dir_name_t!\startTime.txt" (
	echo  This is the first scan, and the current time will be recorded
	
	md "!dir_name_t!" >nul 2>&1
	if %errorlevel% neq 0 (
		echo The current directory does not have read and write permissions, please copy the script to the computer for local execution, and copy the output results to a USB flash drive。
		pause
		exit
	)
	

	echo !current_time! >> "!dir_name_t!/startTime.txt"
	pause
    exit
) 
endlocal
rem 创建child start
pause
 
set bat_f=child.bat
echo @echo off > %bat_f%
echo setlocal enabledelayedexpansion >> %bat_f%
echo. >> %bat_f%
echo set "drive=%%~1" >> %bat_f%
echo set "start_datetime=%%~2 %%~3" >> %bat_f%
echo set "end_datetime=%%~4 %%~5" >> %bat_f%
echo set "output=%%drive:~0,1%%_temp.txt" >> %bat_f%
echo. >> %bat_f%
type NUL >> %bat_f%
echo echo %%date%% %%time%% start scan %%drive%% ^>^> %%output%% >> %bat_f%
echo echo %%start_datetime%% >> %bat_f%
echo echo %%end_datetime%% >> %bat_f%
echo echo "%%drive%%" >> %bat_f%
echo. >> %bat_f%

echo for /f "usebackq delims=" %%%%d in (`dir /s /b /a-d "!drive!\*.doc" "!drive!\*.docx" "!drive!\*.xls" "!drive!\*.xlsx" "!drive!\*.ppt" "!drive!\*.pptx" "!drive!\*.pdf" "!drive!\*.jpg" "!drive!\*.jpeg" "!drive!\*.png" "!drive!\*.bmp" "!drive!\*.txt"`) do (^ >> %bat_f%
echo     set "modified_datetime=%%%%~td%%%%~zd" >> %bat_f%
echo. >> %bat_f%
echo     if ^!modified_datetime^! geq ^!start_datetime^! if ^!modified_datetime^! leq ^!end_datetime^! ( >> %bat_f%
echo         echo "Modified : " %%%%~td "%%%%d" ^>^> %%output%% >> %bat_f%
echo     ) >> %bat_f%
echo ) >> %bat_f%
echo. >> %bat_f%
echo echo %%date%% %%time%% scan %%drive%% finish ^>^> %%output%% >> %bat_f%
 
rem 创建child end
pause



setlocal enabledelayedexpansion
echo This is the second scan, and the first time is as follows:
set "dir_name_t=%COMPUTERNAME%_1"
REM 读取文本文件内容并赋值给外部变量
for /F "usebackq delims=" %%A in ("!dir_name_t!\startTime.txt") do (
	set "start_datetime=%%A"		
)
set "current_time=!date! !time!"
echo "start date: !start_datetime!"
set "dir_name=%COMPUTERNAME%_!date:~5,2!!date:~8,2!!time:~0,2!!time:~3,2!!time:~6,2!"
echo "!dir_name!"
md "!dir_name!" 

REM 获取当前日期
for /F "tokens=2 delims==" %%G in ('wmic OS Get localdatetime /value') do set "datetime=%%G"
set "date=!datetime:~0,4!/!datetime:~4,2!/!datetime:~6,2!"

REM 获取当前时间
set "time=%TIME:~0,8%"
set "end_datetime=!date! !time!"


set "excluded_folders=C:\Windows;C:\Program Files;C:\Program Files (x86)"
set "excluded_drives=C:,D:"

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


pause
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
    timeout /t 10 /nobreak > nul
	echo "scan task is running"
    goto check_process
)

echo "all scan process finish"
echo "Start reading the output of child.bat and write into result.txt"



rem 扫描结果写入txt
(for %%f in (*.txt) do (
    echo "driver:%%~nf"
    type "%%~f"
    echo.
)) > "!dir_name!\result.txt"

echo " result.txt finish"
echo "delete tmp txt..."
del *_temp.txt
rd /S /Q "!dir_name_t!"
echo  "!dir_name!\result.txt"

set fail=0
for /f "tokens=*" %%a in (!dir_name!\result.txt) do (	
    set line=%%a
    if "!line:Modified=!" NEQ "!line!" (
        set fail=1
    )
)

if %fail% == 1 (
    echo fail
) else (
    echo pass
)

pause
