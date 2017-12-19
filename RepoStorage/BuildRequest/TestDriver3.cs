////////////////////////////////////////////////////////////////////////////////
//  TestDriver3.cs - This is a test driver package to run tests               //
//  ver 1.0                                                                   //
//  Language:      Visual C#  2017                                            //
//  Platform:      Lenovo Z580 Windows 10                                     //
//  Application:   BuildServer , FL17                                         //
//  Author:        Harika Bandaru, Syracuse University                        //
//                 (936) 242-5972, hbandaru@syr.edu                           //
////////////////////////////////////////////////////////////////////////////////

/*
Module Operations:
==================
This package is used to test the testcode/tetcases

public:
------
Implements methods of ITest
test() That invokes the test code methods
getTestDriverLogs : Returns Log information

Compiler Command:
-------------------
Should include TestInterfaces; CodeToTested2 and CodeToTested3 in references.

BuildProcess
===============
Reuired files:
-----------------
ITest.cs; CodeToTested1.cs; CodeToTested2.cs

Maintenance History:
====================
ver 1.0

*/

using System;
using System.Text;
using TestInterfaces;
namespace TestDriver
{
    public class TestDriver3 : ITest
    {
        private StringBuilder logs;
//<------------TestDriver3 Constructor------------------>
        public TestDriver3()
        {
            logs = new StringBuilder();
        }
   
//  <--------------------Implemented ITest method for Test Logs---------------------->
        public string getTestDriverLogs()
        {
            return logs.ToString();
        }
// <---------------------Creates an instance of TestDriver3---------------------------->
		public static ITest create()
        {
            return new TestDriver3();
        }

  //----< test method is where all the testing gets done --------->
//<-----------------------test method demonstrate how test harness will handle null pointer exception in test driver that doesn't catch it.------>
        public bool test()
		{
            bool result = false;
            logs.Append("\n entering into TestDriver3 - > test() method");

            logs.Append("\n Demonstrating how test harness will handle null pointer exception in test driver that doesn't catch it");
            logs.Append("with out initializing System.Test.StringBuilder sBuilder,  Test driver is calling one of its method sBuilder.Append()");
            System.Text.StringBuilder sBuilder = null;
            sBuilder.Append("won't work");
            logs.Append("\nResult :- " + result);
            return result;
        }
#if (TestDriver_Test)
    static void main(String[] args)
    {
         Console.Write("\n  Local test:\n");

            ITest test = TestDriver3.create();
        try{
            if (test.test() == true)
                Console.Write("\n  test passed");
            else
                Console.Write("\n  test failed");
        }
        catch(Exception e)
        {
            Console.Write(e.Message);
        }
            Console.Write("\n\n");
    }
#endif
		
    }
	
}