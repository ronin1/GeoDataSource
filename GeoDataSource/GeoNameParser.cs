using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using log4net;

namespace GeoDataSource
{
    public class GeoNameParser : IGeoFileParser<GeoName>
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(GeoNameParser));

        readonly string _file;
        public GeoNameParser(string file)
        {
            _file = file;
        }

        public ICollection<GeoName> ParseFile()
        {
            DateTime started = DateTime.UtcNow;
            _logger.Debug("ParseFile: Start");
            ICollection<GeoName> names = new LinkedList<GeoName>();
            int count = 0;
            using (var rdr = new StreamReader(_file))
            {
                string line = "";
                do
                {
                    line = rdr.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        GeoName n = ParseLine(line);
                        if (n != null && n.FeatureClass.StartsWith("ADM1"))// || n.FeatureClass.StartsWith("ADM2"))
                        {
                            names.Add(n);
                        }                        
                    }
                    count++;
                } while (!string.IsNullOrEmpty(line));
            }
            _logger.InfoFormat("ParseFile: End {0}", DateTime.UtcNow - started);
            return names;
        }

        static GeoName ParseLine(string line)
        {
            try {
                var n = new GeoName();
                string[] parts = line.Split('\t');
                int id = 0;
                if (int.TryParse(parts[0], out id))
                    n.ID = id;

                n.Name = parts[1];
                n.AsciiName = parts[2];
                string names = parts[3];
                if (!string.IsNullOrEmpty(names))
                {
                    n.AlternateNames = new List<string>(names.Split(','));
                    foreach (string name in n.AlternateNames)
                    {
                        if (name.Length == 2) n.TwoLetterName = name;
                    }
                }
                decimal ll = 0;
                if (decimal.TryParse(parts[4], out ll))
                    n.Latitude = ll;
                if (decimal.TryParse(parts[5], out ll))
                    n.Longitude = ll;

                n.FeatureClass = parts[7];
                n.FeatureCodeId = parts[6];
                n.CountryCode = parts[8];
                n.AlternateCountryCode = parts[9];
                n.Admin1Code = parts[10];
                n.Admin2Code = parts[11];
                n.Admin3Code = parts[12];
                n.Admin4Code = parts[13];

                long pop = 0;
                if (long.TryParse(parts[14], out pop))
                    n.Population = pop;
                if (int.TryParse(parts[15], out id))
                    n.Elevation = id;

                n.DigitalElevationModel = parts[16];
                n.TimeZoneId = parts[17];
                DateTime dt;
                if (DateTime.TryParse(parts[18], out dt)) n.LastModified = dt;

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
