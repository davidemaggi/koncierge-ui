﻿using Koncierge.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Contexts
{
    public interface IKonciergeContextService
    {


        public Task<KonciergeActionDataResultDto<List<KonciergeContextDto>>> GetAllContexts(string kubeConfigPath);
        public Task<KonciergeActionResultDto> SetCurrentContext(string kubeConfigPath, string contextName);
        public Task<KonciergeActionDataResultDto<KonciergeContextDto>> GetCurrentContext(string kubeConfigPath);

    }
}
