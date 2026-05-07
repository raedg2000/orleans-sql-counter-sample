using Microsoft.AspNetCore.Mvc;
using OrleansGrains.Interfaces;

namespace OrleansCounterAPI.Controllers;

[ApiController]
[Route("counter")]
public class CounterController : ControllerBase
{
    private readonly IClusterClient _client;

    public CounterController(IClusterClient client)
    {
        _client = client;
    }

    [HttpPost("{counterId}/increment")]
    public async Task<IActionResult> Increment(Guid counterId)
    {
        var grain = _client.GetGrain<ICounterGrain>(counterId);
        int newValue = await grain.Increment();
        return Ok(newValue);
    }

    [HttpGet("{counterId}")]
    public async Task<IActionResult> GetValue(Guid counterId)
    {
        var grain = _client.GetGrain<ICounterGrain>(counterId);
        int value = await grain.GetValue();
        return Ok(value);
    }
}
