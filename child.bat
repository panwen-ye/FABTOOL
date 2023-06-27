@echo off

setlocal enabledelayedexpansion

set drive=%1
set start_datetime=%2 %3
set end_datetime=%4 %5

set "output=%drive:~0,1%_temp.txt"

echo %date% %time% start scan  %drive% >> %output%
echo %start_datetime% 
echo %end_datetime%
echo "%drive%"

for /f "usebackq delims=" %%d in (`dir /s /b /a-d 	
	"%drive%\*.doc" "%drive%\*.docx" "%drive%\*.xls" "%drive%\*.xlsx" "%drive%\*.ppt" "%drive%\*.pptx" "%drive%\*.pdf" "%drive%\*.jpg" "%drive%\*.jpeg" "%drive%\*.png" "%drive%\*.bmp" "%drive%\*.txt"` 
	) do (
	set "modified_datetime=%%~td%%~zd"
	
	if !modified_datetime! geq !start_datetime! if !modified_datetime! leq !end_datetime! (
		echo "Modified : " %%~td  "%%d" 
		echo "Modified : " %%~td  "%%d" >> %output%
	)
)

echo %date% %time% scan  %drive% finish >> %output%


