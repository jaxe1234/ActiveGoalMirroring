namespace MACsharp.DataStructures
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// https://www.codeproject.com/Articles/126751/Priority-Queue-in-Csharp-with-the-Help-of-Heap-Dat
    /// </summary>
    /// <typeparam name="TPriority"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class PriorityQueue<TPriority, TValue> where TValue : IEquatable<TValue>
    {
        private readonly List<KeyValuePair<TPriority, TValue>> _minHeap;
        private readonly HashSet<TValue> _values;
        private readonly IComparer<TPriority> _priorityComparer;

        public PriorityQueue()
            : this(Comparer<TPriority>.Default)
        {
        }

        public PriorityQueue(IComparer<TPriority> comparer)
        {
            _minHeap = new List<KeyValuePair<TPriority, TValue>>();
            _values = new HashSet<TValue>();
            _priorityComparer = comparer ?? throw new ArgumentNullException();
        }

        public void Enqueue(TPriority priority, TValue value)
        {
            Insert(priority, value);
        }

        private void Insert(TPriority priority, TValue value)
        {
            var val =
                new KeyValuePair<TPriority, TValue>(priority, value);
            _minHeap.Add(val);
            _values.Add(value);

            // heapify after insert, from end to beginning
            HeapifyFromEndToBeginning(_minHeap.Count - 1);
        }

        private int HeapifyFromEndToBeginning(int pos)
        {
            if (pos >= _minHeap.Count) return -1;

            // heap[i] have children heap[2*i + 1] and heap[2*i + 2] and parent heap[(i-1)/ 2];

            while (pos > 0)
            {
                var parentPos = (pos - 1) / 2;
                if (_priorityComparer.Compare(_minHeap[parentPos].Key, _minHeap[pos].Key) > 0)
                {
                    ExchangeElements(parentPos, pos);
                    pos = parentPos;
                }
                else break;
            }
            return pos;
        }

        private void ExchangeElements(int pos1, int pos2)
        {
            var val = _minHeap[pos1];
            _minHeap[pos1] = _minHeap[pos2];
            _minHeap[pos2] = val;
        }

        public TValue DequeueValue()
        {
            return Dequeue().Value;
        }

        public KeyValuePair<TPriority, TValue> Dequeue()
        {
            if (!IsEmpty)
            {
                var result = _minHeap[0];
                DeleteRoot();
                return result;
            }

            throw new InvalidOperationException("Priority queue is empty");
        }

        private void DeleteRoot()
        {
            if (_minHeap.Count <= 1)
            {
                _minHeap.Clear();
                _values.Clear();
                return;
            }

            var last = _minHeap.Count - 1;
            var value = _minHeap[0].Value;
            _minHeap[0] = _minHeap[last];
            _minHeap.RemoveAt(last);
            _values.Remove(value);

            // heapify
            HeapifyFromBeginningToEnd(0);
        }

        private void HeapifyFromBeginningToEnd(int pos)
        {
            if (pos >= _minHeap.Count) return;

            // heap[i] have children heap[2*i + 1] and heap[2*i + 2] and parent heap[(i-1)/ 2];

            while (true)
            {
                // on each iteration exchange element with its smallest child
                var smallest = pos;
                var left = 2 * pos + 1;
                var right = 2 * pos + 2;
                if (left < _minHeap.Count &&
                    _priorityComparer.Compare(_minHeap[smallest].Key, _minHeap[left].Key) > 0)
                    smallest = left;
                if (right < _minHeap.Count &&
                    _priorityComparer.Compare(_minHeap[smallest].Key, _minHeap[right].Key) > 0)
                    smallest = right;

                if (smallest != pos)
                {
                    ExchangeElements(smallest, pos);
                    pos = smallest;
                }
                else break;
            }
        }

        public KeyValuePair<TPriority, TValue> Peek()
        {
            if (!IsEmpty)
                return _minHeap[0];
            else
                throw new InvalidOperationException("Priority queue is empty");
        }
        

        public bool IsEmpty => _minHeap.Count == 0;
        
        public bool Contains(TValue value)
        {
            return _values.Contains(value);
        }
    }
}
