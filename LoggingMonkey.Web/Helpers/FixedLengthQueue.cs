using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LoggingMonkey.Web.Helpers
{
    public class FixedLengthQueue<T>
    {
        public uint MaximumLength { get; private set; }

        public int Count { get { return queue.Count; } }

        public FixedLengthQueue(uint maxLength)
        {
            if (maxLength == 0)
            {
                throw new ArgumentOutOfRangeException("Cannot create a fixed-length queue without a length");
            }

            MaximumLength = maxLength;
        }

        public void Enqueue(T item)
        {
            if (Count == MaximumLength)
            {
                Dequeue();
            }

            queue.Enqueue(item);
        }

        public T Dequeue()
        {
            return queue.Dequeue();
        }

        public T Peek()
        {
            return queue.Peek();
        }

        public T[] ToArray()
        {
            return queue.ToArray();
        }

        private readonly Queue<T> queue = new Queue<T>();
    }
}