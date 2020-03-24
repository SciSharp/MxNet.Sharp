﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MxNet
{
    public class SymbolDict : IEnumerable<KeyValuePair<string, Symbol>>
    {
        private readonly Dictionary<string, Symbol> dict = new Dictionary<string, Symbol>();

        public SymbolDict(params string[] names)
        {
            foreach (var item in names) Add(item, null);
        }

        public int Count => dict.Count;

        public string[] Keys => dict.Keys.ToArray();

        public SymbolList Values => dict.Values.ToArray();

        public Symbol this[string name]
        {
            get
            {
                if (!dict.ContainsKey(name))
                    return null;

                return dict[name];
            }
            set => dict[name] = value;
        }

        public IEnumerator<KeyValuePair<string, Symbol>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        public void Add(string name, Symbol value)
        {
            dict.Add(name, value);
        }

        public void Add(SymbolDict other)
        {
            foreach (var item in other) Add(item.Key, item.Value);
        }

        public bool Contains(string name)
        {
            return dict.ContainsKey(name);
        }

        public void Remove(string name)
        {
            dict.Remove(name);
        }
    }
}