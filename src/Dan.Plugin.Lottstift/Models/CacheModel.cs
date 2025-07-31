using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Lottstift.Models
{
    public class CacheModel
    {
        public DateTime LastUpdated { get; set; }
        public List<DanResponse> VolunteerOrgansations { get; set; }
    }
}
