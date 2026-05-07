using System;
using System.Collections.Generic;
using System.Text;

namespace OrleansGrains.Model
{

    [GenerateSerializer, Immutable]
    public class Counter
    {
        [Id(0)]
        public Guid Id { get; init; }

        [Id(1)]
        public string Name { get; init; }

        [Id(2)]
        public int Value { get; init; } = 0;
    }
}
