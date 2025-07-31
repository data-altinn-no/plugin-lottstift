using System;
using Newtonsoft.Json;

namespace Dan.Plugin.Lottstift.Models;

[Serializable]
public class DanResponse
{
    [JsonRequired]
    public string OrganizationNumber { get; set; }

    [JsonRequired]
    public int VatYearCompensated { get; set; }

    [JsonRequired]
    public bool VatCompensated { get; set; }

    public int VolunteerEvaluatedYear { get; set; }

    public bool IsVolunteer { get; set; }
}
