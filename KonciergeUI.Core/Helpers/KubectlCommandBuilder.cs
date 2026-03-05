using KonciergeUI.Models.Forwarding;

namespace KonciergeUI.Core.Helpers;

public static class KubectlCommandBuilder
{
    public static List<string> BuildKubectlCommands(ForwardTemplate template, Enums.KubectlOs os)
    {
        if (template?.Forwards == null || template.Forwards.Count == 0)
        {
            return new List<string>();
        }

        var lines = new List<string>();
        var index = 1;
        if (os == Enums.KubectlOs.Windows)
        {
            foreach (var forward in template.Forwards)
            {
                var resource = BuildResourceRef(forward);
                var portSpec = BuildPortSpec(forward);
                var nsArg = BuildNamespaceArg(forward);
                lines.Add($"Start-Job -Name \"{template.Name}_{index}\" -ScriptBlock {{ kubectl port-forward {resource} {portSpec}{nsArg} }}");
                index++;

            }

            lines.Add(string.Empty);
            lines.Add("# View running jobs:");
            lines.Add("# Get-Job");
            lines.Add(string.Empty);
            lines.Add("# Stop all jobs:");
            lines.Add("# Get-Job | Stop-Job");
            return lines;
        }

        foreach (var forward in template.Forwards)
        {
            var resource = BuildResourceRef(forward);
            var portSpec = BuildPortSpec(forward);
            var nsArg = BuildNamespaceArg(forward);
            lines.Add($"kubectl port-forward {resource} {portSpec}{nsArg} & # Forward {template.Name}_{index}");
            index++;
            
        }

        lines.Add(string.Empty);
        lines.Add("# Wait for all background jobs");
        lines.Add("wait");
        return lines;
    }

    private static string BuildResourceRef(PortForwardDefinition forward)
    {
        var resourceKind = forward.ResourceType == Enums.ResourceType.Pod ? "pod" : "service";
        return $"{resourceKind}/{forward.ResourceName}";
    }

    private static string BuildPortSpec(PortForwardDefinition forward)
    {
        return forward.LocalPort == 0
            ? $":{forward.TargetPort}"
            : $"{forward.LocalPort}:{forward.TargetPort}";
    }

    private static string BuildNamespaceArg(PortForwardDefinition forward)
    {
        return string.IsNullOrWhiteSpace(forward.Namespace) ? string.Empty : $" -n {forward.Namespace}";
    }
}
