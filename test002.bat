@echo off
setlocal enabledelayedexpansion

set "excluded_drives=F:"
set "start_datetime=20230115000000"
set "end_datetime=20230318235959"

(for /f "usebackq delims=" %%d in (`dir /s /b /a-d "C:\Users\Admin\Desktop"\`) do (
	set "modified_datetime=%%~td%%~zd"
	set "modified_datetime=!modified_datetime:/=!"
	set "modified_datetime=!modified_datetime::=!"
	
	if !modified_datetime! geq !start_datetime! if !modified_datetime! leq !end_datetime! (
		echo !modified_datetime!
		echo %%~td %%~tt "%%d"
		
	)
)) > result.txt

pause
