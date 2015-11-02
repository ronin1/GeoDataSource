using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GeoDataSource.Tests
{
    [TestFixture]
    public class ValidationTests
    {
        [TestCase("CA", "", ExpectedResult = false)]
        [TestCase("CA", "BLAH", ExpectedResult = false)]
        [TestCase("CA", "V5B 2Y2", ExpectedResult = true)]
        [TestCase("CA", "V5B2Y2", ExpectedResult = true)]
        [TestCase("US", "90025", ExpectedResult = true)]
        [TestCase("US", "90401", ExpectedResult = true)]
        [TestCase("US", "90405", ExpectedResult = true)]
        [TestCase("US", "90210", ExpectedResult = true)]
        [TestCase("US", "1111", ExpectedResult = false)]
        [TestCase("US", "999999", ExpectedResult = false)]
        [TestCase("US", "9999z", ExpectedResult = false)]
        [TestCase("US", "", ExpectedResult = false)]
        [TestCase("", "", ExpectedResult = false)]
        public bool CanadaValdiationWithSpaceInPostalCode(string country, string postal)
        {
            return GeoData.Current.ValidatePostalCodeByCountry(country, postal);
        }

        [TestCase("", ExpectedException = typeof(NullReferenceException))]
        [TestCase("111", ExpectedException = typeof(NullReferenceException))]
        [TestCase("254(70sdf0) 669!@#$%^&*()_024", ExpectedException = typeof(NullReferenceException))]
        [TestCase("1(604) 338 2512", ExpectedResult = "Canada")]
        [TestCase("52-1-(81) 8309-6670", ExpectedResult = "Mexico")]
        [TestCase("254(700) 669024", ExpectedResult = "Kenya")]
        [TestCase("52-55-12345678", ExpectedResult = "Mexico")]
        [TestCase("1-626-123-4567", ExpectedResult = "United States")]
        [TestCase("1(310) 123-4567", ExpectedResult = "United States")]
        public string PhoneDetection(string phone)
        {
            var p = PhoneManager.Current.AutoDetect(phone);
            return p.Country;
        }

        [TestCase("1(310) 123-4567", "United States", "Santa Monica")]
        [TestCase("1(310) 123-4567", "United States", "Los Angeles")]
        [TestCase("1(604) 338 2512", "Canada", "British Columbia")]
        public void BCCanadaPhoneTest(string phone, string expectCountry, string expectProvince)
        {
            var p = PhoneManager.Current.AutoDetect(phone);
            StringAssert.AreEqualIgnoringCase(expectCountry, p.Country);
            StringAssert.Contains(expectProvince.ToLower(), p.Comment.ToLower());
        }
        
    }
}
