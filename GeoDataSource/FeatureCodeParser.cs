using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using log4net;

namespace GeoDataSource
{
    public class FeatureCodeParser : IGeoFileParser<FeatureCode>
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(FeatureCodeParser));

        readonly string _file;
        public FeatureCodeParser(string file)
        {
            _file = file;
        }

        public ICollection<FeatureCode> ParseFile()
        {
            DateTime started = DateTime.UtcNow;
            _logger.Debug("ParseFile: Start");
            ICollection<FeatureCode> names = new List<FeatureCode>();
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
                        if(n != null)
                            names.Add(n);
                    }
                    count++;
                } while (!string.IsNullOrEmpty(line));
            }
            _logger.InfoFormat("ParseFile: End {0}", DateTime.UtcNow - started);
            return names;
        }

        static FeatureCode ParseLine(string line)
        {
            try {
                FeatureCode n = new FeatureCode();
                string[] parts = line.Split('\t');

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
            catch(Exception ex)
            {
                _logger.Error("ParseLine: " + (line ?? "<null>"), ex);
                return null;
            }
        }
    }
}
