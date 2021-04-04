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
using MxNet.Numpy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MxNet.Gluon.Data
{
    public class _MultiWorkerIter
    {
        public delegate (ndarray, ndarray) WorkerFn(int[] r, Func<(ndarray, ndarray)[], (ndarray, ndarray)> _batchify_fn,
            Dataset<(ndarray, ndarray)> dataset);

        private readonly BatchSampler _batch_sampler;
        private readonly Func<(ndarray, ndarray)[], (ndarray, ndarray)> _batchify_fn;
        private readonly Dictionary<int, (ndarray, ndarray)> _data_buffer;
        private DataLoader _data_loader;
        private readonly Dataset<(ndarray, ndarray)> _dataset;
        private readonly IEnumerator<int[]> _iter;
        private readonly int _pin_device_id;
        private readonly bool _pin_memory;
        private int _rcvd_idx;
        private int _sent_idx;
        private readonly WorkerFn _worker_fn;
        private WorkerPool _worker_pool;

        public _MultiWorkerIter(WorkerPool worker_pool, Func<(ndarray, ndarray)[], (ndarray, ndarray)> batchify_fn,
            BatchSampler batch_sampler,
            bool pin_memory = false, int pin_device_id = 0, WorkerFn worker_fn = null,
            int prefetch = 0, Dataset<(ndarray, ndarray)> dataset = null, DataLoader data_loader = null)
        {
            _worker_pool = worker_pool;
            _batchify_fn = batchify_fn;
            _batch_sampler = batch_sampler;
            _data_buffer = new Dictionary<int, (ndarray, ndarray)>();
            _rcvd_idx = 0;
            _sent_idx = 0;
            _iter = _batch_sampler.GetEnumerator();
            _worker_fn = worker_fn;
            _pin_memory = pin_memory;
            _pin_device_id = pin_device_id;
            _dataset = dataset;
            _data_loader = data_loader;
            foreach (var item in Enumerable.Range(0, prefetch)) PushNext();
        }

        public int Length => _batch_sampler.Length;

        public (ndarray, ndarray) Next()
        {
            PushNext();
            if (_rcvd_idx == _sent_idx)
            {
                if (_data_buffer.Count > 0)
                    throw new Exception("Data buffer should be empty at this moment");

                throw new Exception("Stop Iteration");
            }

            if (_rcvd_idx >= _sent_idx)
                throw new Exception("rcvd_idx must be smaller than sent_idx");

            if (!_data_buffer.ContainsKey(_rcvd_idx))
                throw new Exception("fatal error with _push_next, rcvd_idx missing");

            var batch = _data_buffer[_rcvd_idx];
            if (_pin_memory)
                batch = DataLoader.AsInContext(batch, Context.CpuPinned(_pin_device_id));

            _rcvd_idx += 1;
            return batch;
        }

        public virtual void PushNext()
        {
            if (!_iter.MoveNext())
                return;

            var r = _iter.Current;
            _data_buffer[_sent_idx] = _worker_fn(r, _batchify_fn, _dataset);
            _sent_idx++;
            //ThreadPool.QueueUserWorkItem(obj =>
            //{
                
            //});
        }
    }
}