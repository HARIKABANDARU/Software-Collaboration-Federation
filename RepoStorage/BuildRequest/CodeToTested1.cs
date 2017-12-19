///////////////////////////////////////////////////////////////////////////////
//  CodeToTested1.cs -       Test Code for Test Driver                       //
//  ver 1.0                                                                  //
//  Language:               Visual C#  2017                                  //
//  Platform:              Lenovo Z580 Windows 10                           //
//  Application:           Build Server , FL17                              //
//  Author:        Dr.Fawcet Syracuse University                             //
//                                                                           //
///////////////////////////////////////////////////////////////////////////////
/*
Module Operations:
==================
This module will act as test case for test driver

public:
------
annunciator  : Takes a argument as string and displays it.

Compiler Command:
-------------------
csc /target:exe /define:Test_TestCode CodeToTested1.cs

BuildProcess
===============
Reuired files:
-----------------
TestDriver1.cs

Maintenance History:
====================
ver 1.0

*/

using System;
namespace CodeTest1

{
    public class TestCode
    {
        public static void annunciator(string msg)
        {
            Console.Write("\n  Production Code: {0}", msg);
        }
#if (Test_TestCode)
    static void Main(String[] args)
    {
        TestCode td = new TestCode();
        TestCode.annunciator("Hello World");
     }
#endif
}

}
