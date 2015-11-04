using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;

namespace GeoDataSource
{
    public class Serialize
    {
        public enum SerializerType
        {
            ProtocolBuffer,
            BinaryFormatter,
            Default = ProtocolBuffer,
        }

        public static bool SerializeBinaryToDisk<T>(T request, string filename, 
            SerializerType type = SerializerType.Default)
            where T : class
        {
            if (File.Exists(filename))
                File.Delete(filename);

            using (var fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
            {
                switch(type)
                {
                    case SerializerType.BinaryFormatter:
                        {
                            new BinaryFormatter().Serialize(fs, request);
                            break;
                        }
                    default:
                        {
                            Serializer.Serialize(fs, request);
                            break;
                        }
                }
                fs.Flush();
            }
            return File.Exists(filename);
        }

        public static T DeserializeBinaryFromResource<T>(string name, SerializerType type = SerializerType.Default)
            where T : class
        {
            using(Stream stm = typeof(GeoData).Assembly.GetManifestResourceStream(name))
            {
                object o;
                switch (type)
                {
                    case SerializerType.BinaryFormatter:
                        {
                            o = new BinaryFormatter().Deserialize(stm);
                            break;
                        }
                    default:
                        {
                            o = Serializer.Deserialize<T>(stm);
                            break;
                        }
                }
                if (o != null && o is T)
                    return o as T;
                else
                    return null;
            }
        }

        public static T DeserializeBinaryFromDisk<T>(string filename, SerializerType type = SerializerType.Default)
            where T : class
        {
            using(var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                object o;
                switch (type)
                {
                    case SerializerType.BinaryFormatter:
                        {
                            o = new BinaryFormatter().Deserialize(fs);
                            break;
                        }
                    default:
                        {
                            o = Serializer.Deserialize<T>(fs);
                            break;
                        }
                }
                if (o != null && o is T)
                    return o as T;
                else
                    return null;
            }
        }
    }

}
