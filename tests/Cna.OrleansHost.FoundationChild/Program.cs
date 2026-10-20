using System.Text.Json;
using Cna.OrleansHost.Api;

var input = JsonSerializer.Deserialize<ProbeReadbackInput>(Console.ReadLine() ?? "")
    ?? throw new InvalidOperationException("Missing child readback input.");
var store = P01PrivateHostFoundation.OpenExisting(input.Path);
var credential = new P01PrivateCredential(input.CampaignId, input.Side,
    input.AuthorityId, input.CredentialSecret, input.SeatEpoch, input.CredentialExpiryMs);
var lease = new P01ActivationLease(input.CampaignId, input.Fence,
    input.LeaseSecret, input.OperatorSecret);
var result = store.FindProbe(credential, input.RequestId,
    Convert.FromHexString(input.ExactBytesHex), lease);
Console.WriteLine(result is null ? "missing" : $"{result.Sequence}|{result.RecordHash}");

internal sealed record ProbeReadbackInput(string Path, string CampaignId, string Side,
    string AuthorityId, string CredentialSecret, long SeatEpoch, long CredentialExpiryMs,
    long Fence, string LeaseSecret, string OperatorSecret, string RequestId,
    string ExactBytesHex);
