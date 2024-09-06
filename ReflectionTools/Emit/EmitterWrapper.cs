using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using System.Diagnostics.SymbolStore;
#endif

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Allows for creating easy wrappers around <see cref="IOpCodeEmitter"/> to add extra methods in a certain context.
/// </summary>
/// <remarks><paramref name="disallowedOpCodes"/> is expected to be readonly and won't be copied for performance reasons.</remarks>
public class EmitterWrapper(IOpCodeEmitter underlying, HashSet<OpCode>? disallowedOpCodes) : IOpCodeEmitter
{
    private readonly IOpCodeEmitter _emitter = underlying is EmitterWrapper wrapper ? wrapper._emitter : underlying;
    private readonly HashSet<OpCode>? _disallowedOpCodes = disallowedOpCodes;

#if NET40_OR_GREATER || !NETFRAMEWORK
    /// <inheritdoc />
    public int ILOffset => _emitter.ILOffset;
#endif

    /// <summary>
    /// If any instructions may have been emitted. False-positives are possible depending on the implementation of the underlying emitter.
    /// </summary>
    public bool HasEmittedAnything { get; private set; }
    
    /// <summary>
    /// If any instructions may have been emitted that would change the stack. False-positives are possible depending on the implementation of the underlying emitter.
    /// </summary>
    public bool HasEmittedStackChanges { get; private set; }

    /// <summary>
    /// Convert an emitter that could possibly be a wrapper to it's underlying emitter if it is.
    /// </summary>
    public static void Reduce(ref IOpCodeEmitter emitter)
    {
        if (emitter is EmitterWrapper wrapper)
            emitter = wrapper._emitter;
        else if (emitter is DebuggableEmitter { Generator: EmitterWrapper w } debug)
        {
            IOpCodeEmitter other = w;
            Reduce(ref other);
            emitter = debug.GetEmitterWithDifferentGenerator(other);
        }
    }

    /// <summary>
    /// If an opcode isn't allowed to be used in this wrapper.
    /// </summary>
    protected bool IsDisallowed(OpCode opcode)
    {
        return _disallowedOpCodes != null && disallowedOpCodes.Contains(opcode);
    }

    private void AssertAllowedOpcode(OpCode opcode)
    {
        if (_disallowedOpCodes != null && IsDisallowed(opcode))
            throw new NotSupportedException("The instruction \"" + opcode.Name + "\" can not be used in this context.");
    }

    /// <inheritdoc />
    public virtual void Comment(string comment)
    {
        _emitter.Comment(comment);
        HasEmittedAnything = true;
    }

