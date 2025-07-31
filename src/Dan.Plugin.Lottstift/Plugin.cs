using Altinn.Dan.Plugin.Lottstift.Services.Interfaces;
using Dan.Common;
using Dan.Common.Exceptions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Lottstift.Config;
using Dan.Plugin.Lottstift.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dan.Plugin.Lottstift;

public class Plugin
{
    private readonly IEvidenceSourceMetadata _evidenceSourceMetadata;
    private readonly ILogger _logger;
    private readonly HttpClient _client;
    private readonly Settings _settings;
    private readonly IMemoryCacheProvider _memoryCacheProvider;

    public Plugin(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IOptions<Settings> settings, IEvidenceSourceMetadata evidenceSourceMetadata, IMemoryCacheProvider memCache)
    {
        _client = httpClientFactory.CreateClient(Constants.SafeHttpClient);
        _logger = loggerFactory.CreateLogger<Plugin>();
        _settings = settings.Value;
        _evidenceSourceMetadata = evidenceSourceMetadata;
        _memoryCacheProvider = memCache;
    }

    [Function(PluginConstants.VolunteerOrganisations)]
    public async Task<HttpResponseData> GetSimpleDatasetAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        var evidenceHarvesterRequest = await req.ReadFromJsonAsync<EvidenceHarvesterRequest>();

        return await EvidenceSourceResponse.CreateResponse(req,
            () => GetEvidenceValuesVolunteer(evidenceHarvesterRequest));
    }

    private async Task<List<EvidenceValue>> GetEvidenceValuesVolunteer(EvidenceHarvesterRequest ehr)
    {
        var orgList = await GetFromAndOrUpdateCache();       

        var result = await GetOrg(ehr.SubjectParty.NorwegianOrganizationNumber);

        var ecb = new EvidenceBuilder(_evidenceSourceMetadata, PluginConstants.VolunteerOrganisations);
        ecb.AddEvidenceValue("organizationNumber", result.OrganizationNumber, PluginConstants.SourceName);
        ecb.AddEvidenceValue("volunteerEvaluationYear", result.VolunteerEvaluatedYear, PluginConstants.SourceName);
        ecb.AddEvidenceValue("isVolunteer", result.IsVolunteer, PluginConstants.SourceName);
        ecb.AddEvidenceValue("vatCompensatedYear", result.VatYearCompensated, PluginConstants.SourceName);
        ecb.AddEvidenceValue("isVatCompensated", result.VatCompensated, PluginConstants.SourceName);
        return ecb.GetEvidenceValues();     
    }

    private async Task<CacheModel> RefreshCache()
    {
        _logger.LogInformation("Refreshing Lottstift endpoints cache.");
        var data = await MakeRequest<List<LottstiftResponse>>(_settings.EndpointUrl);
        var result = MapFromLottStiftToCacheModel(data);
        await _memoryCacheProvider.SetCache(PluginConstants.LottstiftEndpointsCacheKey, result, TimeSpan.FromDays(_settings.CacheTimeToLiveDays));
        _logger.LogInformation("Lottstift endpoints cache refreshed {time} with {count} organisations.", DateTime.UtcNow, result.VolunteerOrgansations.Count);
        return result;
    }

    private async Task<CacheModel> GetFromAndOrUpdateCache()
    {        
        var (success, organisations) = await _memoryCacheProvider.TryGet(PluginConstants.LottstiftEndpointsCacheKey);        

        if (!success)
        {
            return await RefreshCache();
        }
        //when cache is getting stale, check if there are updates at source
        else if (organisations.LastUpdated < DateTime.UtcNow.AddDays(-2))
        {
            var lastUpdate = await CheckLastUpdateAtLottStift();
            if (lastUpdate.finishedAt > organisations.LastUpdated)
            {
                //cache is old, update it
                _logger.LogInformation("Lottstift endpoints cache is old, updating cache.");
                return await RefreshCache();
            } else
            {
                //cache is up to date, return it
                _logger.LogInformation("Lottstift endpoints cache is up to date, last updated {date} and last external refresh {externaldate}.", organisations.LastUpdated, lastUpdate.finishedAt);
                return organisations;
            }
        }
        else
        {
            //return from cache
            _logger.LogInformation("Lottstift endpoints cache is up to date with found with {count} entries, last updated {date}.", organisations.VolunteerOrgansations.Count, organisations.LastUpdated);
            return organisations;
        }
    }

    private CacheModel MapFromLottStiftToCacheModel(List<LottstiftResponse> data)
    {
        var result = new CacheModel() { LastUpdated = DateTime.UtcNow, VolunteerOrgansations = new List<DanResponse>() };

        foreach(var item in data)
        result.VolunteerOrgansations.Add(new DanResponse()
        {
            OrganizationNumber = item.organizationNumber,
            VatYearCompensated = item.vatCompensation.yearCompensated,
            VatCompensated = item.vatCompensation.compensated,
            VolunteerEvaluatedYear = item.volunteerEvaluation.evaluationYear,
            IsVolunteer = item.volunteerEvaluation.volunteer
        });

        return result;
    }

    private async Task<DanResponse> GetOrg(string orgNo)
    {
        var (success, cache) = await _memoryCacheProvider.TryGet(PluginConstants.LottstiftEndpointsCacheKey);

        var org = cache.VolunteerOrgansations.SingleOrDefault(x => x.OrganizationNumber == orgNo);

        if (org != null)
        {
            return org;
        }
        else
        {
            throw new EvidenceSourcePermanentClientException(PluginConstants.ErrorNotFound, "Organization was not found in registry");
        }
    }

    private async Task<LastUpdated> CheckLastUpdateAtLottStift()
    {      
        var result = await MakeRequest<LastUpdated>(_settings.LastUpdatedUrl);
        return result;
    }

    private async Task<T> MakeRequest<T>(string target)
    {
        HttpResponseMessage result;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, target);
            result = await _client.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new EvidenceSourceTransientException(PluginConstants.ErrorUpstreamUnavailble, "Error communicating with upstream source", ex);
        }

        if (!result.IsSuccessStatusCode)
        {
            throw result.StatusCode switch
            {
                HttpStatusCode.NotFound => new EvidenceSourcePermanentClientException(PluginConstants.ErrorNotFound, "Upstream source could not find the requested entity (404)"),
                HttpStatusCode.BadRequest => new EvidenceSourcePermanentClientException(PluginConstants.ErrorInvalidInput,  "Upstream source indicated an invalid request (400)"),
                _ => new EvidenceSourceTransientException(PluginConstants.ErrorUpstreamUnavailble, $"Upstream source retuned an HTTP error code ({(int)result.StatusCode})")
            };
        }

        try
        {
            var body = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(body);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to parse data returned from upstream source: {exceptionType}: {exceptionMessage}", ex.GetType().Name, ex.Message);
            throw new EvidenceSourcePermanentServerException(PluginConstants.ErrorUnableToParseResponse, "Could not parse the data model returned from upstream source", ex);
        }
    }
}
