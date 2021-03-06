using System;
using System.Collections.Generic;
using System.Text;
using JsonSrcGen;

namespace JsonSrcGen.TypeGenerators
{
    public class DictionaryGenerator : IJsonGenerator
    {
        readonly Func<JsonType, IJsonGenerator> _getGeneratorForType;
        public string TypeName => "Dictionary"; 

        public DictionaryGenerator(Func<JsonType, IJsonGenerator> getGeneratorForType)
        {
            _getGeneratorForType = getGeneratorForType;
        }

        public void GenerateFromJson(CodeBuilder codeBuilder, int indentLevel, JsonType type, Func<string, string> valueSetter, string valueGetter)
        {
            var dictionaryKeyType = type.GenericArguments[0];
            var dictionaryValueType = type.GenericArguments[1];
            var generator = _getGeneratorForType(dictionaryValueType);

            string foundVariable = $"found{UniqueNumberGenerator.UniqueNumber}";
            codeBuilder.AppendLine(indentLevel, $"json = json.SkipWhitespaceTo('{{', 'n', out char {foundVariable});");

            codeBuilder.AppendLine(indentLevel, $"if({foundVariable} == 'n')");
            codeBuilder.AppendLine(indentLevel, "{");
            codeBuilder.AppendLine(indentLevel+1, "json = json.Slice(3);");
            codeBuilder.AppendLine(indentLevel+1, valueSetter("null"));
            codeBuilder.AppendLine(indentLevel, "}");
            codeBuilder.AppendLine(indentLevel, "else");
            codeBuilder.AppendLine(indentLevel, "{");

            indentLevel++;

            codeBuilder.AppendLine(indentLevel, $"if({valueGetter} == null)");
            codeBuilder.AppendLine(indentLevel, "{");
            codeBuilder.AppendLine(indentLevel+1, valueSetter($"new Dictionary<{dictionaryKeyType.FullName},{dictionaryValueType.FullName}>()"));
            codeBuilder.AppendLine(indentLevel, "}");
            codeBuilder.AppendLine(indentLevel, "else");
            codeBuilder.AppendLine(indentLevel, "{");
            codeBuilder.AppendLine(indentLevel+1, $"{valueGetter}.Clear();");
            codeBuilder.AppendLine(indentLevel, "}");

            

            codeBuilder.AppendLine(indentLevel, "while(true)");
            codeBuilder.AppendLine(indentLevel, "{");

            codeBuilder.AppendLine(indentLevel+1, "json = json.SkipWhitespace();");
            codeBuilder.AppendLine(indentLevel+1, "if(json[0] == '}')");
            codeBuilder.AppendLine(indentLevel+1, "{");
            codeBuilder.AppendLine(indentLevel+2, "json = json.Slice(1);");
            codeBuilder.AppendLine(indentLevel+2, "break;");
            codeBuilder.AppendLine(indentLevel+1, "}");

            //key
            codeBuilder.AppendLine(indentLevel+1, "json = json.Read(out string key);");
            codeBuilder.AppendLine(indentLevel+1, "json = json.SkipWhitespaceTo(':');");

            //value
            var dictionary = new Dictionary<string, int>();
            Func<string, string> dictionaryAdder = value => $"{valueGetter}.Add(key, {value});";
            generator.GenerateFromJson(codeBuilder, indentLevel+1, dictionaryValueType, dictionaryAdder, null);
            codeBuilder.AppendLine(indentLevel+1, "json = json.SkipWhitespace();");
            codeBuilder.AppendLine(indentLevel+1, "switch (json[0])");
            codeBuilder.AppendLine(indentLevel+1, "{");
            codeBuilder.AppendLine(indentLevel+2, "case ',':");
            codeBuilder.AppendLine(indentLevel+3, "json = json.Slice(1);");
            codeBuilder.AppendLine(indentLevel+3, "continue;");
            codeBuilder.AppendLine(indentLevel+2, "case '}':");
            codeBuilder.AppendLine(indentLevel+3, "json = json.Slice(1);");
            codeBuilder.AppendLine(indentLevel+3, "break;");
            codeBuilder.AppendLine(indentLevel+2, "default:");
            codeBuilder.AppendLine(indentLevel+3, "throw new InvalidJsonException($\"Unexpected character while parsing list Expected ',' or ']' but got '{json[0]}'\", json);");
            codeBuilder.AppendLine(indentLevel+1, "}");
            codeBuilder.AppendLine(indentLevel+1, "break;");
            codeBuilder.AppendLine(indentLevel, "}");
            
            
            indentLevel--;
            codeBuilder.AppendLine(indentLevel, "}");
        }

        public void GenerateToJson(CodeBuilder codeBuilder, int indentLevel, StringBuilder appendBuilder, JsonType type, string valueGetter)
        {
           codeBuilder.MakeAppend(indentLevel, appendBuilder);

            string dictionaryName = $"list{UniqueNumberGenerator.UniqueNumber}"; 

            codeBuilder.AppendLine(indentLevel, $"var {dictionaryName} = {valueGetter};");
            codeBuilder.AppendLine(indentLevel, $"if({dictionaryName} == null)");
            codeBuilder.AppendLine(indentLevel, "{");
            appendBuilder.Append("null");
            codeBuilder.MakeAppend(indentLevel+1, appendBuilder);
            codeBuilder.AppendLine(indentLevel, "}");
            codeBuilder.AppendLine(indentLevel, "else");
            codeBuilder.AppendLine(indentLevel, "{");

            var dictionaryValueType = type.GenericArguments[1];
            var generator = _getGeneratorForType(dictionaryValueType);
            appendBuilder.Append("{");
            codeBuilder.MakeAppend(indentLevel+1, appendBuilder);

            codeBuilder.AppendLine(indentLevel+1, "bool isFirst = true;");

            codeBuilder.AppendLine(indentLevel+1, $"foreach(var pair in {dictionaryName})");
            codeBuilder.AppendLine(indentLevel+1, "{");
            
            codeBuilder.AppendLine(indentLevel+2, "if(!isFirst)");
            codeBuilder.AppendLine(indentLevel+2, "{");
            appendBuilder.Append(",");
            codeBuilder.MakeAppend(indentLevel+3, appendBuilder);
            codeBuilder.AppendLine(indentLevel+2, "}");

            codeBuilder.AppendLine(indentLevel+2, "isFirst = false;");


            appendBuilder.Append("\\\"");
            codeBuilder.MakeAppend(indentLevel+2, appendBuilder);

            codeBuilder.AppendLine(indentLevel+2, $"builder.Append(pair.Key);");
            appendBuilder.Append("\\\":");

            generator.GenerateToJson(codeBuilder, indentLevel+2, appendBuilder, dictionaryValueType, $"pair.Value");

            codeBuilder.AppendLine(indentLevel+1, "}");

            appendBuilder.Append("}");
            codeBuilder.MakeAppend(indentLevel+1, appendBuilder);
            codeBuilder.AppendLine(indentLevel, "}");
        }

        public CodeBuilder ClassLevelBuilder => null;
    }
}