@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion
set drive=%1
set start_datetime=%2
set end_datetime=%3
set excluded_folders=%4
set dir_name=%5
set file_type=%6

set "output=%drive:~0,1%_temp.txt"

echo %date% %time% 开始扫描盘符 %drive% >> %output%
echo %start_datetime% 
echo %end_datetime%
echo "%drive%"
:: 修改比较方式，使用 IF NOT 和 GTR/LSS 操作符

for /f "usebackq delims=" %%d in (`dir /s /b /a-d 	
	"%drive%\*.doc" "%drive%\*.docx" "%drive%\*.xls" "%drive%\*.xlsx" "%drive%\*.ppt" "%drive%\*.pptx" "%drive%\*.pdf" "%drive%\*.jpg" "%drive%\*.jpeg" "%drive%\*.png" "%drive%\*.bmp"`
	) do (
	set "modified_datetime=%%~td%%~zd"
	set "modified_datetime=!modified_datetime:/=!"
	set "modified_datetime=!modified_datetime::=!"
	
	if !modified_datetime! geq !start_datetime! if !modified_datetime! leq !end_datetime! (
		echo %%~td %%~tt "%%d" >> %output%
	)
)

echo %date% %time% 扫描盘符 %drive% 完成 >> %output%

