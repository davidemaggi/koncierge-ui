using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{


        public class KonciergeKubeConfigDto : KonciergeBaseK8sDto
        {

            public Guid Id { get; set; }
            public required string Path { get; set; }

            public ICollection<KonciergeForwardContextDto> Contexts { get; set; } = new List<KonciergeForwardContextDto>();

        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Name?? Path;

        public override bool Equals(object obj)
        => Equals(obj as KonciergeKubeConfigDto);

        public bool Equals(KonciergeKubeConfigDto other)
            => other != null && string.Equals(Path, other.Path, StringComparison.Ordinal);


    }


}
