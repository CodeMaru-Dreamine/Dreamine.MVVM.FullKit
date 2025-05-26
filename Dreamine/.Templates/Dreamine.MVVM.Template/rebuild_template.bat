@echo off
chcp 65001 > nul
setlocal

set PROJECT_NAME=Dreamine.Template.csproj
set CONFIGURATION=Release
set OUTPUT_DIR=nupkg

echo 🔄 기존 nupkg 제거...
if exist %OUTPUT_DIR% del /q %OUTPUT_DIR%\*.nupkg
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

echo 🧱 사전 빌드 중...
dotnet restore %PROJECT_NAME%
dotnet build %PROJECT_NAME% -c %CONFIGURATION%

echo 📦 템플릿 패키지 빌드 중...
dotnet pack %PROJECT_NAME% -c %CONFIGURATION% -o %OUTPUT_DIR%
if %errorlevel% neq 0 (
    echo ❌ 빌드 실패!
    pause
    exit /b
)

echo 🔍 기존 템플릿 설치 여부 확인 중...
dotnet new --list | findstr /C:"Dreamine.Templates.MVVM" > nul
if %errorlevel%==0 (
    echo 🔽 기존 템플릿 제거...
    dotnet new uninstall Dreamine.Templates.MVVM
) else (
    echo ⏩ 기존 템플릿이 없으므로 제거 생략.
)

echo 🔁 새 템플릿 등록...
for %%f in (%OUTPUT_DIR%\*.nupkg) do (
    dotnet new install "%%f"
)

echo ♻️ Visual Studio 템플릿 캐시 업데이트 중...
devenv /updateconfiguration

echo ✅ 템플릿 재등록 완료!
dotnet new list

pause
