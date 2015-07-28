using System;
using NUnit.Framework;
using Varekai.Utils;

namespace Varekai.Utils.Tests
{
	[TestFixture]
	public class ParametersCheckExTests
	{
		[Test]
        [Description(
            "GIVEN a number as a string" +
            "WHEN converted to int" +
            "THEN succeeds")]
		public void ConvertToTypeIntCorrect ()
		{
            Assert.AreEqual ((int)5672, "5672".CastFromTo<string, int> ());
		}

		[Test]
        [Description(
            "GIVEN a bool as a string" +
            "WHEN converted to bool" +
            "THEN succeeds")]
		public void ConvertToTypeBoolCorrect ()
		{
            Assert.AreEqual (true, "true".CastFromTo<string, bool> ());
		}

		[Test]
        [Description(
            "GIVEN a decimal number as a string" +
            "WHEN converted to double" +
            "THEN succeeds")]
		public void ConvertToTypeDoubleCorrect ()
		{
            Assert.AreEqual ((double)5672, "5672".CastFromTo<string, double> ());
		}

		[Test]
        [Description(
            "GIVEN a text" +
            "WHEN converted to int" +
            "THEN throws an exception")]
		public void ConvertToTypeIntMalformed ()
		{
            Assert.Throws<ArgumentException>(
                () => "AAAA".CastFromTo<string, int> ("testParam"),
                "The parameter testParam with value AAAA cannot be converted to the type System.Int32"
            );
		}

		[Test]
        [Description(
            "GIVEN a text" +
            "WHEN converted to bool" +
            "THEN throws an exception")]
		public void ConvertToTypeBoolMalformed ()
		{
            Assert.Throws<ArgumentException>(
                () => "AAAA".CastFromTo<string, bool> ("testParam"),
                "The parameter testParam with value AAAA cannot be converted to the type System.Boolean"
            );
		}

		[Test]
        [Description(
            "GIVEN a null string" +
            "WHEN tested to make sure is not null" +
            "THEN throws an exception")]
		public void EnsureIsNotNullNullString ()
		{
			string underTest = null;
            
            Assert.Throws<ArgumentNullException>(
                () => underTest.EnsureIsNotNull ("testArgument"),
                "Argument cannot be null.\nParameter name: testArgument"
            );
		}

		[Test]
        [Description(
            "GIVEN a text" +
            "WHEN tested to make sure is not null" +
            "THEN succeeds")]
		public void EnsureIsNotNullNotNullString ()
		{
			"stringNotNull".EnsureIsNotNull ("testArgument");

			Assert.IsTrue (true);
		}
	}
}