    /// <inheritdoc />
    public virtual void BeginCatchBlock(Type? exceptionType)
    {
        _emitter.BeginCatchBlock(exceptionType);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void BeginExceptFilterBlock()
    {
        _emitter.BeginExceptFilterBlock();
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual Label? BeginExceptionBlock()
    {
        Label? lbl = _emitter.BeginExceptionBlock();
        HasEmittedAnything = true;
        return lbl;
    }

    /// <inheritdoc />
    public virtual void BeginFaultBlock()
    {
        _emitter.BeginFaultBlock();
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void BeginFinallyBlock()
    {
        _emitter.BeginFinallyBlock();
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void BeginScope()
    {
        _emitter.BeginScope();
        HasEmittedAnything = true;
    }

    /// <inheritdoc />
    public virtual LocalBuilder DeclareLocal(Type localType)
    {
        LocalBuilder lcl = _emitter.DeclareLocal(localType);
        HasEmittedAnything = true;
        return lcl;
    }

    /// <inheritdoc />
    public virtual LocalBuilder DeclareLocal(Type localType, bool pinned)
    {
        LocalBuilder lcl = _emitter.DeclareLocal(localType, pinned);
        HasEmittedAnything = true;
        return lcl;
    }

    /// <inheritdoc />
    public virtual Label DefineLabel()
    {
        Label lbl = _emitter.DefineLabel();
        HasEmittedAnything = true;
        return lbl;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, byte arg)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, arg);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, double arg)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, arg);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, float arg)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, arg);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, int arg)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, arg);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, long arg)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, arg);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, sbyte arg)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, arg);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, short arg)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, arg);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, string str)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, str);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, ConstructorInfo con)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, con);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, Label label)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, label);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, Label[] labels)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, labels);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, LocalBuilder local)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, local);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, SignatureHelper signature)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, signature);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, FieldInfo field)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, field);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, MethodInfo meth)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, meth);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, Type cls)
    {
        AssertAllowedOpcode(opcode);
        _emitter.Emit(opcode, cls);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        AssertAllowedOpcode(opcode);
        _emitter.EmitCall(opcode, methodInfo, optionalParameterTypes);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[]? optionalParameterTypes)
    {
        AssertAllowedOpcode(opcode);
        _emitter.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

#if !NETSTANDARD || NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
    {
        AssertAllowedOpcode(opcode);
        _emitter.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }
#endif

    /// <inheritdoc />
    public virtual void EmitWriteLine(string value)
    {
        AssertAllowedOpcode(OpCodes.Ldstr);
        AssertAllowedOpcode(OpCodes.Call);
        _emitter.EmitWriteLine(value);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void EmitWriteLine(LocalBuilder localBuilder)
    {
        AssertAllowedOpcode(OpCodes.Ldloc);
        AssertAllowedOpcode(OpCodes.Ldloc_S);
        AssertAllowedOpcode(OpCodes.Ldloc_0);
        AssertAllowedOpcode(OpCodes.Ldloc_1);
        AssertAllowedOpcode(OpCodes.Ldloc_2);
        AssertAllowedOpcode(OpCodes.Ldloc_3);
        AssertAllowedOpcode(OpCodes.Callvirt);
        AssertAllowedOpcode(OpCodes.Call);
        _emitter.EmitWriteLine(localBuilder);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void EmitWriteLine(FieldInfo fld)
    {
        AssertAllowedOpcode(OpCodes.Ldfld);
        AssertAllowedOpcode(OpCodes.Ldsfld);
        AssertAllowedOpcode(OpCodes.Ldarg_0);
        AssertAllowedOpcode(OpCodes.Callvirt);
        AssertAllowedOpcode(OpCodes.Call);
        _emitter.EmitWriteLine(fld);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void EndExceptionBlock()
    {
        _emitter.EndExceptionBlock();
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void EndScope()
    {
        _emitter.EndScope();
        HasEmittedAnything = true;
    }

    /// <inheritdoc />
    public virtual void MarkLabel(Label loc)
    {
        _emitter.MarkLabel(loc);
        HasEmittedAnything = true;
    }

    /// <inheritdoc />
    public virtual void ThrowException(Type excType)
    {
        AssertAllowedOpcode(OpCodes.Newobj);
        AssertAllowedOpcode(OpCodes.Throw);
        _emitter.ThrowException(excType);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }

    /// <inheritdoc />
    public virtual void UsingNamespace(string usingNamespace)
    {
        _emitter.UsingNamespace(usingNamespace);
        HasEmittedAnything = true;
    }

#if NETFRAMEWORK
    /// <inheritdoc />
    public virtual void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
    {
        _emitter.MarkSequencePoint(document, startLine, startColumn, endLine, endColumn);
        HasEmittedAnything = true;
    }

    /// <inheritdoc />
    void _ILGenerator.GetIDsOfNames(ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        => _emitter.GetIDsOfNames(ref riid, rgszNames, cNames, lcid, rgDispId);

    /// <inheritdoc />
    void _ILGenerator.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        => _emitter.GetTypeInfo(iTInfo, lcid, ppTInfo);

    /// <inheritdoc />
    void _ILGenerator.GetTypeInfoCount(out uint pcTInfo)
        => _emitter.GetTypeInfoCount(out pcTInfo);

    /// <inheritdoc />
    void _ILGenerator.Invoke(uint dispIdMember, ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
    {
        _emitter.Invoke(dispIdMember, ref riid, lcid, wFlags, pDispParams, pVarResult, pExcepInfo, puArgErr);
        HasEmittedAnything = true;
        HasEmittedStackChanges = true;
    }
#endif
}
