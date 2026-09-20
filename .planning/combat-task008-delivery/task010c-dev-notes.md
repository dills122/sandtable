# Task010C implementation note — not yet dispatched

if new projections cache cumulative receipts once, keep their State property get-only (explicit constructor) so record-with replacement cannot pair a new State with an old cached ledger. Alternatively derive ledger consistently on access. Existing positional projections do not cache a separate dependent ledger; do not introduce that inconsistency in the new arms. This is an implementation invariant within the proposed three-file scope.
