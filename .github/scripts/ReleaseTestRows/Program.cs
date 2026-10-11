using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

// Read only the five reviewed data factories and two formatter controls. Never invoke test bodies
// or delegates contained in rows. Names come from this host's compiled Release xUnit formatter.
if (args.Length != 3) throw new ArgumentException("Expected Release bin, discovery log, output JSON");
var bin = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(bin, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var assembly = Load("Cna.Core.Tests");
var common = Load("xunit.v3.common");
var framework = Load("xunit.v3.core");
foreach (var component in new[] { common, framework })
{
    var version = component.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    Require(version.Split('+')[0] == "4.0.1", "Unsupported runner version: " + version);
}
using (var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(bin, "xunit.runner.json"))))
{
    Require(configuration.RootElement.EnumerateObject().All(property => property.Name == "$schema"),
        "Runner configuration/display semantics changed");
}
var format = common.GetType("Xunit.Sdk.ReflectionExtensions")!.GetMethod("GetDisplayNameWithArguments", BindingFlags.Static | BindingFlags.Public)!;
var displayMethod = framework.GetType("Xunit.v3.XunitTestMethod")!.GetMethod("GetDisplayName")!;
Require(displayMethod.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(
    new[] { typeof(string), typeof(string), typeof(object[]), typeof(Type[]) }), "Runner display signature drift");
var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
    .ToDictionary(opcode => unchecked((ushort)opcode.Value));
var il = displayMethod.GetMethodBody()!.GetILAsByteArray()!;
var calls = new List<string>();
for (var cursor = 0; cursor < il.Length;)
{
    ushort code = il[cursor++];
    if (code == 0xfe) code = (ushort)(0xfe00 | il[cursor++]);
    var opcode = opcodes[code];
    if (opcode.OperandType == OperandType.InlineMethod)
    {
        var called = displayMethod.Module.ResolveMethod(BitConverter.ToInt32(il, cursor))!;
        calls.Add(called.DeclaringType!.FullName + "." + called.Name);
    }
    cursor += opcode.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, cursor),
        _ => 4,
    };
}
Require(calls.Contains("Xunit.Sdk.ReflectionExtensions.GetDisplayNameWithArguments"), "Runner display path drift");
var providers = new[]
{
    ("Cna.Core.Tests.Rules.InitiativeRatingTests", "AxisPresenceIsDerivedFromTypedLocationFacts", "AxisClassificationCases"),
    ("Cna.Core.Tests.Campaigns.CampaignTests", "InvalidAuthoritativeSnapshotCannotProduceAnEvent", "InvalidSnapshots"),
    ("Cna.Core.Tests.Rules.ZocStaticFixtureTests", "EveryNamedSourceNegativeFailsIndependently", "NamedSourceNegatives"),
    ("Cna.Core.Tests.Content.ContentPackV5Tests", "PlacementSeedsRejectEveryCompletenessAndBoundsFailure", "InvalidSeeds"),
    ("Cna.Core.Tests.Content.ContentPackV5Tests", "StrictReadbackRejectsEverySuccessorAndInheritedShapeFailure", "InvalidReadbackDocuments"),
};
var discovered = File.ReadLines(args[1]).Where(line => line.StartsWith("  ", StringComparison.Ordinal))
    .Select(line => line[2..]).ToHashSet(StringComparer.Ordinal);
