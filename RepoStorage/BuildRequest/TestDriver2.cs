////////////////////////////////////////////////////////////////////////////////
//  TestDriver2.cs - This is a test driver package to run tests               //
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
ITest.cs; CodeToTested2.cs; CodeToTested3.cd

Maintenance History:
====================
ver 1.0

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestInterfaces;
using CodeTest2;
using CodeTest3;
namespace TestDriver
{
  
        public class TestDriver2 : TestInterfaces.ITest
        {  
            private StringBuilder logs;
            public int[] numbers = { 98, 98, 100, 0 };
            public int[] newlist;
            // will be compiled into separate DLL

        //----< Testdriver constructor >---------------------------------
        
            public TestDriver2()
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
                return new TestDriver2();
            
			}
//----< test method is where all the testing gets done >---------
     public bool test()
            {
                  logs.Append("	entering into TestDriver2 ---> test() method");
					logs.Append("\n Testig Addition of list of numbers Test Case1");
            
                newlist = new List<int>(numbers).GetRange(0, 2).ToArray();
                int sum = TestCode2.addNumbers(newlist);
				
            if (sum == 196)
            {
                logs.Append("\nResult of test case 1 addition of numbers  ::  " + true);
				
                
            }
            else
            {
                logs.Append("\n Sum Execution failed");
                logs.Append("\n Result of test Case   :: "+ false);
            }
			logs.Append("\n Testing Division of numbers Test Case2");
			bool b = TestCode3.division(10,0);
				logs.Append("\nException Caught raised due to divison");
			logs.Append("\n Result of test case 2 division of numbers  :: "+false);
               return false;
            }
			
    
            
            //----< test stub - not run in test harness >--------------------

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
