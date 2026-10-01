using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Services;
using EBikeManager.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBikeManager.Web.Controllers;

[ApiController]
[Route("api/statistics")]
public sealed class StatisticsController : ControllerBase
{
    private readonly StatisticsService _statistics;

    public StatisticsController(StatisticsService statistics)
    {
        _statistics = statistics;
    }

    [HttpGet]
    [Authorize(Policy = AccessPolicies.Statistics)]
    public Task<StatisticsDto> GetAsync(CancellationToken cancellationToken) => _statistics.GetAsync(cancellationToken);
}
