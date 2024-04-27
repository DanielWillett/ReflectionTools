Shared library for generic reflection tools for CLR implementations. Tested on .NET Framework, .NET, and Mono.

NuGet Package: [DanielWillett.ReflectionTools](https://www.nuget.org/packages/DanielWillett.ReflectionTools)

# Reflection Tools
Base module for ReflectionTools.

## Dependency Injection
Use the `AddReflectionTools` extension for `IServiceCollection` to add `IReflectionToolsLogger`, `IOpCodeFormatter`, and `IAccessor` as services.
Configures logging from the registered `ILoggerFactory` service.

## Formatting
`Accessor.Formatter` has methods for formatting members into strings efficiently and accurately. Can be swapped out for custom implementations made either from scratch or derived from `DefaultOpCodeFormatter`.

Formatting methods, fields, properties, types, parameters, and `OpCode`s.
```cs
public class C
{
    public static void M<T>(scoped in T p, params int[] p2) where T : struct { }
}

// elsewhere

MethodInfo method = typeof(C).GetMethod("M", BindingFlags.Public | BindingFlags.Static);

string methodAsString = Accessor.Formatter.Format(method);

/*
 * Value: 'static void C.M<T>(scoped in T p, params int[] p2)'
 */
```

Formatting member definitions
```cs
MethodDefinition method = new MethodDefinition("M")
            .DeclaredIn<C>(isStatic: false)
            .WithGenericParameterDefinition("T")
            .WithParameter<int>("p")
            .ReturningUsingGeneric("T",
                elements: builder =>
                {
                    builder.AddArray(2);
                }
            );

string methodAsString = Accessor.Formatter.Format(method);

/*
 * Value: 'T[,] C.M<T>(int p)'
 */
```

See also `Accessor.ExceptionFormatter`, which is used for exceptions.

## StopwatchExtensions
Contains an extension method for getting the milliseconds elapsed in a decimal form (not easily doable through normal methods) named `GetElapsedMilliseconds`.
```cs
Stopwatch sw = Stopwatch.StartNew();
// do stuff
sw.Stop();

Console.WriteLine($"Elapsed time: {sw.GetElapsedMilliseconds():F2} ms");
```


## Accessor
Expandable class filled with utilities for reflection.

### Generator Methods
Access private members quickly and easily using delegates.
* GenerateInstanceSetter
* GenerateInstanceGetter
* GenerateInstancePropertySetter
* GenerateInstancePropertyGetter
* GenerateStaticSetter
* GenerateStaticGetter
* GenerateStaticPropertySetter
* GenerateStaticPropertyGetter
* GenerateInstanceCaller
* GenerateStaticCaller

Getting the value of static properties.
```cs
public class C
{
    private static int P { get; private set; }
}

// elsewhere

StaticGetter<int> getter = Accessor.GenerateStaticPropertyGetter<C, int>("P", throwOnError: true)!;
int value = getter();
```

Setting instance fields, where the value and instance must be boxed.
```cs
internal struct C
{
    private C F;
}

// elsewhere

// creates a delegate that accesses field 'F', working with boxed instances and values.
Type privateStruct = Type.GetType("C, A");
InstanceSetter<object, object?> setter = Accessor.GenerateInstanceSetter<object?>(privateStruct, "F", throwOnError: true)!;
object instance = Activator.CreateInstance(privateStruct);
object value = Activator.CreateInstance(privateStruct);
setter(instance, value);
```

Calling private methods.
```cs
public class C
{
    private int M() { /* ... */ }
}

Func<C, int> caller = Accessor.GenerateInstanceCaller<C, Func<C, int>>("M", throwOnError: true, parameters: Type.EmptyTypes)

C instance = new C();
int returnValue = caller(instance);
```

### Other Utilities in Accessor
* Generator Methods
* GetVisibility
  - Returns a simplified enum-style visibility for members.
  - GetHighestVisibility
    + Returns the most visible method among the list of methods, used for accessors usually.
* AssemblyGivesInternalAccess
  - Check if an assembly has the InternalsVisibleToAttribute for a given assembly name.
* IsExtern
  - Check if a member is defined as `extern`.
* IsDefinedSafe
* HasAttributeSafe
* GetAttributeSafe
* GetAttributesSafe
* TryGetAttributeSafe
  - Error-safe, optionally generic, methods for looking for attributes on members.
* HasCompilerAttributeSafe
* IsCompilerAttributeDefinedSafe
  - Check if a compiler-generated attribute is defined that may not be available in the given runtime, like `IsByRefLikeAttribute`.
* IsReadOnly
  - Checks if a field, struct, or method is readonly.
* IsByRefLikeType
  - Checks if a struct is a ref struct.
* IsIgnored
  - Checks if a member has the `IgnoreAttribute`.
* GetPriority
  - Gets the priority of a member from the `PriorityAttribute`, defaulting to 0.
* GetMethod
  - Easily get a method from a method group using implicit delegate casting.
* GetDefaultDelegate
  - Get the default variant of `Action` or `Func` for the given parameter info.
* ForEachBaseType
  - Executes a callback for each base type, optionally including `object`.
* GetTypesSafe
  - Gets a list of all types in the given assembly, properly catching and handling `ReflectionTypeLoadException`.
* GetImplementedMethod
  - Given an interface method, gets the implementation in the parent type.
* GetDelegateSignature
  - Get signature information about a delegate type.
* GetReturnType
  - Gets the return type of a delegate type.
* GetReturnParameter
  - Gets the return parameter of a delegate type.
* GetParameters
  - Gets the parameters of a delegate type.
* GetInvokeMethod
  - Gets the `Invoke(...)` method of a delegate type.
* GetMemberType
  - Gets the generic 'type' of a member, ex. `FieldType`, `ReturnType`, `PropertyType`, etc.
* GetIsStatic
  - Checks if a member is static, be it a type, method, field, property, etc.
* ShouldCallvirtRuntime
  - Decide if a method should be `callvirt`'d instead of `call`'d at runtime. Doesn't account for future changes.
* ShouldCallvirt
  - Decide if a method should be `callvirt`'d instead of `call`'d.
* GetCallRuntime
  - Extension method for getting the proper call for a method at runtime. Doesn't account for future changes.
* GetCall
  - Extension method for getting the proper call for a method.
* [Try]GetUnderlyingArray
  - Quickly get the underlying array of a list.
* [Try]GetListVersion
  - Quickly get the underlying version of a list.
* CouldBeAssignedTo
  - If it's possible that an object of a type could be in a variable of another type.
* AsEmitter
  - Creates a generic `IOpCodeEmitter` from an `ILGenerator`, `DynamicMethod`, or `MethodBuilder`.

## Variables
`IVariable` abstraction for fields and properties.

Both `IAccessor` and `Variables` contain methods for getting variables. The ones in `IAccessor` should be used in a DI environment, otherwise the ones in `Variables` will do.

```cs
// looks for an instance variable named "F" in class "C", returning a type-safe variable
IInstanceVariable<C, int>? variable = Variables.FindInstance<C, int>("F");

// looks for a static variable named "F" in class "C", returning a type-safe variable
IStaticVariable<int>? variable = Variables.FindStatic<C, int>("F");

// looks for a variable named "F" in class "C"
IVariable? variable = Variables.Find<C, int>("F");
```

## OpCode Emitters
`IOpCodeEmitter` is an abstraction based off `ILGenerator`.

`DebuggableEmitter` implements this interface fully and logs any emitted instructions to `Accessor.Logger` (or the specified accessor).

It also has tools for adding 'breakpoints' to the method, which log every instruction to the logger as it executes in real-time.

```cs
DynamicMethod dynMethod = new DyanmicMethod(...);

IOpCodeEmitter emitter = dynMethod
                            .AsEmitter(debuggable: true, addBreakpoints: false)
                            .WithLogSource("...");

emitter.Emit(OpCodes.Ldarg_0);
// ...
emitter.Emit(OpCodes.Ret);
```
*TranspileContext from [DanielWillett.ReflectionTools.Harmony](https://www.nuget.org/packages/DanielWillett.ReflectionTools.Harmony) also implements `IOpCodeEmitter`.*

# Tools for [Lib.Harmony](https://www.nuget.org/packages/Lib.Harmony) 2.3.3+
Requires `Lib.Harmony 2.3.3+`.
NuGet Package: [DanielWillett.ReflectionTools.Harmony](https://www.nuget.org/packages/DanielWillett.ReflectionTools.Harmony)
Relies on [DanielWillett.ReflectionTools](https://www.nuget.org/packages/DanielWillett.ReflectionTools)

[Lib.Harmony](https://www.nuget.org/packages/Lib.Harmony) module for ReflectionTools. In versions before 3.0.0-prerelease1, this was part of the primary module.

## HarmonyLog

`HarmonyLog` helps you keep an auto-clearing file log:
```cs
public static void Main(string[] args)
{
    // reset the log on startup and configure Harmony to use the log.

    string logFilePath = Path.Combine(Environment.CurrentDirectory, "harmony.log");
    HarmonyLog.Reset(logFilePath);
}
```

It will be cleared on startup (not deleted, allowing any file editors to stay open).

You can also use `HarmonyLog.ResetConditional`, which is ignored if the compiler flag `REFLECTION_TOOLS_ENABLE_HARMONY_LOG` is not defined.

## PatchUtility
`PatchUtility` contains many helper methods for transpiling with a `List<CodeInstruction>` or a `TranspilerContext` object.
* ContinueUntil/ContinueWhile
  - Skips instructions until/while a given pattern matches.
* CopyWithoutSpecial
  - Copies an instruction without labels or blocks.
* FindLabelDestinationIndex
  - Finds an instruction with the given label.
* FollowPattern
  - Advances the current index to directly after a matched pattern.
* GetLocal
  - Gets the `LocalBuilder` or index of the local variable in an instruction.
* GetLocalIndex
  - Gets the index of the local variable in an instruction.
* GetNextBranchTarget
  - Get the label of the next branch instruction.
* IsBeginBlockType
  - Extension method for `ExceptionBlockType`, returning whether the type starts an exception block.
* IsEndBlockType
  - Extension method for `ExceptionBlockType`, returning whether the type ends an exception block.
* LabelNext[OrReturn]
  - Get or add a label to the next instruction that matches a pattern.
* LoadConstantI4
  - Gets a code instruction that loads an int32 constant, using shorter forms when possible.
* MatchPattern
  - Matches a set of delegates to the current instruction index.
* MoveBlocksAndLabels
  - Cut and pastes all labels and blocks to the target instruction.
* [Try]RemovePattern
  - Removes the next match to a given set of patterns.
* ReturnIfFalse
  - Takes a static function and inserts instructions calling that function and returning or branching if it returns false.
* Throw
  - Returns a list of instructions that throw an error with an optional message.
* TransferStartingInstructionNeeds
  - Moves instructions that would need to stay at the start of a logical instruction block from one instruction to another.
* TransferEndingInstructionNeeds
  - Moves instructions that would need to stay at the end of a logical instruction block from one instruction to another.
* WithStartBlocksFrom
  - Chainable version of TransferStartingInstructionNeeds
* WithEndBlocksFrom
  - Chainable version of TransferEndingInstructionNeeds

## TranspilerContext
`TranspilerContext` can be used with `PatchUtility` in transpilers to simplify modifying methods and fetching existing members with reflection.

Partially implements `IOpCodeEmitter`.

The following transpiler replaces "Test1" and "Test2" with "test source" and "test message".
```cs
public void TranspilerTarget()
{
    if (1 == int.Parse("2"))
        return;
    Accessor.Logger!.LogInfo("Test1", "Test2");
}

private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
{
    TranspileContext ctx = new TranspileContext(method, generator, instructions);

    MethodInfo? logInfo = typeof(IReflectionToolsLogger).GetMethod("LogInfo", BindingFlags.Public | BindingFlags.Instance, null, [ typeof(string), typeof(string) ], null);
    if (logInfo == null)
    {
        // ctx.Fail returns the original instructions set.
        // you could also throw an exception here instead of returning.
        return ctx.Fail(new MethodDefinition("LogInfo")
            .DeclaredIn<IReflectionToolsLogger>(false)
            .WithParameter<string>("source")
            .WithParameter<string>("message")
        );
    }

    MethodInfo? getLogger = typeof(Accessor).GetProperty("Logger", BindingFlags.Public | BindingFlags.Static)?.GetMethod;
    if (getLogger == null)
    {
        return ctx.Fail(new PropertyDefinition("Logger")
            .DeclaredIn(typeof(Accessor), true)
            .WithNoSetter()
        );
    }

    while (ctx.MoveNext())
    {
        if (PatchUtility.TryRemovePattern(ctx,
                out BlockInfo block,
                null,
                x => x.LoadsConstant("Test1"),
                x => x.LoadsConstant("Test2"),
                x => x.Calls(logInfo)
            ))
        {
            ctx.Emit(getLogger.GetCallRuntime(), getLogger)
                // transfer labels and start blocks from old first instruction
                .SetupBlockStart(in block);

            ctx.Emit(OpCodes.Ldstr, "test source");

            ctx.Emit(OpCodes.Ldstr, "test message");

            ctx.Emit(logInfo.GetCallRuntime(), logInfo)
                // transfer end blocks from old last instruction
                .SetupBlockEnd(in block);

            ctx.LogDebug("Patched arguments to LogInfo.");
        }
    }

    return ctx;
}
```