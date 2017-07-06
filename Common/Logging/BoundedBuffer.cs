using System;
using System.Threading;

namespace Common.Logging
{
    internal class BoundedBuffer<T>
        where T : class
    {
        public BoundedBuffer(int capacity)
        {
            items = new T[capacity];
        }

        public bool TryAdd(T item)
        {
            while (true)
            {
                var currentCount = count;
                if (currentCount >= items.Length)
                    return false;

                if (Interlocked.CompareExchange(ref count, currentCount + 1, currentCount) == currentCount)
                {
                    while (true)
                    {
                        var currentFrontPointer = frontPointer;

                        if (Interlocked.CompareExchange(ref frontPointer, (currentFrontPointer + 1) % items.Length, currentFrontPointer) == currentFrontPointer)
                        {
                            items[currentFrontPointer] = item;
                            return true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method must not be called concurrently with itself.
        /// </summary>
        public T[] Drain()
        {
            var currentCount = count;
            if (currentCount == 0)
                return EmptyArray;

            var result = new T[currentCount];
            var resultCount = 0;

            for (int i = 0; i < currentCount; i++)
            {
                var index = (backPointer + i) % items.Length;
                var item = Interlocked.Exchange(ref items[index], null);
                if (item == null)
                    break;

                result[resultCount++] = item;
            }

            backPointer += resultCount;
            backPointer %= items.Length;

            Interlocked.Add(ref count, -resultCount);

            if (resultCount == currentCount)
                return result;

            if (resultCount == 0)
                return EmptyArray;

            var trimmedResult = new T[resultCount];

            Array.Copy(result, 0, trimmedResult, 0, resultCount);

            return trimmedResult;
        }

        private readonly T[] items;
        private int count;
        private int frontPointer;
        private volatile int backPointer;

        private static readonly T[] EmptyArray = new T[0];
    }
}