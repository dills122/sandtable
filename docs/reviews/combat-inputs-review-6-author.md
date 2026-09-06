# Author Explanation
## Intent And Success Criteria
Move beyond merged003B into exact Combat input and decision-time configuration contracts, preserving accepted rules and existing runtime identities. Strict byte reader and checked time value comparisons must reject malformed, unsupported or rebound inputs; this does not activate Combat.
## Plan-To-Implementation Traceability
003C crosses Rules/config/creation/snapshot/sealed-event families and needs003D sequence bytes for its full manifest. Existing plan requires splitting independent subsystems before editing. Refined003C1 inputs/config →003D1 sequence/cycle →003C2 fullRules/creation/snapshot →003C3 commands/events →003D2 movement/history reconciliation. Parent003C/D and checkpointB stayopen; no placeholder hash or fulltaskcompletion claim.
## Technical Approach And Flow
Normative schema descriptors plus fixed closed selected RulesInput. Literal rule fixture generated from retained optical coordinate table+accepted amendment; verifier independently expands retained source ranges+amendment. Config carries validated input artifact hash and all7 explicit window budgets/fallbacks. Timing copies config hash/budget, computes checked deadline, preserves high-water. Pure comparison requires already validated collecting window; it does not accept commands or mutate high-water.
## Changed-Component Walkthrough
Spec owns exact semantics/hash/identity/error/handoff. Schema inventory owns ordered typed shapes. Fixture owns3literalcanonicalgoldens,47mutations and7clockcases. Python oracle owns strict validation/sourcecrosscheck/config/timing bounds and executable checks. Combinedplan records boundeddependencyrefinement and preparedstatus.
## Decisions And Rejected Alternatives
Preserve existing RulesetManifest raw-hex fullhash vs prefixed artifact/confighash distinction. Do not reuseRules9 or inventplaceholderRules10. Timing budgets explicitpositiveint32;30000ms is onlysyntheticgolden config. Separate trustedinput validation from futurehistoryauthentication. No newpackage orruntimecodec.
## Invariants And Boundary Conditions
Preserve357definedcells, accepted3amendments, all36Morale coords andcorrelatedflags. Eight roleordereddice+conditionalcapture; cost/resourcesremainWorldtruth. Missing/extra/duplicate/unknown/noncanonical fieldsreject. Deadlineequality expires, clockregression unavailable, no mutation onprobe. All7potentialwindows explicit; empty/system-owned outcomes do notcreatefakewindows. FirstreleasefallbackconvertsremainingI, laterIIretains; cyclecontrolfallbackfinishes.
## Verification Performed And Results
Test-first readerRED NotImplementedError thenGREEN. Cross-design review corrected initialreservefallbacktoken and addedcyclecontrol window: updatedfixture producedRED CMB-INP005, correctedreaderGREEN. Current:3goldens47mutations39rawbyte7clock14budget/UTC checks; missing/duplicate lists, shuffled construction, rehashunsupportedrule, changedconfig samename rejection. Existing source/Content/creation/Worldoracles allpass. No .NET build/tests; no C#changes.
## Risks, Tradeoffs, And Maintenance Costs
Oracle has deliberate fixed-policy constants alongside spec/fixture and imports source normalizer; no new visualaudit. Configvariants admitted only underexactartifact. No fullRuleshash while sequenceunspecified. Metadata/sourceprovenance bind artifact, later inputchange requires explicitversioning and new dependenthashes. Contractfixturedata islarge becauseall360cells retained.
## Deviations, Deferrals, And Known Gaps
Deliveredbounded003C1 ratherthanclaiming whole003C. Lifecycle/opening identity/accepted history binding of Timing isfuture003C3. Shape/highwater arithmetic cannotdetectcoordinatedcontext+Worldtamper; requiredfuturecase retained. FullC#byteparity, authentication, atomicpublish, restart, publicprivacyandhostclocktrust notproved.
## Challenge Points For The Reviewer
Checkdependencyordering and parentstatus; Configwindows/fallbacks vsDES003/CYCLE; acceptedscalarvalues vsWorld; exactrawhashdomain distinction; canonical/errorgrammar vsoracle; testability and whetherfuturegates hidea blockerneededforthisslice. No desiredreadinessverdict supplied.
