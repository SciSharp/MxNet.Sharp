﻿using MxNet.Interop;
using System;
using System.Linq;

namespace MxNet.RTC
{
    public class CudaKernel : IDisposable
    {
        public IntPtr Handle { get; }
        public string Name { get; }
        public bool[] IsNdarray { get; }
        public DType[] Dtypes { get; }

        public CudaKernel(IntPtr handle, string name, bool[] is_ndarray, DType[] dtypes)
        {
            Handle = handle;
            Name = name;
            IsNdarray = is_ndarray;
            Dtypes = dtypes;
        }

        public void Dispose()
        {
            if (Handle != null)
                NativeMethods.MXRtcCudaModuleFree(Handle);
        }

        public void Launch(object[] args, Context ctx, (int, int, int) grid_dims, (int, int, int) block_dims,
            int shared_mem = 0)
        {
            if (ctx.GetDeviceType() != DeviceType.GPU)
                throw new ArgumentException("Device type GPU supported");

            if (args.Length != Dtypes.Length)
                throw new ArgumentException($"CudaKernel({Name}) expects {Dtypes.Length} arguments but got {args.Length}");

            NativeMethods.MXRtcCudaKernelCall(Handle, ctx.GetDeviceId(), args.Select(x => (x.GetMemPtr())).ToArray(), grid_dims.Item1, grid_dims.Item2, grid_dims.Item3, block_dims.Item1, block_dims.Item2, block_dims.Item3, shared_mem);
        }
    }
}