using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircularBuffer
{
    interface ICircularBuffer<T> : IEnumerable
    {
        int Capacity { get; }
        int Count { get;  }
        bool IsEmpty { get;  }
        bool IsFull { get;  }

        void Produce(T element);
        T Consume();
        void Clear();
        int ProduceAll(IEnumerable<T> elements);
        void ConsumeAll();
        void WaitProduce(IEnumerable<T> elements);
        T WaitConsume();
    }
}
