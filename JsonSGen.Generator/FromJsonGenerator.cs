using System.Text;
using System;
using System.Linq;
using JsonSGen.Generator.PropertyHashing;
using System.Collections.Generic;
using JsonSGen.TypeGenerators;

namespace JsonSGen.Generator
{
    public class FromJsonGenerator
    {
        readonly IJsonGenerator _customTypeGenerator = new CustomTypeGenerator();
        readonly Dictionary<string, IJsonGenerator> _generators;

        public FromJsonGenerator(IEnumerable<IJsonGenerator> generators)
        {
            _generators = new Dictionary<string, IJsonGenerator>();
            foreach(var generator in generators)
            {
                _generators.Add(generator.TypeName, generator);
            }
        }

        public void Generate(JsonClass jsonClass, CodeBuilder classBuilder)
        {
            classBuilder.AppendLine(2, $"public void FromJson({jsonClass.Namespace}.{jsonClass.Name} value, string jsonString)");
            classBuilder.AppendLine(2, "{");
            classBuilder.AppendLine(3, "FromJson(value, jsonString.AsSpan());");
            classBuilder.AppendLine(2, "}"); 

            classBuilder.AppendLine(2, $"public ReadOnlySpan<char> FromJson({jsonClass.Namespace}.{jsonClass.Name} value, ReadOnlySpan<char> json)");
            classBuilder.AppendLine(2, "{");

            classBuilder.AppendLine(3, "json = json.SkipWhitespaceTo('{');");

            classBuilder.AppendLine(3, "while(true)");
            classBuilder.AppendLine(3, "{");

            classBuilder.AppendLine(4, "json = json.SkipWhitespaceTo('\\\"');");
            classBuilder.AppendLine(4, "var propertyName = json.ReadTo('\\\"');");
            classBuilder.AppendLine(4, "json = json.Slice(propertyName.Length + 1);");
            classBuilder.AppendLine(4, "json = json.SkipWhitespaceTo(':');");
            
            GenerateProperties(jsonClass.Properties, 4, classBuilder);

            classBuilder.AppendLine(4, "json = json.SkipWhitespaceTo(',', '}', out char found);"); 
            classBuilder.AppendLine(4, "if(found == '}')");
            classBuilder.AppendLine(4, "{");
            classBuilder.AppendLine(5, "return json;");
            classBuilder.AppendLine(4, "}");

            classBuilder.AppendLine(3, "}");
            classBuilder.AppendLine(2, "}");
        }

        public void GenerateProperties(IReadOnlyCollection<JsonProperty> properties, int indentLevel, CodeBuilder classBuilder)
        {
            var propertyHashFactory = new PropertyHashFactory();
            var propertyHash = propertyHashFactory.FindBestHash(properties.Select(p => p.JsonName).ToArray());

            var hashesQuery =
                from property in properties
                let hash = propertyHash.Hash(property.JsonName)
                group property by hash into hashGroup
                orderby hashGroup.Key
                select hashGroup;

            var hashes = hashesQuery.ToArray();
            var switchGroups = FindSwitchGroups(hashes);

            foreach(var switchGroup in switchGroups)
            {
                // if(switchGroup.Count <= 2)
                // {
                //     GenerateIfGroup(switchGroup, propertyHandlers, unknownPropertyLabel, loopCheckLabel, hashLocal);
                //     continue;
                // }
                GenerateSwitchGroup(switchGroup, classBuilder, indentLevel, propertyHash);
            }
        }

        void GenerateSwitchGroup(SwitchGroup switchGroup, CodeBuilder classBuilder, int indentLevel, PropertyHash propertyHash)
        {
            classBuilder.AppendLine(indentLevel, $"switch({propertyHash.GenerateHashCode()})");
            classBuilder.AppendLine(indentLevel, "{");
            
            foreach(var hashGroup in switchGroup)
            {
                classBuilder.AppendLine(indentLevel+1, $"case {hashGroup.Key}:");
                var subProperties = hashGroup.ToArray();
                if(subProperties.Length != 1)
                {
                    GenerateProperties(subProperties, indentLevel+2, classBuilder);
                    classBuilder.AppendLine(indentLevel+2, "break;");
                    continue;
                }
                var property = subProperties[0];
                var propertyNameBuilder = new StringBuilder();
                propertyNameBuilder.AppendDoubleEscaped(property.JsonName);
                string jsonName = propertyNameBuilder.ToString();
                classBuilder.AppendLine(indentLevel+2, $"if(!propertyName.EqualsString(\"{jsonName}\"))");
                classBuilder.AppendLine(indentLevel+2, "{");
                classBuilder.AppendLine(indentLevel+3, "json = json.SkipProperty();");
                classBuilder.AppendLine(indentLevel+3, "break;"); //todo: need to read to the next property (could be an object list so need to count '{' and '}')
                classBuilder.AppendLine(indentLevel+2, "}");

                switch(property.Type.Name)
                {
                    case "Int32":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadInt(out int property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "UInt32":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadUInt(out uint property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "UInt64":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadULong(out ulong property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "UInt64?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableULong(out ulong? property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Int64":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadLong(out long property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Int64?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableLong(out long? property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Int16":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadShort(out short property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Int32?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableInt(out int? property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Int16?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableInt(out int? property{property.CodeName}Int);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = ({property.Type.Name})property{property.CodeName}Int;");
                        break;
                    case "UInt32?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableUInt(out uint? property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "UInt16?":
                    case "Byte?": 
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableUInt(out uint? property{property.CodeName}Uint);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = ({property.Type.Name})property{property.CodeName}Uint;");
                        break;
                    case "UInt16":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadUShort(out ushort property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Byte":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadByte(out byte property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Double":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadDouble(out double property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Single":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadDouble(out double property{property.CodeName}Double);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = (float)property{property.CodeName}Double;");
                        break;
                    case "Double?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableDouble(out double? property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Single?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadNullableDouble(out double? property{property.CodeName}Double);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = (float?)property{property.CodeName}Double;");
                        break;
                    case "String":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadString(out string property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Boolean":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadBool(out bool property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    case "Boolean?":
                        classBuilder.AppendLine(indentLevel+2, $"json = json.ReadBool(out bool? property{property.CodeName}Value);");
                        classBuilder.AppendLine(indentLevel+2, $"value.{property.CodeName} = property{property.CodeName}Value;");
                        break;
                    default:
                        var generator = GetGeneratorForType(property.Type);
                        generator.GenerateFromJson(classBuilder, indentLevel+2, property);
                        break;
                }
                classBuilder.AppendLine(indentLevel+2, "break;"); 
            }

            classBuilder.AppendLine(indentLevel, "}"); // end of switch
        }

        IJsonGenerator GetGeneratorForType(JsonType type)
        {
            if(type.IsCustomType)
            {
                return _customTypeGenerator;
            } 
            if(_generators.TryGetValue(type.Name, out var generator))
            {
                return generator;
            }
            throw new Exception($"Unsupported type {type.FullName} in from json generator");

        }

        class SwitchGroup : List<IGrouping<int, JsonProperty>>{}

        IEnumerable<SwitchGroup> FindSwitchGroups(IGrouping<int, JsonProperty>[] hashes)
        {
            int last = 0;
            int gaps = 0;
            var switchGroup = new SwitchGroup();
            foreach(var grouping in hashes)
            {
                int hash = grouping.Key;
                gaps += hash - last -1;
                if(gaps > 8)
                {
                    //to many gaps this switch group is finished
                    yield return switchGroup;
                    switchGroup = new SwitchGroup();
                    gaps = 0;
                }
                switchGroup.Add(grouping);
            }
            yield return switchGroup;
        }
    }
}