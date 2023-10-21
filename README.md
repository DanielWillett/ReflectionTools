Shared library for generic reflection tools for CLR implementations. Tested on .NET Framework and Mono.

Requires `HarmonyLib 2.2.2+`.

NuGet Package: [DanielWillett.ReflectionTools](https://www.nuget.org/packages/DanielWillett.ReflectionTools)

### Accessor
* `IsMono` property
* Generating static or instance setters and getters for fields and properties.
* Generating static or instance callers for private methods.
* Extensions:
    * `IsExtern`
    * `IsIgnored` (using a new IgnoredAttribute)
    * `GetPriority` (using a new PriorityAttribute)
* `GetMethod` method for easily converting a static method to a MethodInfo.
* `ForEachBaseType` for iterating through each type in a type's hierarchy.
* `GetTypesSafe` - `Assembly.GetTypes` but it won't throw an exception and will sort by PriorityAttribute.
* `GetImplementedMethod` for getting the implementation of an interface method (even explicitly implemented).
* `GetDelegateSignature` and similar to get parameters, return type, and `Invoke` method of a delegate type.

### EmitUtility
Various tools for Harmony patching.
* Generic `Throw` method for Harmony transpilers with optional message.
* Methods to use with for loop + `List` Harmony transpiling.
    * `MatchPattern`
    * `FollowPattern`
    * `RemovePattern`
    * `ReturnIfFalse`
    * `ContinueUntil`
    * `ContinueWhile`
    * `LabelNext`
    * `LabelNextOrReturn`
    * `GetNextBranchTarget`
    * `FindLabelDestinationIndex`
    * `GetLabelId`
    * `EmitArgument`
    * `GetLocalIndex`
    * `LoadConstantI4`
    * `GetParameter`
    * `EmitParameter`
    * `GetLocal`
* Methods for `CodeInstruction` manipulation
    * `CopyWithoutSpecial`
    * `TransferStartingInstructionNeeds`
    * `TransferEndingInstructionNeeds`
    * `MoveBlocksAndLabels`
    * `IsBeginBlockType`
    * `IsEndBlockType`
* Extensions for `OpCode` matching
    * `IsOfType`
    * `IsStLoc`
    * `IsLdLoc`
    * `IsStArg`
    * `IsLdArg`
    * `IsBr`
    * `IsBrAny`
    * `IsLdc`
    * `IsConv`
* Extensions for emitting `call` vs `callvirt` codes.
    * `GetCall`
    * `GetCallRuntime`
