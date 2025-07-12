using k8s;
using k8s.KubeConfigModels;
using Koncierge.Domain.DTOs;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
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

        public async Task<KonciergeActionDataResultDto<List<KonciergeContextDto>>> GetAllContexts(string kubeConfigPath)
        {


            try
            {
                if (!File.Exists(kubeConfigPath))
                {


                    return KonciergeActionDataResultDto<List<KonciergeContextDto>>.Fail($"KubeConfig '{kubeConfigPath}' Not found", new List<KonciergeContextDto>());

                }

                // Load kubeconfig file
                var config = await KubernetesClientConfiguration.LoadKubeConfigAsync(kubeConfigPath);

                // Extract context names

                var contexts = new List<KonciergeContextDto>();

                foreach (var c in config.Contexts) {

                    contexts.Add(new KonciergeContextDto { Name=c.Name, DefaultNamespace=c.ContextDetails.Namespace });


                }

                return contexts.Count > 0 ?
                     KonciergeActionDataResultDto<List<KonciergeContextDto>>.Success(contexts)
                     : KonciergeActionDataResultDto<List<KonciergeContextDto>>.Fail($"No Context found", contexts);
            }
            catch (Exception ex)
            {

                return KonciergeActionDataResultDto<List<KonciergeContextDto>>.Fail($"Error Retrieving Contexts", new List<KonciergeContextDto>());

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


        public async Task<KonciergeActionDataResultDto<KonciergeContextDto>> GetCurrentContext(string kubeConfigPath)
        {
            try
            {
                if (!File.Exists(kubeConfigPath))
                    return KonciergeActionDataResultDto<KonciergeContextDto>.Fail($"KubeConfig '{kubeConfigPath}' Not found", new KonciergeContextDto());

                // Load kubeconfig
                var config = await KubernetesClientConfiguration.LoadKubeConfigAsync(kubeConfigPath);

                var current=config.Contexts.First(x=>x.Name== config.CurrentContext);

                // Return current context
                return KonciergeActionDataResultDto<KonciergeContextDto>.Success(new KonciergeContextDto { Name = current.Name, DefaultNamespace = current.ContextDetails.Namespace });

            }
            catch (Exception ex)
            {
                return KonciergeActionDataResultDto<KonciergeContextDto>.Fail($"Error Retrieving Default Context", new KonciergeContextDto());

            }




        }


    }
}

