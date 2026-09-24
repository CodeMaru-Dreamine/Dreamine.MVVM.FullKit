# 마루 원정 오디오 분석 및 적용 기록

분석일: 2026-08-21

이 문서는 사용자가 Suno에서 직접 생성해 제공한 네 개의 MP3를 전투 BGM과 효과음으로 적용하기 위해 수행한 분석, 편집 구간, 출력 정책을 기록한다. 원본 MP3는 저장소에 복사하지 않으며, 웹 런타임에는 편집·압축한 OGG 자산만 포함한다.

## 원본 분석

- `Steel Against Stone.mp3`
  - 1.813초, 48 kHz 스테레오, 약 116 kbps
  - 피크 -0.321 dBFS, RMS -29.790 dBFS, 클리핑 없음
  - 0.05초에 단일 금속성 트랜지언트가 있고 약 0.8초 안에 실질적으로 감쇠한다.
  - 0.450~1.800초는 -50 dBFS 이하의 무음이다.
  - 반복 가능한 음악이 아니라 타격·검풍 효과음의 원본으로 분류했다.

- `Steel Relic Reel.mp3`
  - 213.733초, 48 kHz 스테레오, 약 179 kbps
  - 피크 -0.565 dBFS, RMS -13.262 dBFS, 클리핑 없음
  - 네 곡 중 가장 밀도가 높고 공격적인 편곡이다.
  - 102.5~192.5초가 경계 유사도 기준 최적 루프 후보여서 보스 전투 BGM으로 사용했다.

- `Ironfang Relics.mp3`
  - 309.214초, 48 kHz 스테레오, 약 192 kbps
  - 피크 +0.129 dBFS, RMS -18.965 dBFS
  - 전체 분석 샘플 중 1개만 0 dBFS를 넘었으며 편집 출력에서는 피크 제한으로 제거했다.
  - 208~298초가 가장 안정적인 루프 후보여서 일반 전투 BGM으로 사용했다.

- `Ironfang Relics (1).mp3`
  - 344.253초, 48 kHz 스테레오, 약 190 kbps
  - 피크 -2.104 dBFS, RMS -16.712 dBFS, 클리핑 없음
  - 342.45초 이후 약 1.8초의 종결 무음이 있다.
  - 111.5~201.5초를 지역·메뉴용 루프로 사용했다.

원본 완성곡 세 개는 음악과 타격 성분이 촘촘히 섞인 최종 마스터다. 소스 분리 없이 여기서 효과음을 강제로 뽑으면 음악 잔향과 박자가 같이 들리므로 전투 효과음으로 사용하지 않았다.

## 런타임 자산

모든 BGM은 선택한 90초 구간의 앞뒤 1.5초를 동일 출력 파일 안에서 크로스페이드해 88.5초 길이의 OGG Opus 루프로 만들었다. 전환 시에도 플레이어 서비스의 1.5초 크로스페이드를 사용한다.

- `ironfang-relics-battle.ogg`: `Ironfang Relics.mp3` 208~298초, 일반 전투
- `steel-relic-reel-boss.ogg`: `Steel Relic Reel.mp3` 102.5~192.5초, 보스 전투
- `ironfang-relics-region.ogg`: `Ironfang Relics (1).mp3` 111.5~201.5초, 지역·메뉴
- `sword-impact-normal.ogg`: `Steel Against Stone.mp3`의 단일 금속 타격을 정리한 일반 타격
- `sword-impact-heavy.ogg`: 같은 원본을 저역 보강·피치 다운한 치명타/처치 타격
- `sword-whoosh-light.ogg`: 같은 원본의 고역과 짧은 역방향 성분을 이용한 검풍

효과음은 논리 키별 최소 재생 간격과 최대 동시 보이스 수를 제한하고, 매 재생마다 작은 피치 편차를 적용한다. 같은 파일을 빠르게 반복해도 기계적인 플랜저 현상과 과도한 음량 합산이 줄어든다.

## 기본 믹스

- 전체 음량 80%
- BGM 40%
- 전투 효과음 80%
- UI 효과음 55%
- 음소거 시 모든 버스 출력 0
- 실제 BGM 출력은 `Master × BGM × TrackNormalization`으로 계산
- 실제 효과음 출력은 `Master × SFX × EffectNormalization`으로 계산

## 재생 연결

- 지역 일반 전투: `bgm-ironfang-battle`
- 10의 배수 보스 스테이지: `bgm-steel-relic-boss`
- 곡 탐색 목록: 위 두 곡과 `bgm-ironfang-region`
- 주인공/동료 공격: 검풍 후 일반 또는 치명타 타격
- 적 공격: `enemy-impact`
- 적 처치: `enemy-defeat`

브라우저 자동재생 정책 때문에 첫 공격이나 첫 화면 입력에서 `AudioContext`를 활성화한다. 활성화 전 재생 실패는 전투를 중단하지 않으며 기존 안내 UI가 한 번 표시된다.

## 재생성

FFmpeg 경로와 네 원본 파일이 준비된 환경에서 다음 스크립트를 실행한다.

```powershell
pwsh -File .\Assets\build_audio_assets.ps1
```

세부 측정치는 `analysis.json`과 `analysis.csv`, 파형 및 스펙트로그램 이미지는 이 폴더에 보관한다.

## 권리 메모

메타데이터에는 `User-provided Suno generation`과 `Project use per account terms`를 기록했다. 실제 배포·상업 이용 가능 범위는 생성 당시 사용자의 Suno 요금제와 약관에 따라 달라질 수 있으므로 출시 전에 계정의 생성 이력과 이용권을 별도로 확인해야 한다.
