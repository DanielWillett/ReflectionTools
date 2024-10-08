﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using System.Diagnostics.SymbolStore;
#endif
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Extensions for <see cref="IOpCodeEmitter"/> objects.
/// </summary>
public static class OpCodeEmitters
{
    /// <summary>
    /// Extension method to get a <see cref="IOpCodeEmitter"/>.
    /// </summary>
    /// <param name="generator"><see cref="ILGenerator"/> to wrap.</param>
    /// <param name="debuggable">Shows debug logging as the method generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the method executes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public static IOpCodeEmitter AsEmitter(this ILGenerator generator, bool debuggable = false, bool addBreakpoints = false)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter((ILGeneratorEmitter)generator, null) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)generator;

    /// <summary>
    /// Extension method to get a <see cref="IOpCodeEmitter"/>.
    /// </summary>
    /// <param name="dynMethod">Dynamic method.</param>
    /// <param name="debuggable">Shows debug logging as the method generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the method executes.</param>
    /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public static IOpCodeEmitter AsEmitter(this DynamicMethod dynMethod, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter(dynMethod) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)dynMethod.GetILGenerator(streamSize);

    /// <summary>
    /// Extension method to get a <see cref="IOpCodeEmitter"/>.
    /// </summary>
    /// <param name="methodBuilder">Dynamic method builder.</param>
    /// <param name="debuggable">Shows debug logging as the method generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the method executes.</param>
    /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public static IOpCodeEmitter AsEmitter(this MethodBuilder methodBuilder, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter(methodBuilder) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)methodBuilder.GetILGenerator(streamSize);

    /// <summary>
    /// Extension method to get a <see cref="IOpCodeEmitter"/>.
    /// </summary>
    /// <param name="constructorBuilder">Dynamic constructor builder.</param>
    /// <param name="debuggable">Shows debug logging as the constructor generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the constructor executes.</param>
    /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public static IOpCodeEmitter AsEmitter(this ConstructorBuilder constructorBuilder, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter(constructorBuilder) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)constructorBuilder.GetILGenerator(streamSize);

    /// <summary>
    /// For emitters that support it (implement <see cref="IOpCodeEmitterLogSource"/>), sets the log source to <paramref name="source"/>.
    /// </summary>
    /// <remarks>A reference to <paramref name="emitter"/> for chaining.</remarks>
    [EmitBehavior]
    public static IOpCodeEmitter WithLogSource(this IOpCodeEmitter emitter, string source)
    {
        if (emitter is IOpCodeEmitterLogSource logSrc)
            logSrc.LogSource = source;

        return emitter;
    }

    /// <summary>
    /// Check if an emitter implements one of it's implementing interfaces like <see cref="IRootOpCodeEmitter"/>.
    /// </summary>
    public static bool IsEmitterType<TEmitter>(this IOpCodeEmitter emitter) where TEmitter : IOpCodeEmitter
    {
        return emitter is TEmitter or DebuggableEmitter { Generator: TEmitter };
    }

    /// <summary>
    /// Check if an emitter implements one of it's implementing interfaces like <see cref="IRootOpCodeEmitter"/>.
    /// </summary>
    public static bool IsEmitterType<TEmitter>(this IOpCodeEmitter emitter,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        [MaybeNullWhen(false)]
#endif
        out TEmitter typedEmitter) where TEmitter : IOpCodeEmitter
    {
        if (emitter is TEmitter t)
        {
            typedEmitter = t;
            return true;
        }
        if (emitter is DebuggableEmitter { Generator: TEmitter t2 })
        {
            typedEmitter = t2;
            return true;
        }

        typedEmitter = default!;
        return false;
    }
}

/// <summary>
/// Abstraction of <see cref="ILGenerator"/>.
/// </summary>
public interface IOpCodeEmitter
#if NETFRAMEWORK
    : _ILGenerator
