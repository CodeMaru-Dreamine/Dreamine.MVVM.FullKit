# SonarCloud 프로젝트 동기화

`Sync-SonarCloudProjects.ps1`은 루트 `.gitmodules`에서 Dreamine 저장소를 읽어
SonarCloud 프로젝트 등록 상태를 점검하고, 누락된 프로젝트를 생성한 뒤 같은 이름의
GitHub 저장소와 연결합니다.

GitHub 소유자가 Organization 계정인지 일반 User 계정인지 API로 자동 판별하므로
두 유형을 같은 명령으로 처리할 수 있습니다.

비공개 GitHub 저장소를 연결하려면 GitHub CLI 로그인이 필요합니다. 로그인은 PC에서
한 번만 하면 되며 저장소별로 반복하지 않습니다.

```powershell
gh auth login -h github.com -p https -w
gh auth status
```

로그인되어 있으면 스크립트는 `gh api`로 공개·비공개 저장소의 숫자 ID를 읽습니다.
로그인되어 있지 않으면 공개 저장소만 조회하며, 공개 저장소도 발견되지 않으면 실제
반영 전에 중단합니다.

## 안전한 실행 순서

PowerShell에서 저장소 루트로 이동한 뒤 먼저 읽기 전용 점검을 실행합니다.

```powershell
.\50_SETUP\SonarCloud\Sync-SonarCloudProjects.ps1 -Mode Audit
```

결과를 확인한 다음 실제 반영을 실행합니다.

```powershell
.\50_SETUP\SonarCloud\Sync-SonarCloudProjects.ps1 -Mode Apply
```

`SONAR_TOKEN` 환경 변수가 없으면 토큰을 마스킹 입력으로 요청합니다. 입력한 토큰은
파일에 저장하거나 화면에 출력하지 않습니다. 토큰은 SonarCloud의
`My Account > Security`에서 만들 수 있으며, 프로젝트 생성 및 설정 변경 권한이
필요합니다.

환경 변수로 한 번만 전달하려면 현재 PowerShell 세션에서 다음과 같이 실행할 수도
있습니다.

```powershell
$secure = Read-Host 'SonarCloud token' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $env:SONAR_TOKEN = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    .\50_SETUP\SonarCloud\Sync-SonarCloudProjects.ps1 -Mode Apply
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    Remove-Item Env:SONAR_TOKEN -ErrorAction SilentlyContinue
}
```

## 동작 범위

- `.gitmodules`의 저장소와 `Dreamine.Communication.FullKit`,
  `Dreamine.MVVM.FullKit`을 대상으로 합니다.
- 이미 존재하는 프로젝트는 다시 만들지 않습니다.
- 연결되지 않은 SonarCloud 프로젝트만 동일한 GitHub 저장소와 연결합니다.
- 새 코드 기준을 `Previous Version`으로 설정합니다.
- 프로젝트나 분석 기록을 삭제하지 않습니다.
- 코드 분석과 Quality Gate 수정은 별도 CI 작업입니다. 이 스크립트는 프로젝트
  등록과 연결 상태를 정리하는 도구입니다.

## CI 분석 전 필수 작업

SonarCloud 자동 분석과 GitHub Actions의 CI 분석은 동시에 사용할 수 없습니다.
각 프로젝트의 `Administration > Analysis Method`에서 **Automatic Analysis**를
끈 다음 CI 분석을 실행해야 합니다. 이 설정은 현재 SonarCloud UI에서 변경해야
하며, 이 스크립트는 자동으로 끄지 않습니다.

자동 분석 결과에는 테스트 커버리지가 포함되지 않습니다. `Coverage`가 비어 있는
FullKit 화면은 기존 자동 분석 결과이며, 저장소의 CI 워크플로가 커버리지 파일과
함께 새 분석을 완료해야 갱신됩니다.

새 코드 기준을 건드리지 않으려면 다음 옵션을 사용합니다.

```powershell
.\50_SETUP\SonarCloud\Sync-SonarCloudProjects.ps1 -Mode Apply -SkipNewCodeDefinition
```

## 참고

- [SonarQube Cloud Web API](https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/web-api)
- [SonarQube Cloud 프로젝트 설정](https://docs.sonarsource.com/sonarqube-cloud/managing-your-projects/administering-your-projects/setting-up-project)
- [새 코드 정의](https://docs.sonarsource.com/sonarqube-cloud/managing-your-projects/project-analysis/configuring-new-code-calculation)
- [자동 분석 비활성화와 CI 분석 충돌](https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/automatic-analysis)
