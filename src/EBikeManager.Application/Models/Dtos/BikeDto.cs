using EBikeManager.Application.Models.Entities;

namespace EBikeManager.Application.Models.Dtos;

public sealed record BikeDto(string Id, string Name)
{
    public static BikeDto From(Bike bike) => new(bike.Id, bike.Name);
}
