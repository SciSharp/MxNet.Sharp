﻿using MxNet.Gluon;
using MxNet.Gluon.NN;
using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.GluonCV.ModelZoo.Yolo
{
    public class DarknetBasicBlockV3 : HybridBlock
    {
        private HybridSequential body;
        public DarknetBasicBlockV3(int channel, string norm_layer= "BatchNorm", FuncArgs norm_kwargs= null, string prefix = null, ParameterDict @params = null) : base(prefix, @params)
        {
            this.body = new HybridSequential(prefix: "");
            // 1x1 reduce
            this.body.Add(DarknetV3.Conv2d(channel, 1, 0, 1, norm_layer: norm_layer, norm_kwargs: norm_kwargs));
            // 3x3 conv expand
            this.body.Add(DarknetV3.Conv2d(channel * 2, 3, 1, 1, norm_layer: norm_layer, norm_kwargs: norm_kwargs));
        }

        public override NDArrayOrSymbol HybridForward(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            var residual = x;
            x = this.body.Call(x);
            return x + residual;
        }
    }
}
