@echo off
chcp 65001 > nul

:: Set folders to delete
set folders=bin obj .vs Release Debug html latex xml rtf man

:: Delete target folders
for %%f in (%folders%) do (
    echo [Clean] Searching for folder: %%f ...
    for /d /r %%d in (%%f) do (
        echo [Delete] Removing folder: %%d
        rmdir /s /q "%%d"
    )
)

:: Delete .bak files
echo [Clean] Searching for *.bak files ...
for /r %%f in (*.bak) do (
    echo [Delete] Removing file: %%f
    del /q "%%f"
)

echo [Done] Cleanup completed.
pause