using System;
using System.Collections.Generic;
using System.Text;

namespace OrleansGrains.Interfaces
{
    public interface ICounterGrain : IGrainWithGuidKey
    {
        Task<int> Increment();
        Task<int> GetValue();
    }
}
