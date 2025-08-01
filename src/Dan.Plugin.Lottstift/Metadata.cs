using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Plugin.Lottstift.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Dan.Plugin.Lottstift;

/// <summary>
/// All plugins must implement IEvidenceSourceMetadata, which describes that datasets returned by this plugin. An example is implemented below.
/// </summary>
public class Metadata : IEvidenceSourceMetadata
{
    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public List<EvidenceCode> GetEvidenceCodes()
    {
        return
        [
            new EvidenceCode
            {
                EvidenceCodeName = PluginConstants.VolunteerOrganisations,
                EvidenceSource = PluginConstants.SourceName,
                IsPublic = true,
                BelongsToServiceContexts = new List<string>
                {
                    PluginConstants.VolunteerServiceContext
                },
                Values =
                [
                    new EvidenceValue
                    {
                        EvidenceValueName = "organizationNumber",
                        ValueType = EvidenceValueType.String
                    },

                    new EvidenceValue
                    {
                        EvidenceValueName = "volunteerEvaluationYear",
                        ValueType = EvidenceValueType.Number
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "isVolunteer",
                        ValueType = EvidenceValueType.Boolean
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "vatCompensatedYear",
                        ValueType = EvidenceValueType.Number
                    },
                    new EvidenceValue
                    {
                        EvidenceValueName = "isVatCompensated",
                        ValueType = EvidenceValueType.Boolean
                    }
                ]
            }
        ];
    }
    /*
     *  ecb.AddEvidenceValue("organisationNumber", result.OrganisationNumber, PluginConstants.SourceName);
       ecb.AddEvidenceValue("volunteerEvaluationYear", result.VolunteerEvaluatedYear, PluginConstants.SourceName);
       ecb.AddEvidenceValue("isVolunteer", result.IsVolunteer, PluginConstants.SourceName);
       ecb.AddEvidenceValue("vatCompensatedYear", result.VatYearCompensated, PluginConstants.SourceName);
       ecb.AddEvidenceValue("isVatCompensated", result.VatCompensated, PluginConstants.SourceName);
     */

    /// <summary>
    /// This function must be defined in all DAN plugins, and is used by core to enumerate the available datasets across all plugins.
    /// Normally this should not be changed.
    /// </summary>
    /// <param name="req"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    [Function(Constants.EvidenceSourceMetadataFunctionName)]
    public async Task<HttpResponseData> GetMetadataAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(GetEvidenceCodes());
        return response;
    }

}
