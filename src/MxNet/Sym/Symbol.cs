﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MxNet.Initializers;
using MxNet.Interop;
using mx_uint = System.UInt32;
using SymbolHandle = System.IntPtr;

// ReSharper disable once CheckNamespace
namespace MxNet
{
    public class Symbol : DisposableMXNetObject
    {
        #region Fields

        #endregion

        #region Constructors

        public Symbol()
            : this(SymbolHandle.Zero)
        {
        }

        public Symbol(SymbolHandle handle)
        {
            NativePtr = handle;
        }

        public Symbol(string name)
        {
            if (NativeMethods.MXSymbolCreateVariable(name, out var @out) != NativeMethods.OK)
                throw new MXNetException($"Failed to create {nameof(Symbol)}");

            NativePtr = @out;
        }

        //public Symbol(string operatorName, 
        //              string name,
        //              IList<string> inputKeys,
        //              IList<SymbolHandle> inputValues,
        //              IList<string> configKeys,
        //              IList<string> configValues)
        //{
        //    if (inputKeys == null)
        //        throw new ArgumentNullException(nameof(inputKeys));
        //    if (inputValues == null)
        //        throw new ArgumentNullException(nameof(inputValues));
        //    if (configKeys == null)
        //        throw new ArgumentNullException(nameof(configKeys));
        //    if (configValues == null)
        //        throw new ArgumentNullException(nameof(configValues));

        //    var creator = OpMap.GetSymbolCreator(operatorName);
        //    NativeMethods.MXSymbolCreateAtomicSymbol(creator, 
        //                                             (uint)configKeys.Count,
        //                                             configKeys.ToArray(),
        //                                             configValues.ToArray(),
        //                                             out var handle);

        //    NativeMethods.MXSymbolCompose(handle, 
        //                                  operatorName,
        //                                  (uint)inputKeys.Count,
        //                                  inputKeys.ToArray(),
        //                                  inputValues.ToArray());

        //    blob_ptr_ = std::make_shared<SymBlob>(handle);
        //    this.NativePtr = @out;
        //}

        #endregion

        #region Properties

        public string Name
        {
            get
            {
                ThrowIfDisposed();
                if (NativePtr == SymbolHandle.Zero)
                    return null;

                NativeMethods.MXSymbolGetName(NativePtr, out var @out, out var success);
                if (@out == SymbolHandle.Zero)
                    return null;

                return Marshal.PtrToStringAnsi(@out);
            }
        }

        public Symbol this[int index]
        {
            get
            {
                ThrowIfDisposed();

                NativeMethods.MXSymbolGetOutput(NativePtr, (uint) index, out var @out);
                return new Symbol(@out);
            }
        }

        public Symbol this[string slice]
        {
            get
            {
                if (string.IsNullOrEmpty(slice))
                    return this;

                var split = slice.Split(':');

                var rowBegin = Convert.ToInt32(split[0]);
                var rowEnd = Convert.ToInt32(split[1]);

                return sym.Slice(this, new Shape(rowBegin), new Shape(rowEnd));
            }
        }

        #endregion

        #region Methods

        public Executor Bind(Context context,
            NDArrayList argArrays,
            NDArrayList gradArrays,
            IList<OpGradReq> gradReqs,
            NDArrayList auxArrays)
        {
            return new Executor(this,
                context,
                argArrays,
                gradArrays,
                gradReqs,
                auxArrays,
                new Dictionary<string, Context>());
        }

        public Executor Bind(Context context,
            NDArrayList argArrays,
            NDArrayList gradArrays,
            IList<OpGradReq> gradReqs,
            NDArrayList auxArrays,
            IDictionary<string, Context> groupToCtx)
        {
            return new Executor(this,
                context,
                argArrays,
                gradArrays,
                gradReqs,
                auxArrays,
                groupToCtx,
                null);
        }

        public Executor Bind(Context context,
            NDArrayList argArrays,
            NDArrayList gradArrays,
            IList<OpGradReq> gradReqs,
            NDArrayList auxArrays,
            IDictionary<string, Context> groupToCtx,
            Executor sharedExec)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (argArrays == null)
                throw new ArgumentNullException(nameof(argArrays));
            if (gradArrays == null)
                throw new ArgumentNullException(nameof(gradArrays));
            if (gradReqs == null)
                throw new ArgumentNullException(nameof(gradReqs));
            if (auxArrays == null)
                throw new ArgumentNullException(nameof(auxArrays));
            if (groupToCtx == null)
                throw new ArgumentNullException(nameof(groupToCtx));

