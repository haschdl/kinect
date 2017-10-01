using System.Collections.Concurrent;

namespace Kinect.Service
{
    internal class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        private int _count;
        public FixedSizedQueue(int size)
        {
            Size = size;
            _count = 0;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            
            lock (syncObject)
            {
                _count++;
                //while (base.Count > Size)
                while (_count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                    _count--;
                }
            }
        }
    }
}
