namespace Cna.Core.Rules;

/// <summary>Dormant selected Combat arithmetic; no campaign or RNG activation.</summary>
internal static class Cna1979CombatAdjudication
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.combat-selected-inputs.v1";
    public const string ProfileId = "sandtable.capability.combat-cycle-infantry.v1";
    public const string ContentHash = "sha256:fafb24792c9e3f774c368f85c02d1d068f84c9c78e67bf8324723257d0f13029";

    public static CombatRulesInputDefinition Definition { get; } = CreateDefinition();

    public static int LookupMoraleAdjustment(int coordinate) =>
        Definition.Morale[CoordinateIndex(coordinate)].Adjustment;

    public static int CalculateDifferential(int attackerMoraleCoordinate, int defenderMoraleCoordinate) =>
        LookupMoraleAdjustment(attackerMoraleCoordinate) - LookupMoraleAdjustment(defenderMoraleCoordinate);

    public static int LookupLossPercent(CombatRole role, int differential, int coordinate)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(differential, -2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(differential, 2);
        var index = CoordinateIndex(coordinate);
        return Definition.Losses[(int)role * 180 + (differential + 2) * 36 + index].LossPercent;
    }

    public static CombatSelectedResult Resolve(int attackerMoraleCoordinate, int defenderMoraleCoordinate,
        int attackerAssaultCoordinate, int defenderAssaultCoordinate, bool refuseRetreat = false,
        int? captureDie = null)
    {
        var differential = CalculateDifferential(attackerMoraleCoordinate, defenderMoraleCoordinate);
        var attackerPercent = LookupLossPercent(CombatRole.Attacker, differential, attackerAssaultCoordinate);
        var defenderBasePercent = LookupLossPercent(CombatRole.Defender, differential, defenderAssaultCoordinate);
        var effects = Definition.Effects[differential + 2];
        var attackerSum = attackerAssaultCoordinate / 10 + attackerAssaultCoordinate % 10;
        var defenderSum = defenderAssaultCoordinate / 10 + defenderAssaultCoordinate % 10;
        var retreatHexes = effects.DefenderRetreatOneHexSums.Contains(defenderSum) ? 1 : 0;
        if (refuseRetreat && retreatHexes == 0)
        {
            throw new ArgumentException("Refusal requires a selected Retreat result.", nameof(refuseRetreat));
        }
        CombatRole? capturedRole = effects.AttackerCaptureSums.Contains(attackerSum) ? CombatRole.Attacker
            : effects.DefenderCaptureSums.Contains(defenderSum) ? CombatRole.Defender : null;
        if (capturedRole.HasValue != captureDie.HasValue)
        {
            throw new ArgumentException("A capture die is required exactly when capture triggers.", nameof(captureDie));
        }
        int? share = null;
        if (captureDie is int die)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(die, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(die, 6);
            share = Definition.CaptureShares[die - 1].Percent;
        }
        var defenderPercent = checked(defenderBasePercent + (refuseRetreat
            ? Definition.Settlement.RefusalPercentPerHex * retreatHexes : 0));
        return new CombatSelectedResult(differential, attackerPercent, defenderBasePercent, defenderPercent,
            effects.AttackerEngagedSums.Contains(attackerSum), retreatHexes, refuseRetreat, capturedRole, share,
            Loss(CombatRole.Attacker, attackerPercent, capturedRole == CombatRole.Attacker ? share : null),
            Loss(CombatRole.Defender, defenderPercent, capturedRole == CombatRole.Defender ? share : null));
    }

    private static CombatRoleLoss Loss(CombatRole role, int percent, int? capturePercent)
    {
        var toe = Definition.Costs.CommittedToePerRole;
        var product = checked(toe * percent);
        var loss = role == CombatRole.Attacker ? checked((product + 99) / 100) : product / 100;
        // Combat chart15.89 says literal percent, including33/100, unlike Breakdown's1/3 ruling.
        var captured = capturePercent is int share ? checked((loss * share + 99) / 100) : 0;
        return new CombatRoleLoss(loss, captured, loss - captured, toe - loss,
            loss >= Definition.Settlement.LossDpThresholdToe ? Definition.Settlement.LossDpPoints : 0);
    }

    private static int CoordinateIndex(int coordinate)
    {
        var tens = coordinate / 10;
        var ones = coordinate % 10;
        if (tens is < 1 or > 6 || ones is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate,
                "A sequential coordinate requires two ordered d6 faces.");
        }
        return (tens - 1) * 6 + ones - 1;
    }

    private static CombatRulesInputDefinition CreateDefinition()
    {
        // Source15.79/17.4 inclusive printed bands; enumerate legal ordered d6 coordinates only.
        // Independent tests consume expanded optical values, not these production bands.
        (int Percent, int First, int Last)[][] attacker =
        [
            [(25, 11, 12), (20, 13, 18), (15, 21, 33), (10, 34, 44), (5, 45, 54), (0, 55, 66)],
            [(25, 11, 11), (20, 12, 13), (15, 14, 26), (10, 31, 42), (5, 43, 52), (0, 53, 66)],
            [(25, 11, 11), (20, 12, 12), (15, 13, 24), (10, 25, 36), (5, 41, 51), (0, 52, 66)],
            [(25, 11, 11), (20, 12, 12), (15, 13, 22), (10, 23, 33), (5, 34, 46), (0, 51, 66)],
            [(25, 11, 11), (20, 12, 12), (15, 13, 16), (10, 21, 31), (5, 32, 44), (0, 45, 66)],
        ];
        (int Percent, int First, int Last)[][] defender =
        [
            [(20, 11, 11), (15, 12, 13), (10, 14, 23), (5, 24, 44), (0, 45, 66)],
            [(20, 11, 11), (15, 12, 16), (10, 21, 26), (5, 31, 46), (0, 51, 66)],
            [(20, 11, 12), (15, 13, 23), (10, 24, 32), (5, 33, 51), (0, 52, 66)],
            [(20, 11, 13), (15, 14, 22), (10, 23, 33), (5, 34, 51), (0, 52, 66)],
            [(25, 11, 11), (20, 12, 13), (15, 14, 23), (10, 24, 33), (5, 41, 52), (0, 53, 66)],
        ];
        var amendment = new CombatSourceAmendment("CMB-SRC-RUL-001", CombatRole.Defender, 2, [34, 35, 36], 10);
        var coordinates = Enumerable.Range(1, 6).SelectMany(tens =>
            Enumerable.Range(1, 6).Select(ones => tens * 10 + ones)).ToArray();
        var losses = new List<CombatLossCell>();
        foreach (var role in Enum.GetValues<CombatRole>())
        {
            for (var differential = -2; differential <= 2; differential++)
            {
                var bands = (role == CombatRole.Attacker ? attacker : defender)[differential + 2];
                foreach (var coordinate in coordinates)
                {
                    // Preserve357 source values; accepted Sandtable amendment fills only3 gaps.
                    var percent = role == amendment.Role && differential == amendment.Differential
                        && amendment.Coordinates.Contains(coordinate) ? amendment.LossPercent
                        : bands.Single(band => coordinate >= band.First && coordinate <= band.Last).Percent;
                    losses.Add(new CombatLossCell(role, differential, coordinate, percent));
                }
            }
        }
        return new CombatRulesInputDefinition(SchemaVersion, ArtifactId, ProfileId,
            [
                new("sandtable-rules-lab", "CMB-POL-001-008;CMB-SRC-RUL-001"),
                new("spi-1979-common-charts", "15.79;15.89;17.4"),
                new("spi-1979-compilation", "Logistics50.0-50.17"),
                new("spi-1979-land-rules", "6.21-6.24;11.21-11.27;15.61-15.87;20.21;28.24"),
                new("spi-1979-september-errata", "15.27;20.72;50.2;50.12"),
            ],
            [
                new("spi-1979-common-charts", "sha256:51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0"),
                new("spi-1979-compilation", "sha256:836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5"),
                new("spi-1979-land-rules", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99"),
                new("spi-1979-september-errata", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb"),
            ], amendment,
            coordinates.Select(c => new CombatMoraleCell(c, c == 11 ? 1 : c == 66 ? -1 : 0)), losses,
            [
                new(-2, [10, 11], [9], [2], []),
                new(-1, [10, 11, 12], [8], [], []),
                new(0, [9, 10, 12], [5, 6], [], [2]),
                new(1, [9, 10, 11], [4, 5, 6], [], [2]),
                new(2, [9, 10, 11, 12], [5, 6, 7], [], [2]),
            ],
            [new(1, 10), new(2, 25), new(3, 33), new(4, 50), new(5, 50), new(6, 75)],
            new CombatProcedure("sandtable.combat.role-ordered-d6.v1", "sandtable.sha256-counter.v1", 252, 6,
                ["attacker.morale.tens", "attacker.morale.ones", "defender.morale.tens", "defender.morale.ones",
                 "attacker.assault.tens", "attacker.assault.ones", "defender.assault.tens", "defender.assault.ones"],
                ["attacker.capture.share", "defender.capture.share"]),
            new CombatCosts(10, 1, 0, 0, 10, 5, 3, 1),
            new CombatSettlementRules("ceiling", "floor", "ceiling", 10, 3, 3, 3, 10, 1, 1, 10, 0, 1, 0, 3, 8, 12, 3),
            [
                new("CMB-POL-001", 1, "closed-singleton-infantry"),
                new("CMB-POL-002", 1, "explicit-content-seeds-and-provenance"),
                new("CMB-POL-003", 1, "single-ledger-atomic-costs-role-ordered-rng"),
                new("CMB-POL-004", 1, "persisted-deadline-fallback"),
                new("CMB-POL-005", 1, "atomic-custody-rendezvous-and-delayed-escape"),
                new("CMB-POL-006", 1, "side-disclosure-allowlist"),
                new("CMB-POL-007", 1, "relative-release-and-cumulative-history"),
                new("CMB-POL-008", 1, "supported-cycle-continuation-to-truck-entry"),
            ]);
    }
}
