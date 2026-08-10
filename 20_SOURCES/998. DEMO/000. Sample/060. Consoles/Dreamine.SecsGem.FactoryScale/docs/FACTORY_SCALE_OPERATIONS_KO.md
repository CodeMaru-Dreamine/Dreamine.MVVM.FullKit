# Factory-Scale 운영 가이드

## 도구 성격과 현재 증거 상태

`Dreamine.SecsGem.FactoryScale`은 Dreamine SECS/GEM Host를 다수의 자체 TCP loopback Equipment와 함께 검증하는 headless Demo/Test 실행기다. 제품용 Simulator, SEMI compliance 도구, 외부 Simulator 자동화 도구가 아니다.

현재 자체 loopback 증거에서 in-process staged 100/250/500/1,000대, Idle-1000 1시간, Normal-500 1시간, multi-process 250대/5-worker smoke, multi-process 500대/5-worker 중 100 endpoint reconnect가 `Passed`다. 20대/2-worker Host restart는 실행 판정은 `Passed`지만 용량 증거가 아닌 `Experimental`이다. 6시간과 24시간은 `Not Run`, External Simulator와 Real Equipment는 `Not Run / Waiting for User`다.

## Release 빌드

Repository root에서 다음과 같이 실행한다.

```powershell
dotnet build <FactoryScale.csproj> --configuration Release
```

장시간 시험 전에는 전체 solution의 Release build 및 관련 SECS/GEM 회귀 시험도 별도로 완료한다. 이 프로젝트는 Demo/Test 실행기이며 pack 대상이 아니다.

명령 예시는 `dotnet run`을 사용하지만, 반복 측정에서는 먼저 Release build한 뒤 같은 binary를 직접 실행해 build 변동과 build 시간을 측정 구간에서 제외하는 편이 좋다.

기록된 검증 기준선은 다음과 같다.

| 항목 | 결과 |
|---|---|
| Release builds | FactoryScale과 WPF 모두 warning 0, error 0 |
| FactoryScale tests | 42/42 passed |
| WPF integration tests | 19/19 passed |
| SECS/GEM Core tests | 222 passed |
| 전체 solution tests | 664 passed, 이미 알려진 Ontology failure 1개 별도 존재 |
| Public/package surface | SECS/GEM Core와 NuGet API, package version, tag, release 변경 없음 |

따라서 전체 test가 모두 green이라고 표현하면 안 된다. 알려진 실패를 분리해서 기록하고, 이번 FactoryScale 결과와 관련 회귀가 통과했다는 범위만 주장한다.

## 명령 형식

```text
Dreamine.SecsGem.FactoryScale host [options]
Dreamine.SecsGem.FactoryScale worker --start-index N --count N [options]
Dreamine.SecsGem.FactoryScale equipment [options]
Dreamine.SecsGem.FactoryScale scenario NAME [options]
Dreamine.SecsGem.FactoryScale in-process NAME [options]
Dreamine.SecsGem.FactoryScale multi-process NAME [options]
```

지원 scenario:

```text
factory-smoke | scale | idle-factory | factory-normal | factory-busy
trace-burst | large-message | reconnect-storm | host-restart
fault-isolation | soak
```

`normal-factory`, `busy-factory` 별칭도 parser가 허용한다. 문서와 evidence 이름에는 canonical 이름인 `factory-normal`, `factory-busy`를 사용한다.

Option은 반드시 `--name value` 형식으로 쓴다. `--name=value`, 알 수 없는 option, 해당 subcommand에 허용되지 않은 option, 중복 option, 값 누락은 usage error다. Duration은 invariant `TimeSpan` 형식의 양수이며 최대 7일이다.

