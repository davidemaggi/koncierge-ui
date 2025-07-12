using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeBaseK8sDto: IKonciergeBaseK8sDto
    {

        public string Name { get; set; }


        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;

        public override bool Equals(object obj)
        => Equals(obj as KonciergeBaseK8sDto);

        public bool Equals(KonciergeBaseK8sDto other)
            => other != null && string.Equals(Name, other.Name, StringComparison.Ordinal);

    }
}
