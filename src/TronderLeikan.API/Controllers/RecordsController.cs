using Microsoft.AspNetCore.Mvc;
using TronderLeikan.API.Common;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Statistics.Queries.GetRecords;
using TronderLeikan.Application.Statistics.Responses;

namespace TronderLeikan.API.Controllers;

// Rekorder over alle ferdige spill, på tvers av turneringer
public sealed class RecordsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RecordsResponse>> Get(CancellationToken ct) =>
        (await sender.Query(new GetRecordsQuery(), ct)).Match(Ok, Problem);
}
