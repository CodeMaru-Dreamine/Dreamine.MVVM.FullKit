@echo off
chcp 65001 > nul

:: 삭제할 폴더 목록 설정
set folders=bin obj .vs Release Debug

:: 현재 디렉토리 및 하위 디렉토리에서 폴더 삭제
for %%f in (%folders%) do (
    echo ✅ %%f 폴더 삭제 중...
    for /d /r %%d in (%%f) do (
        echo 🔄 폴더 삭제 중: %%d
        rmdir /s /q "%%d"
    )
)

:: 모든 .bak 파일 삭제
echo 🧹 .bak 파일 삭제 중...
for /r %%f in (*.bak) do (
    echo ❌ 삭제 중: %%f
    del /q "%%f"
)

:: devenv가 있는 경로 확인 후 실행
set VSDEVENVDIR=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE

if exist "%VSDEVENVDIR%\devenv.exe" (
    echo ♻️ Visual Studio 구성 갱신 중...
    "%VSDEVENVDIR%\devenv.exe" /updateconfiguration
    echo ✅ 갱신 완료!
) else (
    echo ⚠️ devenv.exe를 찾을 수 없습니다. 경로를 확인하세요.
)

:: NuGet 및 Visual Studio 캐시 삭제
echo 💣 캐시 삭제 중...
rd /s /q "%USERPROFILE%\.nuget\packages\dreamine.mvvm.generators"
rd /s /q "%LOCALAPPDATA%\Microsoft\VisualStudio\17.*\ComponentModelCache"
echo ✅ 캐시 삭제 완료. 이제 빌드하면 됩니다!

pause
