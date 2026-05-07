using Orleans.Runtime;
using OrleansGrains.Interfaces;
using OrleansGrains.Model;

namespace OrleansGrains.Grains
{
    public class CounterGrain : Grain, ICounterGrain
    {
        private readonly IPersistentState<Counter> _counterState;

        public CounterGrain([PersistentState("counter", "counterStore")] IPersistentState<Counter> counterState)
        {
            _counterState = counterState;
        }

        public async Task<int> Increment()
        {
            _counterState.State = new Counter
            {
                Id    = _counterState.State.Id,
                Name  = _counterState.State.Name,
                Value = _counterState.State.Value + 1
            };
            await _counterState.WriteStateAsync();
            return _counterState.State.Value;
        }

        public Task<int> GetValue() =>
            Task.FromResult(_counterState.State.Value);
    }
}
