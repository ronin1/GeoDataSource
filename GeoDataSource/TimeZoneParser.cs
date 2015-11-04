using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using log4net;

namespace GeoDataSource
{
    public class TimeZoneParser : IGeoFileParser<TimeZone>
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(TimeZoneParser));

        readonly string _file;
        public TimeZoneParser(string file)
        {
            _file = file;
        }

        public ICollection<TimeZone> ParseFile()
        {
            DateTime started = DateTime.UtcNow;
            _logger.Debug("ParseFile: Start");
            ICollection<TimeZone> names = new List<TimeZone>();
            int count = 0;
            using (var rdr = new StreamReader(_file))
            {
                string line = "";
                do
                {
                    line = rdr.ReadLine();
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith("CountryCode"))
                    {
                        TimeZone n = ParseLine(line);
                        if(n != null)
                            names.Add(n);
                    }
                    count++;
                } while (!string.IsNullOrEmpty(line));
            }
            _logger.InfoFormat("ParseFile: End {0}", DateTime.UtcNow - started);
            return names;
        }

        static TimeZone ParseLine(string line)
        {
            try {
                TimeZone n = new TimeZone();
                string[] parts = line.Split('\t');
                double id = 0;

                n.CountryCode = parts[0];
                n.TimeZoneId = parts[1];

                if (double.TryParse(parts[2], out id))
                    n.GMTOffSet = id;
                if (double.TryParse(parts[3], out id))
                    n.DSTOffSet = id;
                if (double.TryParse(parts[4], out id))
                    n.RawOffSet = id;

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
