using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GeoDataSource
{
    public class FeatureCodeParser : IGeoFileParser<FeatureCode>
    {
        readonly string _file;
        public FeatureCodeParser(string file)
        {
            _file = file;
        }

        public ICollection<FeatureCode> ParseFile()
        {
            ICollection<FeatureCode> Names = new List<FeatureCode>();
            int count = 0;
            using (var rdr = new StreamReader(_file))
            {
                string line = "";
                do
                {
                    line = rdr.ReadLine();
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith("CountryCode"))
                    {
                        FeatureCode n = ParseLine(line);
                        Names.Add(n);
                    }
                    count++;
                } while (!string.IsNullOrEmpty(line));
            }
            return Names;
        }

        static FeatureCode ParseLine(string Line)
        {
            FeatureCode n = new FeatureCode();
            string[] parts = Line.Split('\t');

            if (parts[0].Contains("."))
            {
                var codeAndClass = parts[0].Split('.');
                n.Code = codeAndClass[0];
                n.Class = codeAndClass[1];
            }
            else
            {
                n.Code = parts[0];
                n.Class = parts[0];
            }
            n.Name = parts[1];
            n.Description = parts[2];

            return n;
        }
    }
}
