# 📄 Dreamine.MVVM.Behaviors - WindowDragBehavior

이 모듈은 Microsoft의 [XamlBehaviors for WPF](https://github.com/microsoft/XamlBehaviorsWpf) 프로젝트에서 일부 코드를 참고하여 Dreamine 내부 구조에 맞게 리팩토링된 구현입니다.

---

## 🔍 원본 출처 및 라이선스

- 원본: https://github.com/microsoft/XamlBehaviorsWpf  
- 라이선스: MIT License (아래 참조)

---

## 📜 MIT License Notice

> Copyright (c) Microsoft  
> Permission is hereby granted, free of charge, to any person obtaining a copy  
> of this software and associated documentation files (the "Software"), to deal  
> in the Software without restriction, including without limitation the rights  
> to use, copy, modify, merge, publish, distribute, sublicense, and/or sell  
> copies of the Software, and to permit persons to whom the Software is  
> furnished to do so, subject to the following conditions:
> 
> The above copyright notice and this permission notice shall be included in  
> all copies or substantial portions of the Software.
> 
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND...

---

## ✍️ 리팩토링 내역

- 네임스페이스 변경  
  ↳ Microsoft.Xaml.Behaviors → Dreamine.MVVM.Behaviors  
  ↳ 외부 네임스페이스 제거 및 Dreamine 내부 구조에 통합

- 내부 구조 최적화  
  ↳ 불필요한 외부 참조 제거  
  ↳ Dreamine 내부 설계 관례에 맞게 클래스/메서드 재정렬

- 종속성 최소화 및 내부화 처리  
  ↳ NuGet 없이 독립적으로 관리 가능하도록 구조 정비

- 💬 주석 한글화 및 Doxygen 포맷 적용  
  ↳ 설계자의 의도를 전달하는 **한글 기반 기술 문서 주석** 형식 적용

---

## 🛠️ Dreamine 빌드 및 템플릿 자동화 도구

Dreamine 프로젝트에는 개발자의 생산성을 높이기 위한 자동화 배치파일들이 포함되어 있습니다.

### 🔹 `./CleanFile.bat`
- `bin/`, `obj/`, `.vs/`, `*.bak` 파일을 일괄 정리합니다.
- NuGet 로컬 패키지 및 VS 캐시도 함께 정리합니다.

### 🔹 `./Dreamine/.Templates/Dreamine.MVVM.Template/rebuild_template.bat`
- 템플릿 `.csproj`를 빌드 및 `.nupkg` 생성
- 기존 템플릿 제거 후 새 템플릿 설치
- Visual Studio 템플릿 캐시 자동 갱신  
- 아래와 같은 템플릿 선택 화면이 생성됩니다:

![template_preview](./Dreamine.MVVM.Template.preview.png)

### 🔹 `./Dreamine/Library/build-nuget.bat`
- Dreamine 모든 NuGet 패키지를 일괄 빌드 및 `.nupkg` 생성
- 필요시 NuGet.org 배포 (API Key 필요)

---

## 📦 결과물 경로

모든 `.nupkg` 파일은 다음 경로에 생성됩니다:

```
Dreamine\LocalPackages\
```

해당 경로에서 수동 설치 또는 `dotnet new install`, `dotnet nuget push`가 가능합니다.
