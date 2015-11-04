/// Geo Database Used under a Creative Commons License
/// License: http://creativecommons.org/licenses/by/3.0/legalcode
/// License Summary: http://creativecommons.org/licenses/by/3.0/
/// Data Source: http://www.geonames.org/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ProtoBuf;

namespace GeoDataSource
{
    [ProtoContract]
	[Serializable]
	public sealed class GeoData
	{
		//GeoNameDatabase.allCountries.dat

        public static Task<GeoData> LoadAsync()
	    {
            return Task.FromResult(Current);
        }

        internal GeoData() { }
        class Inner
        {
            static readonly internal GeoData SINGLETON = new GeoData();
            static Inner()
            {
                if (System.IO.File.Exists(DataManager.Instance.DataFile))
                    SINGLETON = Serialize.DeserializeBinaryFromDisk<GeoData>(DataManager.Instance.DataFile);
                else
                    SINGLETON = Serialize.DeserializeBinaryFromResource<GeoData>("GeoDataSource.GeoDataSource.dat");

                if (SINGLETON.PostalCodes != null && SINGLETON.PostalCodes.Count > 0)
                {
                    //NOTE: over sample at search time is more effective & less ram usage
                    SINGLETON.PostalGeoHash = (from p in SINGLETON.PostalCodes
                                               //let harr = HashesSamples(p.Latitude, p.Longitude, .05m)
                                               let h = GetGeohash(p.Latitude, p.Longitude)
                                               where h != '\0'
                                               group p by h into hg
                                               select hg).ToDictionary(g => g.Key, g => new HashSet<PostalCode>(g) as ICollection<PostalCode>);
                }
            }
        }
        
        public static GeoData Current { get { return Inner.SINGLETON; } }

        [ProtoMember(1)]
        public ICollection<TimeZone> TimeZones { get; internal set; }
        [ProtoMember(2)]
        public ICollection<FeatureCode> FeatureCodes { get; internal set; }
        [ProtoMember(3)]
        public ICollection<Country> Countries { get; internal set; }
        [ProtoMember(4)]
        public ICollection<GeoName> GeoNames { get; internal set; }        
        [ProtoMember(5)]
        public ICollection<PostalCode> PostalCodes { get; internal set; }
        //[ProtoMember(6)]
        private IDictionary<char, ICollection<PostalCode>> PostalGeoHash { get; set; }

		//private static List<System.Globalization.RegionInfo> countries = null;

	    public FeatureCode FeatureCode(GeoName geoName)
	    {
	        return (from _fc in FeatureCodes
	                where _fc.Class == geoName.FeatureClass && _fc.Code == geoName.FeatureCodeId
	                select _fc).FirstOrDefault();
	    }

	    public TimeZone TimeZone(GeoName geoName)
        {
            return (from _tz in TimeZones
                    where string.Compare(_tz.TimeZoneId, geoName.TimeZoneId, true)==0
                    select _tz).FirstOrDefault();
        }
        public TimeZone TimeZone(string timeZoneId)
        {
            return (from _tz in TimeZones
                    where string.Compare(_tz.TimeZoneId, timeZoneId, true)==0
                    select _tz).FirstOrDefault();            
        }

		//private static Dictionary<string, List<GeoName>> countryProvinces = null;
		private static object _pLock = new object();

        public bool CountryHasProvince(string country, string province)
        {
            IEnumerable<GeoName> provinces = ProvincesByCountry(country);
            if (provinces != null)
            {
                var prov = (from p in provinces
                            where p.AlternateNames.Contains(province) || string.Compare(p.AsciiName, province, true)==0
                            select p);

                return (prov != null && prov.Count() > 0);
            }
            return false;
        }

        public bool ValidatePostalCodeByCountry(string Country, string Input)
        {
            var c = GetCountry(Country);
            var r = c.PostalCodeRegularExpression.Trim();
            if (c == null || string.IsNullOrEmpty(c.PostalCodeRegularExpression))
                return false;

            return Regex.Match(Input, r).Success;
        }

        public IEnumerable<PostalCode> PostalCodeInfo(string country, string code)
        {
            Country c = GetCountry(country);
            return (from p in PostalCodes
                    where p.Country.GeoNameId == c.GeoNameId && string.Compare(p.Code, code, true) == 0
                    select p);
        }

