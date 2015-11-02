using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GeoDataSource.Tests
{
    [TestFixture]
    public class CountrieProvinceTests
    {
        [Test]
        public void GetCountries()
        {
            var countries = GeoData.Current.Countries;
            Assert.IsNotNull(countries);
            Assert.Greater(countries.Count(), 200);
            countries.ForEach(c =>
            {
                Assert.IsNotNull(c);
                CollectionAssert.IsNotEmpty(c.ContinentId, c.Name);
                CollectionAssert.IsNotEmpty(c.Name);
            });
        }

        [TestCase("124", ExpectedResult = "Canada")]
        [TestCase("CA", ExpectedResult = "Canada")]
        [TestCase("CAN", ExpectedResult = "Canada")]
        [TestCase("US", ExpectedResult = "United States")]
        [TestCase("USA", ExpectedResult = "United States")]
        [TestCase("United States", ExpectedResult = "United States")]
        [TestCase("JP", ExpectedResult = "Japan")]
        [TestCase("Japan", ExpectedResult = "Japan")]
        [TestCase("JPN", ExpectedResult = "Japan")]
        [TestCase("3R!", ExpectedException = typeof(NullReferenceException))]
        [TestCase("#R!", ExpectedException = typeof(NullReferenceException))]
        [TestCase(null, ExpectedException = typeof(NullReferenceException))]
        public string GetCountry(string name)
        {
            Country c = GeoData.Current.GetCountry(name);
            return c.Name;
        }

        //[TestCase("JP", "Aichi-Ken", ExpectedResult = true)]
        //[TestCase("US", "New York", ExpectedResult = true)]
        [TestCase("US", "California", ExpectedResult = true)]
        //[TestCase("CA", "British Columbia", ExpectedResult = true)]
        public bool CountryHasProvince(string country, string province)
        {
            var c = GeoData.Current.GetCountry(country);
            var provinces = GeoData.Current.ProvincesByCountry(c);
            return provinces.Any(p => string.Compare(p.Name, province, true) == 0);
        }

        [TestCase("US", "New York", ExpectedResult = "America/New_York")]
        [TestCase("US", "California", ExpectedResult = "America/Los_Angeles")]
        [TestCase("CA", "British Columbia", ExpectedResult = "America/Vancouver")]
        public string GetProvinceBCTimeZone(string country, string province)
        {
            var c = GeoData.Current.GetCountry(country);
            var pp = GeoData.Current.ProvincesByCountry(c);
            var prov = (from p in pp
                        where string.Compare(p.Name, province, true) == 0
                        select p).FirstOrDefault();

            var tz = GeoData.Current.TimeZone(prov);
            return tz.TimeZoneId;
        }

        [TestCase("US", "New York", ExpectedResult = -5.0d)]
        [TestCase("US", "California", ExpectedResult = -8.0d)]
        [TestCase("CA", "British Columbia", ExpectedResult = -8.0d)]
        public double GetProvinceTimeGmtOffset(string country, string province)
        {
            var c = GeoData.Current.GetCountry(country);
            var pp = GeoData.Current.ProvincesByCountry(c);
            var prov = (from p in pp
                      where string.Compare(p.Name, province, true) == 0
                      select p).FirstOrDefault();

            var tz = GeoData.Current.TimeZone(prov);
            return tz.GMTOffSet;
        }

        [TestCase("Mexico", ExpectedResult = 52)]
        [TestCase("Canada", ExpectedResult = 1)]
        [TestCase("United States", ExpectedResult = 1)]
        public int USAPhones(string country)
        {
            var phones = PhoneManager.Current.AllByCountry(country);
            CollectionAssert.IsNotEmpty(phones);
            return phones.First().CountryCode;
        }


    }
}
