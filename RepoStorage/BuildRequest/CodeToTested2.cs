///////////////////////////////////////////////////////////////////////////////
//  CodeToTested2.cs -       Test Code for Test Driver                       //
//  ver 1.0                                                                  //
//  Language:               Visual C#  2017                                  //
//  Platform:              Lenovo Z580 Windows 10                           //
//  Application:           Build Server , FL17                              //
//  Author:                Harika Bandaru                                   //
//                         hbandaru@syr.edu (936)-242-5972                  //
///////////////////////////////////////////////////////////////////////////////
/*
Module Operations:
==================
This module will act as test case for test driver

public:
------
addNumbers : Take List of Numbers and return the sum.

Compiler Command:
-------------------
csc /target:exe /define:Test_TestCode2 CodeToTested2.cs

BuildProcess
===============
Reuired files:
-----------------
TestDriver1.cs or TestDriver2.cs

Maintenance History:
====================
ver 1.0

*/


using System;

namespace CodeTest2
{
    public class TestCode2
    {
//  <----------------------Addition of Numbers---------------------------------->
        public static int addNumbers(params int[] numbers)
        {
            int sum = 0;
            foreach (int num in numbers)
                sum += num;
            return sum;
        }
//-----------------------< Test Stub >--------------------------------
#if (Test_TestCode2)
    static void Main(String[] args)
        {
          
          int[] num = {1,2,3,4};    
        int sum = TestCode.addNumbers(num);
        Console.Write("\n Sum of Numbers"+sum);
        }
#endif
    }
}
