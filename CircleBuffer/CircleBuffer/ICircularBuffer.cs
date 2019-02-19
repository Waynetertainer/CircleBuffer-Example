using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircularBuffer
{
    interface ICircularBuffer
    {
        int Capacity { get; set; }
        int Count { get; set; }
        bool IsEmpty { get; set; }
        bool IsFull { get; set; }

        void Produce(IEnumerable T);
        IEnumerable Consume();
        void Clear();
        int ProduceAll(IEnumerable T);
        void ClearAll();
        void WaitProduce(IEnumerable T);
        IEnumerable WaitConsume();
    }
}