var expectedDeferred = providers.Select(spec => spec.Item1 + "." + spec.Item2).ToHashSet(StringComparer.Ordinal);
foreach (var name in discovered.Where(name => !name.Contains('(')))
{
    var separator = name.LastIndexOf('.');
    var method = assembly.GetType(name[..separator])!.GetMethod(name[(separator + 1)..])!;
    Require(method.GetParameters().Length == 0 || expectedDeferred.Contains(name), "Unknown deferred provider: " + name);
}
Require(expectedDeferred.IsSubsetOf(discovered), "Deferred discovery set changed");
var proof = providers.Select(spec => Rows(spec.Item1, spec.Item2, spec.Item3)).ToArray();
var controls = new[]
{
    Rows("Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests", "NativeActualEntryRetainsExactPreparedAndCancelledTerminalProofs", "Cases"),
    InlineControl("Cna.Core.Tests.Campaigns.BreakdownRecordTests", "InvalidFiniteShapesAndNoncanonicalBytesReject"),
};
Require(controls.All(control => control.names.All(discovered.Contains)), "Formatter differs from actual runner output");
string[] binaries = ["Cna.Core.Tests.dll", "Cna.Core.dll", "xunit.v3.common.dll", "xunit.v3.core.dll"];
var fingerprints = binaries.ToDictionary(name => name, name => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(bin, name)))));
File.WriteAllText(args[2], JsonSerializer.Serialize(new
{
    schema_version = 1,
    runner_version = "4.0.1",
    providers = proof.Select(row => new { row.method, row.provider, row.names }),
    controls = controls.Select(row => new { row.method, row.provider, row.names }),
    display_calls = calls,
    fingerprints,
    helper_fingerprint = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location))),
}));
Console.WriteLine("Validated compiled Release providers and actual-runner formatter controls.");

Assembly Load(string name) => AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name + ".dll"));
static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
(string method, string provider, string[] names) Rows(string className, string methodName, string memberName)
{
    var type = assembly.GetType(className)!;
    var method = type.GetMethod(methodName)!;
    var attributes = method.GetCustomAttributes().ToArray();
    var member = attributes.Single(attribute => attribute.GetType().Name == "MemberDataAttribute");
    Require((string)member.GetType().GetProperty("MemberName")!.GetValue(member)! == memberName,
        "Provider binding drift: " + methodName);
    Require(((object[])member.GetType().GetProperty("Arguments")!.GetValue(member)!).Length == 0,
        "Parameterized provider is unsupported");
    var memberType = member.GetType().GetProperty("MemberType")!.GetValue(member);
    Require(memberType is null || Equals(memberType, type), "External provider is unsupported");
    foreach (var attribute in attributes.Where(attribute => attribute.GetType().Name == "TheoryAttribute"))
    {
        Require(attribute.GetType().GetProperty("DisplayName")!.GetValue(attribute) is null,
            "Custom method display name is unsupported");
    }
    var first = Enumerate();
    Require(first.Length > 0 && first.Distinct(StringComparer.Ordinal).Count() == first.Length,
        "Empty/ambiguous provider rows: " + memberName);
    Require(first.SequenceEqual(Enumerate()), "Unstable provider rows: " + memberName);
    return (className + "." + methodName, memberName, first);

    string[] Enumerate()
    {
        var provider = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? type.GetMethod(memberName, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
        var names = new List<string>();
        foreach (var row in (IEnumerable)provider)
        {
            object?[] data;
            if (row is object?[] raw) data = raw;
            else
            {
                var contract = row.GetType().GetInterfaces().Single(candidate => candidate.Name == "ITheoryDataRow");
                Require(contract.GetProperty("TestDisplayName")!.GetValue(row) is null, "Custom row display name is unsupported");
                data = (object?[])contract.GetMethod("GetData")!.Invoke(row, null)!;
            }
            Require(data.Length == method.GetParameters().Length, "Provider argument count drift");
            names.Add((string)format.Invoke(null, [method, className + "." + methodName, data, null])!);
        }
        return names.ToArray();
    }
}

(string method, string provider, string[] names) InlineControl(string className, string methodName)
{
    var method = assembly.GetType(className)!.GetMethod(methodName)!;
    Require(method.GetCustomAttributes().Where(attribute => attribute.GetType().Name == "TheoryAttribute")
        .All(attribute => attribute.GetType().GetProperty("DisplayName")!.GetValue(attribute) is null),
        "Custom control display name is unsupported");
    var names = method.GetCustomAttributes().Where(attribute => attribute.GetType().Name == "InlineDataAttribute")
        .Select(attribute => (object?[])attribute.GetType().GetProperty("Data")!.GetValue(attribute)!)
        .Select(data => (string)format.Invoke(null, [method, className + "." + methodName, data, null])!).ToArray();
    Require(names.Length > 0 && names.Distinct(StringComparer.Ordinal).Count() == names.Length, "Empty/ambiguous inline control");
    return (className + "." + methodName, "InlineData", names);
}
