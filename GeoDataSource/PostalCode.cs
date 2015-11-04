using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoDataSource
{
    [Serializable]
    public class PostalCode
    {
        public Country Country { get; set; }

        /// <summary>
        /// Actual zip or postal code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Place name
        /// </summary>
        public string Name { get; set; }

        public Admin1Code Admin1 { get; set; }
        public Admin2Code Admin2 { get; set; }
        public Admin3Code Amdin3 { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        /// <summary>
        /// GPS Accuracy
        /// </summary>
        public GPSAccuracy Accuracy { get; set; }
    }
}
