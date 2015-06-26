using System;
using NUnit.Framework;
using Varekai.Utils;

namespace Varekai.Tests.Utils
{
	[TestFixture]
	public class ParametersCheckExTests
	{
		[Test]
		public void ConvertToType_intCorrect ()
		{
			Assert.AreEqual ((int)5672, "5672".ConvertToType<string, int> ());
		}

		[Test]
		public void ConvertToType_boolCorrect ()
		{
			Assert.AreEqual (true, "true".ConvertToType<string, bool> ());
		}

		[Test]
		public void ConvertToType_doubleCorrect ()
		{
			Assert.AreEqual ((double)5672, "5672".ConvertToType<string, double> ());
		}

		[Test]
		[ExpectedException (typeof(ArgumentException))]
		public void ConvertToType_intMalformed ()
		{
			TestEx.AssertExceptionMessageEquals (() => "AAAA".ConvertToType<string, int> ("testParam")
				, "The parameter testParam with value AAAA cannot be converted to the type System.Int32");
		}

		[Test]
		[ExpectedException (typeof(ArgumentException))]
		public void ConvertToType_boolMalformed ()
		{
			TestEx.AssertExceptionMessageEquals (() => "AAAA".ConvertToType<string, bool> ("testParam")
				, "The parameter testParam with value AAAA cannot be converted to the type System.Boolean");
		}

		[Test]
		[ExpectedException (typeof(ArgumentNullException))]
		public void EnsureIsNotNull_nullString ()
		{
			string underTest = null;

			TestEx.AssertExceptionMessageEquals (() => underTest.EnsureIsNotNull ("testArgument")
				, "Argument cannot be null.\nParameter name: testArgument");
		}

		[Test]
		public void EnsureIsNotNull_notNullString ()
		{
			"stringNotNull".EnsureIsNotNull ("testArgument");

			Assert.IsTrue (true);
		}
	}
}

