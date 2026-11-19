namespace Cna.OrleansHost.Persistence;

/// <summary>
/// The first event of every campaign stream (Phase C3b of issue #439,
/// <c>docs/work/plans/2026-11-19-phase-c3b-durable-state-design-spike.md</c> &#167;3.1, option G1).
/// One instance carries exactly what SQLite's <c>campaigns</c> row carried before the swap, minus
/// the derived <c>head_sequence</c>/<c>head_hash</c> pair (now the stream's own tail): the
/// <c>CampaignCreationRequest</c> fields plus Core's own canonical creation bytes, the initial
/// snapshot, the initial contact records and the canonical decision-timing configuration.
/// </summary>
/// <remarks>
/// <para><b>Why genesis is an event, not a side document.</b> It is immutable, it is the seed
/// <c>CampaignDurableAuthorityJournal.InitialHash</c> folds into the hash chain before step 1, and
/// <c>DET-001</c>'s replay needs it first. Keeping it inside the stream makes each campaign stream
/// self-contained (the whole replay input is one ordered source -- what design doc &#167;2.1's
/// hot&#8594;cold archival assumes) and gives exactly-once creation for free
/// (<c>StartStream</c> rejects an existing stream key, replacing <c>campaigns.campaign_id PRIMARY
/// KEY</c>). Its payload is Core-produced canonical bytes under one versioned envelope, so it is a
/// versioned event in <c>EVT-001</c>'s sense, not host bookkeeping.</para>
/// <para><b>C1 schema amendment (version offset).</b> Genesis occupies stream version 1 and carries
/// the <c>sequence</c> header <c>0</c> (its <c>record_hash</c> header is the chain's initial hash;
/// <c>prior_hash</c> is the fixed <see cref="PriorHashSentinel"/>). Accepted step <i>n</i> therefore
/// sits at stream version <i>n + 1</i> -- see <see cref="CampaignEventStreamSchema"/>'s remarks. C1
/// already carries <c>sequence</c> as an explicit header precisely so readers assert it rather than
/// trust <c>Version</c>; the journal's loader asserts <c>sequence == Version - 1</c> on every event.</para>
/// </remarks>
public sealed record CampaignGenesisEvent(
    int ContractVersion,
    string RulesetHash,
    string Seed,
    string SetupId,
    string SetupHash,
    string ContentPackId,
    string ContentHash,
    string ScenarioId,
    byte[] CreationEvent,
    byte[] InitialSnapshot,
    byte[] InitialContacts,
    byte[] TimingConfiguration)
{
    /// <summary>Envelope version of this event type.</summary>
    public const string EnvelopeVersion = "campaign-genesis-v1";

    /// <summary>The <c>prior_hash</c> header value of a genesis event (it has no predecessor).</summary>
    public const string PriorHashSentinel = "genesis";
}
