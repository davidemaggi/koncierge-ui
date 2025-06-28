using k8s;
using k8s.KubeConfigModels;
using Koncierge.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Koncierge.Core.K8s.Contexts
{
    public class KonciergeContextService : IKonciergeContextService
    {

        public async Task<KonciergeActionDataResultDto<List<string>>> GetAllContexts(string kubeConfigPath)
        {


            try
            {
                if (!File.Exists(kubeConfigPath))
                {


                    return KonciergeActionDataResultDto<List<string>>.Fail($"KubeConfig '{kubeConfigPath}' Not found", new List<string>());

                }

                // Load kubeconfig file
                var config = await KubernetesClientConfiguration.LoadKubeConfigAsync(kubeConfigPath);

                // Extract context names

                var contexts = config.Contexts?.Select(c => c.Name).ToList() ?? new List<string>();

                return contexts.Count > 0 ?
                     KonciergeActionDataResultDto<List<string>>.Success(contexts)
                     : KonciergeActionDataResultDto<List<string>>.Fail($"No Context found", contexts);
            }
            catch (Exception ex)
            {

                return KonciergeActionDataResultDto<List<string>>.Fail($"Error Retrieving Contexts", new List<string>());

            }
        }

        public async Task<KonciergeActionResultDto> SetCurrentContext(string kubeConfigPath, string contextName)
        {
            try
            {
                if (!File.Exists(kubeConfigPath))
                    return KonciergeActionResultDto.Fail($"KubeConfig '{kubeConfigPath}' Not found");

                // Load kubeconfig
                var config = KubernetesClientConfiguration.LoadKubeConfig(kubeConfigPath);

                // Validate context exists
                if (config.Contexts.All(c => c.Name != contextName))
                    return KonciergeActionResultDto.Fail($"Context '{contextName}' not found in kubeconfig");


                // Set as current context
                config.CurrentContext = contextName;

                // Serialize and save back to file
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                await File.WriteAllTextAsync(kubeConfigPath, serializer.Serialize(config));

                return KonciergeActionResultDto.Success();
            }
            catch (Exception ex)
            {
                return KonciergeActionDataResultDto<List<string>>.Fail($"Error switching to {contextName} Contexts", new List<string>());

            }
        }


        public async Task<KonciergeActionDataResultDto<string>> GetCurrentContext(string kubeConfigPath)
        {
            try
            {
                if (!File.Exists(kubeConfigPath))
                    return KonciergeActionDataResultDto<string>.Fail($"KubeConfig '{kubeConfigPath}' Not found", "");

                // Load kubeconfig
                var config = await KubernetesClientConfiguration.LoadKubeConfigAsync(kubeConfigPath);

                // Return current context
                return KonciergeActionDataResultDto<string>.Success(config.CurrentContext);

            }
            catch (Exception ex)
            {
                return KonciergeActionDataResultDto<string>.Fail($"Error Retrieving Default Context", "");

            }




        }


    }
}

