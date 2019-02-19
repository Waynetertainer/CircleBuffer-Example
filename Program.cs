using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CircularBuffer
{
    /// <summary>
    /// Dummy class to test circular buffer.
    /// </summary>
    public class MyClass
    {
        public string value { get; }

        public MyClass(string value)
        {
            this.value = value;
        }
    }

    /// <summary>
    /// Dummy class to test circular buffer. Inherits from first dummy class to check ICircularBuffer.ProduceAll.
    /// </summary>
    public class MyInheritedClass : MyClass
    {
        public int newValue { get; }

        public MyInheritedClass(string value, int newValue) : base(value)
        {
            this.newValue = newValue;
        }
    }

    class Program
    {
        /// <summary>
        /// Helper method to test the properties Count, IsEmpty and IsFull of a circular buffer.
        /// </summary>
        /// <typeparam name="T">Base type of the circular buffer</typeparam>
        /// <param name="buffer">Circular buffer to test</param>
        /// <param name="count">Expected number of items</param>
        static void TestProperties<T>(ICircularBuffer<T> buffer, uint count)
        {
            if (buffer.Count != count)
                throw new Exception(String.Format("buffer.Count {0} != {1}", buffer.Count, count));
            if (buffer.IsEmpty != (count == 0))
                throw new Exception(String.Format("buffer.IsEmpty {0} != {1}", buffer.IsEmpty, count == 0));
            if (buffer.IsFull != (count >= buffer.Capacity))
                throw new Exception(String.Format("buffer.IsFull {0} != {1}", buffer.IsFull, count >= buffer.Capacity));
        }

        /// <summary>
        /// Helper method to test, if a specific exception is thrown.
        /// </summary>
        /// <typeparam name="E">Type of expected exception</typeparam>
        /// <param name="action">Lambda which shall throw an exception</param>
        static void TestException<E>(Action action) where E : Exception
        {
            bool noException = true;
            try
            {
                action();
            }
            catch (E)
            {
                noException = false;
            }
            if (noException)
                throw new Exception(String.Format("exception {0} expected", typeof(E).Name));
        }

        /// <summary>
        /// Test standard methods of the circular buffer implementation.
        /// </summary>
        static void TestCircularBufferStandard()
        {
            ICircularBuffer<string> buffer = new CircularBuffer<string>(2);
            TestProperties(buffer, 0);

            // test Produce and Consume
            buffer.Produce("one");
            TestProperties(buffer, 1);

            buffer.Produce("two");
            TestProperties(buffer, 2);

            string item = buffer.Consume();
            if (item != "one")
                throw new Exception(String.Format("item {0} != {1}", item, "one"));
            TestProperties(buffer, 1);

            buffer.Produce("three");
            TestProperties(buffer, 2);

            // test exceptions
            TestException<BufferOverflowException>(() => buffer.Produce("four"));

            item = buffer.Consume();
            if (item != "two")
                throw new Exception(String.Format("item {0} != {1}", item, "two"));
            TestProperties(buffer, 1);

            item = buffer.Consume();
            if (item != "three")
                throw new Exception(String.Format("item {0} != {1}", item, "three"));
            TestProperties(buffer, 0);

            TestException<BufferUnderflowException>(() => buffer.Consume());

            // test Clear
            buffer.Produce("five");
            TestProperties(buffer, 1);

            buffer.Produce("six");
            TestProperties(buffer, 2);

            buffer.Clear();
            TestProperties(buffer, 0);
        }

        /// <summary>
        /// Test the ProduceAll and ConsumeAll methods of the circular buffer implementation.
        /// </summary>
        static void TestCircularBufferCollection()
        {
            ICircularBuffer<MyClass> buffer = new CircularBuffer<MyClass>(1000);
            TestProperties(buffer, 0);

            // fill collection with dummy values
            IList<MyInheritedClass> collection = new List<MyInheritedClass>();
            for (int i = 0; i < 800; ++i)
                collection.Add(new MyInheritedClass(String.Format("{0}", i), i));

            // test ProduceAll
            var produced = buffer.ProduceAll(collection);
            if (produced != 800)
                throw new Exception(String.Format("produced {0} != {1}", produced, 800));
            TestProperties(buffer, 800);

            produced = buffer.ProduceAll(collection);
            if (produced != 200)
                throw new Exception(String.Format("produced {0} != {1}", produced, 200));
            TestProperties(buffer, 1000);

            // test ConsumeAll
            uint consumed = 0;
            buffer.ConsumeAll(item =>
            {
                if (!(item is MyInheritedClass))
                    throw new Exception(String.Format("type {0} instead of {1} expected", typeof(MyInheritedClass).Name, item.GetType().Name));
                if ((item as MyInheritedClass).newValue != (consumed % 800))
                    throw new Exception(String.Format("newValue {0} != {1}", (item as MyInheritedClass).newValue, consumed % 800));
                consumed += 1;
            });
            if (consumed != 1000)
                throw new Exception(String.Format("consumed {0} != {1}", consumed, 1000));
            TestProperties(buffer, 0);

            // test if Produce inside of ConsumeAll works
            produced = buffer.ProduceAll(collection);
            if (produced != 800)
                throw new Exception(String.Format("produced {0} != {1}", produced, 800));
            TestProperties(buffer, 800);

            consumed = 0;
            buffer.ConsumeAll(item =>
            {
                if (!(item is MyInheritedClass))
                    throw new Exception(String.Format("type {0} instead of {1} expected", typeof(MyInheritedClass).Name, item.GetType().Name));
                if ((item as MyInheritedClass).newValue != (consumed % 800))
                    throw new Exception(String.Format("newValue {0} != {1}", (item as MyInheritedClass).newValue, consumed % 800));
                consumed += 1;
                if (consumed <= 2000)
                    buffer.Produce(item);
            });
            if (consumed != 2800)
                throw new Exception(String.Format("consumed {0} != {1}", consumed, 2800));
            TestProperties(buffer, 0);
        }

        /// <summary>
        /// Test the standard methods of the circular buffer implementation, if they have problems in thread safety.
        /// </summary>
        static void TestCircularBufferConcurrency()
        {
            const long n = 1999999;

            ICircularBuffer<long> buffer = new CircularBuffer<long>(10);
            Task<long>[] tasks = new Task<long>[4];

            // start worker threads, which produce and consume numbers of 0..n
            for (int i = 0; i < tasks.Length; ++i)
                tasks[i] = Task<long>.Run(() =>
                    {
                        long s = 0;
                        for (long j = 0; j <= n; j += 2)
                        {
                            buffer.Produce(j);
                            buffer.Produce(j + 1);
                            s += buffer.Consume();
                            s += buffer.Consume();
                        }
                        return s;
                    });

            // wait for all worker threads to finish
            Task.WaitAll(tasks);
            TestProperties(buffer, 0);

            // check calculated sum over all worker threads
            long sum = tasks.Sum(task => task.Result);
            if (sum != tasks.Length * n * (n + 1) / 2)
                throw new Exception(String.Format("sum {0} != {1}", sum, tasks.Length * n * (n + 1) / 2));
        }

        /// <summary>
        /// Test the blocking methods of the circular buffer implementation.
        /// </summary>
        static void TestCircularBufferBlocking()
        {
            const long n = 1999999;

            ICircularBuffer<long> buffer = new CircularBuffer<long>(10);
            Task<long>[] tasks = new Task<long>[2];

            // start consumer worker thread
            tasks[0] = Task<long>.Run(() =>
                {
                    long s = 0;
                    for (long j = 0; j <= n; ++j)
                        s += buffer.WaitConsume();
                    return s;
                });

            // start producer worker thread
            tasks[1] = Task<long>.Run(() =>
                {
                    long s = 0;
                    for (long j = 0; j <= n; ++j)
                    {
                        buffer.WaitProduce(j);
                        s += j;
                    }
                    return s;
                });

            // wait for both worker threads to finish
            Task.WaitAll(tasks);
            TestProperties(buffer, 0);

            // check calculated sum over all worker threads
            long sum = tasks.Sum(task => task.Result);
            if (sum != tasks.Length * n * (n + 1) / 2)
                throw new Exception(String.Format("sum {0} != {1}", sum, tasks.Length * n * (n + 1) / 2));
        }

        static void Main(string[] args)
        {
            TestCircularBufferStandard();
            TestCircularBufferCollection();
            TestCircularBufferConcurrency();
            TestCircularBufferBlocking();
        }
    }
}
