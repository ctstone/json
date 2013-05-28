using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
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
        public static dynamic Parse(string input)
        {
            Json json = new Json(input);
            return Parse(json);
        }

        private static dynamic Parse(Json json)
        {
            Trace.WriteLine("Parsing generic");
            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                if (Char.IsWhiteSpace(json.Char))
                    continue;

                if (json.Char == '{')
                {
                    json.Pos++;
                    return ParseObject(json);
                }
                else if (json.Char == '[')
                {
                    json.Pos++;
                    return ParseArray(json);
                }
                else if (json.Char == '"')
                {
                    json.Pos++;
                    return ParseString(json);
                }
                else
                {
                    return ParseValue(json);
                }
            }

            return String.Empty;
        }

        private static dynamic ParseValue(Json json)
        {
            Trace.WriteLine("Parsing value");

            StringBuilder sb = new StringBuilder();
            bool is_number = true;

            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                if (Char.IsWhiteSpace(json.Char))
                {
                    json.Pos++;
                    break;
                }
                if (json.Char == ',' || json.Char == '}' || json.Char == ']')
                    break;

                is_number = is_number && Char.IsNumber(json.Char);
                sb.Append(json.Char);
            }

            string value = sb.ToString();

            if (String.IsNullOrEmpty(value))
                return String.Empty;
            else if (is_number)
                return Int64.Parse(value);
            else if (value == "true")
                return true;
            else if (value == "false")
                return false;
            else if (value == "null")
                return null;
            else
                return Double.Parse(value, NumberStyles.Any);
        }

        private static dynamic[] ParseArray(Json json)
        {
            Trace.WriteLine("Parsing array");

            List<dynamic> obj = new List<dynamic>();
            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                if (Char.IsWhiteSpace(json.Char))
                    continue;
                
                if (json.Char == ',')
                    continue;

                dynamic value = Parse(json);
                Trace.WriteLine("Adding " + (value.ToString() as String) + " to array");
                (obj as IList).Add(value);

                if (json.Char == ']')
                {
                    json.Pos++;
                    return obj.ToArray();
                }
            }

            throw new JsonTokenException(']', json.Char, json.Pos);
        }

        private static dynamic ParseObject(Json json)
        {
            Trace.WriteLine("Parsing object");

            dynamic obj = new ExpandoObject();
            string key = String.Empty;
            bool parsing_key = true;
            bool parsing_value = false;

            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                if (Char.IsWhiteSpace(json.Char))
                    continue;

                if (parsing_key)
                {
                    if (json.Char != '"')
                        throw new JsonTokenException('"', json.Char, json.Pos);
                    json.Pos++;
                    key = ParseString(json);
                    Trace.WriteLine("Object key is " + key);
                    parsing_key = false;
                }

                if (json.Char == ':')
                {
                    json.Pos++;
                    parsing_value = true;
                }

                if (parsing_value)
                {
                    dynamic value = Parse(json);
                    Trace.WriteLine("Object value is " + ((value ?? String.Empty).ToString() as String));
                    (obj as IDictionary<string, object>)[key] = value;
                    parsing_value = false;
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
            Trace.WriteLine("Parsing string");

            StringBuilder sb = new StringBuilder();
            for (; json.Pos < json.Input.Length; json.Pos++)
            {
                if (json.Char == '"')
                {
                    json.Pos++;
                    return sb.ToString();
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
        }
    }
}
