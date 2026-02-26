namespace KonciergeUI.Models.Kube
{
    public class SecretInfo
    {
        public string Name { get; set; }
        public string NameSpace { get; set; }
        public string? Type { get; set; }
        public Dictionary<string,string> Data { get; set; }=new Dictionary<string,string>();

    }
}
