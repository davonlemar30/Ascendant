# Bounded Linux cinematic harness repair

User authorization: test-only repair in parallel with the Observatory art pass, existing Web artifact, every interaction and animation assertion retained, no game changes, at most three attempts or ninety minutes. #167/#168/#170 were already merged at main `8f24cf6f5b7ecd4943d4a31cd41ee40a33948e07`.

The bounded window began 2026-11-06 06:58:11 UTC. The final cutoff was set conservatively to 08:28:00 UTC. No fourth workflow dispatch is authorized in this window.

## Source and scope

Repair source: `9361052922d088d73dc7f6283233e5b412d40362`. Changes are test/harness only. The player tree `Assets/CelestialDial` has hash `513a2d9459fa3a525e114103aa0e3b1d02fbdd25` on main8f, original artifacta7, and this candidate. No game timing or behavior changed. Browser launch/prologue assertion callsite counts remain 11/7; Editor Check occurrences remain334. Assertions, native input checks, animation ordering and reduced-motion checks remain.

Browser captures now pair canvas PNGs with in-page cinematic observations, and native taps execute in live early windows. The reduced-motion check observes the complete desk shot before testing its history. Editor zero-delay waits run the following assertion in the same tick, avoiding a frame advancing between observation and assertion.

## Conservative three-dispatch inventory

1. [42178429156](https://github.com/davonlemar30/Ascendant/actions/runs/42178429156): setup-only dispatch on the old proof source, canceled immediately after an integration conflict. No candidate test executed. Counted conservatively as one dispatch.
2. [42178485878](https://github.com/davonlemar30/Ascendant/actions/runs/42178485878): focused diagnosis, both density jobs successful, **174 desktop +174 phone =348 PASS**. Both390/360, normal/reduced motion and native input assertions. Reused existing `web-player` artifact13479313762 from run42168038641; stamped identity `a7b253f121b7a3c7c096002e120377df51badce8`. No Unity build or deploy in this diagnostic. Phone runtime6m52s; desktop6m40s. The included proof workflow is an evidence copy, not an active release workflow. Proof branch `codex/release-gate-failure-proof` must not be merged. These focused checks do not replace the full suite or clear release.
3. [42179392368](https://github.com/davonlemar30/Ascendant/actions/runs/42179392368): final normal full rehearsal on9361052. **Canceled at the cutoff; no release clearance.** At08:28:07UTC the cancellation request was submitted while Build with Unity was still active; final cancellation completed at08:29:21UTC. No fixture/browser execution occurred, and all downstream jobs are canceled. No artifacts were produced. The final log has no pre-cancellation output after07:17:14UTC; its cause is unestablished. See record.json, canceled-build-excerpt.txt and cutoff/final workflow snapshots.

## Other checks

Exact source9361052's PR-time Build Web player passed in7m23s (run42179397338), including **1028 mechanical PASS**, WebGL47,860,514 bytes, zero errors/warnings, Unity build3m22.393s. Guard passed in4s (run42179710612). These are not full release clearance.

Existing four-file Unity import churn remains unstaged and untouched. No settings, rulesets or secrets changed. No #169 merge or #169 production deploy/check may be claimed without a successful normal rehearsal.

## Production main8f24cf6 (performed independently)

Root completed the production check after successful deploy42176532471, using detached main8f's own unmodified suite,390/360 at scale2/mobile. Deployed Web.data SHA256 `e1dfd0f93459159ab39be671af3dca8aacefacc44773d3dbbd036de4bf74a1ac` exactly matched the8f Pages artifact. The suite exited1 on the reduced-motion390 prologue assertion (next-shot history had already been observed). This is a recorded production FAILURE, not another bounded repair attempt. [Immutable production record](https://github.com/davonlemar30/Ascendant/blob/042ec86cfa32476b7ae4ca7f820e94daf6caea9a/Tools/ValidationEvidence/observatory-art/production-8f24cf6/record.json).

## Written stop question

If further release work is desired after this bounded stop, may a new separately bounded normal Linux rehearsal be authorized after investigation of the incomplete build? No answer is needed to finish this authorized run. No further run, merge or game change is authorized or attempted by this record. #169 stays open; Observatory art proceeds independently in #172.