        public IEnumerable<PostalCode> PostalCodeNearBy(double lat, double lng, double radiusKm)
        {
            return PostalCodeNearBy((decimal)lat, (decimal)lng, radiusKm);
        }
        //SEE: http://gis.stackexchange.com/questions/8650/how-to-measure-the-accuracy-of-latitude-and-longitude
        public IEnumerable<PostalCode> PostalCodeNearBy(decimal lat, decimal lng, double radiusKm)
        {
            IEnumerable<PostalCode> res = null;
            if (PostalGeoHash != null && PostalGeoHash.Count > 0)
            {                
                if (radiusKm < 1)
                    radiusKm = 1;
                else if (radiusKm > 100)
                    radiusKm = 100; //max radius is 100k!

                decimal sampleSize = radiusKm < 1 ? 0 : (.01m * (decimal)radiusKm);
                IEnumerable<char> keys = HashesSamples(lat, lng, sampleSize);
                res = (from k in keys
                       where PostalGeoHash.ContainsKey(k)
                       from c in PostalGeoHash[k]
                       let dist = Distance.BetweenPlaces(c.Longitude, c.Latitude, lng, lat)
                       where dist <= radiusKm
                       select new { Dist = dist, Code = c }).OrderBy(o => o.Dist).Select(o => o.Code);
            }
            return res;
        }
        static decimal[] OverSample(decimal og, decimal sample)
        {
            return new[] { og, og + sample, og - sample };
        }
        static ICollection<char> HashesSamples(decimal lat, decimal lng, decimal sample)
        {
            decimal[] lats = OverSample(lat, sample);
            decimal[] lngs = OverSample(lng, sample);
            var h = new HashSet<char>();
            foreach (decimal y in lats)
            {
                foreach (decimal x in lngs)
                {
                    char c = GetGeohash(y, x);
                    if (c != '\0')
                        h.Add(c);
                }
            }
            return h;
        }

        static char GetGeohash(decimal lat, decimal lng)
        {
            try {
                if (lat < -90 || lat > 90)
                {
                    //lat = lat % 90;
                    return '\0';
                }
                if (lng < -180 || lng > 180)
                {
                    //lng = lng % 180;
                    return '\0';
                }

                //byte slat = (byte)(int) (Math.Floor(lat / 10) * 10);
                //byte slng = (byte)(int) (Math.Floor(lng / 10) * 10);

                byte slat = (byte)(int)lat;
                byte slng = (byte)(int)lng;

                var arr = new byte[2];
                arr[0] = BitConverter.GetBytes(slat)[0];
                arr[1] = BitConverter.GetBytes(slng)[0];
                return BitConverter.ToChar(arr, 0);
            }
            catch(OverflowException oex)
            {
                Console.Error.WriteLine("GetGeohash: lat{0} lng{1} => {2}", lat, lng, oex.Message);
                throw;
            }
        }

        public IEnumerable<GeoName> ProvincesByCountry(Country Country)
        {
            return from p in GeoNames
                   where p.FeatureClass == "ADM1" && string.Compare(p.CountryCode, Country.ISOAlpha2, true)==0
                   select p;
        }
        public IEnumerable<GeoName> ProvincesByCountry(string Country)
        {
            var country = GetCountry(Country);
            if (country != null)
                return ProvincesByCountry(country);
            else
                return null;
        }

	    public Country GetCountry(string Input)
	    {
	        var country = (from c in Countries where c.ISOAlpha2 == Input select c).FirstOrDefault();
	        if (country == null)
	        {
	            country = (from c in Countries where c.ISOAlpha3 == Input select c).FirstOrDefault();
                if (country == null)
                {
                    country = (from c in Countries where c.ISONumeric == Input select c).FirstOrDefault();
                    if (country == null)
                    {
                        country = (from c in Countries where c.Name == Input select c).FirstOrDefault();
                        if (country == null)
                        {
                            country = (from c in Countries where c.FipsCode == Input select c).FirstOrDefault();
                        }
                    }
                }
            }
            if(country!=null)
                country.PhoneInformation = PhoneManager.Current.AllByCountry(country.Name);

	        return country;
	    }
	  
	}
}