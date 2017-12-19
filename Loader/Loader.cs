////////////////////////////////////////////////////////////////////////////////////////
//  Loader.cs   :     This module provides a loader which acts a proxy for AppDomain  //
//  ver 1.0                                                                           //
//  Language:         Visual C#  2017                                                 //
//  Platform:         Lenovo Z580 Windows 10                                          //
//  Application :     FederationComm , FL17                                           //
//  Author      :     Harika Bandaru, Syracuse University                             //
//                    hbandaru@syr.edu (936)-242-5972)                                //
////////////////////////////////////////////////////////////////////////////////////////
/*
 * ModularOperation
 * ================
 * This module helps to load all test libraries into the child AppDomain and execute the test if the
 * library inherits the ITest interface
 * 
 * Interface:
 * ===========
 * public
 * -------
 * loadAllAssemblies             : Loads all assemblies present in the TestRequest file into the proxy
 * createInstaceforTesting       : Create an instance to test driver that are inherited from ITest interface
 * run                           : Test the test code files with test driver instance.
 * 
 * BuildProcess:
 * =============
 * RequiredFiles:
 * --------------
 * Loader.cs;ITest.cs; AppDomainModule.cs
 * 
 * 
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TestInterfaces;
using FederationCommService;
namespace BuildServerFederation
{
    public class LoaderProxy : MarshalByRefObject
    {
        public String assemblyLookUpPath { get; set; }
        CommMessage treply { get; set; } = null;
        //-------------< A struct data type for storing the testdriver name and it's corresponding test driver instance >-----------
        private struct TestData
        {
            public String Name { get; set; }
            public ITest testDriver { get; set; }
        }
        public LoaderProxy()
        {
            treply = new CommMessage(CommMessage.MessageType.TestResult);
        }
        List<TestData> testDriver = new List<TestData>();
        //-------------------< load the assembely files into proxy >-----------------------------------
        public void loadAllAssemblies(String[] files)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("=========================================\n    Requirement #9 Loading Dll's  \n===========================================");
            Console.ResetColor();
            Console.Write("\n Loading all assemblies related to test request \n");
            foreach (string file in files)
            {
                Assembly assem = Assembly.LoadFrom(file);
                Console.Write("\n Loading " + assem.FullName);
            }
            Console.Write("\n\n Loaded successfuly");

        }

        public CommMessage loaderProxyMessage()
        {
            treply.to = ClientEnvironment.endPoint;
            treply.from = TestHarness.endPoint;
            return treply;
        }
        //-----------------------< create an instance of test driver if it implements ITest interface using reflections >---------------
        public bool createInstaceforTesting()
        {
            ArrayList testLogs = new ArrayList();
            try
            {
                Assembly[] assembiles = AppDomain.CurrentDomain.GetAssemblies();
                if (assembiles.Length == 0)
                    return false;
                foreach (Assembly assembly in assembiles)
                {
                    if (assembly.FullName.IndexOf("mscorlib") != -1)
                        continue;
                    if (assembly.FullName.IndexOf("Loader") != -1)
                        continue;
                    Type[] types = assembly.GetExportedTypes();
                    foreach (Type t in types)
                    {
                        Type[] typeInterfaces = typeof(ITest).GetInterfaces();
                        if (t.IsClass && typeof(ITest).IsAssignableFrom(t))
                        {
                            ITest tdr = (ITest)Activator.CreateInstance(t);
                            TestData td = new TestData();
                            td.Name = t.Name;
                            td.testDriver = tdr;
                            testDriver.Add(td);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("\n exception caught " + e);
                treply.errorMsg = e.Message;
            }
            return testDriver.Count > 0;
        }
        //----------------------------< test the test code using the intance of test driver created by using reflections >----------
        public ArrayList run()
        {
            ArrayList testLogs = new ArrayList();
            if (testDriver.Count == 0)
                return testLogs;
            foreach (TestData td in testDriver)
            {
                try
                {
                    Console.Write("\n=====================================================================\n   Execute Test ## Requirement #9\n=====================================================================");
                    Console.Write("\n\n {0,-12} :  {1}", "testing", td.Name);
                    if (td.testDriver.test() == true)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write("\n test Passed");
                        treply.body = treply.body + "  " + td.Name + " Passed Warnings : 0 Errors : 0";
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write("\n test Failed");
                        treply.body = treply.body + " " + td.Name + " Test Failed";
                    }
                    testLogs.Add(td.testDriver.getTestDriverLogs());
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("\n Exception Caught by Test Harness   {0}", e.Message);
                    testLogs.Add(e.Message);
                    treply.errorMsg = e.Message;
                }
                finally
                {
                    Console.ResetColor();
                }
            }
            testDriver.Clear();
            return testLogs;
        }

        //-------------------< Test-stub >-------------------------------------------------------------
#if (Test_LoaderModule)
         static void Main(string[] args)
            {
                Loader loader = new Loader();
                loader.createInstaceforTesting();
                loader.run();
            }
#endif

    }
}
