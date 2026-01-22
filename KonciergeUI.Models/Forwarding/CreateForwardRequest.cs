using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Models.Forwarding;

public class CreateForwardRequest
{
    public required string ResourceName { get; set; }
    public required string ResourceNamespace { get; set; }
    public required ResourceType ResourceKind { get; set; } // Pod / Service / Deployment etc.
    public required int RemotePort { get; set; }
    public int LocalPort { get; set; }
    public ForwardProtocol Protocol { get; set; } = ForwardProtocol.Http;

    public List<LinkedItem> LinkedEntries { get; set; } = new();
   
}


public class LinkedItem {
    public required string Name { get; set; }
    public required string Namespace { get; set; }
    public required string Key { get; set; }
    public required LinkedResourceType Kind { get; set; }




}