도움말:

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- --help
```

## Exit code

| Code | 의미 | 운영 조치 |
|---:|---|---|
| `0` | 실행 결과 success | JSON의 status와 모든 check를 확인한 뒤에만 해당 실행을 인용한다. |
| `1` | runtime failure | exception, child exit, I/O, process supervision, 환경 오류를 조사한다. |
| `2` | acceptance failure | 실행은 끝났지만 protocol·성능·격리·cleanup 판정을 만족하지 못했다. |
| `64` | usage error | command, scenario, option 이름·형식·범위를 수정한다. |
| `130` | caller cancellation | 취소 원인과 child cleanup/kill fallback 여부를 확인한다. |

Process exit 0만으로 Factory-scale `Passed`를 선언하지 않는다. 결과 JSON의 status, checks, first failure, Host cleanup, worker별 cleanup이 모두 해당 기준에 맞아야 한다.

## 주요 기본값

명시적으로 변경하지 않을 때 적용되는 주요 값은 다음과 같다.

| 항목 | 기본값 |
|---|---:|
| Port range | 20000–48000 |
| Connect concurrency | 64 |
| Reconnect concurrency | 16 |
| Message concurrency | 128 |
| Protocol receive queue | 4,096 |
| Diagnostic log queue | 2,048 |
| Result export queue | 64 |
| Pending per Equipment | 16 |
| Pending global | 4,096 |
| Worker 한 개의 최대 Equipment | 100 |
| Worker heartbeat | 1 s |
| Worker ready timeout | 30 s, 명시 option으로 변경 가능 |
| Graceful shutdown timeout | 15 s, 명시 option으로 변경 가능 |

Scenario별 기본 Equipment 수는 `idle-factory` 1,000, `factory-normal` 500, `factory-busy` 100, `trace-burst` 20, `reconnect-storm` 500, `large-message` 20, `fault-isolation` 6이며 그 밖에는 100이다. 이 값은 권장 또는 검증된 용량이 아니다. 운영자는 첫 실행에서 `--equipment-count`와 `--duration`을 명시해야 한다.

## 빠른 in-process 확인

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  in-process factory-smoke `
  --equipment-count 2 `
  --output <artifact-dir>/in-process-smoke.json
```

이 명령은 한 process 안에서 Host와 합성 passive Equipment fleet를 실행한다. 빠른 개발 회귀에는 유용하지만 process isolation 증거는 아니다. 새로 실행한 결과를 확인하기 전 상태는 `Not Run`이다.

## Multi-process 확인

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  multi-process factory-smoke `
  --equipment-count 4 `
  --worker-count 2 `
  --equipment-per-worker 2 `
  --ready-timeout 00:00:30 `
  --shutdown-timeout 00:00:15 `
  --output <artifact-dir>/multi-process-smoke.json
```

Coordinator는 worker를 시작하고 ready/manifest identity를 검증한 뒤 실제 TCP 연결을 만든다. `worker-count × equipment-per-worker`가 요청 수보다 작으면 parser가 거부한다. worker는 1~100개 endpoint만 소유할 수 있다.

새로운 실행을 자동으로 과거 `Experimental` 결과와 같다고 간주하지 않는다. 결과 파일에 기록된 실행 자체만 판정한다.

기록된 multi-process 기준선은 다음과 같다.

| Scenario | 범위 | 결과 | 핵심 판정 |
|---|---|---|---|
| `factory-smoke` | 250대 / 5 workers | `Passed` | 250/250 Selected, application pair 502/502, timeout/failure/correlation 0, 5 workers zero-resource graceful exit |
| `reconnect-storm` | 500대 / 5 workers, 100 endpoint restart | `Passed` | reconnect 100/100, pair 1,001/1,001, peak reconnect 16/16, Host와 5 workers cleanup 0 |
| `host-restart` | 20대 / 유지된 2 workers | `Experimental` — run `Passed` | 서로 다른 두 Host PID, 20/20 ConnectionId 교체, 두 Host와 workers cleanup 0 |

이 결과는 exact self-loopback 실행에만 적용되며 다른 count, worker partition, 외부 장비로 일반화하지 않는다.

## Scenario 선택

### Scale와 collision isolation

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  multi-process scale `
  --equipment-count <count> `
  --worker-count <workers> `
  --equipment-per-worker <capacity> `
  --output <artifact-dir>/scale.json
```

Baseline Linktest, S1F1/S1F2, S1F13/S1F14와 서로 다른 연결에서 동일 Session ID/System Bytes가 격리되는지를 확인한다. 기록된 in-process staged 결과는 다음과 같다.

| Equipment | Selected | Requests / Responses | Timeout / Failure / Correlation | 결과 |
|---:|---:|---:|---:|---|
| 100 | 100 | 203 / 203 | 0 / 0 / 0 | `Passed` |
| 250 | 250 | 503 / 503 | 0 / 0 / 0 | `Passed` |
| 500 | 500 | 1,003 / 1,003 | 0 / 0 / 0 | `Passed` |
| 1,000 | 1,000 | 2,003 / 2,003 | 0 / 0 / 0 | `Passed` |

