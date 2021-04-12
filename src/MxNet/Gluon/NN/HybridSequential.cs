﻿/*****************************************************************************
   Copyright 2018 The MxNet.Sharp Authors. All Rights Reserved.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
******************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace MxNet.Gluon.NN
{
    public class HybridSequential : HybridBlock
    {
        private List<Block> _layers;
        private bool _v2;
        private bool _v2_checked;
        private bool _forward;

        public HybridSequential() : base()
        {
            this._layers = new List<Block>();
            this._v2_checked = false;
        }

        public new HybridSequential this[string key]
        {
            get
            {
                var layer = this._childrens[key];
                var net = new HybridSequential();
                net.Add((HybridBlock)layer);
                return net;
            }
        }

        public HybridSequential(Dictionary<string, Block> blocks, bool loadkeys = false)
           : this()
        {
            foreach (var item in blocks)
            {
                if (loadkeys)
                    RegisterChild(item.Value, item.Key);
                else
                    RegisterChild(item.Value);
            }
        }

        public int Length => _childrens.Count;

        public void Add(params HybridBlock[] blocks)
        {
            foreach (var item in blocks)
            {
                _layers.Add(item);
                RegisterChild(item);
            }
        }

        public override NDArrayOrSymbol Call(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            if (this._active && !this._v2_checked && !DeferredCompute.IsDeferredCompute())
            {
                // If any of the child Blocks implements the Gluon 2 interface, the
                // container must not pass a _Symbol to them
                if ((from chld in this._childrens.Values
                        select chld is HybridBlock).Any())
                {
                    this._v2 = true;
                    this._v2_checked = true;
                    this._forward = true;
                }
            }

            return base.Call(x, args);
        }

        public override NDArrayOrSymbol Forward(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            if (_forward)
            {
                foreach (var item in _childrens) x = item.Value.Call(x, args);
                return x;
            }
            else
            {
                return base.Forward(x, args);
            }
        }

        public override NDArrayOrSymbol HybridForward(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            foreach (var item in _childrens) x = item.Value.Call(x, args);

            return x;
        }

        public override string ToString()
        {
            var modstr = string.Join("\n", _childrens.Select(c => $"  ({c.Key}): {Utils.Indent(c.Value.ToString(), 2)}"));
            return $"{GetType().Name}(\n{modstr}\n)";
        }
    }
}