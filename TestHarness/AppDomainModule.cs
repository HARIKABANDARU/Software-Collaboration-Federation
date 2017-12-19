//////////////////////////////////////////////////////////////////////////////////////
//  AppDomainModule.cs - ApplicationDomain that manages the AppDomain Creation and  //
//                       processing                                                 //
//  ver 1.0                                                                         //
//  Language:         Visual C#  2017                                               //
//  Platform:         Lenovo Z580 Windows 10                                        //
//  Application :     FederationComm , FL17                                         //
//  Author      :     Harika Bandaru, Syracuse University                           //
//                    hbandaru@syr.edu (936)-242-5972)                              //
//////////////////////////////////////////////////////////////////////////////////////
/*
 * Module Operations
 * ==================
 * This package helps in creating an appdomain, instance to loader that helps in loading test dll's amd
 * unloading the application domains
 * 
 * Methods
 * =================
 * public:
 * -------
 * createAppDomain  - creates a ChildAppDomain by taking the name and the path of testDll's to load into 
 *                     ChildAppDomain
 * getLoaderProxyInstance - to load the loader that performs testHarness functionality
 * unloadAppDomain  - Unloads Application Domain on test completion
 * 
 * private:
 * --------
 * configureAppDomainSetup : provides ChildAppDomain Configuration Information
 * 
 * BuildProcess:
 * =============
 * Required Files:
 * ---------------
 * Loader.cs; MockTestHarness.cs
 * 
 * Compiler Command:
 * -=================
 * csc /target:exe  /r:"..../Loader/bin/debug/Display.dll" /define:MockTestHarness AppDomainModule.cs
 * 
 * Maintenanace History
 * =====================
 * ver 1.0
 * 
 * */
using System;
using System.Runtime.Remoting;
using System.Security.Policy;

namespace BuildServerFederation
{
    class AppDomainModule
    {
        private AppDomain childAppDomain;

        //--------------< configuring application domain for new child applicationdomain using appDomainSetup >-----------
        private AppDomainSetup configureAppDomainSetup(String testHarnessPath)
        {
            AppDomainSetup appDomainSetup = new AppDomainSetup();
            appDomainSetup.PrivateBinPath = testHarnessPath;
            appDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            return appDomainSetup;
        }


        //---------------------------< Creating AppDomain with appDomain name and Directory where the Dll are present >--------
        public AppDomain createAppDomain(String appDomain, String privateBinPath)
        {
            try
            {
                AppDomainSetup appDomainSetup = configureAppDomainSetup(privateBinPath);
                Evidence evidence = AppDomain.CurrentDomain.Evidence;
                childAppDomain = AppDomain.CreateDomain(appDomain, evidence, appDomainSetup);
            }
            catch (Exception e)
            {
                Console.Write("\n Exception caught{0}", e.Message);
            }
            return childAppDomain;
        }

        //-------------------< loader proxy instance by using ChildAppDomain >------------------------------
        public LoaderProxy getLoaderProxyInstance()
        {
            childAppDomain.Load("Loader");
            ObjectHandle oh = childAppDomain.CreateInstance(typeof(LoaderProxy).Assembly.FullName, typeof(LoaderProxy).FullName);
            LoaderProxy loaderProxy = oh.Unwrap() as LoaderProxy;
            return loaderProxy;
        }

        //------------------------------< Unloading loaded AppDomain >-----------------------------------
        public void unloadAppDomain(AppDomain domain)
        {
            Console.Write("\n------------------------------------------------------------------------\n");
            Console.Write("     Unloading child application domain");
            AppDomain.Unload(domain);
            Console.Write("\n unloaded successfuly ");
            Console.Write("\n---------------------------------------------------------------------------\n");
        }
        //----------------< Test-stub >--------------------------------------------------------------
#if (Test_AppDomainModule)
         static void Main(string[] args)
            {
                AppDomainmodule appDomainModule = new AppDomainModule();
                AppDomain childAppDomain = appDomainModule.createAppDomain(authorName, privateBinPath);
                LoaderProxy loaderProxy = appDomainModule.getLoaderProxyInstance();
             
            }
#endif

    }
}