### Normal/Busy/Trace workload

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  scenario factory-normal `
  --mode in-process `
  --equipment-count <count> `
  --messages-per-second <aggregate-rate> `
  --duration 00:05:00 `
  --snapshot-directory <artifact-dir>/snapshots `
  --output <artifact-dir>/normal.json
```

`--messages-per-second`은 workload target이며 달성 결과가 아니다. JSON의 actual rate, completed request/response, timeout, correlation error, latency, queue peak를 함께 본다.

단위를 분리해서 읽어야 한다.

- Scenario check의 기존 `achieved ... msg/s`는 수치상 Primary/request-reply **pair/s**다.
- JSON `messagesPerSecond`는 `requestsPerSecond + responsesPerSecond`, 즉 양방향 SECS data-message rate다.
- 정상 1:1 응답이면 JSON message rate는 pair rate의 약 2배다.
- Select/Linktest control frame은 application message·latency counter에서 제외되지만 frame byte와 pending-control에는 포함된다.
- 현재 실행기는 sustained profile final에 마지막 non-zero interval rate를 보존한다. WPF는 terminal no-delta rate가 0이던 기존 JSON도 fallback 해석한다. Final total과 interval 의미는 계속 함께 본다.

기록된 exact 결과:

| Profile | Final / workload pairs | Last pair/s | Last bidirectional messages/s | Avg / P95 / P99 / Max (ms) |
|---|---:|---:|---:|---|
| Normal-500, 1 h | 1,800,999 / 1,799,999 | 499.9985256759284 | 999.9970513518568 | 2.4885078757400754 / 15.02 / 15.62 / 55.3114 |
| Busy-100, 1 min | 60,187 / 59,987 | 999.2392142731878 | 1,998.4784285463757 | 2.481821772143486 / 14.94 / 15.52 / 42.9831 |
| Trace-20, 1 min | 120,028 / 119,988 | 1,998.8966852619521 | 3,997.7933705239043 | 2.6960764246675777 / 14.81 / 15.35 / 36.1535 |

Idle-1000 1시간도 `Passed`다. 1,000/1,000 Selected, baseline application 2,000/2,000, baseline을 포함한 Linktest exchange 62,000, timeout/failure/correlation 0이었다. Idle checkpoint의 application `messagesPerSecond=0`과 약 466–467 bytes/s는 control frame이 byte에만 포함되는 계측 정의와 일치한다.

### Large message

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  in-process large-message `
  --equipment-count <count> `
  --message-bytes 1048576 `
  --output <artifact-dir>/large-message.json
```

`--message-bytes` 허용 범위는 0부터 protocol limit에서 profile overhead를 뺀 parser 상한까지다. 값 0은 scenario 기본 payload를 사용한다. OS memory와 frame limit을 먼저 확인하고 소규모부터 늘린다.

기록된 실행은 16개 × 1,048,576-byte body와 4개 × 16,777,202-byte body가 모두 `Passed`였다. 각각 final pair 56/56 및 12/12, sent bytes 16,778,664 및 67,109,112였다. 최대 body는 SECS item header 4 bytes와 HSMS header 10 bytes를 더해 declared frame 16,777,216, length prefix를 포함한 wire frame 16,777,220 bytes다.

### Reconnect storm

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  multi-process reconnect-storm `
  --equipment-count <count> `
  --worker-count <workers> `
  --equipment-per-worker <capacity> `
  --disconnect-count <target> `
  --reconnect-concurrency 16 `
  --output <artifact-dir>/reconnect.json
```

Multi-process reconnect는 worker 단위로 재시작하므로 실제 affected count가 `disconnect-count`를 넘을 수 있다. 결과의 요청 count와 실제 affected Equipment 수, unaffected peer probe, peak reconnect operation, recovery time, 재Select, old/new `ConnectionId`, worker pre-stop/final cleanup을 확인한다.

기록된 in-process 500대 실행은 target 10/50/100/250에서 각각 reconnect 10/10, 50/50, 100/100, 250/250으로 `Passed`했고, 각 결과는 pair 1,001/1,001 및 timeout/failure/correlation 0이었다. Peak reconnect는 10/16/16/16이었다. Multi-process 500대/5-worker 중 100 endpoint restart도 reconnect 100/100, peak 16/16, pair 1,001/1,001로 `Passed`했다.

