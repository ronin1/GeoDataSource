using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using log4net;

namespace GeoDataSource
{
    public class CountryParser : IGeoFileParser<Country>
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(CountryParser));

        readonly string _file;
        public CountryParser(string file)
        {
            _file = file;
        }

        public ICollection<Country> ParseFile()
        {
            DateTime started = DateTime.UtcNow;
            _logger.Debug("ParseFile: Start");
            ICollection<Country> names = new List<Country>();
            int count = 0;
            using (var rdr = new StreamReader(_file))
            {
                string line = "";
                do
                {
                    line = rdr.ReadLine();
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                    {
                        Country n = ParseLine(line);
                        if(n != null)
                            names.Add(n);
                    }
                    count++;
                } while (!string.IsNullOrEmpty(line));
            }
            _logger.InfoFormat("ParseFile: End {0}", DateTime.UtcNow - started);
            return names;
        }

        static Country ParseLine(string line)
        {
            try {
                Country n = new Country();
                string[] parts = line.Split('\t');
                int id = 0;

                n.ISOAlpha2 = parts[0];
                n.ISOAlpha3 = parts[1];
                n.ISONumeric = parts[2];
                n.FipsCode = parts[3];
                n.Name = parts[4];
                n.Capital = parts[5];
                n.Population = parts[6];
                n.ContinentId = parts[7];
                n.Continent = parts[8];
                n.tld = parts[9];
                n.CurrencyCode = parts[10];
                n.CurrencyName = parts[11];
                n.PhonePrefix = parts[12];
                n.PostalCodeFormat = parts[13];
                n.PostalCodeRegularExpression = parts[14].Trim();
                n.Languages = parts[15];
                n.Neighbours = parts[17];
                n.EquivalentFipsCode = parts[18];

                if (int.TryParse(parts[16], out id))
                {
                    n.GeoNameId = id;
                }

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
