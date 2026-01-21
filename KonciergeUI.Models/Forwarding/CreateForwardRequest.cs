namespace KonciergeUI.Models.Forwarding;

public class CreateForwardRequest
{
    public required string ResourceName { get; set; }
    public required string ResourceNamespace { get; set; }
    public required string ResourceKind { get; set; } // Pod / Service / Deployment etc.
    public required int RemotePort { get; set; }
    public int LocalPort { get; set; }
    public Enums.ForwardProtocol Protocol { get; set; } = Enums.ForwardProtocol.Http;

    public List<(string Name, string Namespace, string Key, string Kind)> LinkedEntries { get; set; } = new();
    // Kind: "Secret" or "ConfigMap"
}
