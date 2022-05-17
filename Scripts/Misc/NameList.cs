using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server
{
    public class NameList
    {
        private static readonly Dictionary<string, NameList> m_Table;
        private readonly string[] m_List;

        public NameList(XmlNode xml)
        {
            m_List = xml.InnerText.Split(',');

            for (int i = 0; i < m_List.Length; ++i)
            {
                m_List[i] = Utility.Intern(m_List[i].Trim());
            }
        }

        static NameList()
        {
            m_Table = new Dictionary<string, NameList>(StringComparer.OrdinalIgnoreCase);

            string filePath = Path.Combine(Core.BaseDirectory, "Data/names.xml");

            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                Load(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Warning: Exception caught loading name lists:");
                Diagnostics.ExceptionLogging.LogException(e);
            }
        }

        public string[] List => m_List;

        public static NameList GetNameList(string type)
        {
            m_Table.TryGetValue(type, out var n);

            return n;
        }

        public static string RandomName(string type)
        {
            NameList list = GetNameList(type);

            if (list != null)
            {
                return list.GetRandomName();
            }

            return "";
        }

        public bool ContainsName(string name)
        {
            for (int i = 0; i < m_List.Length; i++)
            {
                if (name == m_List[i])
                {
                    return true;
                }
            }

            return false;
        }

        public string GetRandomName()
        {
            if (m_List.Length > 0)
            {
                return m_List[Utility.Random(m_List.Length)];
            }

            return "";
        }

        private static void Load(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["names"];

            if (root != null)
            {
                var name = root.GetElementsByTagName("namelist");

                for (var index = 0; index < name.Count; index++)
                {
                    var element = (XmlElement) name[index];

                    string type = element.GetAttribute("type");

                    if (string.IsNullOrEmpty(type))
                    {
                        continue;
                    }

                    try
                    {
                        NameList list = new NameList(element);

                        m_Table[type] = list;
                    }
                    catch (Exception e)
                    {
                        Diagnostics.ExceptionLogging.LogException(e);
                    }
                }
            }
        }
    }
}
