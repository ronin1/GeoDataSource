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
            foreach(Country c in countries)
            {
                Assert.IsNotNull(c);
                CollectionAssert.IsNotEmpty(c.ContinentId, c.Name);
                CollectionAssert.IsNotEmpty(c.Name);
            }
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

        [TestCase("JP", "Aichi-Ken", ExpectedResult = true)]
        [TestCase("US", "New York", ExpectedResult = true)]
        [TestCase("US", "California", ExpectedResult = true)]
        [TestCase("CA", "British Columbia", ExpectedResult = true)]
        public bool CountryHasProvince(string country, string province)
        {
            var c = GeoData.Current.GetCountry(country);
            var provinces = GeoData.Current.ProvincesByCountry(c);
            return provinces.Any(p => string.Compare(p.Name, province, true) == 0);
        }

        [TestCase("US", "New York", ExpectedResult = "America/New_York")]
        [TestCase("US", "California", ExpectedResult = "America/Los_Angeles")]
        [TestCase("CA", "British Columbia", ExpectedResult = "America/Vancouver")]
        public string GetProvinceTimeZone(string country, string province)
        {
            var c = GeoData.Current.GetCountry(country);
            var pp = GeoData.Current.ProvincesByCountry(c);
            var prov = (from p in pp
                        where string.Compare(p.Name, province, true) == 0
                        select p).FirstOrDefault();

            Assert.IsNotNull(prov);
            Assert.IsNotNull(prov.Country);
            Assert.IsNotNull(prov.TimeZone);
            Assert.AreNotEqual(0, prov.Latitude + prov.Longitude);

            var tz = GeoData.Current.TimeZone(prov);           
            Assert.AreEqual(prov.TimeZone.TimeZoneId, tz.TimeZoneId);
            Assert.AreEqual(prov.TimeZone.CountryCode, tz.CountryCode);

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

        [TestCase("US", "90250", new[] { "Hawthorne", "California" })]
        [TestCase("US", "90210", new[] { "Beverly Hills", "California" })]
        [TestCase("US", "90405", new[] { "Santa Monica", "California" })]
        [TestCase("US", "90401", new[] { "Santa Monica", "California" })]
        public void PostalCodeInfo(string country, string code, string[] expectedNames)
        {
            IEnumerable<PostalCode> codes = GeoData.Current.PostalCodeInfo(country, code);
            CollectionAssert.IsNotEmpty(codes);
            foreach(string n in expectedNames)
            {
                PostalCode pc = codes.FirstOrDefault(c => string.Compare(c.Name, n, true) == 0);
                if (pc == null)
                    codes.FirstOrDefault(c => c.Admin3 != null && string.Compare(c.Admin3.Name, n, true) == 0);
                if (pc == null)
                    pc = codes.FirstOrDefault(c => c.Admin2 != null && string.Compare(c.Admin2.Name, n, true) == 0);
                if (pc == null)
                    pc = codes.FirstOrDefault(c => c.Admin1.Name != null && string.Compare(c.Admin1.Name, n, true) == 0);

                Assert.IsNotNull(pc, n);
            }
        }

        [TestCase(34.017667d, -118.494103d, 10d, new[] { "Santa Monica", "Venice", "Pacific Palisades", "Culver City" })]
        [TestCase(34.017667d, -118.494103d, 5d, new[] { "Santa Monica", "Venice" })]
        [TestCase(34.017667d, -118.494103d, .5d, new[] { "Santa Monica" })]
        [TestCase(34.017667d, -118.494103d, 5d, new[] { "Pacific Palisades", "Culver City" }, ExpectedException = typeof(AssertionException))]
        [TestCase(34.017667d, -118.494103d, .5d, new[] { "Venice", "Pacific Palisades", "Culver City" }, ExpectedException = typeof(AssertionException))]
        public void PostalCodeNearBy(double lat, double lng, double radiusKm, string[] expectedNames)
        {
            IEnumerable<PostalCode> codes = GeoData.Current.PostalCodeNearBy(lat, lng, radiusKm);
            CollectionAssert.IsNotEmpty(codes);

            foreach(PostalCode pc in codes)
            {
                double dist = Distance.BetweenPlaces((double)pc.Longitude, (double)pc.Latitude, lng, lat);
                Assert.LessOrEqual(Math.Floor(dist), Math.Ceiling(radiusKm), pc.Name + " dist: " + dist);
            }

            string missing = "";
            foreach (string n in expectedNames)
            {
                PostalCode pc = codes.FirstOrDefault(c => string.Compare(c.Name, n, true) == 0);
                if(pc == null)
                    missing += " " + n;
            }
            Assert.That(missing.Length == 0, missing);
        }
    }
}
