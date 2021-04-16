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
using System;

namespace MxNet.Gluon.Data.Vision.Transforms
{
    public class RandomHue : HybridBlock
    {
        private readonly float _hue;

        public RandomHue(float hue)
        {
            _hue = hue;
        }

        public override NDArrayOrSymbolList HybridForward(NDArrayOrSymbolList args)
        {
            var x = args[0];
            var min_factor = Math.Max(0, 1 - _hue);
            var max_factor = 1 + _hue;

            if (x.IsNDArray)
                return nd.Image.RandomHue(x, min_factor, max_factor);

            return sym.Image.RandomHue(x, min_factor, max_factor);
        }
    }
}