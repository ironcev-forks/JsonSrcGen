using System;
using System.Text;
using JsonSrcGen;

namespace JsonSrcGen.TypeGenerators
{
    public class CustomTypeGenerator : IJsonGenerator
    {
        public string TypeName => "Custom"; 

        public void GenerateFromJson(CodeBuilder codeBuilder, int indentLevel, JsonType type, Func<string, string> valueSetter, string valueGetter)
        {
            string propertyValueName = $"property{UniqueNumberGenerator.UniqueNumber}Value";

            codeBuilder.AppendLine(indentLevel, "json = json.SkipWhitespace();");
            codeBuilder.AppendLine(indentLevel, "if(json[0] == 'n')");
            codeBuilder.AppendLine(indentLevel, "{");
            codeBuilder.AppendLine(indentLevel+1, valueSetter("null"));
            codeBuilder.AppendLine(indentLevel+1, $"json = json.Slice(4);");
            codeBuilder.AppendLine(indentLevel, "}");
            codeBuilder.AppendLine(indentLevel, "else");
            codeBuilder.AppendLine(indentLevel, "{");

            indentLevel++;

            if(valueGetter != null)
            {
                codeBuilder.AppendLine(indentLevel, $"if({valueGetter} == null)"); 
                codeBuilder.AppendLine(indentLevel, "{");
                codeBuilder.AppendLine(indentLevel+1, valueSetter($"new {type.FullName}()"));
                codeBuilder.AppendLine(indentLevel, "}");
            }
            else
            {
                string valueName = $"value{UniqueNumberGenerator.UniqueNumber}";
                valueGetter = valueName;
                codeBuilder.AppendLine(indentLevel, $"var {valueName} = new {type.FullName}();");
                codeBuilder.AppendLine(indentLevel, valueSetter(valueName));
            }

            codeBuilder.AppendLine(indentLevel, $"json = FromJson({valueGetter}, json);");
            indentLevel--;
            codeBuilder.AppendLine(indentLevel, "}");
        }

        public void GenerateToJson(CodeBuilder codeBuilder, int indentLevel, StringBuilder appendBuilder, JsonType type, string valueGetter)
        {
            codeBuilder.AppendLine(indentLevel, $"if({valueGetter} == null)");
            codeBuilder.AppendLine(indentLevel, "{");
            var nullAppendBuilder = new StringBuilder(appendBuilder.ToString());
            nullAppendBuilder.Append("null");
            codeBuilder.MakeAppend(indentLevel+1, nullAppendBuilder);
            codeBuilder.AppendLine(indentLevel, "}");
            codeBuilder.AppendLine(indentLevel, "else"); 
            codeBuilder.AppendLine(indentLevel, "{");
            codeBuilder.MakeAppend(indentLevel+1, appendBuilder);
            codeBuilder.AppendLine(indentLevel+1, $"ToJson({valueGetter}, builder);");
            codeBuilder.AppendLine(indentLevel, "}");
        }
        public CodeBuilder ClassLevelBuilder => null;
    }
}