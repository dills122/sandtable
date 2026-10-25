using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Frozen dependency revisions; registration is code-owned, never supplied by a proposal.</summary>
internal static class CombatEvidenceRevisions
{
    // Refreshed entries are frozen only after provenance regeneration and semantic comparison.
    private static readonly Dictionary<string, (string Legacy, string Refreshed)> Changes = new(StringComparer.Ordinal)
    {
        ["docs/specs/combat-actual-assault-v1.schema.json"] = ("a16d852d5a4463a295e315c824e5298f4ff22b979a7c307187c2a8d926136f0d", "a6d24f1ff562b34bb8013d187c3da38783725f062efe9ccc9bf7195493bf0cc2"),
        ["docs/specs/combat-actual-moved-assault-v1.schema.json"] = ("21097270c395bd1a3937d6787db2a992b8c145e42e06aec080652800b9e19e4b", "fc28dcd402de585123c9f5212678ba3e436d7416a3e12483d2862d1d95b3c7f2"),
        ["docs/specs/combat-actual-moved-continuation-v1.schema.json"] = ("0a2a0d8089398246b38c84d043ea343a0e3d72d84378b90f0d6da155c2fe3986", "58e11bd1e781b93a2914f04ceeb73f0cb83391991e601af530ba4be2f1920ec1"),
        ["docs/specs/combat-actual-moved-cycle-control-v1.schema.json"] = ("91f235388faa66ef397a9f466f503075da8443148489aef1e7ac27f2dd1751e8", "ad45b4c515c918f62d0463d1e15602335c651e607b3450ae89dc04980bd55b99"),
        ["docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json"] = ("26909e67501f59a0d0892181b9c9499099552c721585369983c3dbde5784c955", "84decfebf61df7acfd3a616a799ba3d522f8e740b4be40e53707a354ab572b8a"),
        ["docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json"] = ("3a80a1cd94b41e8a88590d8eadddcc4c821bfefa7b0021489ad4e7cfe50af8ef", "1ba0e2eb56b47928495d1edd0903500125d613a272fb96b01f436efdee8a0f7a"),
        ["docs/specs/combat-actual-moved-repeat-movement-v1.schema.json"] = ("eb1cf39788cc78376b6d798a13a4a3272906217c51ff02b90310843654122468", "5f09b224d65f89dffcdb4e2c4851061ffae211b459c892f75ccb198d23fd9418"),
        ["docs/specs/combat-actual-paid-assault-v1.schema.json"] = ("be1c5fcc36641ae1ee9f58b4b1c73baca098e7a8cde43d8dc568197f59399324", "af7bca1870a3243421129ba1986d24b82d5863bbccbcb83b9e9e832cab728ba4"),
        ["docs/specs/combat-actual-post-assault-continuation-v1.schema.json"] = ("e948daf499acbb365d9be52b083a10d1692ccbb89fd4bf94e0b4a13601f41c36", "951cf5bf35a16a3954ad5db4734d2d47b93f45e9a31a9067c0f5ada1f76a0783"),
        ["docs/specs/combat-actual-repeated-breakdown-completion-v1.schema.json"] = ("df4ad5e51e683ebddd0d185cb3042042132d8312447c08faf1f237ecb2274680", "fdd3fc791568cae58af116617fc145504d43a007e507589423f9671aa3116ec7"),
        ["docs/specs/combat-actual-repeated-movement-completion-v1.schema.json"] = ("77ef1573e24e131fdddbe4daf733a4472a6dd79b15c6eb227be545ae7c687a2c", "97a8eabda9b7482c315fdecdddf5033b735c3bbdabd176d37ee3f841a7d78946"),
        ["docs/specs/combat-actual-repeated-movement-v1.schema.json"] = ("a3ebd21e0fd1b1d85b7335ceb2afd304511a2efd5e84bf59710a786526dfd900", "0a9ca20e1dd08b10225e0549789bbd9588ca80d911db0f358f2783ff27b0165d"),
        ["docs/specs/combat-actual-round-entry-v1.schema.json"] = ("677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8", "31e5b53db04cf9b96e9285cfc4e3ca51b152a5995d71b76048c3e239aadf3b6d"),
        ["docs/specs/fixtures/combat-actual-assault-v1.json"] = ("9a486f77d0463c071c55af9196511ec2f1214763ed2278ba89962b30746aba09", "e851e76fcf7cf1f584b82d83674e13a86ef6391a86443f5ec9a41ed72a418f01"),
        ["docs/specs/fixtures/combat-actual-moved-repeat-movement-completion-v1.json"] = ("7c887ebbfe8889279fcf5d9269cef2538af6fb38cacf0279a420f2e3496f2258", "c687f59ad2f06f97ff05d73ba4bdfb5c4e3e2ea7634408ed040fce9d4740b6fb"),
        ["docs/specs/fixtures/combat-actual-moved-repeat-movement-v1.json"] = ("481f93d483d15dd7425f2d025c8d823cfe680ebc36acd3b408ae32bb64e20b8d", "b6216cfdbed869e029a8bc1c71eb92d73fca98fcd87534a95eebd16aac432965"),
        ["docs/specs/fixtures/combat-cycle-sequence-v1.json"] = ("de89ebcd86f171119d206d0a90553cdeb815e7b8b6fc8903c9dfcacbd1db5b3f", "805dc0453bf678957ea92ce0c609a7bf67af1c1b8898bc5f99df2b0777810005"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedAssault.cs"] = ("d2ce7b17a832e8fa8691d5c39935cecd3056be61f86769b98648eaeefc061e35", "40b6cbb519968ce55a00cda2a7ac0cc09f5a52e215fbaefab270dd21c9fa4fc8"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedAssaultCodec.cs"] = ("d73751bf1caadb0a643954ad8127b0bddc29d3323d59b7d45507b0ae5150f8c4", "87f767cf2b699762894ffe97392cb398fd135510b09f327051b7096666e1bb79"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuation.cs"] = ("26363b3d4949bfc502a0c0e8c6093fd1599c4b2e8827b3af7829b3f2012b53a7", "dd83f11fcbf431e3deaf52fe7f62ac6bad08ad487b592af335560145f921e1f6"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuationCodec.cs"] = ("067660bfab0eef4790e8e4fc94b0fdcaf2f7d13424dcd403612745cb37d74d9d", "edea1f1b9810c707d4a12663d41309a78c15d383239378c9f348796625d0b7cf"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualPaidAssault.cs"] = ("1ef9b054c461d4a2e35514d699bbcfaff86da12278015869454f9424e3590004", "9810348b70692150c560ab8841cebd68519c3a0c41ece9901dba7b082d4c4d4d"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualPaidAssaultSupport.cs"] = ("b73a62ed471118bf3470145621bb72190918ed56704d9c460b3ef5c9b927457d", "90c9ac00e1451fd84ba653cced39294da4bbb6e2f5c57486ba0de196ec3a040d"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualPostAssaultContinuation.cs"] = ("25a01c667976ba05b1c2242910c08eccb83615c2fe39be4f93b44702cd9a69da", "bc19952f947853da149e63d49fc5811fe6de4e160ee3c8d068937e8c89bfe8ec"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualPostAssaultContinuationCodec.cs"] = ("790775b6df49aa11451778bd52a9fd41a61af40009018ee87e27f4e22bdd73ba", "0936a88dfd0a50157d99bd81e294e8c33704647d15ebbcd480193aec960fc339"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs"] = ("00f30d4abd24598ebea9e12319aef6ba7c7285e64565ed9eff76ac37f4d04fcf", "b0fa2d27a380e6a743c21a9fd291ecd5151e88622594683e588a8364f2e15073"),
        ["src/Cna.Core/Campaigns/CampaignCombatActualRoundEntryCodec.cs"] = ("e224bb57183b1b0febf9b90a297744e9ce99cf3b4124ce5d320eb3fb60e48065", "3dfea67974770a571d08e848ba594006b14c58c8d21b98b1841034d73885659b"),
    };

    internal static Func<string, byte[]> Capture(Func<string, byte[]> read,
        IReadOnlyDictionary<string, string> pins, IReadOnlyDictionary<string, string> delivery,
        long maximumBytes, string error, bool revalidateExisting = false)
    {
        if (read is null) throw new JsonException(error);
        if (revalidateExisting && read.Target is CombatDependencySnapshot existing)
        {
            try { existing.Revalidate(); }
            catch (JsonException exception) { throw new JsonException(error, exception); }
        }
        var legacy = pins.Concat(delivery).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        var refreshed = legacy.ToDictionary(p => p.Key, p => Current(p.Key, p.Value), StringComparer.Ordinal);
        var revisions = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["legacy"] = legacy,
            ["refreshed"] = refreshed
        };
        return CombatDependencySnapshot.Capture(read, revisions, maximumBytes, error).Read;
    }

    internal static JsonObject Manifest(IReadOnlyDictionary<string, string> pins, Func<string, byte[]> dependencies)
    {
        if (dependencies.Target is not CombatDependencySnapshot snapshot)
            throw new JsonException("Authenticated dependency snapshot required.");
        var value = new JsonObject();
        foreach (var path in pins.Keys.Order(StringComparer.Ordinal)) value[path] = snapshot.HashFor(path);
        return value;
    }

    internal static string Expected(Func<string, byte[]> dependencies, string path, string legacy)
        => dependencies.Target is CombatDependencySnapshot snapshot ? snapshot.HashFor(path) : legacy;

    private static string Current(string path, string legacy)
    {
        if (!Changes.TryGetValue(path, out var change)) return legacy;
        if (legacy != change.Legacy) throw new InvalidOperationException("Unexpected legacy dependency registration: " + path);
        return change.Refreshed;
    }
}
