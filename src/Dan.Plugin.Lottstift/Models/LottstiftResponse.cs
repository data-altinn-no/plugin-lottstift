using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Lottstift.Models
{
    public class LottstiftResponse
    {
        public string organizationNumber { get; set; }
        public VolunteerEvaluation volunteerEvaluation { get; set; }
        public VatCompensation vatCompensation { get; set; }
    }

    public class VatCompensation
    {
        public int yearCompensated { get; set; }
        public bool compensated { get; set; }
    }

    public class VolunteerEvaluation
    {
        public bool volunteer { get; set; }
        public int evaluationYear { get; set; }
    }


}
