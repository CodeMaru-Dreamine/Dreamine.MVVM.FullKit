@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

set "PACKAGE_OUTPUT=..\LocalPackages"

if not exist "%PACKAGE_OUTPUT%" (
    mkdir "%PACKAGE_OUTPUT%"
)

echo.
echo [🔧] Dreamine 내부 NuGet 패키지 빌드 시작...
echo.

for %%P in (
    Dreamine.MVVM.Attributes
    Dreamine.MVVM.Interfaces    
    Dreamine.MVVM.Extensions
    Dreamine.MVVM.ViewModels
    Dreamine.MVVM.Locators
    Dreamine.MVVM.Locators.Wpf
    Dreamine.MVVM.Core
    Dreamine.MVVM.Behaviors.Core
    Dreamine.MVVM.Behaviors
    Dreamine.MVVM.Behaviors.Wpf
    Dreamine.MVVM.Generators
) do (
    echo [🧹] %%P 정리 중...
    rd /s /q "%%P\bin" >nul 2>&1
    rd /s /q "%%P\obj" >nul 2>&1
    dotnet clean "%%P\%%P.csproj"

    echo [📦] %%P 복원 중...
    dotnet restore "%%P\%%P.csproj"

    echo [⚙️] %%P 빌드 중...
    dotnet build "%%P\%%P.csproj" -c Release

    echo [📦] %%P 패키징 중...
    dotnet pack "%%P\%%P.csproj" -c Release -o "%PACKAGE_OUTPUT%"
    echo.
)

echo [✅] NuGet 패키지 빌드 완료!
echo 저장 경로: %~dp0%PACKAGE_OUTPUT%

echo.
echo ♻️ Visual Studio 구성 캐시 갱신 중...
where devenv >nul 2>&1
if %errorlevel%==0 (
    call devenv /updateconfiguration
    echo [✔] Visual Studio 구성 갱신 완료!
) else (
    echo [⚠] devenv 명령을 찾을 수 없습니다. Visual Studio가 PATH에 없을 수 있습니다.
)
pause >nul
