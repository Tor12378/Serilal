using System.Collections;
using System.Text;

namespace Serilal
{
    class Program
    {
        private static readonly string path = Directory.GetCurrentDirectory() + @"/path.txt";

        static async Task Main()
        {
            var person = new Person()
            {
                FirstName = "Max",
                LastName = "Smith",
                Age = 18,
                Addresses = new List<string> { "Address 1", "Address 2" }
            };

            var json = SerializeObject(person);
            await WriteJsonInFile(json);

            var obj = await DeserializeObject<Person>();
            Console.WriteLine($"First Name: {obj.FirstName}");
            Console.WriteLine($"Last Name: {obj.LastName}");
            Console.WriteLine($"Age: {obj.Age}");
            foreach (var address in obj.Addresses)
            {
                Console.WriteLine($"Address: {address}");
            }
        }

        public static async Task<T> DeserializeObject<T>() where T : new()
        {
            var json = await File.ReadAllTextAsync(path);
            var obj = new T();
            var properties = json.Trim('{', '}').Split(',');

            foreach (var property in properties)
            {
                var subs = property.Trim().Split(':');
                var propName = subs[0].Trim('"');
                var propValue = subs[1].Trim('"');

                var propInfo = obj.GetType().GetProperty(propName);
                if (propInfo != null)
                {
                    if (IsArrayOrList(propInfo.PropertyType))
                    {
                        var values = propValue.Trim('[', ']').Trim('"').Split(';');
                        var elementType = propInfo.PropertyType.GetGenericArguments()[0];
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        var list = (IList)Activator.CreateInstance(listType);

                        foreach (var value in values)
                        {
                            var target = value.Trim(new char[] { '}', ']', '"' });
                            var val = Convert.ChangeType(target, elementType);
                            list.Add(val);
                        }

                        propInfo.SetValue(obj, list);
                    }
                    else
                    {
                        var val = Convert.ChangeType(propValue, propInfo.PropertyType);
                        propInfo.SetValue(obj, val);
                    }
                }
            }

            return obj;
        }

        private static string SerializeObject<T>(T obj)
        {
            var type = typeof(T);
            var properties = type.GetProperties();
            var jsonBuilder = new StringBuilder("{");
            var idx = 0;

            foreach (var propertyInfo in properties)
            {
                var isArray = IsArrayOrList(propertyInfo.PropertyType);
                var propName = propertyInfo.Name;
                var propValue = propertyInfo.GetValue(obj);

                if (isArray)
                {
                    if (propValue is IList list)
                    {
                        jsonBuilder.AppendFormat("\"{0}\":[", propName);

                        for (int i = 0; i < list.Count; i++)
                        {
                            var element = list[i];
                            jsonBuilder.AppendFormat("\"{0}\"", element);
                            if (i < list.Count - 1)
                                jsonBuilder.Append(";");
                        }

                        jsonBuilder.Append("]");
                    }
                }
                else
                {
                    jsonBuilder.AppendFormat("\"{0}\":\"{1}\"", propName, propValue);
                }

                if (idx < properties.Length - 1)
                {
                    jsonBuilder.Append(",");
                    idx++;
                }
            }

            jsonBuilder.Append("}");

            return jsonBuilder.ToString();
        }

        private static async Task WriteJsonInFile(string json)
        {
            await File.WriteAllTextAsync(path, json);
        }

        private static bool IsArrayOrList(Type type)
        {
            return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }
    }
}