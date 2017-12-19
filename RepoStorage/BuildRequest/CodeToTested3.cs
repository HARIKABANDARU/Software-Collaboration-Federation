///////////////////////////////////////////////////////////////////////////////
//  CodeToTested3.cs -       Test Code for Test Driver                       //
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
csc /target:exe /define:Test_TestCode3 CodeToTested3.cs

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeTest3
{
    public class TestCode3
    {        
//<-----------Exception caught that is raised due to division by Zero------------------------------>
		public static bool division(int num1,int num2){
			try{
				int result = num1/num2;
				return true;			
			}
	catch(DivideByZeroException)
	{
		 Console.ForegroundColor = ConsoleColor.DarkRed;
		Console.WriteLine("\nException in child AppDomain ",num1);
		Console.ResetColor();
		return false;
	}
	
}
}
}
