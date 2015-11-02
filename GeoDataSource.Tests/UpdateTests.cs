using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GeoDataSource.Tests
{
    [TestFixture(Category = "NotAppVeyor")]
    public class UpdateTests
    {
        [Explicit("This is not really a test. Run to update local dat file.")]
        [Test]
        public void TestUpdateProcess()
        {
            //just make sure that this sucker doesn't throw an exception
            DataManager.Instance.Update().Wait();
        }
    }
}
