# Interoperability Test Matrix

Status date: 2026-08-10. `Passed` means local automated evidence unless the peer column says External Simulator. External results are not inferred.

| ID | Peer / mode | Scenario | Status | Evidence / next action |
|---|---|---|---|---|
| A-01 | Dreamine Host Active ↔ Dreamine Equipment Passive | TCP, Select, 100 Linktests | Passed | Headless self-test |
| A-02 | Dreamine self loopback | 1,000 S1F1/S1F2 correlated replies | Passed | 1,000/1,000; timeout 0 |
| A-03 | Dreamine self loopback | 100 independent connect/select/dispose cycles | Passed | 100/100 |
| A-04 | Dreamine self loopback | 20 concurrent Primary transactions | Passed | Unique System Bytes and correlated replies |
| B-01 | Simulator Host Active ↔ Dreamine Equipment Passive | Connect, Select, Linktest | Not Run | Waiting for User; follow KO/EN procedure |
| B-02 | Same as B-01 | S1F13/F14 and S1F1/F2 | Not Run | Waiting for User |
| B-03 | Same as B-01 | S1F3/F4 and S1F11/F12 | Not Run | Waiting for User |
| B-04 | Same as B-01 | S1F15/F16 and S1F17/F18 | Not Run | Waiting for User |
| B-05 | Same as B-01 | Disconnect/reconnect and repeat traffic | Not Run | Waiting for User |
| C-01 | Dreamine Host Active ↔ Simulator Equipment Passive | Basic HSMS and confirmed S1 messages | Not Run | Verify Equipment/Local mode in the running simulator first |
| D-01 | Dreamine Passive ↔ Simulator Active | Active/passive cross-check | Not Run | Waiting for User |
| D-02 | Dreamine Active ↔ Simulator Passive | Active/passive cross-check | Not Run | Waiting for User |
| F-01 | External fault peer | T3/T6/T7/T8, partial/malformed frame | Not Run | Library regression evidence exists; external result is Not Run |
| G3-01 | External simulator | GEM300 wire scenarios | Not Run | Blocked/unimplemented handlers are not represented as passed |

## Regression evidence

| Test project | Passed |
|---|---:|
| Dreamine.Secs.Abstractions.Tests | 10 |
| Dreamine.Secs.Com.Tests | 107 |
| Dreamine.Gem.Abstractions.Tests | 9 |
| Dreamine.Gem.Tests | 35 |
| Dreamine.Gem300.Abstractions.Tests | 9 |
| Dreamine.Gem300.Tests | 33 |
| **Total** | **203** |

No external simulator scenario has passed yet. Therefore the current result must not be described as production-ready, fully compliant, certified, or validated on real equipment.
