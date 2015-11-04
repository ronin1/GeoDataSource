using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;

namespace GeoDataSource
{
    public class PostalCodeParser : IGeoFileParser<PostalCode>
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(PostalCodeParser));

        readonly string _file;
        public PostalCodeParser(string file)
        {
            _file = file;
        }

        public ICollection<PostalCode> ParseFile()
        {
            DateTime started = DateTime.UtcNow;
            _logger.Debug("ParseFile: Start");
            ICollection<PostalCode> codes = new LinkedList<PostalCode>();
            int count = 0;
            using (var rdr = new StreamReader(_file))
            {
                string line = "";
                do
                {
                    line = rdr.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        PostalCode n = ParseLine(line);
                        if (n != null)
                        {
                            codes.Add(n);
                        }
                    }
                    count++;
                } while (!string.IsNullOrEmpty(line));
            }
            _logger.InfoFormat("ParseFile: End {0}", DateTime.UtcNow - started);
            return codes;
        }

        static PostalCode ParseLine(string line)
        {
            try {
                if (string.IsNullOrWhiteSpace(line))
                    return null;

                string[] arr = line.Split('\t');
                var c = new PostalCode();
                c.Country = new Country { ISOAlpha2 = arr[0] };
                c.Code = arr[1];
                c.Name = arr[2];
                c.Admin1 = new Admin1Code { Name = arr[3], Code = arr[4] };
                if (string.IsNullOrWhiteSpace(c.Admin1.Name) && string.IsNullOrWhiteSpace(c.Admin1.Code))
                    c.Admin1 = null;

                c.Admin2 = new Admin2Code { Name = arr[5], Code = arr[6] };
                if (string.IsNullOrWhiteSpace(c.Admin2.Name) && string.IsNullOrWhiteSpace(c.Admin2.Code))
                    c.Admin2 = null;

                c.Amdin3 = new Admin3Code { Name = arr[7], Code = arr[8] };
                if (string.IsNullOrWhiteSpace(c.Amdin3.Name) && string.IsNullOrWhiteSpace(c.Amdin3.Code))
                    c.Amdin3 = null;

                decimal lat, lng;
                if (!string.IsNullOrWhiteSpace(arr[9]) && decimal.TryParse(arr[9], out lat))
                    c.Latitude = lat;

                if (!string.IsNullOrWhiteSpace(arr[10]) && decimal.TryParse(arr[10], out lng))
                    c.Longitude = lng;

                GPSAccuracy gps = GPSAccuracy.Unknown;
                if (!string.IsNullOrWhiteSpace(arr[11]) && Enum.TryParse(arr[11], true, out gps))
                    c.Accuracy = gps;

                return c;
            }
            catch(Exception ex)
            {
                _logger.Error("ParseLine: " + (line ?? "<null>"), ex);
                return null;
            }
        }
    }
}
