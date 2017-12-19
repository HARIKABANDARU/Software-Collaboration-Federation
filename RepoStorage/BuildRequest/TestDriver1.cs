////////////////////////////////////////////////////////////////////////////////
// TestDriver1.cs - define a test to run                                      //
//  ver 1.0                                                                   //
//  Language:      Visual C#  2017                                            //
//  Platform:      Lenovo Z580 Windows 10                                     //
//  Application:   BuildServer , FL17                                         //
//  Author:        Harika Bandaru, Syracuse University                        //
//                 (936) 242-5972, hbandaru@syr.edu                           //
//  Source          Jim Fawcett, Syracuse University, CST 2-187               //    
//                  jfawcett @twcny.rr.com, (315) 443-3948                    //
////////////////////////////////////////////////////////////////////////////////
/*
*   Test driver needs to know the types and their interfaces
*   used by the code it will test.  It doesn't need to know
*   anything about the test harness.
*/
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
Should include TestInterfaces and CodeToTest1 in references.

BuildProcess
===============
Reuired files:
-----------------
ITest.cs; CodeToTested1.cs

Maintenance History:
====================
ver 1.0

*/
using System;
using System.Text;
using TestInterfaces;
using CodeTest1;

namespace TestDriver
{

    public class TestDriver1 : TestInterfaces.ITest
    {
        
        private StringBuilder logs;
//<------------TestDriver1 Constructor------------------>
        public TestDriver1()
        {
           
            logs = new StringBuilder();
        }
//  <--------------------Implemented ITest method for Test Logs---------------------->
        public String getTestDriverLogs()
        {
            return logs.ToString();
        }
 // <---------------------Creates an instance of TestDriver1---------------------------->
        public static ITest create()
        {
            return new TestDriver1();
        }
        //----< test method is where all the testing gets done >---------

        public bool test()
        {
            logs.Append("entering into TestDriver1-----> test() method");
            TestCode.annunciator("first being tested");
            logs.Append("\n Execution of Test Code from test Driver");
            logs.Append("\n Result of test case" + true);
            return true;
        }

#if (TestDriverTest)
    
    static void Main(string[] args)
        {
            Console.Write("\n  Local test:\n");

            ITest test = TestDriver1.create();

            if (test.test() == true)
                Console.Write("\n  test passed");
            else
                Console.Write("\n  test failed");
            Console.Write("\n\n");
        }
#endif
    }
}
