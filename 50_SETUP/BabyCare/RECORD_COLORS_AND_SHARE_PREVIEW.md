# 2026-09-25 기록 색상 및 공유 미리보기

## 동작 및 검증
- 수유 간격은 이전 수유의 시작부터 현재 수유의 시작까지입니다. 이전 기록이 없으면 그 사실을 표시합니다.
- 새 기저귀 기록: 대변 노랑/소변 연노랑 기본 선택. 실제 관찰 확인과 기본값을 구분합니다. 기존 기록은 미확인입니다.
- 색상만으로 정상/질병을 판정하지 않으며, 주의 색상은 상담 안내를 보여줍니다. 초기 태변과 생후 첫 주 요산 결정은 별도 설명합니다.
- 42개 테스트 통과. 실제 SQLite에서 보호자 간 공유/수정 검증. 격리 UI에서 클릭/키보드 편집, 삭제 확인 독립 동작, 색상 저장 및 390px 화면 검증.
- 초기 HTML OG 4개 항목 단일 출력, 익명 PNG 응답 검증. 실제 KakaoTalk 미리보기와 운영 캐시는 배포 후 확인해야 합니다.

## 의학 안내 참고
- https://www.healthychildren.org/English/ages-stages/baby/Pages/The-Many-Colors-of-Poop.aspx
- https://www.healthychildren.org/English/ages-stages/baby/Pages/Babys-First-Days-Bowel-Movements-and-Urination.aspx
- https://www.niddk.nih.gov/health-information/urologic-diseases/hematuria-blood-urine

## 공유 이미지
내장 imagegen 도구로 제작한 PNG. BabyCare wwwroot/images/babycare-share-20260924.png, CodeMaru wwwroot/img/babycare-service.png에서 같은 이미지를 사용합니다.
디자인 지시: 넓은 1.91:1 비율, 아이보리/세이지/포레스트 그린, 한국어 브랜드 아기하루, 큰 문구 말로 남기는 / 우리 아기의 하루, 보조 문구 수유·수면·기저귀, 가족과 함께 기록해요. 마이크 말풍선과 젖병·초승달·구름의 부드러운 입체 일러스트, 서비스 주소 포함.
Games 카드는 기존 코드 스타일에 맞춘 자체 SVG 일러스트입니다.

배포 후 오래된 공유 정보가 남으면 https://developers.kakao.com/tool/clear/og 에서 해당 URL의 캐시를 초기화한 뒤 새 메시지로 확인하세요.