### Host OS process restart

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  multi-process host-restart `
  --equipment-count 4 `
  --worker-count 2 `
  --equipment-per-worker 2 `
  --output <artifact-dir>/host-restart.json
```

이 경로는 worker fleet를 유지하고 Host-only child 1과 child 2를 순서대로 실행한다. 다음을 모두 확인한다.

- 두 Host PID가 다름
- 모든 장비의 1차·2차 `ConnectionId`가 다름
- System Bytes/pending transaction이 process 사이에서 승계되지 않음
- worker가 사이 단계에서 같은 endpoint로 실제 `Listening` 복원
- 2차 Host가 전 장비 재Select 후 baseline traffic 완료
- 두 Host 결과 모두 pending/control/session/socket cleanup 0
- 모든 worker가 마지막에 session/listener/operation/queue/socket cleanup 0
- normal exit이며 kill fallback을 사용하지 않음

현재 기록된 대표 결과는 20대/2-worker이며 JSON run status는 `Passed`, 증거 성숙도는 `Experimental`이다. 같은 명령을 다시 실행한다고 이전 판정이 자동으로 승계되거나 Factory-scale capacity `Passed`가 되지 않는다.

### Fault isolation

In-process `fault-isolation`은 local fleet behavior와 raw protocol fault suite를 사용한다. Multi-process worker의 remote behavior를 바꾸는 command/ack IPC는 현재 구현되어 있지 않다. 따라서 multi-process `fault-isolation`은 의도적으로 성공 결과로 위장하지 않으며, 해당 경로의 상태는 `Pending`이다.

기록된 in-process 6대 fault 실행은 `Passed`다. Final은 request 15, response 12, expected timeout 2, expected failure 3, failed Equipment 1, correlation error 0이었다. Non-zero timeout/failure는 주입한 no-response, callback/no-response, remote-close의 기대 결과이며 healthy peer는 S1F1과 Linktest를 계속 성공했다.

## Equipment-only와 Host-only 분리 실행

외부 제품을 자동화하지 않고 runner 양쪽을 수동으로 나누어 관찰할 때 사용한다.

### 1. 합성 Equipment endpoint 시작

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  equipment `
  --start-index 1 `
  --count 2 `
  --duration 00:10:00 `
  --port-range-start 20000 `
  --port-range-end 20010 `
  --control-directory <artifact-dir>/control `
  --output <artifact-dir>/equipment-result.json
```

Console에 endpoint manifest 경로가 출력된다. `ready`는 listener가 준비되었다는 뜻이며 HSMS Selected를 뜻하지 않는다. Equipment process는 duration, Ctrl+C, identity가 맞는 stop request 또는 parent 종료까지 유지된다.

### 2. Manifest를 사용하는 Host-only 실행

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  host `
  --manifest <endpoint-manifest.json> `
  --equipment-count 2 `
  --output <artifact-dir>/host-result.json
```

Host-only에는 manifest가 반드시 필요하다. manifest의 endpoint 수보다 큰 `--equipment-count`는 실패한다. Host-only는 manifest의 앞쪽 요청 수만 사용하며 baseline protocol 확인 후 정리하고 종료한다.

Manifest는 실행기가 만든 자체 loopback format을 사용한다. 임의 파일을 신뢰하지 않으며 schema, 중복 Equipment ID/endpoint, connection definition을 검증한다. Multi-process supervisor는 추가로 manifest path가 control directory 안에 있는지와 run/worker/PID/count가 launch/ready record와 일치하는지 검증한다.

## Worker subcommand

`worker`는 보통 coordinator 전용 내부 진입점이다.

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  worker `
  --start-index 1 `
  --count 2 `
  --run-id <run-id> `
  --worker-id <worker-id> `
  --control-directory <artifact-dir>/control `
  --output <artifact-dir>/worker-result.json
```

수동 worker 실행은 제어 protocol을 이해하는 경우에만 사용한다. Coordinator가 시작한 worker를 별도로 재사용하거나 같은 run/worker identity로 두 개 실행하지 않는다.

## Bounded option 조정