#endif
{

#if NET40_OR_GREATER || !NETFRAMEWORK
    /// <summary>Gets the current offset, in bytes, in the Microsoft intermediate language (MSIL) stream that is being emitted by the <see cref="T:System.Reflection.Emit.ILGenerator" />.</summary>
    /// <returns>The offset in the MSIL stream at which the next instruction will be emitted.</returns>
    int ILOffset { get; }
#endif

    /// <summary>
    /// If the implementation supports it, adds a comment to the IL code.
    /// </summary>
    /// <remarks>Does not throw <see cref="NotSupportedException"/>.</remarks>
    [EmitBehavior]
    void Comment(string comment);

    /// <summary>Begins a catch block.</summary>
    /// <param name="exceptionType">The <see cref="T:System.Type" /> object that represents the exception.</param>
    /// <exception cref="T:System.ArgumentException">The catch block is within a filtered exception.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="exceptionType" /> is <see langword="null" />, and the exception filter block has not returned a value that indicates that finally blocks should be run until this catch block is located.</exception>
    /// <exception cref="T:System.NotSupportedException">The Microsoft intermediate language (MSIL) being generated is not currently in an exception block.</exception>
    [EmitBehavior]
    void BeginCatchBlock(Type? exceptionType);

    /// <summary>Begins an exception block for a filtered exception.</summary>
    /// <exception cref="T:System.NotSupportedException">The Microsoft intermediate language (MSIL) being generated is not currently in an exception block.
    /// -or-
    /// This <see cref="T:System.Reflection.Emit.ILGenerator" /> belongs to a <see cref="T:System.Reflection.Emit.DynamicMethod" />.</exception>
    [EmitBehavior]
    void BeginExceptFilterBlock();

    /// <summary>Begins an exception block for a non-filtered exception.</summary>
    /// <returns>The label for the end of the block, if the implementation supports it, otherwise <see langword="null"/>. This will leave you in the correct place to execute finally blocks or to finish the try.</returns>
    [EmitBehavior]
    Label? BeginExceptionBlock();

    /// <summary>Begins an exception fault block in the Microsoft intermediate language (MSIL) stream.</summary>
    /// <exception cref="T:System.NotSupportedException">The MSIL being generated is not currently in an exception block.
    /// -or-
    /// This <see cref="T:System.Reflection.Emit.ILGenerator" /> belongs to a <see cref="T:System.Reflection.Emit.DynamicMethod" />.</exception>
    [EmitBehavior]
    void BeginFaultBlock();

    /// <summary>Begins a finally block in the Microsoft intermediate language (MSIL) instruction stream.</summary>
    /// <exception cref="T:System.NotSupportedException">The MSIL being generated is not currently in an exception block.</exception>
    [EmitBehavior]
    void BeginFinallyBlock();

    /// <summary>Begins a lexical scope.</summary>
    /// <exception cref="T:System.NotSupportedException">This <see cref="T:System.Reflection.Emit.ILGenerator" /> belongs to a <see cref="T:System.Reflection.Emit.DynamicMethod" />.</exception>
    [EmitBehavior]
    void BeginScope();

    /// <summary>Declares a local variable of the specified type.</summary>
    /// <param name="localType">A <see cref="T:System.Type" /> object that represents the type of the local variable.</param>
    /// <returns>The declared local variable.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="localType" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">The containing type has been created by the <see cref="M:System.Reflection.Emit.TypeBuilder.CreateType" /> method.</exception>
    [EmitBehavior]
    LocalBuilder DeclareLocal(Type localType);

    /// <summary>Declares a local variable of the specified type, optionally pinning the object referred to by the variable.</summary>
    /// <param name="localType">A <see cref="T:System.Type" /> object that represents the type of the local variable.</param>
    /// <param name="pinned">
    /// <see langword="true" /> to pin the object in memory; otherwise, <see langword="false" />.</param>
    /// <returns>A <see cref="T:System.Reflection.Emit.LocalBuilder" /> object that represents the local variable.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="localType" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">The containing type has been created by the <see cref="M:System.Reflection.Emit.TypeBuilder.CreateType" /> method.
    /// -or-
    /// The method body of the enclosing method has been created by the <see cref="M:System.Reflection.Emit.MethodBuilder.CreateMethodBody(System.Byte[],System.Int32)" /> method.</exception>
    /// <exception cref="T:System.NotSupportedException">The method with which this <see cref="T:System.Reflection.Emit.ILGenerator" /> is associated is not represented by a <see cref="T:System.Reflection.Emit.MethodBuilder" />.</exception>
    [EmitBehavior]
    LocalBuilder DeclareLocal(Type localType, bool pinned);

    /// <summary>Declares a new label.</summary>
    /// <returns>A new label that can be used as a token for branching.</returns>
    [EmitBehavior]
    Label DefineLabel();

    /// <summary>Puts the specified instruction onto the stream of instructions.</summary>
    /// <param name="opcode">The Microsoft Intermediate Language (MSIL) instruction to be put onto the stream.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode);

    /// <summary>Puts the specified instruction and character argument onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be put onto the stream.</param>
    /// <param name="arg">The character argument pushed onto the stream immediately after the instruction.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, byte arg);

    /// <summary>Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be put onto the stream. Defined in the <see langword="OpCodes" /> enumeration.</param>
    /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, double arg);

    /// <summary>Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be put onto the stream.</param>
    /// <param name="arg">The <see langword="Single" /> argument pushed onto the stream immediately after the instruction.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, float arg);

    /// <summary>Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be put onto the stream.</param>
    /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, int arg);

    /// <summary>Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be put onto the stream.</param>
    /// <param name="arg">The numerical argument pushed onto the stream immediately after the instruction.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, long arg);

    /// <summary>Puts the specified instruction and character argument onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be put onto the stream.</param>
    /// <param name="arg">The character argument pushed onto the stream immediately after the instruction.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, sbyte arg);

    /// <summary>Puts the specified instruction and numerical argument onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="arg">The <see langword="Int" /> argument pushed onto the stream immediately after the instruction.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, short arg);

    /// <summary>Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given string.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="str">The <see langword="String" /> to be emitted.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, string str);

    /// <summary>Puts the specified instruction and metadata token for the specified constructor onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="con">A <see langword="ConstructorInfo" /> representing a constructor.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="con" /> is <see langword="null" />. This exception is new in the .NET Framework 4.</exception>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, ConstructorInfo con);

    /// <summary>Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream and leaves space to include a label when fixes are done.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="label">The label to which to branch from this location.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, Label label);

    /// <summary>Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream and leaves space to include a label when fixes are done.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="labels">The array of label objects to which to branch from this location. All of the labels will be used.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="labels" /> is <see langword="null" />. This exception is new in the .NET Framework 4.</exception>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, Label[] labels);

    /// <summary>Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the index of the given local variable.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="local">A local variable.</param>
    /// <exception cref="T:System.ArgumentException">The parent method of the <paramref name="local" /> parameter does not match the method associated with this <see cref="T:System.Reflection.Emit.ILGenerator" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="local" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="opcode" /> is a single-byte instruction, and <paramref name="local" /> represents a local variable with an index greater than <see langword="Byte.MaxValue" />.</exception>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, LocalBuilder local);

    /// <summary>Puts the specified instruction and a signature token onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="signature">A helper for constructing a signature token.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="signature" /> is <see langword="null" />.</exception>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, SignatureHelper signature);

    /// <summary>Puts the specified instruction and metadata token for the specified field onto the Microsoft intermediate language (MSIL) stream of instructions.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="field">A <see langword="FieldInfo" /> representing a field.</param>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, FieldInfo field);

    /// <summary>Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given method.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream.</param>
    /// <param name="meth">A <see langword="MethodInfo" /> representing a method.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="meth" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.NotSupportedException">
    /// <paramref name="meth" /> is a generic method for which the <see cref="P:System.Reflection.MethodBase.IsGenericMethodDefinition" /> property is <see langword="false" />.</exception>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, MethodInfo meth);

    /// <summary>Puts the specified instruction onto the Microsoft intermediate language (MSIL) stream followed by the metadata token for the given type.</summary>
    /// <param name="opcode">The MSIL instruction to be put onto the stream.</param>
    /// <param name="cls">A <see langword="Type" />.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="cls" /> is <see langword="null" />.</exception>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    void Emit(OpCode opcode, Type cls);

    /// <summary>Puts a <see langword="call" /> or <see langword="callvirt" /> instruction onto the Microsoft intermediate language (MSIL) stream to call a <see langword="varargs" /> method.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream. Must be <see cref="F:System.Reflection.Emit.OpCodes.Call" />, <see cref="F:System.Reflection.Emit.OpCodes.Callvirt" />, or <see cref="F:System.Reflection.Emit.OpCodes.Newobj" />.</param>
    /// <param name="methodInfo">The <see langword="varargs" /> method to be called.</param>
    /// <param name="optionalParameterTypes">The types of the optional arguments if the method is a <see langword="varargs" /> method; otherwise, <see langword="null" />.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="opcode" /> does not specify a method call.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="methodInfo" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">The calling convention for the method is not <see langword="varargs" />, but optional parameter types are supplied. This exception is thrown in the .NET Framework versions 1.0 and 1.1, In subsequent versions, no exception is thrown.</exception>
    /// <remarks>Recommended to use the extensions in <see cref="EmitterExtensions"/> instead of manually emitting codes.</remarks>
    [EmitBehavior(StackBehaviour.Varpop, StackBehaviour.Varpush)]
    void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes);

    /// <summary>Puts a <see cref="F:System.Reflection.Emit.OpCodes.Calli" /> instruction onto the Microsoft intermediate language (MSIL) stream, specifying a managed calling convention for the indirect call.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream. Must be <see cref="F:System.Reflection.Emit.OpCodes.Calli" />.</param>
    /// <param name="callingConvention">The managed calling convention to be used.</param>
    /// <param name="returnType">The <see cref="T:System.Type" /> of the result.</param>
    /// <param name="parameterTypes">The types of the required arguments to the instruction.</param>
    /// <param name="optionalParameterTypes">The types of the optional arguments for <see langword="varargs" /> calls.</param>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="optionalParameterTypes" /> is not <see langword="null" />, but <paramref name="callingConvention" /> does not include the <see cref="F:System.Reflection.CallingConventions.VarArgs" /> flag.</exception>
    [EmitBehavior(StackBehaviour.Varpop, StackBehaviour.Varpush)]
    void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[]? optionalParameterTypes);

