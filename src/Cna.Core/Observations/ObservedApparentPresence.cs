using Cna.Core.Content;

namespace Cna.Core.Observations;

public sealed record ObservedApparentPresence
{
    internal ObservedApparentPresence(
        string representationId,
        string currentLocationId,
        bool exertsZoc)
    {
        RepresentationId = ContentContractGuards.RequireStableId(
            representationId,
            nameof(representationId));
        CurrentLocationId = ContentContractGuards.RequireStableId(
            currentLocationId,
            nameof(currentLocationId));
        ExertsZoc = exertsZoc;
    }

    public string RepresentationId { get; }

    public string CurrentLocationId { get; }

    public bool ExertsZoc { get; }
}
