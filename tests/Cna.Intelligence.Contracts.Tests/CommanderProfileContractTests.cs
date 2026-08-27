using Cna.Intelligence.Contracts.V1;
using Google.Protobuf;

namespace Cna.Intelligence.Contracts.Tests;

public sealed class CommanderProfileContractTests
{
    [Fact]
    public void CommanderProfileCarriesIdentityAndTraitCoordinates()
    {
        var profile = new CommanderProfile
        {
            CommanderId = "commander-1",
            PersonaVersion = "persona-v1",
            Traits = { "aggressive", "cautious" },
        };

        var roundTripped = CommanderProfile.Parser.ParseFrom(profile.ToByteArray());

        Assert.Equal(1, CommanderProfile.Descriptor.FindFieldByName("commander_id").FieldNumber);
        Assert.Equal(
            2, CommanderProfile.Descriptor.FindFieldByName("persona_version").FieldNumber);
        Assert.Equal(3, CommanderProfile.Descriptor.FindFieldByName("traits").FieldNumber);

        Assert.Equal("commander-1", roundTripped.CommanderId);
        Assert.Equal("persona-v1", roundTripped.PersonaVersion);
        Assert.Equal(["aggressive", "cautious"], roundTripped.Traits);
    }
}
