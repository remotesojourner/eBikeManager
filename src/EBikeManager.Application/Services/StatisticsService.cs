using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Repositories.Interfaces;

namespace EBikeManager.Application.Services;

public sealed class StatisticsService
{
    private readonly IBikeRepository _bikes;
    private readonly IRideRepository _rides;

    public StatisticsService(IBikeRepository bikes, IRideRepository rides)
    {
        _bikes = bikes;
        _rides = rides;
    }

    public async Task<StatisticsDto> GetAsync(CancellationToken cancellationToken = default) =>
        StatisticsDto.From((await _bikes.GetAllAsync(cancellationToken)).Count, await _rides.TotalsSinceAsync(null, cancellationToken));
}
