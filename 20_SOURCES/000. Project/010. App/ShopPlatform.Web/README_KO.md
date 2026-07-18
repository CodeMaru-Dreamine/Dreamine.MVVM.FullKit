# Shop Store

> 농산물, 소프트웨어 라이선스와 개발 용역을 직접 판매하는 CodeMaru 직영 쇼핑몰입니다.

![.NET](https://img.shields.io/badge/.NET-net8.0-512BD4) ![Version](https://img.shields.io/badge/version-1.0.0.0-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)

[서비스 열기](https://shop.codemaru.co.kr/) · [이용 설명서](https://codemaru.co.kr/guide/shop) · [GitHub](https://github.com/CodeMaru-Dreamine)

## 프로젝트 소개

농산물, 소프트웨어 라이선스와 개발 용역을 직접 판매하는 CodeMaru 직영 쇼핑몰입니다.

상품 탐색, 장바구니, 주문·회원 관리, 배송·환불 정책과 입점 신청 흐름을 제공합니다.

## 주요 기능

- 검색·카테고리·가격/최신순 정렬
- 실시간 합계 장바구니
- 배송·환불·교환 정책 페이지
- 농산물·소프트웨어·개발 용역 상품
- 외부 판매자 입점 신청

## 이용 순서

1. 상품을 검색하거나 카테고리에서 선택합니다.
2. 상세 설명과 이미지를 확인해 장바구니에 담습니다.
3. 수량과 합계를 확인하고 배송·결제 정보를 입력합니다.
4. 주문 완료 화면에서 주문 내역을 확인합니다.

## 프로젝트 정보

| 항목 | 값 |
|---|---|
| 프로젝트 | ShopPlatform.Web |
| 버전 | 1.0.0.0 |
| 대상 프레임워크 | net8.0 |
| 프로젝트 파일 | ShopPlatform.Web.csproj |

## 개발 환경에서 실행

```powershell
dotnet run --project "ShopPlatform.Web.csproj"
```

## API 문서 생성

```powershell
doxygen Doxyfile.kr
```
영문 문서는 `Doxyfile.en`으로 생성합니다.