| Option | 역할 | 조정 원칙 |
|---|---|---|
| `--connect-concurrency` | 초기 connect/Select 동시 작업 | CPU/socket headroom을 보고 소규모부터 증가 |
| `--reconnect-concurrency` | reconnect 동시 작업 | storm의 peak가 이 값을 넘지 않는지 확인 |
| `--message-concurrency` | Host message 작업 동시 수 | pending-global 및 queue capacity와 함께 조정 |
| `--receive-queue-capacity` | protocol receive work queue | 무조건 크게 하지 말고 peak와 메모리를 함께 측정 |
| `--log-queue-capacity` | diagnostic queue | 가득 차면 newest diagnostic drop이 count됨 |
| `--export-queue-capacity` | result/snapshot export queue | 느린 disk에서 backlog 관찰 |
| `--pending-per-equipment` | 장비별 pending admission | 한 장비가 전역 permit을 독점하지 않게 제한 |
| `--pending-global` | 전체 pending admission | message concurrency 이상이 되도록 의도적으로 설정 |
| `--ready-timeout` | worker readiness deadline | 대규모 startup 시간과 실패 탐지 균형 |
| `--shutdown-timeout` | cooperative stop deadline | 만료 시 owned child-tree kill fallback이 failure evidence에 남음 |

Host message와 result-export queue는 bounded `Wait`를 사용한다. Equipment receive queue는 synchronous callback이 ThreadPool thread를 막지 않도록 capacity 초과를 명시적으로 `Reject`하고 reject counter를 증가시키며, 정상 scenario는 reject 0을 요구한다. Diagnostic queue는 capacity에서 newest 항목을 drop하고 count를 증가시킨다. Diagnostic drop 0은 protocol loss 0과 같은 의미가 아니다.

## Process supervision

Coordinator가 child를 시작할 때 shell을 사용하지 않고 argument를 개별 전달하며 stdout/stderr를 동시에 drain한다. evidence tail은 line 수, 전체 byte, line 길이가 제한된다. 무한히 출력하는 child 때문에 parent가 교착되거나 메모리를 무제한 사용하는 것을 방지하기 위한 정책이다.

정상 종료 순서:

1. stop request atomic write
2. worker의 workload/heartbeat 종료
3. fleet disconnect/dispose
4. worker result atomic write
5. bounded process wait 및 stdout/stderr drain 완료
6. child dispose

Deadline이 지나면 coordinator가 소유한 child tree만 kill fallback 대상으로 삼는다. kill을 사용한 run은 성공 cleanup 증거가 아니다. process 이름 전체 검색으로 unrelated process를 종료하지 않는다.

동일 coordinator에 동시에 dispose가 호출되면 모든 caller가 하나의 cleanup completion을 기다린다. 취소나 worker exit 상황에서도 child result/exit를 확인한다.

## Result 판독

최종 JSON에서 다음 순서로 본다.

1. `Status`, `FirstFailure`, `EvidenceScope`
2. requested/connected/selected와 protocol counter
3. latency와 achieved rate
4. queue capacity/depth/peak/rejected/dropped
5. pending 및 operation current/peak
6. 장비별 `EquipmentId`, `ConnectionId`, state, request/response/timeout
7. `CleanupBeforeForcedGc`
8. 보충용 `CleanupAfterForcedGc`
9. multi-process worker result와 process exit/kill 여부

Forced GC 전 cleanup이 1차 판정이다. Post-GC 감소로 explicit dispose 실패를 덮지 않는다. OS metric이 unavailable이면 0으로 해석하지 않는다.

Qualified open-socket 0 판정은 현재 Windows PID TCP table provider가 필요하다. 다른 플랫폼 또는 provider unavailable 상태에서는 multi-process worker cleanup을 실패로 처리하며, 해당 실행을 zero-socket 합격 증거로 사용하지 않는다.

Cleanup 0은 Factory-owned session/listener/current pending·control·reconnect operation/business queue/scenario worker와 process-owned socket을 뜻한다. `resultExportWorkers=1`은 caller-owned exporter baseline도 1인 `1/1` 비교이며 전체 CLR Task 수가 아니다. Working set, managed heap, thread, handle은 cleanup 후에도 process 시작 baseline보다 높을 수 있다.

실제 1시간 결과의 forced-GC 후 residual은 다음과 같았다.

- Normal-500: WS +45,326,336 bytes, heap +2,900,968 bytes, handles +197
- Idle-1000: WS +41,271,296 bytes, heap +5,185,816 bytes, threads +30, handles +240