            return new Executor(this,
                context,
                argArrays,
                gradArrays,
                gradReqs,
                auxArrays,
                groupToCtx,
                sharedExec);
        }

        public SymbolHandle GetHandle()
        {
            ThrowIfDisposed();
            return NativePtr;
        }

        public static Symbol Group(IList<Symbol> symbols)
        {
            var handleList = symbols.Select(symbol => symbol.GetHandle()).ToArray();
            NativeMethods.MXSymbolCreateGroup((uint) handleList.Length, handleList, out var @out);
            return new Symbol(@out);
        }

        public void InferArgsMap(Context context,
            NDArrayDict argsMap,
            NDArrayDict knownArgs)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (argsMap == null)
                throw new ArgumentNullException(nameof(argsMap));
            if (knownArgs == null)
                throw new ArgumentNullException(nameof(knownArgs));

            ThrowIfDisposed();

            var argShapes = new Dictionary<string, Shape>();

            var argNameList = ListArguments();
            foreach (var argName in argNameList)
                if (knownArgs[argName] != null)
                    argShapes[argName] = knownArgs[argName].Shape;

            var (inShapes, outShapes, auxShapes) = InferShape(argShapes);

            for (var i = 0; i < inShapes.Length; ++i)
            {
                var shape = inShapes[i];
                var argName = argNameList[i];
                if (knownArgs[argName] != null)
                {
                    argsMap[argName] = knownArgs[argName];
                }
                else
                {
                    var array = new NDArray(shape, false);
                    argsMap[argName] = array;
                    //NDArray.SampleGaussian(0, 1, array);
                    nd.Random.Uniform(0, 1, array.Shape).CopyTo(array);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="argShapes"></param>
        /// <returns>Return arg_shapes, out_shapes, aux_shapes</returns>
        public (Shape[], Shape[], Shape[]) InferShape(Dictionary<string, Shape> argShapes)
        {
            if (argShapes == null)
                throw new ArgumentNullException(nameof(argShapes));

            var inShape = new List<Shape>();
            var auxShape = new List<Shape>();
            var outShape = new List<Shape>();

            ThrowIfDisposed();
            var argIndPtr = new List<int> {0};
            var argShapeData = new List<int>();

            foreach (var item in argShapes.Values)
            {
                foreach (var i in item.Data)
                {
                    if (i == 0)
                        continue;

                    argShapeData.Add(i);
                }

                argIndPtr.Add(argShapeData.Count);
            }

            unsafe
            {
                var keys = argShapes.Keys.ToArray();
                var argIndPtrArray = argIndPtr.ToArray();
                var argShapeDataArray = argShapeData.ToArray();
                {
                    int inShapeSize;
                    int* inShapeNdim;
                    int** inShapeData;

                    Logging.CHECK_EQ(NativeMethods.MXSymbolInferShapeEx(NativePtr,
                        (uint) argShapes.Count,
                        keys,
                        argIndPtrArray,
                        argShapeDataArray,
                        &inShapeSize,
                        &inShapeNdim,
                        &inShapeData,
                        out var outShapeSize,
                        out var outShapeNdim,
                        out var outShapeData,
                        out var auxShapeSize,
                        out var auxShapeNdim,
                        out var auxShapeData,
                        out var complete), NativeMethods.OK);

                    if (complete == 0)
                        return (null, null, null);

                    for (var i = 0; i < inShapeSize; ++i)
                    {
                        inShape.Add(new Shape());
                        for (var j = 0; j < inShapeNdim[i]; ++j)
                            inShape[i].Add(inShapeData[i][j]);
                    }

                    for (var i = 0; i < auxShapeSize; ++i)
                    {
                        auxShape.Add(new Shape());
                        for (var j = 0; j < auxShapeNdim[i]; ++j)
                            auxShape[i].Add(auxShapeData[i][j]);
                    }

                    for (var i = 0; i < outShapeSize; ++i)
                    {
                        outShape.Add(new Shape());
                        for (var j = 0; j < outShapeNdim[i]; ++j)
                            outShape[i].Add(outShapeData[i][j]);
                    }
                }
            }

            return (inShape.ToArray(), outShape.ToArray(), auxShape.ToArray());
        }

        public (Shape[], Shape[], Shape[]) InferShapePartial(Dictionary<string, Shape> argShapes)
        {
            if (argShapes == null)
                throw new ArgumentNullException(nameof(argShapes));

            var inShape = new List<Shape>();
            var auxShape = new List<Shape>();
            var outShape = new List<Shape>();

            ThrowIfDisposed();
            var argIndPtr = new List<int> {0};
            var argShapeData = new List<int>();

            foreach (var item in argShapes.Values)
            {
                foreach (var i in item.Data)
                {
                    if (i == 0)
                        continue;

                    argShapeData.Add(i);
                }

                argIndPtr.Add(argShapeData.Count);
            }

            unsafe
            {
                var keys = argShapes.Keys.ToArray();
                var argIndPtrArray = argIndPtr.ToArray();
                var argShapeDataArray = argShapeData.ToArray();
                {
                    int inShapeSize;
                    int* inShapeNdim;
                    int** inShapeData;

                    Logging.CHECK_EQ(NativeMethods.MXSymbolInferShapePartialEx(NativePtr,
                        (uint) argShapes.Count,
                        keys,
                        argIndPtrArray,
                        argShapeDataArray,
                        &inShapeSize,
                        &inShapeNdim,
                        &inShapeData,
                        out var outShapeSize,
                        out var outShapeNdim,
                        out var outShapeData,
                        out var auxShapeSize,
                        out var auxShapeNdim,
                        out var auxShapeData,
                        out var complete), NativeMethods.OK);

                    if (complete == 0)
                        return (null, null, null);

                    for (var i = 0; i < inShapeSize; ++i)
                    {
                        inShape.Add(new Shape());
                        for (var j = 0; j < inShapeNdim[i]; ++j)
                            inShape[i].Add(inShapeData[i][j]);
                    }

                    for (var i = 0; i < auxShapeSize; ++i)
                    {
                        auxShape.Add(new Shape());
                        for (var j = 0; j < auxShapeNdim[i]; ++j)
                            auxShape[i].Add(auxShapeData[i][j]);
                    }

                    for (var i = 0; i < outShapeSize; ++i)
                    {
                        outShape.Add(new Shape());
                        for (var j = 0; j < outShapeNdim[i]; ++j)
                            outShape[i].Add(outShapeData[i][j]);
                    }
                }
            }

            return (inShape.ToArray(), outShape.ToArray(), auxShape.ToArray());
        }

        public (DType[], DType[], DType[]) InferType(Dictionary<string, DType> argTypes = null)
        {
            if (argTypes == null)
                throw new ArgumentNullException(nameof(argTypes));

            var inType = new List<DType>();
            var auxType = new List<DType>();
            var outType = new List<DType>();

            ThrowIfDisposed();
            var argTypeData = argTypes.Values.Select(x => (x.Index)).ToList();

            unsafe
            {
                var keys = argTypes.Keys.ToArray();
                var argShapeDataArray = argTypeData.ToArray();
                {
                    int inShapeSize;
                    int* inShapeData;

                    Logging.CHECK_EQ(NativeMethods.MXSymbolInferType(NativePtr,
                        (uint)argTypes.Count,
                        keys,
                        argShapeDataArray,
                        &inShapeSize,
                        &inShapeData,
                        out var outShapeSize,
                        out var outShapeData,
                        out var auxShapeSize,
                        out var auxShapeData,
                        out var complete), NativeMethods.OK);

                    if (complete == 0)
                        return (null, null, null);

                    for (var i = 0; i < inShapeSize; ++i)
                        inType.Add(DType.GetType(inShapeData[i]));

                    for (var i = 0; i < auxShapeSize; ++i)
                        auxType.Add(DType.GetType(auxShapeData[i]));

                    for (var i = 0; i < outShapeSize; ++i)
                        outType.Add(DType.GetType(outShapeData[i]));
                }
            }

            return (inType.ToArray(), outType.ToArray(), auxType.ToArray());
        }

        public (DType[], DType[], DType[]) InferTypePartial(Dictionary<string, DType> argTypes = null)
        {
            if (argTypes == null)
                throw new ArgumentNullException(nameof(argTypes));

            var inType = new List<DType>();
            var auxType = new List<DType>();
            var outType = new List<DType>();

            ThrowIfDisposed();
            var argTypeData = argTypes.Values.Select(x => (x.Index)).ToList();

            unsafe
            {
                var keys = argTypes.Keys.ToArray();
                var argShapeDataArray = argTypeData.ToArray();
                {
                    int inShapeSize;
                    int* inShapeData;

                    Logging.CHECK_EQ(NativeMethods.MXSymbolInferTypePartial(NativePtr,
                        (uint)argTypes.Count,
                        keys,
                        argShapeDataArray,
                        &inShapeSize,
                        &inShapeData,
                        out var outShapeSize,
                        out var outShapeData,
                        out var auxShapeSize,
                        out var auxShapeData,
                        out var complete), NativeMethods.OK);

                    if (complete == 0)
                        return (null, null, null);

                    for (var i = 0; i < inShapeSize; ++i)
                        inType.Add(DType.GetType(inShapeData[i]));

                    for (var i = 0; i < auxShapeSize; ++i)
                        auxType.Add(DType.GetType(auxShapeData[i]));

                    for (var i = 0; i < outShapeSize; ++i)
                        outType.Add(DType.GetType(outShapeData[i]));
                }
            }

            return (inType.ToArray(), outType.ToArray(), auxType.ToArray());
        }

        public void InferExecutorArrays(Context context,
            NDArrayList argArrays,
            NDArrayList gradArrays,
            IList<OpGradReq> gradReqs,
            NDArrayList auxArrays,
            NDArrayDict argsMap)
        {
            InferExecutorArrays(context,
                argArrays,
                gradArrays,
                gradReqs,
                auxArrays,
                argsMap,
                new NDArrayDict());
        }

        public void InferExecutorArrays(Context context,
            NDArrayList argArrays,
            NDArrayList gradArrays,
            IList<OpGradReq> gradReqs,
            NDArrayList auxArrays,
            NDArrayDict argsMap,
            NDArrayDict argGradStore)
        {
            InferExecutorArrays(context,
                argArrays,
                gradArrays,
                gradReqs,
                auxArrays,
                argsMap,
                argGradStore,
                new Dictionary<string, OpGradReq>());
        }

        public void InferExecutorArrays(Context context,
            NDArrayList argArrays,
            NDArrayList gradArrays,
            IList<OpGradReq> gradReqs,
            NDArrayList auxArrays,
            NDArrayDict argsMap,
            NDArrayDict argGradStore,
            IDictionary<string, OpGradReq> gradReqType)
        {
            InferExecutorArrays(context,
                argArrays,
                gradArrays,
                gradReqs,
                auxArrays,
                argsMap,
                argGradStore,
                gradReqType,
                new NDArrayDict());
        }

        public void InferExecutorArrays(Context context,
            NDArrayList argArrays,
            NDArrayList gradArrays,
            IList<OpGradReq> gradReqs,
            NDArrayList auxArrays,
            NDArrayDict argsMap,
            NDArrayDict argGradStore,
            IDictionary<string, OpGradReq> gradReqType,
            NDArrayDict auxMap)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (argArrays == null)
                throw new ArgumentNullException(nameof(argArrays));
            if (gradArrays == null)
                throw new ArgumentNullException(nameof(gradArrays));
            if (gradReqs == null)
                throw new ArgumentNullException(nameof(gradReqs));
            if (auxArrays == null)
                throw new ArgumentNullException(nameof(auxArrays));
            if (argsMap == null)
                throw new ArgumentNullException(nameof(argsMap));
            if (argGradStore == null)
                throw new ArgumentNullException(nameof(argGradStore));
            if (gradReqType == null)
                throw new ArgumentNullException(nameof(gradReqType));
            if (auxMap == null)
                throw new ArgumentNullException(nameof(auxMap));

            ThrowIfDisposed();

            var argNameList = ListArguments();
            var argShapes = new Dictionary<string, Shape>();

            foreach (var argName in argNameList)
                if (argsMap[argName] != null)
                    argShapes[argName] = argsMap[argName].Shape;

            var (inShapes, auxShapes, outShapes) = InferShape(argShapes);

            for (var i = 0; i < inShapes.Length; ++i)
            {
                var shape = inShapes[i];
                var argName = argNameList[i];
                if (argsMap[argName] != null)
                {
                    argArrays.Add(argsMap[argName]);
                }
                else
                {
                    argArrays.Add(new NDArray(shape, false));
                    //NDArray.SampleGaussian(0, 1, argArrays.Last());
                    var argArr = argArrays.Last();
                    nd.Random.Uniform(0, 1, argArr.Shape).CopyTo(argArr);
                }

                if (argGradStore[argName] != null)
                    gradArrays.Add(argGradStore[argName]);
                else
                    gradArrays.Add(new NDArray(shape, false));

                if (gradReqType.TryGetValue(argName, out var value3))
                    gradReqs.Add(value3);
                else if (argName.LastIndexOf("data", StringComparison.InvariantCulture) == argName.Length - 4 ||
                         argName.LastIndexOf("label", StringComparison.InvariantCulture) == argName.Length - 5)
                    gradReqs.Add(OpGradReq.Null);
                else
                    gradReqs.Add(OpGradReq.Write);
            }

            var auxNameList = ListAuxiliaryStates();
            for (var i = 0; i < auxShapes.Length; ++i)
            {
                var shape = auxShapes[i];
                var auxName = auxNameList[i];
                if (auxMap[auxName] != null)
                {
                    auxArrays.Add(auxMap[auxName]);
                }
                else
                {
                    auxArrays.Add(new NDArray(shape, false));
                    var aux = auxArrays.Last();
                    //NDArray.SampleGaussian(0, 1, auxArrays.Last());
                    nd.Random.Uniform(0, 1, aux.Shape).CopyTo(aux);
                }
            }
        }

        public IList<string> ListArguments()
        {
            ThrowIfDisposed();

            NativeMethods.MXSymbolListArguments(GetHandle(), out var size, out var sarry);
            var sarryArray = InteropHelper.ToPointerArray(sarry, size);

            var ret = new string[size];
            for (var i = 0; i < size; i++)
                ret[i] = Marshal.PtrToStringAnsi(sarryArray[i]);

            return ret;
        }

        public Dictionary<string, Dictionary<string, string>> ListAttributeDict()
        {
            ThrowIfDisposed();

            NativeMethods.MXSymbolListAuxiliaryStates(GetHandle(), out var size, out var sarry);
            var sarryArray = InteropHelper.ToPointerArray(sarry, size);

            Dictionary<string, Dictionary<string, string>> ret = new Dictionary<string, Dictionary<string, string>>();
            for (var i = 0; i < size; i++)
            {
                string[] pair = Marshal.PtrToStringAnsi(sarryArray[i * 2]).Split('$');
                string name = pair[0];
                string key = pair[1];
                string val = Marshal.PtrToStringAnsi(sarryArray[i * 2 + 1]);
                if (!ret.ContainsKey(name))
                    ret.Add(name, new Dictionary<string, string>());

                ret[name][key] = val;
            }

            return ret;
        }

        public IList<string> ListAuxiliaryStates()
        {
            ThrowIfDisposed();

            NativeMethods.MXSymbolListAuxiliaryStates(GetHandle(), out var size, out var sarry);
            var sarryArray = InteropHelper.ToPointerArray(sarry, size);

            var ret = new string[size];
            for (var i = 0; i < size; i++)
                ret[i] = Marshal.PtrToStringAnsi(sarryArray[i]);

            return ret;
        }

        public IList<string> ListOutputs()
        {
            ThrowIfDisposed();

            NativeMethods.MXSymbolListOutputs(GetHandle(), out var size, out var sarry);
            var sarryArray = InteropHelper.ToPointerArray(sarry, size);
            var ret = new string[size];
            for (var i = 0; i < size; i++)
                ret[i] = Marshal.PtrToStringAnsi(sarryArray[i]);

            return ret;
        }

        public static Symbol Load(string fileName)
        {
            Logging.CHECK_EQ(NativeMethods.MXSymbolCreateFromFile(fileName, out var handle), NativeMethods.OK);
            return new Symbol(handle);
        }

        public static Symbol LoadJSON(string json)
        {
            Logging.CHECK_EQ(NativeMethods.MXSymbolCreateFromJSON(json, out var handle), NativeMethods.OK);
            return new Symbol(handle);
        }

        public void Save(string fileName, bool remove_amp_cast = true)
        {
            if (remove_amp_cast)
            {
                Logging.CHECK_EQ(NativeMethods.MXSymbolRemoveAmpCast(GetHandle(), out var h), NativeMethods.OK);
                Logging.CHECK_EQ(NativeMethods.MXSymbolSaveToFile(h, fileName), NativeMethods.OK);
            }
            else
            {
                Logging.CHECK_EQ(NativeMethods.MXSymbolSaveToFile(GetHandle(), fileName), NativeMethods.OK);
            }
        }

        public Executor SimpleBind(Context context,
            NDArrayDict argsMap)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (argsMap == null)
                throw new ArgumentNullException(nameof(argsMap));

            ThrowIfDisposed();

            return SimpleBind(context, argsMap, new NDArrayDict());
        }