#if !NETSTANDARD || NETSTANDARD2_1_OR_GREATER
    /// <summary>Puts a <see cref="F:System.Reflection.Emit.OpCodes.Calli" /> instruction onto the Microsoft intermediate language (MSIL) stream, specifying an unmanaged calling convention for the indirect call.</summary>
    /// <param name="opcode">The MSIL instruction to be emitted onto the stream. Must be <see cref="F:System.Reflection.Emit.OpCodes.Calli" />.</param>
    /// <param name="unmanagedCallConv">The unmanaged calling convention to be used.</param>
    /// <param name="returnType">The <see cref="T:System.Type" /> of the result.</param>
    /// <param name="parameterTypes">The types of the required arguments to the instruction.</param>
    [EmitBehavior(StackBehaviour.Varpop, StackBehaviour.Varpush)]
    void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes);
#endif

    /// <summary>Emits the Microsoft intermediate language (MSIL) to call <see cref="Console.WriteLine(string)" /> with a string.</summary>
    /// <param name="value">The string to be printed.</param>
    [EmitBehavior]
    void EmitWriteLine(string value);

    /// <summary>Emits the Microsoft intermediate language (MSIL) necessary to call <see cref="Console.WriteLine(object)" /> with the given local variable.</summary>
    /// <param name="localBuilder">The local variable whose value is to be written to the console.</param>
    /// <exception cref="T:System.ArgumentException">The type of <paramref name="localBuilder" /> is <see cref="T:System.Reflection.Emit.TypeBuilder" /> or <see cref="T:System.Reflection.Emit.EnumBuilder" />, which are not supported.
    /// -or-
    /// There is no overload of <see cref="Console.WriteLine(object)" /> that accepts the type of <paramref name="localBuilder" />.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="localBuilder" /> is <see langword="null" />.</exception>
    [EmitBehavior]
    void EmitWriteLine(LocalBuilder localBuilder);

    /// <summary>Emits the Microsoft intermediate language (MSIL) necessary to call <see cref="Console.WriteLine(object)" /> with the given field.</summary>
    /// <param name="fld">The field whose value is to be written to the console.</param>
    /// <exception cref="T:System.ArgumentException">There is no overload of the <see cref="Console.WriteLine(object)" /> method that accepts the type of the specified field.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="fld" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.NotSupportedException">The type of the field is <see cref="T:System.Reflection.Emit.TypeBuilder" /> or <see cref="T:System.Reflection.Emit.EnumBuilder" />, which are not supported.</exception>
    [EmitBehavior]
    void EmitWriteLine(FieldInfo fld);

    /// <summary>Ends an exception block.</summary>
    /// <exception cref="T:System.InvalidOperationException">The end exception block occurs in an unexpected place in the code stream.</exception>
    /// <exception cref="T:System.NotSupportedException">The Microsoft intermediate language (MSIL) being generated is not currently in an exception block.</exception>
    [EmitBehavior]
    void EndExceptionBlock();

    /// <summary>Ends a lexical scope.</summary>
    /// <exception cref="T:System.NotSupportedException">This <see cref="T:System.Reflection.Emit.ILGenerator" /> belongs to a <see cref="T:System.Reflection.Emit.DynamicMethod" />.</exception>
    [EmitBehavior]
    void EndScope();

    /// <summary>Marks the Microsoft intermediate language (MSIL) stream's current position with the given label.</summary>
    /// <param name="loc">The label for which to set an index.</param>
    /// <exception cref="T:System.ArgumentException">
    ///         <paramref name="loc" /> represents an invalid index into the label array.
    /// -or-
    /// An index for <paramref name="loc" /> has already been defined.</exception>
    [EmitBehavior(SpecialBehavior = EmitSpecialBehavior.MarksLabel)]
    void MarkLabel(Label loc);

