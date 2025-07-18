# 🧾 RelayCommandAttribute.cs

## 📌 개요
이 속성은 **Dreamine.MVVM**에서 필드에 붙여 `INotifyPropertyChanged` 속성으로 자동 변환되도록 설계된 Attribute입니다.  
보통 SourceGenerator와 함께 사용되며, ViewModel 속성 정의를 간결하게 만듭니다.
## 📂 파일 경로
Dreamine.MVVM.Attributes/RelayCommandAttribute.cs

## 🧠 주요 기능
- 필드에 적용하여 `get/set` 속성 자동 생성  
- 속성 이름 지정 가능 (생략 시 필드명 기반)  
- SourceGenerator와 연계되어 속성 자동 생성  
## 💡 사용 예시
```csharp
[Property] 
private int _count;
```
Source Generator에 의해 다음과 같은 속성 생성됨:

```csharp
public int Count
{
    get => _count;
    set
    {
        _count = value;
        OnPropertyChanged(nameof(Count));
    }
}
```

명시적 이름 지정도 가능:

```csharp
[Property("TotalCount")] 
private int _total;
```

## 🛠️ 내부 구조 _(클래스 구성 요소 요약)_

| 멤버 이름 | 접근 수준 | 타입 | 설명 |
|-----------|-----------|------|------|
| `CommandName` | `public` | `string` | 생성할 커맨드 속성 이름 (옵션) |
| `RelayCommandAttribute()` | `public` | `ctor` | 기본 생성자 |
| `RelayCommandAttribute(string commandName)` | `public` | `ctor` | 명시적 커맨드 속성 이름을 지정하는 생성자 |

## 🔒 제약 사항
- `[AttributeUsage(AttributeTargets.Field)]` 으로 설정됨  
- **필드에만 사용 가능**  
- 클래스, 속성, 메서드 등에는 사용할 수 없음


## 🧩 관련 모듈
- Dreamine.MVVM.Attributes
- Dreamine.MVVM.Generators (사용 측)

## 🗂️ 버전 관리

| 버전 | 변경 내용           | 날짜         |
|------|--------------------|--------------|
| v1.0 | RelayCommandAttribute.cs 문서 자동 생성 | 2025-05-10 |

## 📁 소속 모듈
Dreamine.MVVM.Attributes

---
✍️ 기록자: 아키로그 드림  
🤖 협력자: ChatGPT (프레임워크 유혹자)  
📅 자동 생성됨 · Dreamine 문서화 도구  

---
## ⛏️ 자유 작성 영역

- [ ] 설명 추가 또는 TODO 항목 작성
- [ ] 특이점, 예외 상황, 사용자 주석 등 기술 메모 작성 가능
- [ ] 이 영역은 자동 생성 도구에 의해 변경되지 않습니다.
```yaml
TODO:
  - 여기에 설명 또는 작업 내용을 작성하세요
```
