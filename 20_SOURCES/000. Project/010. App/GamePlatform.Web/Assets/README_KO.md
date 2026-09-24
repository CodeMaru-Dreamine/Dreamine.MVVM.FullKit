# 마루 원정 자산 출처

## 2026-08-21 개별 동료·보스 호위 교체

- 동료는 더 이상 한 장의 합성 아틀라스를 잘라 쓰지 않는다. OpenAI 내장 ImageGen으로 한 명씩 별도 생성한 실제 알파 PNG를 사용하며, 런타임 파일은 512×768로 최적화했다.
- `wwwroot/images/maru-idle/party/companions/haejin-v1.png`: 남색·비취색 갑주의 남성 창 수호자 해진.
- `wwwroot/images/maru-idle/party/companions/seolbi-v1.png`: 흑자색·은색 복식과 초승달 활을 든 월궁 사수 설비.
- `wwwroot/images/maru-idle/party/companions/muwon-v1.png`: 상아색·먹청색 도포, 지팡이와 부적을 쓰는 치유사 무원.
- `wwwroot/images/maru-idle/party/companions/gajin-v1.png`: 원형 방패와 철퇴를 든 남청색 철벽 호위 가진.
- `wwwroot/images/maru-idle/party/companions/cheongha-v1.png`: 검정·심청색 경장과 쌍곡도를 쓰는 척후 청하.
- `wwwroot/images/maru-idle/enemies/guards/shadow-jackal-v1.png`: 장병기를 든 뿔 달린 그림자 호위수.
- `wwwroot/images/maru-idle/enemies/guards/stone-lion-v1.png`: 사슬 철퇴를 든 석갑 사자 호위수.
- 공통 생성 방향: 독자적인 동양 암흑 판타지 반실사 전신 캐릭터, 3/4 전투 자세, 투명 여백, 머리카락·무기 외곽의 깨끗한 알파, 문자·UI·로고·워터마크·배경·다른 인물 없음.
- 외부 게임 자산, 첨부 화면의 캐릭터, 제3자 일러스트는 입력 이미지나 재료로 사용하지 않았다.

## 2026-08-20 파티·보스 호위대

- `wwwroot/images/maru-idle/party/support-party.png`: 프로젝트 전용 AI 독자 생성 5인 지원대 합성 전투 레이어(OpenAI 내장 ImageGen).
- `wwwroot/images/maru-idle/party/companions-v2.png`: 메인 검객과 동일 계열의 화풍으로 정리한 5인 동료 가로 스프라이트 시트(OpenAI 내장 ImageGen). 캐릭터당 동일한 1/5 영역을 사용하며 순검정 배경을 알파 채널로 변환해 전투·카드에서 공용으로 표시한다.
- `wwwroot/images/maru-idle/enemies/boss-guards.png`: 프로젝트 전용 AI 독자 생성 보스 호위대 합성 전투 레이어(OpenAI 내장 ImageGen).
- 세 자산 모두 제3자 게임 캐릭터·명칭·UI를 참조하거나 복제하지 않았다.
- `support-party.png`, `companions-v2.png`, `boss-guards.png`는 이전 시안 보존용이며 현재 전투 화면에서는 사용하지 않는다.
- `companions-v2.png` 생성 프롬프트 요약: 창 수호·월궁 사수·부적 치유·철벽 호위·쌍검 척후를 동일 크기 5열 전신 시트로 구성하고, 동양 판타지 반실사 화풍과 남색·청록·아이보리·고금색 팔레트를 통일한다. 알파 생성이 지원되지 않은 출력은 체크무늬를 사용하지 않고 균일한 순검정 배경으로 다시 생성했다.

## 2026-08-20 검객 투명 경계 보정

- `wwwroot/images/maru-idle/swordswoman-idle-v2.png`: 기존 프로젝트 전용 검객을 입력 자산으로 사용해 OpenAI 내장 ImageGen의 `background-extraction` 방식으로 다시 생성한 실제 알파 PNG.
- 최종 프롬프트: 가짜 체크무늬·흰색 매트·머리카락과 검 주변의 배경 픽셀을 제거하고 얼굴, 복식, 검, 전신 대기 자세를 유지하며 외곽을 실제 투명 알파로 출력.
- 공격 시에는 깨진 체크무늬가 들어 있던 별도 공격 이미지를 사용하지 않고 동일한 투명 스프라이트에 CSS 이동·검격 레이어를 적용한다.

이 폴더와 `wwwroot/images/maru-idle`의 자산은 첨부 목업을 잘라 쓰거나 기존 게임에서 가져오지 않았다. 2026-08-20에 이 프로젝트 전용으로 생성·제작했다.

## 생성 이미지

- 생성 도구: OpenAI 내장 이미지 생성 도구
- 외부 원본·스톡·기존 게임 자산: 사용하지 않음
- 원본 PNG: `Assets/Source`
- 런타임 투명 WebP: `wwwroot/images/maru-idle/*.webp`
- 런타임 지역 WebP: `wwwroot/images/maru-idle/regions/*.webp`
- 배경 최적화: `optimize_regions.py`, 모두 500KB 이하
- 스프라이트 최적화: `optimize_sprites.py`
- 생성기가 체크무늬 미리보기를 실제 알파로 내보내지 않은 경우 `remove_checkerboard.py`로 연결된 배경만 제거하고 캐릭터 픽셀은 유지함

