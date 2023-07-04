@echo off

set "message=this is a message this is a messagethis is a messagethis is a messagethis is a messagethis is a message"
set "title=Rminder"

(
    echo MsgBox "%message%", vbCritical + vbOKOnly, "%title%"
) > "%temp%\msgbox.vbs"

cscript //nologo "%temp%\msgbox.vbs"

del "%temp%\msgbox.vbs"

vbCritical: 16    '显示一个带有临界图标的消息框
vbQuestion: 32    '显示一个带有询问图标的消息框
vbExclamation: 48 '显示一个带有感叹图标的消息框
vbInformation: 64 '显示一个带有信息图标的消息框
