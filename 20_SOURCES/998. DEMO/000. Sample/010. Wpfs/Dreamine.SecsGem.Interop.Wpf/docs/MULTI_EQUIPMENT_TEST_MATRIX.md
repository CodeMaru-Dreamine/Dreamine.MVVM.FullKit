# Multi-Equipment Test Matrix

Evidence snapshot: local headless run started `2026-08-10T10:38:37.7927176+00:00` and completed `2026-08-10T10:38:51.3549447+00:00`. Result: `Passed`.

`Passed` below means an assertion passed against local synthetic TCP/HSMS loopback peers. It does not mean an external simulator, customer system, production network, or real equipment passed.

| ID | Area | Assertion | Result | Evidence in the run |
|---|---|---|---|---|
| ME-01 | Scale | Host 1 ↔ Equipment 1, 2, 10, and 50 all Connect, Select, exchange S1F1/F2 and S1F13/F14, Linktest, and Disconnect. | Passed | Four scale checks; maximum 50 equipment |
| ME-02 | GEM ownership | Sending S1F13 to only one of two contexts changes only that context to communicating; all contexts have distinct `GemRuntime` instances. | Passed | `Per-equipment GEM state divergence: isolated` |
| ME-03 | Registration order | Registering definitions in reverse order preserves that registry order; Connect All still leaves every context selected and operational. | Passed | Reverse-order check |
| ME-04 | Message volume | Ten equipment complete 100 S1F1/F2 broadcast rounds with every reply correlated. | Passed | 1,000/1,000 burst replies |
| ME-05 | Slow peer | A deliberately held response on one equipment does not prevent a later request to another equipment from completing first. | Passed | Deterministic response gate check |
| ME-06 | Collision isolation | Two connections both use `SessionId=0` and forced `SystemBytes=1`; replies remain bound to their own equipment and connection. | Passed | Connection-scoped correlation check |
| ME-07 | Reconnect isolation | One of ten equipment reconnects 100 times with a fresh `ConnectionId`; nine peer connections stay selected and unchanged. Three equipment also reconnect concurrently. | Passed | 100 single-target + 3 concurrent reconnects |
| ME-08 | Pending transaction | Disconnecting one context fails its pending request locally while a peer request completes; reconnect creates fresh connection and GEM state. | Passed | Selective-disconnect check |
| ME-09 | HSMS state | Separating one context produces Selected/NotSelected divergence without changing its selected peer. | Passed | State-isolation check |
| ME-10 | Automatic reconnect | Remote drop is observed as ConnectedNotSelected, then selection is restored with a fresh `ConnectionId` and `GemRuntime`; peer identity/state remains unchanged. | Passed | AutoReconnect recovery check |
| ME-11 | T3 | One non-responsive equipment records a T3 error while the responsive peer remains selected and passes Linktest. | Passed | Injected T3 timeout |
| ME-12 | Connect failure | An unavailable endpoint fails its own Connect result while the available equipment selects and passes Linktest. | Passed | Per-equipment aggregate result |
| ME-13 | T6 | A peer that withholds the Select response fails with T6 while healthy equipment remains selected and operational. | Passed | Injected T6 timeout |
| ME-14 | T8 | A partial frame expires with T8 while healthy equipment remains selected and operational. | Passed | Injected T8 timeout |
| ME-15 | Malformed frame | A malformed frame records a protocol error on its own context without stopping healthy equipment. | Passed | Raw fault peer check |
| ME-16 | Abrupt close | Closing one connection after Select does not affect healthy equipment. | Passed | Raw fault peer check |
| ME-17 | Callback failure | An injected receive callback exception is recorded on the affected context while its peer remains usable. | Passed | Callback observation and LastError check |
| ME-18 | Large inbound message | A 1 MiB Equipment→Host binary item is observed and a peer Linktest still completes. | Passed | Length assertion + peer Linktest |
| ME-19 | Cleanup | Harness and loopback live-session deltas and tracked background-operation deltas return to zero after disposal. | Passed | `RemainingSessions=0`, `RemainingBackgroundOperations=0` |

## Run totals

| Measure | Value | Interpretation |
|---|---:|---|
| Connections attempted | 98 | Includes scale, isolation, fault, and reconnect setup attempts counted by the scenario. |
| Messages requested | 1,241 | Includes expected negative-path requests. |
| Messages passed | 1,239 | Two non-success results are deliberately induced: a disconnected pending request and a T3 request. The overall run passes only when their isolation assertions pass. |
| Timeout count | 3 | Deliberately injected T3, T6, and T8 paths. |
| Reconnect count | 105 | 100 one-of-ten cycles, 3 concurrent reconnects, 1 selective reconnect, and 1 automatic reconnect. |

## Evidence boundaries

| Peer / evidence source | Current status | Rule |
|---|---|---|
| Local synthetic loopback | Passed | May be reported only as local automated/self-loopback evidence. |
| External SEComSimulator | **Not Run / Waiting for User** | Requires a person to execute and record the external procedure. No local result may promote this status. |
| Real equipment / production network | Not Run | No claim is made by this harness run. |

The matrix checks behavior implemented by the harness and underlying libraries. It does not certify complete SEMI conformance or cover every timing/network/platform combination.
