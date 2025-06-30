using AutoMapper;
using k8s.Models;
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
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Metadata.Name));

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
