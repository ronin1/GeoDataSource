using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GeoDataSource
{
    public class TimeZoneParser : IGeoFileParser<TimeZone>
    {
        readonly string _file;
        public TimeZoneParser(string file)
        {
            _file = file;
        }

        public ICollection<TimeZone> ParseFile()
        {
            ICollection<TimeZone> Names = new List<TimeZone>();
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
                        Names.Add(n);
                    }
                    count++;
                } while (!string.IsNullOrEmpty(line));
            }
            return Names;
        }

        static TimeZone ParseLine(string Line)
        {
            TimeZone n = new TimeZone();
            string[] parts = Line.Split('\t');
            double id = 0;

            n.CountryCode = parts[0];
            n.TimeZoneId = parts[1];

            if (double.TryParse(parts[2], out id)) n.GMTOffSet = id;
            if (double.TryParse(parts[3], out id)) n.DSTOffSet = id;
            if (double.TryParse(parts[4], out id)) n.RawOffSet = id;

            return n;
        }
    }
}
