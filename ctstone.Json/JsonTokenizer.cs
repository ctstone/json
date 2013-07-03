using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ctstone.Json
{
    public class JsonTokenException : Exception
    {
        public JsonTokenException(char expected, char found, long position)
            : base(String.Format("Unexpected '{0}' at position {1}. Expected '{2}'", found == (char)3 ? "EOF" : found.ToString(), position, expected))
        { }
    }

    public static class JsonTokenizer
    {
        static BindingFlags _bindingFlags 
            = BindingFlags.IgnoreCase 
            | BindingFlags.Instance 
            | BindingFlags.Public;

        public static T Parse<T>(string input)
        {
            Json json = new Json(input);
            return (T)Parse(json, typeof(T));
        }

        public static dynamic Parse(string input)
        {
            Json json = new Json(input);
            return Parse(json, null);
        }

        private static object Parse(Json json, Type type)
        {
            Trace.WriteLine("Parsing generic");
            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                json.Chomp();

                if (json.Char == '{')
                {
                    json.Pos++;
                    return ParseObject(json, type);
                }
                else if (json.Char == '[')
                {
                    json.Pos++;
                    return ParseArray(json, type);
                }
                else if (json.Char == '"')
                {
                    json.Pos++;
                    return ParseString(json, type);
                }
                else
                    return ParseValue(json, type);
            }

            return String.Empty;
        }

        private static object ParseValue(Json json, Type type)
        {
            Trace.WriteLine("Parsing value");

            StringBuilder sb = new StringBuilder();
            bool is_number = true;

            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                json.Chomp();

                if (json.Char == ',' || json.Char == '}' || json.Char == ']')
                    break;

                is_number = is_number && Char.IsNumber(json.Char);
                sb.Append(json.Char);
            }

            string value = sb.ToString();

            if (value == "true")
                return true;
            else if (value == "false")
                return false;
            else if (value == "null")
                return null;
            else if (type == null && String.IsNullOrEmpty(value))
                return String.Empty;
            else if (type == null && is_number)
            {
                long number = Int64.Parse(value);
                if (number <= Int32.MaxValue)
                    return (int)number;
                else
                    return number;
            }
            else if (type == null)
                return Double.Parse(value, NumberStyles.Any);
            else
                return Convert.ChangeType(value, type);
        }

        private static object ParseArray(Json json, Type type)
        {
            Trace.WriteLine("Parsing array");

            Type elem_type;
            if (type == null)
                elem_type = null;
            else if (type.IsGenericType)
                elem_type = type.GetGenericArguments()[0];
            else if (type.IsArray)
                elem_type = type.GetElementType();
            else
                elem_type = null;

            dynamic values = new DynamicList();
            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                json.Chomp();

                if (json.Char == ',')
                    continue;

                if (json.Char != ']')
                    values.Add(Parse(json, elem_type));

                json.Chomp();

                if (json.Char == ']')
                {
                    json.Pos++;
                    
                    if (type == null)
                        return values;
                    
                    object ar = Activator.CreateInstance(type, values.Count());
                    for (int i = 0; i < values.Count(); i++)
                    {
                        if (type.IsArray)
                            (ar as Array).SetValue(values[i], i);
                        else if (ar as IList != null)
                            (ar as IList).Insert(i, values[i]);
                    }
                    return ar;
                }
            }

            throw new JsonTokenException(']', json.Char, json.Pos);
        }

        private static object ParseObject(Json json, Type type)
        {
            Trace.WriteLine("Parsing object");

            dynamic obj = null;
            if (type == null)
                obj = new DynamicDictionary();
            else
                obj = Activator.CreateInstance(type);

            string key = String.Empty;
            bool parsing_key = true;
            bool parsing_value = false;

            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                json.Chomp();

                if (json.Char == '"' && parsing_key)
                {
                    json.Pos++;
                    key = ParseString(json);
                    Trace.WriteLine("Object key is " + key);
                    parsing_key = false;
                    json.Chomp();
                }

                if (json.Char == ':')
                {
                    json.Pos++;
                    parsing_value = true;
                    json.Chomp();
                }

                if (parsing_value)
                {
                    parsing_value = false;

                    if (type == null)
                        obj[key] = Parse(json, null);
                    else if (typeof(IDictionary).IsAssignableFrom(type) && type.IsGenericType)
                    {
                        Type value_type = type.GetGenericArguments()[1];
                        (obj as IDictionary).Add(key, Parse(json, value_type));
                    }
                    else
                    {
                        PropertyInfo prop = obj.GetType().GetProperty(key, _bindingFlags);
                        Type member_type = prop == null ? null : prop.PropertyType;
                        object value = Parse(json, member_type);
                        Trace.WriteLine(String.Format("Object value is {0}", value));
                        if (prop != null)
                            prop.SetValue(obj, value, null);
                    }
                    json.Chomp();
                }

                if (json.Char == ',')
                    parsing_key = true;

                if (json.Char == '}')
                {
                    json.Pos++;
                    return obj;
                }
            }

            throw new JsonTokenException('}', json.Char, json.Pos);
        }

        private static string ParseString(Json json)
        {
            return ParseString(json, typeof(String)) as String;
        }
        private static object ParseString(Json json, Type type)
        {
            Trace.WriteLine("Parsing string");

            StringBuilder sb = new StringBuilder();
            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                if (json.Char == '"')
                {
                    json.Pos++;
                    if (type == null || typeof(String).IsAssignableFrom(type))
                        return sb.ToString();
                    else if (type == typeof(DateTime))
                        return DateTime.Parse(sb.ToString());
                    else
                        return null;
                }

                if (json.Char == '\\')
                {
                    json.Pos++;
                    sb.Append(ParseEscape(json));
                }
                else
                    sb.Append(json.Char);
            }

            throw new JsonTokenException('"', json.Char, json.Pos);
        }

        private static string ParseEscape(Json json)
        {
            switch (json.Char)
            {
                case 'u':
                    json.Pos++;
                    byte[] bytes = new byte[2];
                    for (int i = 0; i < 4; i += 2)
                    {
                        int j = json.Pos + i;
                        bytes[(bytes.Length - 1) - i / 2] = Byte.Parse(json.Input.Substring(j, 2), NumberStyles.HexNumber);
                    }
                    json.Pos += 3;
                    return Encoding.Unicode.GetString(bytes);

                case 'b':
                    return "\b";

                case 'f':
                    return "\f";

                case 'n':
                    return "\n";

                case 'r':
                    return "\r";

                case 't':
                    return "\t";

                default:
                    return json.Char.ToString();
            }
        }

        class Json
        {
            public string Input { get; private set; }
            public int Pos { get; set; }
            public char Char
            {
                get { return Pos >= Input.Length ? (char)3 : Input[Pos]; }
            }

            public Json(string input)
            {
                Input = input;
            }

            /// <summary>
            /// Skip whitespace
            /// </summary>
            public void Chomp()
            {
                while (Char.IsWhiteSpace(Char))
                    Pos++;
            }
        }
    }
}
