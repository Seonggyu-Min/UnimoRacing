using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSG
{
    public class PriorityQueue<T>
    {
        private SortedDictionary<int, Queue<T>> _dict = new();

        public int Count { get; private set; }

        public void Enqueue(T item, int priority)
        {
            if (!_dict.TryGetValue(priority, out var queue))
            {
                queue = new Queue<T>();
                _dict.Add(priority, queue);
            }

            queue.Enqueue(item);
            Count++;
        }

        public T Dequeue()
        {
            foreach (var pair in _dict)
            {
                if (pair.Value.Count > 0)
                {
                    Count--;
                    return pair.Value.Dequeue();
                }
            }

            throw new InvalidOperationException("Queue is empty");
        }

        public void Clear()
        {
            _dict.Clear();
            Count = 0;
        }
    }
}