Residual만으로 leak이라고 단정하지 않고 같은 build·machine·workload의 반복 실행으로 비교한다. 반대로 forced GC로 heap이 감소해도 explicit dispose failure를 pass로 바꾸지 않는다.

Multi-process의 단일 Host `Process` snapshot을 Host+worker 전체 합계로 해석하지 않는다. Host와 worker별 process 결과를 분리해서 보며, 합산한 logical count는 aggregate라고 명시한다.

## 문제 해결

### Worker ready timeout

- port 범위 용량과 충돌을 확인한다.
- child stderr tail과 exit code를 확인한다.
- ready record와 manifest가 같은 run/worker/PID/count인지 확인한다.
- timeout을 무조건 늘리기 전에 listener 생성 실패를 찾는다.

### Address already in use

- 현재 run의 계산된 port partition을 확인한다.
- 이전 비정상 run의 소유 process가 남아 있는지 확인한다.
- unrelated process가 사용하는 port를 빼앗거나 종료하지 않는다.
- 안전한 별도 port range로 새 run ID를 사용한다.

### Connected지만 Selected가 부족함

- Host와 endpoint mode가 Active/Passive로 맞는지 확인한다.
- T6/Select 실패와 child heartbeat를 확인한다.
- worker `ready`를 `Selected` 증거로 오해하지 않는다.
- Host result의 장비별 상태와 protocol log를 확인한다.

### Reconnect가 일부만 회복됨

- worker restart가 target 장비 단위가 아니라 worker 단위임을 확인한다.
- worker가 `Listening`, connected 0으로 복원되었는지 본다.
- Host reconnect peak가 configured limit 안인지 확인한다.
- old context가 정리되고 새 `ConnectionId`가 생성되었는지 확인한다.

### Cleanup 실패

- Host와 각 worker를 분리해서 session/listener/operation/pending/queue/socket을 확인한다.
- kill fallback 여부를 확인한다.
- post-GC 값만으로 pass 처리하지 않는다.
- 같은 소규모 조건으로 재현한 뒤 profiler를 사용한다.

### Diagnostic drop

- protocol request/response counter로 business traffic 건전성을 별도 확인한다.
- diagnostic queue peak와 configured capacity를 기록한다.
- 누락된 log를 완전한 protocol evidence로 사용하지 않는다.
- 원인을 찾지 않고 capacity만 크게 늘리지 않는다.

## External Simulator / Real Equipment

이 실행기는 외부 GUI, Simulator, 실제 장비를 자동 실행·조작하지 않는다. 시험하려면 사용자가 별도 승인된 절차로 peer를 설정하고 연결·message·disconnect를 수행해야 한다. 실제 실행 전과 evidence 미제공 상태는 항상 다음과 같이 기록한다.

```text
External Simulator: Not Run / Waiting for User
Real Equipment: Not Run / Waiting for User
```

6시간과 24시간 duration tier도 `Not Run`이며 두 개의 1시간 성공 결과로 승격하지 않는다.

외부 자료는 구현·시험 아이디어를 위한 참고일 뿐 normative 근거가 아니다. 고객사명, 내부 사양, 원문, 실제 경로, 민감한 payload를 코드·README·test 이름·문서·commit evidence에 넣지 않는다.

## 증거 보관과 상태 갱신

Raw result, periodic snapshot, control file, manifest, stdout/stderr tail은 source tree 밖의 run별 artifact directory에 보관한다. 공유 전에는 민감 정보를 mask한다. 크기와 관계없이 raw JSON, snapshot, worker/control/manifest evidence, process log는 commit하지 않는다. 문서에는 일반화한 검토 요약과 통제된 evidence identity만 남기며 machine-specific path를 노출하지 않는다.

`Passed`로 기록하기 전에 [FACTORY_SCALE_TEST_MATRIX.md](FACTORY_SCALE_TEST_MATRIX.md)의 해당 행과 acceptance gate를 확인한다. 1시간 이상 실행은 [FACTORY_SCALE_SOAK.md](FACTORY_SCALE_SOAK.md), 지표 해석은 [FACTORY_SCALE_PERFORMANCE.md](FACTORY_SCALE_PERFORMANCE.md), process·격리 경계는 [FACTORY_SCALE_ARCHITECTURE_KO.md](FACTORY_SCALE_ARCHITECTURE_KO.md)를 따른다.
