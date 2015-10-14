using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZBrad.WcfLib;


namespace WcfLibTests
{
    /// <summary>
    /// Summary description for UtilTests
    /// </summary>
    [TestClass]
    public class UtilTests
    {
        public UtilTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void ListEquals()
        {
            var list1 = new List<int>(new int[] { 1, 2, 3 }); // baseline
            var list2 = new List<int>(new int[] { 1, 2, 3 }); // equal
            var list3 = new List<int>(new int[] { 0, 1, 2, 3 }); // longer
            var list4 = new List<int>(new int[] { 1, 2 }); // shorter

            Assert.IsTrue(Util.ListEquals(list1, list2));
            Assert.IsFalse(Util.ListEquals(list1, list3));
            Assert.IsFalse(Util.ListEquals(list1, list4));
        }
    }
}
