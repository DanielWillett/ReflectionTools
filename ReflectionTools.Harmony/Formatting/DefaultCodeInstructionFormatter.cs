using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Globalization;
using System.Reflection.Emit;
using System.Text;

namespace DanielWillett.ReflectionTools.Harmony.Formatting;

/// <summary>
/// Default plain-text implementation of <see cref="ICodeInstructionFormatter"/>.
/// </summary>
public class DefaultCodeInstructionFormatter : ICodeInstructionFormatter
{
    /// <inheritdoc/>
    public string FormatCodeInstruction(CodeInstruction codeInstruction, OpCodeFormattingContext usageContext)
    {
        string str = Accessor.Formatter.Format(codeInstruction.opcode, codeInstruction.operand, usageContext);
        if (codeInstruction.labels.Count == 0 && codeInstruction.blocks.Count == 0)
            return str;

        StringBuilder sb = new StringBuilder();
        if (usageContext == OpCodeFormattingContext.InLine)
        {
            for (int i = 0; i < codeInstruction.blocks.Count; i++)
            {
                ExceptionBlock block = codeInstruction.blocks[i];

                if (i != 0)
                    sb.Append(' ');

                sb.Append('[').Append(FormatBlock(block)).Append(']');
            }

            sb.Append(str);

            for (int i = 0; i < codeInstruction.labels.Count; i++)
            {
                Label lbl = codeInstruction.labels[i];
                if (i != 0)
                    sb.Append(", ");
                sb.Append(".lbl ").Append(((uint)lbl.GetLabelId()).ToString(CultureInfo.InvariantCulture));
            }
        }
        else
        {
            for (int i = 0; i < codeInstruction.blocks.Count; i++)
            {
                ExceptionBlock block = codeInstruction.blocks[i];

                if (i != 0)
                    sb.Append(Environment.NewLine);

                switch (block.blockType)
                {
                    case ExceptionBlockType.BeginCatchBlock:
                        sb.Append('}').Append(Environment.NewLine).Append("catch").Append(Environment.NewLine).Append('{');
                        break;
                    case ExceptionBlockType.BeginExceptionBlock:
                        sb.Append("try").Append(Environment.NewLine).Append('{');
                        break;
                    case ExceptionBlockType.EndExceptionBlock:
                        sb.Append('}');
                        break;
                    case ExceptionBlockType.BeginFinallyBlock:
                        sb.Append('}').Append(Environment.NewLine).Append("finally").Append(Environment.NewLine).Append('{');
                        break;
                    case ExceptionBlockType.BeginFaultBlock:
                        sb.Append('}').Append(Environment.NewLine).Append("fault").Append(Environment.NewLine).Append('{');
                        break;
                    case ExceptionBlockType.BeginExceptFilterBlock:
                        sb.Append('}').Append(Environment.NewLine).Append("when").Append(Environment.NewLine).Append('{');
                        break;
                }
            }

            for (int i = 0; i < codeInstruction.labels.Count; i++)
            {
                Label lbl = codeInstruction.labels[i];
                if (i != 0)
                    sb.Append(Environment.NewLine);
                sb.Append(".lbl ").Append(((uint)lbl.GetLabelId()).ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(str);

        }

        return sb.ToString();
    }
    public string FormatBlock(ExceptionBlock block)
    {
        return block.blockType switch
        {
            ExceptionBlockType.BeginCatchBlock => "catch",
            ExceptionBlockType.BeginExceptFilterBlock => "filter",
            ExceptionBlockType.BeginExceptionBlock => "try",
            ExceptionBlockType.BeginFaultBlock => "fault",
            ExceptionBlockType.BeginFinallyBlock => "finally",
            ExceptionBlockType.EndExceptionBlock => "endtry",
            _ => block.blockType.ToString()
        };
    }
}
