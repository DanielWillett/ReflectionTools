using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace DanielWillett.ReflectionTools.Formatting;
public class DefaultCodeInstructionFormatter : ICodeInstructionFormatter
{
    public string FormatCodeInstruction(CodeInstruction codeInstruction, CodeInstructionFormattingUsage expectedUsage)
    {
        if (instruction == null)
            return ((object)null!).Format();
        string op = instruction.opcode.Format();
        switch (instruction.opcode.OperandType)
        {
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                if (instruction.operand is Label lbl)
                    op += " " + lbl.Format();
                break;
            case OperandType.InlineField:
                if (instruction.operand is FieldInfo field)
                    op += " " + field.Format();
                break;
            case OperandType.ShortInlineI:
            case OperandType.InlineI:
                try
                {
                    int num = Convert.ToInt32(instruction.operand);
                    op += " " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.ExtraDataColor) + num + GetResetSuffix();
                }
                catch
                {
                    // ignored
                }
                break;
            case OperandType.InlineI8:
                try
                {
                    long lng = Convert.ToInt64(instruction.operand);
                    op += " " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.ExtraDataColor) + lng + GetResetSuffix();
                }
                catch
                {
                    // ignored
                }
                break;
            case OperandType.InlineMethod:
                if (instruction.operand is MethodBase method)
                    op += " " + method.Format();
                break;
            case OperandType.ShortInlineR:
            case OperandType.InlineR:
                try
                {
                    double dbl = Convert.ToDouble(instruction.operand);
                    op += " " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.ExtraDataColor) + dbl + GetResetSuffix();
                }
                catch
                {
                    // ignored
                }
                break;
            case OperandType.InlineSig:
                try
                {
                    int num = Convert.ToInt32(instruction.operand);
                    op += " " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.ExtraDataColor) + num + GetResetSuffix();
                }
                catch
                {
                    // ignored
                }
                break;
            case OperandType.InlineString:
                if (instruction.operand is string str)
                    op += " " + GetColorPrefix(ToArgb(new Color32(214, 157, 133, 255))) + "\"" + str + "\"" + GetResetSuffix();
                break;
            case OperandType.InlineSwitch:
                if (instruction.operand is Label[] jumps)
                {
                    op += Environment.NewLine + "{";
                    for (int i = 0; i < jumps.Length; ++i)
                        op += Environment.NewLine + "  " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.ExtraDataColor) + i + GetResetSuffix() + " => " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.StructColor) + " Label #" + jumps[i].GetLabelId() + GetResetSuffix();

                    op += Environment.NewLine + "}";
                }
                break;
            case OperandType.InlineTok:
                switch (instruction.operand)
                {
                    case Type typeToken:
                        op += " " + typeToken.Format();
                        break;
                    case MethodBase methodToken:
                        op += " " + methodToken.Format();
                        break;
                    case FieldInfo fieldToken:
                        op += " " + fieldToken.Format();
                        break;
                }
                break;
            case OperandType.InlineType:
                if (instruction.operand is Type type)
                    op += " " + type.Format();
                break;
            case OperandType.ShortInlineVar:
            case OperandType.InlineVar:
                if (instruction.operand is LocalBuilder lb)
                    op += " " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.ExtraDataColor) + lb.LocalIndex + GetResetSuffix() + " : " + lb.LocalType!.Format();
                else if (instruction.operand is int index)
                    op += " " + GetColorPrefix(FormatProvider.StackCleaner.Configuration.Colors!.ExtraDataColor) + index + GetResetSuffix();
                break;
        }

        foreach (Label lbl in instruction.labels)
        {
            op += " .lbl #".Colorize(ConsoleColor.DarkRed) + lbl.GetLabelId().Format();
        }


        return op;
    }
}
