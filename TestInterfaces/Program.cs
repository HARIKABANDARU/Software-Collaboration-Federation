////////////////////////////////////////////////////////////////////////////////
//  ITest.cs    -     This declares ITest Interface                           //
//  ver 1.0                                                                   //
//  Language:         Visual C#  2017                                         //
//  Platform:         Lenovo Z580 Windows 10                                  //
//  Application :     BuildServer , FL17                                      //
//  Author      :     Harika Bandaru, Syracuse University                     //
//  Source:     :      Jim Fawcett, Syracuse University, CST 2-187            //
//                    jfawcett@twcny.rr.com, (315) 443-3948                   //
////////////////////////////////////////////////////////////////////////////////

/*
Module Operations:
==================
This module provides ITest interface, declaring bool Test and getTestDriverLogs

public:
--------
test                :   Function used for calling the test cases that need to be tested
getTetDriverLogs    :   Provides the log information for all the TestDrivers and kept in TestLog XML file.

Compiler Command:
-----------------
csc /target:exe /define:Test_TestInterface ITest.cs

Maintenance History:
====================
ver 1.0

*/

using System;

namespace TestInterfaces
{
    // <----------------Interface provides two methods that are implemented by every TestDriver------>
    public interface ITest
    {
        bool test();
        String getTestDriverLogs();

    }
    //----------------------------< that are required to be implemented in test driver as it derives from ITest >-----
    class ITestDemo : ITest
    {
        public String getTestDriverLogs()
        {
            return "Hello World";
        }
        public bool test()
        {
            return true;
        }
        //----------------------------------------<Test Stub >----------------------------------------------------
#if (Test_TestInterface)
        public static void Main(String[] args)
        {
            ITest test = new ITestDemo();
            Console.Write("\n" + test.getTestDriverLogs());
            test.test();
        }
#endif
    }
}
