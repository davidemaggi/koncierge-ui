namespace KonciergeUI.Models.Kube
{
    public class ConfigMapInfo
    {
        public string Name { get; set; }
        public string NameSpace { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
