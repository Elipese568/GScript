using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GScript.Editer;

class MJsonStringEnumConverter<T> : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(T);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        JsonStringEnumConverter converter = new JsonStringEnumConverter();
        return converter.CreateConverter(typeToConvert, options);
    }
}


[JsonConverter(typeof(MJsonStringEnumConverter<KeyType>))]
internal enum KeyType
{
    Control,
    CritialVariable,
    Variable,
    Type,
    Digit,
    Float,
    String,
    Char,
    Boolean,
    KnownType,
    Operator,
    Symbol,
    Tag,
    Definition,
    Special,
    Property,
    Expression,
    Parenthesis,
    Text,
    Split,
    Unknown
}
