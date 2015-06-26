using System;
using NUnit.Framework;

namespace Varekai.Tests
{
    public static class TestEx
    {
        public static void AssertExceptionMessageEquals(Action failingAction, string expectedErrorMessage)
        {
            try
            {
                failingAction();
            }
            catch (Exception ex)
            {
                Assert.AreEqual(expectedErrorMessage, ex.Message);

                throw;
            }
        }
    }
}

