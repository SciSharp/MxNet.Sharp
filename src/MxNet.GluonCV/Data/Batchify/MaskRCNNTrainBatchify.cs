﻿using MxNet;
using MxNet.Gluon;
using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.GluonCV.Data.Batchify
{
    public class MaskRCNNTrainBatchify
    {
        public MaskRCNNTrainBatchify(HybridBlock net, int num_shards= 1)
        {
            throw new NotImplementedException();
        }

        public (NDArray, NDArray, NDArray, NDArrayList, NDArrayList, NDArrayList) Call(NDArrayList data)
        {
            throw new NotImplementedException();
        }
    }
}
