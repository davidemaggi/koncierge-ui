using AutoMapper;
using k8s.Models;
using Koncierge.Core.K8s.Extensions;
using Koncierge.Domain.DTOs;
using Koncierge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Koncierge.Core.K8s.Mappers
{
    public class KonciergeDbToDtoProfile : Profile
    {

        public KonciergeDbToDtoProfile()
        {
            // Map from V1Namespace to KonciergeNamespaceDto
            CreateMap<KonciergeKubeConfig, KonciergeKubeConfigDto>()
                //.ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Contexts, opt => opt.MapFrom(src => src.Contexts))
                ;

            CreateMap<KonciergeForwardContext, KonciergeForwardContextDto>()
               //.ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
               .ForMember(dest => dest.Namespaces, opt => opt.MapFrom(src => src.Namespaces))
               ;

            CreateMap<KonciergeForwardNamespace, KonciergeForwardNamespaceDto>()
               //.ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
               .ForMember(dest => dest.Forwards, opt => opt.MapFrom(src => src.Forwards))
               ;


            CreateMap<KonciergeForward, KonciergeForwardDto>()
              //.ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
              .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
              .ForMember(dest => dest.HostPort, opt => opt.MapFrom(src => src.HostPort))
              .ForMember(dest => dest.ContainerPort, opt => opt.MapFrom(src => src.ContainerPort))
              .ForMember(dest => dest.LocalPort, opt => opt.MapFrom(src => src.LocalPort))
              .ForMember(dest => dest.TargetName, opt => opt.MapFrom(src => src.TargetName))
              .ForMember(dest => dest.TargetType, opt => opt.MapFrom(src => src.TargetType))
              .ForMember(dest => dest.AdditionalConfigs, opt => opt.MapFrom(src => src.AdditionalConfigs))
              ;


            CreateMap<KonciergeForwardAdditionalConfig, KonciergeForwardAdditionalConfigDto>()
              //.ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
              .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
              .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
              .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
              .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))

              ;

            CreateMap<KonciergeForwardAdditionalConfigItem, KonciergeForwardAdditionalConfigItemDto>()
            //.ForMember(dest => dest.Namespace, opt => opt.MapFrom(src => src.Namespace()))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Name))
         
            ;
        }



        public static IMapper GetAsmapper() {

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new KonciergeDbToDtoProfile());
            });

            // Create and use mapper
            var mapper = config.CreateMapper();
            return mapper;



        }


    }
}
