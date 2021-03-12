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
using MxNet.Gluon.NN;

namespace MxNet.Gluon.Contrib.NN
{
    public class SyncBatchNorm : BatchNorm
    {
        public SyncBatchNorm(int in_channels = 0, int? num_devices = null, float momentum = 0.9f, float epsilon = 1e-5f,
            bool center = true, bool scale = true, bool use_global_stats = false, string beta_initializer = "zeros",
            string gamma_initializer = "ones", string running_mean_initializer = "zeros",
            string running_variance_initializer = "ones",
            string prefix = "", ParameterDict @params = null)
            : base(1, momentum, epsilon, center, scale, use_global_stats, beta_initializer, gamma_initializer,
                running_mean_initializer, running_variance_initializer, in_channels)
        {
            throw new NotImplementedException();
        }

        internal int GetNumDevices()
        {
            throw new NotImplementedException();
        }

        public override NDArrayOrSymbol HybridForward(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            throw new NotImplementedException();
        }
    }
}