#if NETFRAMEWORK
    /// <summary>Marks a sequence point in the Microsoft intermediate language (MSIL) stream.</summary>
    /// <param name="document">The document for which the sequence point is being defined.</param>
    /// <param name="startLine">The line where the sequence point begins. 1-indexed.</param>
    /// <param name="startColumn">The column in the line where the sequence point begins. 0-indexed.</param>
    /// <param name="endLine">The line where the sequence point ends. 1-indexed.</param>
    /// <param name="endColumn">The column in the line where the sequence point ends. 0-indexed.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startLine" /> or <paramref name="endLine" /> is &lt;= 0.</exception>
    /// <exception cref="T:System.NotSupportedException">This <see cref="T:System.Reflection.Emit.ILGenerator" /> belongs to a <see cref="T:System.Reflection.Emit.DynamicMethod" />.</exception>
    void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn);
#endif

    /// <summary>Emits an instruction to throw an exception.</summary>
    /// <param name="excType">The class of the type of exception to throw.</param>
    /// <exception cref="T:System.ArgumentException">
    ///         <paramref name="excType" /> is not the <see cref="T:System.Exception" /> class or a derived class of <see cref="T:System.Exception" />.
    /// -or-
    /// The type does not have a default constructor.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="excType" /> is <see langword="null" />.</exception>
    [EmitBehavior(SpecialBehavior = EmitSpecialBehavior.TerminatesBranch)]
    void ThrowException(Type excType);

    /// <summary>Specifies the namespace to be used in evaluating locals and watches for the current active lexical scope.</summary>
    /// <param name="usingNamespace">The namespace to be used in evaluating locals and watches for the current active lexical scope</param>
    /// <exception cref="T:System.ArgumentException">Length of <paramref name="usingNamespace" /> is zero.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="usingNamespace" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.NotSupportedException">This <see cref="T:System.Reflection.Emit.ILGenerator" /> belongs to a <see cref="T:System.Reflection.Emit.DynamicMethod" />.</exception>
    [EmitBehavior]
    void UsingNamespace(string usingNamespace);
}

/// <summary>
/// An <see cref="IOpCodeEmitter"/> that supports a log source.
/// </summary>
public interface IOpCodeEmitterLogSource : IOpCodeEmitter
{
    /// <summary>
    /// Source to show when debug logging.
    /// </summary>
    string LogSource { get; set; }
}