        public Executor SimpleBind(Context context,
            NDArrayDict argsMap,
            NDArrayDict argGradStore)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (argsMap == null)
                throw new ArgumentNullException(nameof(argsMap));
            if (argGradStore == null)
                throw new ArgumentNullException(nameof(argGradStore));

            ThrowIfDisposed();

            return SimpleBind(context, argsMap, argGradStore, new Dictionary<string, OpGradReq>());
        }

        public Executor SimpleBind(Context context,
            NDArrayDict argsMap,
            NDArrayDict argGradStore,
            IDictionary<string, OpGradReq> gradReqType)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (argsMap == null)
                throw new ArgumentNullException(nameof(argsMap));
            if (argGradStore == null)
                throw new ArgumentNullException(nameof(argGradStore));
            if (gradReqType == null)
                throw new ArgumentNullException(nameof(gradReqType));

            ThrowIfDisposed();

            return SimpleBind(context, argsMap, argGradStore, gradReqType, new NDArrayDict());
        }

        public Executor SimpleBind(Context context,
            NDArrayDict argsMap,
            NDArrayDict argGradStore,
            IDictionary<string, OpGradReq> gradReqType,
            NDArrayDict auxMap)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (argsMap == null)
                throw new ArgumentNullException(nameof(argsMap));
            if (argGradStore == null)
                throw new ArgumentNullException(nameof(argGradStore));
            if (gradReqType == null)
                throw new ArgumentNullException(nameof(gradReqType));
            if (auxMap == null)
                throw new ArgumentNullException(nameof(auxMap));

            ThrowIfDisposed();

            var argArrays = new NDArrayList();
            var gradArrays = new NDArrayList();
            var gradReqs = new List<OpGradReq>();
            var auxArrays = new NDArrayList();

            InferExecutorArrays(context,
                argArrays,
                gradArrays,
                gradReqs,
                auxArrays,
                argsMap,
                argGradStore,
                gradReqType,
                auxMap);

            return new Executor(this, context, argArrays, gradArrays, gradReqs, auxArrays);
        }

        public string ToJSON()
        {
            Logging.CHECK_EQ(NativeMethods.MXSymbolSaveToJSON(GetHandle(), out var outJson), NativeMethods.OK);
            return Marshal.PtrToStringAnsi(outJson);
        }

        public static Symbol Variable(string name)
        {
            return Var(name);
        }

        public static Symbol Var(string name, Dictionary<string, string> attr = null, Shape shape = null,
            float? lr_mult = null, float? wd_mult = null,
            DType dtype = null, Initializer init = null, StorageStype? stype = null)
        {
            NativeMethods.MXSymbolCreateVariable(name, out var handle);
            var ret = new Symbol(handle);
            if (attr == null)
                attr = new Dictionary<string, string>();

            if (shape != null)
                attr.Add("__shape__", shape.ToString());

            if (lr_mult.HasValue)
                attr.Add("__lr_mult__", lr_mult.Value.ToString());

            if (wd_mult.HasValue)
                attr.Add("__wd_mult__", wd_mult.Value.ToString());

            if (dtype != null)
                attr.Add("__dtype__", dtype.Name);

            if (init != null)
            {
                var init_string = init.Dumps();
                attr.Add("__init__", init_string);
            }

            if (stype.HasValue)
                attr.Add("__storage_type__", ((int) stype).ToString());

            ret.SetAttr(attr);

            return ret;
        }

        public string Attr(string key)
        {
            NativeMethods.MXSymbolGetAttr(GetHandle(), key, out var @out, out var success);
            if (success != 0)
                return @out;

            return null;
        }

        public Dictionary<string, string> ListAttr()
        {
            var pairs = new List<string>();
            NativeMethods.MXSymbolListAttrShallow(GetHandle(), out var out_size, pairs.ToArray());
            var dict = new Dictionary<string, string>();
            var i = 0;
            while (i < out_size) dict[pairs[i * 2]] = pairs[i * 2 + 1];

            return dict;
        }

        public Dictionary<string, Dictionary<string, string>> AttrDict()
        {
            var pairs = new List<string>();
            NativeMethods.MXSymbolListAttr(GetHandle(), out var out_size, pairs.ToArray());
            var dict = new Dictionary<string, Dictionary<string, string>>();
            var i = 0;
            while (i < out_size)
            {
                var keys = pairs[i * 2].Split('$');
                dict[keys[0]] = new Dictionary<string, string>();
                dict[keys[0]][keys[1]] = pairs[i * 2 + 1];
            }

            return dict;
        }

        public void SetAttr(Dictionary<string, string> attrs)
        {
            foreach (var attr in attrs) NativeMethods.MXSymbolSetAttr(GetHandle(), attr.Key, attr.Value);
        }

        public virtual Symbol Reshape(Shape shape, bool reverse = false)
        {
            return sym.Reshape(this, shape, reverse);
        }

        public virtual Symbol Reshape(params int[] shape)
        {
            //int[] targetShape = new int[shape.Length];
            //long prod = -1 * shape.Aggregate(1L, (a, b) => a * b);
            //for (int i = 0; i < targetShape.Length; i++)
            //{
            //    if (shape[i] > 0)
            //    {
            //        targetShape[i] = shape[i];
            //    }
            //    else
            //    {
            //        targetShape[i] = Size / (int)prod;
            //    }
            //}

            return Reshape(new Shape(shape));
        }

        #region Overrides

        #region Operators

        public static Symbol operator +(Symbol lhs, Symbol rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            lhs.ThrowIfDisposed();
            rhs.ThrowIfDisposed();

            return OperatorSupply.Plus(lhs, rhs);
        }

        public static Symbol operator -(Symbol lhs, Symbol rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            lhs.ThrowIfDisposed();
            rhs.ThrowIfDisposed();

            return OperatorSupply.Minus(lhs, rhs);
        }

        public static Symbol operator *(Symbol lhs, Symbol rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            lhs.ThrowIfDisposed();
            rhs.ThrowIfDisposed();

            return OperatorSupply.Mul(lhs, rhs);
        }

        public static Symbol operator /(Symbol lhs, Symbol rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            lhs.ThrowIfDisposed();
            rhs.ThrowIfDisposed();

            return OperatorSupply.Div(lhs, rhs);
        }

        public static Symbol operator %(Symbol lhs, Symbol rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            lhs.ThrowIfDisposed();
            rhs.ThrowIfDisposed();

            return OperatorSupply.Mod(lhs, rhs);
        }

        public static Symbol operator +(Symbol lhs, float scalar)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            lhs.ThrowIfDisposed();

            return OperatorSupply.PlusScalar(lhs, scalar);
        }

        public static Symbol operator +(float lhs, Symbol rhs)
        {
            return rhs + lhs;
        }

        public static Symbol operator -(Symbol lhs, float scalar)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            lhs.ThrowIfDisposed();

            return OperatorSupply.MinusScalar(lhs, scalar);
        }

        public static Symbol operator -(float lhs, Symbol rhs)
        {
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            rhs.ThrowIfDisposed();

            return OperatorSupply.RMinusScalar(lhs, rhs);
        }

        public static Symbol operator *(Symbol lhs, float scalar)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            lhs.ThrowIfDisposed();

            return OperatorSupply.MulScalar(lhs, scalar);
        }

        public static Symbol operator *(float lhs, Symbol rhs)
        {
            return rhs * lhs;
        }

        public static Symbol operator /(Symbol lhs, float scalar)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            lhs.ThrowIfDisposed();

            return OperatorSupply.DivScalar(lhs, scalar);
        }

        public static Symbol operator /(float lhs, Symbol rhs)
        {
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            rhs.ThrowIfDisposed();

            return OperatorSupply.RDivScalar(lhs, rhs);
        }

        public static Symbol operator %(Symbol lhs, float scalar)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            lhs.ThrowIfDisposed();

            return OperatorSupply.ModScalar(lhs, scalar);
        }

        public static Symbol operator %(float lhs, Symbol rhs)
        {
            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            rhs.ThrowIfDisposed();

            return OperatorSupply.RModScalar(lhs, rhs);
        }

        #endregion

        #endregion

        #region Overrides

        protected override void DisposeUnmanaged()
        {
            base.DisposeUnmanaged();
            NativeMethods.MXSymbolFree(NativePtr);
        }

        #endregion

        #endregion
    }
}