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
            }
        }
        public static GeoData Current { get { return Inner.SINGLETON; } }

        [ProtoMember(1)]
        public ICollection<TimeZone> TimeZones { get; internal set; }
        [ProtoMember(2)]
        public ICollection<FeatureCode> FeatureCodes { get; internal set; }
        [ProtoMember(3)]
        public ICollection<GeoName> GeoNames { get; internal set; }
        [ProtoMember(4)]
        public ICollection<Country> Countries { get; internal set; }
        [ProtoMember(5)]
        public ICollection<PostalCode> PostalCodes { get; internal set; }

		//private static List<System.Globalization.RegionInfo> countries = null;

	    public FeatureCode FeatureCode(GeoName geoName)
	    {
	        return (from _fc in FeatureCodes
	                where _fc.Class == geoName.FeatureClass && _fc.Code == geoName.FeatureCodeId
	                select _fc).FirstOrDefault();
	    }

	    public TimeZone TimeZone(GeoName geoName)
        {
            return (from _tz in TimeZones where _tz.TimeZoneId == geoName.TimeZoneId select _tz).FirstOrDefault();
        }
        public TimeZone TimeZone(string TimeZoneId)
        {
            return (from _tz in TimeZones where _tz.TimeZoneId == TimeZoneId select _tz).FirstOrDefault();            
        }

		//private static Dictionary<string, List<GeoName>> countryProvinces = null;
		private static object _pLock = new object();

        public bool CountryHasProvince(string Country, string Province)
        {
            IEnumerable<GeoName> provinces = ProvincesByCountry(Country);
            if (provinces != null)
            {
                var prov = (from p in provinces
                            where p.AlternateNames.Contains(Province) || p.AsciiName == Province
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

        public IEnumerable<GeoName> ProvincesByCountry(Country Country)
        {
            return from p in GeoNames where p.FeatureClass == "ADM1" && p.CountryCode == Country.ISOAlpha2 select p;
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