공통 프롬프트 원칙은 “독자적인 동양 판타지 2D 게임 자산, UI·문자·로고·기존 작품 모방 없음, 캐릭터와 배경 분리”였다. 최종 자산별 핵심 프롬프트는 다음과 같다.

- `swordswoman-idle`: 남색 여행복·비취색 숄·둥근 투각 코등이의 젊은 떠돌이 검객, 전신 3/4 대기 자세, 투명 배경
- `swordswoman-attack`: 동일 인물·복식·색을 유지한 낮은 전진 베기 자세, 투명 배경
- `guardian-idle`: 석맥 몸체·삼나무 뿔·이끼·청록 균열을 가진 독자적인 산 수호수, 전신 대기 자세, 투명 배경
- `guardian-hit`: 동일 수호수가 충격을 받아 짧게 뒤로 물러나는 피격 자세, 투명 배경
- `enemy-bell-fox`: 청동 종 형태 흉곽과 음파 빛을 지닌 점판암 여우
- `enemy-cinder-cat`: 흑요석 발과 붉은 잎 갈기의 산고양이
- `enemy-frost-yak`: 얼음 깃 갑주와 바람 깃발을 단 설원 들소
- `enemy-rain-heron`: 잉어 비늘과 우산형 날개를 지닌 수상 해오라기
- `enemy-amber-scarab`: 사암 다리와 청동 천문환을 지닌 유리 풍뎅이
- `enemy-blue-salamander`: 광물 지느러미와 푸른 불꽃 촉각의 동굴 도롱뇽
- `boss-blue-wake`: 청혼 숨굴 도롱뇽 계열을 확장한 고대 영맥룡, 수정 지느러미·흑요석 뿔·깨진 종과 사슬을 지닌 보스, 투명 배경
- `enemy-cloud-antelope`: 백자 갑주와 풍경을 단 구름 영양
- `enemy-rift-crawler`: 숯빛 갑각과 자색 균열을 지닌 육지행충
- `enemy-eclipse-lion`: 암금 칠갑과 일식 고리를 지닌 황정 사자
- `silver-reed-marsh`: 물안개, 은빛 갈대, 얕은 수로와 낡은 나무다리
- `hollow-bell-sanctum`: 푸른 달빛 산악 관측원, 청동 풍경, 젖은 석조 단상
- `cinderleaf-pass`: 검은 현무암과 붉은 잎, 호박색 석양의 고갯길
- `white-kite-rampart`: 설산 성벽, 창백한 바람 깃발과 빙결 계단
- `rainmirror-quays`: 비 내리는 수상 누각, 청록 기와와 등불 반사
- `amber-hour-reliquary`: 사암 고리와 청동 천문 구조물이 남은 황혼 사구
- `bluewake-hollows`: 청록 영천과 반투명 돌종이 있는 코발트 동굴
- `bluewake-hollows-boss`: 청혼 숨굴의 보스 전용 흑요석 원형 제단, 청록 룬·거대 쇠종·영혼 안개가 있는 9:16 전장
- `ninewind-court`: 구름바다 위 백석 궁정과 거대한 바람 띠
- `ashrift-march`: 잿빛 전야, 공중 균열과 기울어진 봉화탑
- `eclipsed-gold-palace`: 검은 일식 아래 비어 있는 암금빛 황정
- `fallback`: 안개 산맥과 단순한 젖은 석조 전장

## 코드 제작 SVG

다음 자산은 이 프로젝트에서 직접 작성한 독자 SVG이며 제3자 아이콘 묶음을 사용하지 않았다.

- `wwwroot/images/maru-idle/effects/slash.svg`
- `wwwroot/images/maru-idle/effects/impact.svg`
- `wwwroot/images/maru-idle/icons/forge.svg`
- `wwwroot/images/maru-idle/icons/skill.svg`
- `wwwroot/images/maru-idle/icons/attack.svg`
- `wwwroot/images/maru-idle/icons/armor.svg`
- `wwwroot/images/maru-idle/icons/archive.svg`

프로젝트 외부로 자산을 재배포할 때에는 해당 시점의 OpenAI 생성물 이용 조건과 저장소 라이선스를 별도로 확인한다.

## 독자 음악 자산

- 런타임 파일: `wwwroot/audio/maru-idle/*.mp3` (44.1kHz 스테레오, 96kbps, 곡당 8초 루프)
- 메타데이터: `Content/audio-tracks.json`
- 생성 스크립트: `Assets/generate_music.py`
- 작곡·출처: `CodeMaru procedural score`
- 라이선스: `Project original`
- 외부 OST·스톡 음원·인터넷 다운로드 음원: 사용하지 않음

각 일반 지역과 보스는 서로 다른 `AssetKey`, 곡명, 기준 음정과 재현 시드를 가진다. 메타데이터에는 `ComposerOrSource`, `License`, `LoopStart`, `LoopEnd`, `VolumeNormalization`을 기록했다. 브라우저는 MP3를 우선 디코딩하며 파일 재생 실패 시 같은 메타데이터의 프로젝트 전용 프로시저럴 스코어로 폴백한다.
