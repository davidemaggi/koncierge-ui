using AutoMapper;
using k8s.Models;
using Koncierge.Core.K8s.Extensions;
using Koncierge.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Koncierge.Core.K8s.Mappers
{
    public class KonciergeK8sProfile : Profile
    {

        public KonciergeK8sProfile()
        {
            // Map from V1Namespace to KonciergeNamespaceDto
            CreateMap<V1Namespace, KonciergeNamespaceDto>()
                //.ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name()));


            CreateMap<V1Pod, KonciergePodDto>()
               .ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name()))
               .ForMember(dest => dest.Ports, opt => opt.MapFrom(src => src.GetExposedPorts()))
               ;

            CreateMap<V1Service, KonciergeServiceDto>()
               .ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name()))
               .ForMember(dest => dest.Ports, opt => opt.MapFrom(src => src.GetExposedPorts()))
               ;

            CreateMap<V1ContainerPort, KonciergePortDto>()
               .ForMember(dest => dest.ContainerPort, opt => opt.MapFrom(src => src.ContainerPort))
               .ForMember(dest => dest.HostPort, opt => opt.MapFrom(src => src.HostPort))
               .ForMember(dest => dest.Protocol, opt => opt.MapFrom(src => src.Protocol))
               ;

            CreateMap<V1ServicePort, KonciergePortDto>()
              .ForMember(dest => dest.ContainerPort, opt => opt.MapFrom(src => src.TargetPort))
              .ForMember(dest => dest.HostPort, opt => opt.MapFrom(src => src.Port))
              .ForMember(dest => dest.Protocol, opt => opt.MapFrom(src => src.Protocol))
              ;

            CreateMap<V1Secret, KonciergeAdditionalConfigDto>()
             .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name()))
             .ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => AdditionalConfigType.Secret))
 .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
        src.Data.ToList()
            .ConvertAll(kvp => new KonciergeAdditionalConfigItemDto
            {
                Name = kvp.Key,
                Value = Encoding.UTF8.GetString(kvp.Value)
            })));

            CreateMap<V1ConfigMap, KonciergeAdditionalConfigDto>()
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name()))
               .ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
               
               .ForMember(dest => dest.Type, opt => opt.MapFrom(src => AdditionalConfigType.ConfigMap))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
        src.Data.ToList()
            .ConvertAll(kvp => new KonciergeAdditionalConfigItemDto
            {
                Name = kvp.Key,
                Value = kvp.Value
            })))
               ;




        }



        public static IMapper GetAsmapper() {

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new KonciergeK8sProfile());
            });

            // Create and use mapper
            var mapper = config.CreateMapper();
            return mapper;



        }


    }
}
