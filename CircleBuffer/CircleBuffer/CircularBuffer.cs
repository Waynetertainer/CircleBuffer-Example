using System;
using System.Collections;
using System.Collections.Generic;

namespace CircularBuffer
{
    class CircularBuffer<T> : ICircularBuffer<T>
    {
        private T[] buffer;
        private int startPosition;
        private int endPosition;
        public int Capacity { get => buffer.Length; }
        public int Count { get => ((endPosition + Capacity - startPosition) % Capacity) + 1; }
        public bool IsEmpty { get => startPosition == endPosition; }
        public bool IsFull { get => ((startPosition + Capacity - 1) % Capacity) == endPosition; }

        public CircularBuffer(int size)
        {
            buffer = new T[size];
            startPosition = endPosition = 0;
        }

        public void Clear()
        {
            lock (this)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = default(T);
                }
                startPosition = endPosition = 0; 
            }
        }

        public void ConsumeAll()
        {
            while (!IsEmpty)
            {
                Consume();
            }
        }

        public T Consume()
        {
            if (!IsEmpty)
            {
                lock (this)
                {
                    T result = buffer[startPosition];
                    buffer[startPosition] = default(T);
                    startPosition = (startPosition + 1) % Capacity;
                    return result; 
                }
            }
            else
            {
                throw new BufferUnderflowException();
            }
        }

        public void Produce(T element)
        {
            if (!IsFull)
            {
                lock (this)
                {
                    buffer[(endPosition + 1) % Capacity] = element;
                    endPosition = (endPosition + 1) % Capacity; 
                }
            }
            else
            {
                throw new BufferOverflowException();
            }
        }

        public int ProduceAll(IEnumerable<T> elements)
        {
            int index = 0;
            foreach (var element in elements)
            {
                if (IsFull)
                {
                    break;
                }

                Produce(element);
                index++;
            }

            return index;
        }

        public T WaitConsume()
        {
            throw new NotImplementedException();
        }

        public void WaitProduce(IEnumerable<T> elements)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
