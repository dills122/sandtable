# Result2 writer verification

Four-file freeze confirmed; all SHA256 values match result-v2-frozen-sha256.json.
New oracle exit0:10groups/32traces/304cuts/3728mutations/1360raw/384timing/200same-owner comparisons.
Bidirectional version-isolation extension added before hash freeze; full process loaded earlier
function body, then exact final function ran separately via runpy and passed. Reviewer independently
runs complete frozen oracle. No file changed after published hashes.

Direct historical result1 exit0:8cases/68cuts/728mutations/340raw/96timing.
Round2 exit0:12groups/10traces/68cuts/610mutations/340raw/288clock/480retry/30invalid.
World7 exit0:6goldens/57rejects/112receiptcuts/20scenarios/8840arithmetic/8calendar.
Diffcheck exit0.

Observed RED→GREEN: v1 commitment consumption; prior time gating new mandatory opening; old
reader accepted successor; malformed context KeyError; non-string effect tag TypeError; forged
committed timing through read_event; missing mandatory fixture. Exact byte fixture rejects floats,
bools and duplicate keys. Synthetic lineage; same-owner isolation only. Root acceptance pending.
