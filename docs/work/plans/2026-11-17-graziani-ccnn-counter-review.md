# 1st CCNN headquarters counter: independent visual check

**Disposition:** The visible counter confirms the HQ identity and markings,
but does **not** settle either initial gun presence or weapon type. No contrary
reading found in the [counter follow-up](2026-11-17-graziani-ccnn-counter-next.md).
Do not infer zero, a 65/17, or a complete formation combat value from this face.

## Source and crop

Visually inspected [counter scan](https://spigames.net/PDFv10/CNA_Counters.pdf)
PDF p. 2, Counter Section Nr. 3, Italian Blackshirt block. SHA-256 matches
`bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`.
Reproduced the local 300-dpi close crop with:

```sh
pdftoppm -f 2 -l 2 -r 300 -x 950 -y 900 -W 650 -H 260 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/gra-ccnn-close
```

Rendered PNG SHA-256 is
`63ffd15212b042123323dfa9f874fbdbdff4a2c8a4edd86a42c744b858eb0312`;
renderer versions may alter bytes. The PDF hash identifies the scanned file,
not verified source rights or an independently inspected physical counter.
This prompted check uses the same scanned edition, not a blind transcription or
an independent publisher clarification.

The first counter in that block reads `1 CCNN`, with `(1CN)` below, an infantry
box, `XX` above and `(5)` to the left. Neighboring counters read `219Lgn` and
`233Lgn` and are distinct brigade-size counters. I see no weapon-system
name, gun-point figure or separate attack/defense number on the visible 1st
CCNN face. The parenthesized `(5)` is the HQ's printed stacking-point notation
under Land §§9.11–.12, not five artillery points; `XX` denotes division size.
The parenthesized `(1CN)` printed on the counter is not the separate OA `N`
entry. A counter can represent attached units, so the face also does not prove
that the division lacks artillery or combat capability.

## Chart comparison and unresolved source facts

Visually rechecked hash-matched [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
PDF pp. 4, 17 and 26; SHA-256
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`.
OA §4.44b names the 1st CCNN division HQ, counter `1 CCNN`, ID `g`, TOE
entry `N`, arrival `D`. The key uses `N` for maximum strength of a non-tank/
non-gun unit and says gun and tank points, including points assigned to other
unit types, are listed by number and weapon system. Characteristics §4.46b
lists `g` CPA 20 and permits assignment of one artillery TOE point. The
capacity and OA `N` do not identify an actual starting weapon or resolve the
key's weapon-specific listing convention. The neighboring legion HQs each
have a listed 65/17; that is evidence for those **other** HQs, not a transfer
to the division HQ.

Land source hash matches
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
I did not independently repeat the previous bounded errata search; other
counter faces, later addenda and editions remain outside this check. Keep
initial gun presence and weapon type unknown until an authoritative source or
explicit reviewed versioned interpretation addresses the conflict. No code,
content or readiness flag changed; no gameplay tests ran.
