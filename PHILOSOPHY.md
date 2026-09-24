# Why We Are Building This Project

The SECS/GEM and GEM300 ecosystem already includes mature commercial products that have been proven in production for many years. There is real value in a validated solution backed by professional support. Using commercial software is not the problem.

The problem begins when meaningful choice disappears.

In factory automation, even a small machine or a straightforward communication requirement may come with a mandate to use a particular commercial stack. When an equipment supplier proposes an in-house implementation or a different library, the same questions often follow:

> Who will take responsibility when something goes wrong?<br>
> How will the failure be diagnosed and resolved?

These are legitimate questions. However, choosing a closed-source DLL does not automatically make incident response fast, transparent, or reliable.

When a failure occurs inside a component whose source code is unavailable, the engineer on site cannot inspect the execution path or determine the root cause directly. Exporting logs from a secured factory environment may itself require significant coordination. The logs must then be delivered to the vendor, analyzed outside the site, and followed by another DLL release. That release must be deployed and tested again under the original production conditions.

The equipment engineer ends up repeatedly explaining, waiting, relaying information, and redeploying code they did not write and cannot inspect—while still carrying responsibility for the machine as a whole.

Open source does not eliminate failures, nor does it guarantee that every user can solve every problem alone. It provides something more fundamental: the ability to investigate.

With access to the source, engineers can trace execution, reproduce a failure, add a regression test, identify the responsible component, propose a precise fix, or verify a fix supplied by someone else. The essential difference is that the technical ability to inspect and validate the system remains with its users.

We believe limited observability, slow black-box diagnostics, and excessive vendor lock-in are among the conditions that cause capable software engineers to burn out in factory automation. Engineers are asked to take responsibility for systems while being denied the information and control required to diagnose them. Problems that are technically solvable become waiting exercises because the relevant implementation is hidden behind organizational and contractual boundaries.

Dreamine SECS/GEM began with this experience.

Our goal is not to diminish existing commercial products or insist that everyone replace them. Our goal is to provide a transparent, vendor-neutral alternative that equipment builders and developers can choose when it fits their needs.

- The source code should be available for inspection.
- Communication state and message flow should be observable.
- Failures should be reproducible through deterministic tests.
- The implementation should not depend on a single vendor.
- Users should be able to extend the features they need.
- Changes and compatibility boundaries should be visible to everyone.

For these reasons, `Dreamine.Secs`, `Dreamine.Gem`, and `Dreamine.Gem300` are intended to be released under the MIT License.

We want individuals and companies alike to be able to use, study, modify, and improve the software. Rather than restricting access, we intend to build trust through open code, reproducible tests, clear documentation, and publicly verifiable compatibility results.

We would rather build a transparent tool whose problems can be discovered, understood, and fixed together than offer another black box that merely claims to be complete.

That is why this project exists, and why we intend to keep it open.
