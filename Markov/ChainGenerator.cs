﻿//-----------------------------------------------------------------------
// <copyright file="ChainGenerator.cs" company="(none)">
//  Copyright © 2011 John Gietzen.
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// </copyright>
// <author>John Gietzen</author>
//-----------------------------------------------------------------------

namespace Markov
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ChainGenerator<T> where T : IEquatable<T>
    {
        private readonly int order;

        private readonly Dictionary<ChainState<T>, Dictionary<T, int>> items = new Dictionary<ChainState<T>, Dictionary<T, int>>();
        private readonly Dictionary<ChainState<T>, int> terminals = new Dictionary<ChainState<T>, int>();

        public ChainGenerator(int order)
        {
            if (order <= 0)
            {
                throw new ArgumentOutOfRangeException("order");
            }

            this.order = order;
        }

        public void Add(IEnumerable<T> items, int weight)
        {
            Queue<T> previous = new Queue<T>();
            foreach (var item in items)
            {
                var key = new ChainState<T>(previous.ToArray());

                Dictionary<T, int> weights;
                if (!this.items.TryGetValue(key, out weights))
                {
                    weights = new Dictionary<T, int>();
                    this.items.Add(key, weights);
                }

                weights[item] = weights.ContainsKey(item)
                    ? weight + weights[item]
                    : weight;

                previous.Enqueue(item);
                if (previous.Count > this.order)
                {
                    previous.Dequeue();
                }
            }

            var terminalKey = new ChainState<T>(previous.ToArray());
            this.terminals[terminalKey] = this.terminals.ContainsKey(terminalKey)
                ? weight + this.terminals[terminalKey]
                : weight;
        }

        public IEnumerable<T> Chain()
        {
            return this.Chain(new Random());
        }

        public IEnumerable<T> Chain(int seed)
        {
            return this.Chain(new Random(seed));
        }

        public IEnumerable<T> Chain(Random rand)
        {
            Queue<T> previous = new Queue<T>();
            while (true)
            {
                var key = new ChainState<T>(previous.ToArray());

                Dictionary<T, int> weights;
                if (!this.items.TryGetValue(key, out weights))
                {
                    yield break;
                }

                int terminalWeight;
                this.terminals.TryGetValue(key, out terminalWeight);

                var total = weights.Sum(w => w.Value);
                var value = rand.Next(total + terminalWeight) + 1;

                if (value > total)
                {
                    yield break;
                }

                var currentWeight = 0;
                foreach (var nextItem in weights)
                {
                    currentWeight += nextItem.Value;
                    if (currentWeight >= value)
                    {
                        yield return nextItem.Key;
                        previous.Enqueue(nextItem.Key);
                        break;
                    }
                }

                if (previous.Count > this.order)
                {
                    previous.Dequeue();
                }
            }
        }
    }
}
