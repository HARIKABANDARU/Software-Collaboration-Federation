////////////////////////////////////////////////////////////////////////////////
//  BuildLibrary.cs -   Build a DLL on Cliet's Build Request                  //
//  ver 1.0                                                                   //
//  Language:         Visual C#  2017                                         //
//  Platform:         Lenovo Z580 Windows 10                                  //
//  Application :     BuildServer , FL17                                      //
//  Author      :     Harika Bandaru, Syracuse University                     //
//                    hbandaru@syr.edu (936)-242-5972)                        //
////////////////////////////////////////////////////////////////////////////////

/*
Module Operations:
==================
The main purpose of the module is to Build a DLL by using the Process class.

public:
-------
libraryCreation         :  Provides information regarding the DLL name and testData to create DLL
                           to libraryCreationHelper
libraryCreationHelper   :   Uses the Process class to create a library which invokes cmd.exe.

Build Process:
================
Required files:
BuildServerController.cs


Maintenance History:
====================
ver 1.0

*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using FederationCommService;
#pragma warning disable CS2002

namespace BuildServerFederation
{
    class BuildLibrary
    {
        public String dllName { get; set; } = "";
        public String buildStatus { get; set; }
        public BuildServer bldse { get; set; }
        public CommMessage rply { get; set; } = new CommMessage(CommMessage.MessageType.TestResult);
        String logInfo { get; set; }
        //-----------< Used to intialisise the Environment for Client Comm >---------------------------------
        public BuildLibrary()
        {
            rply.to = ClientEnvironment.endPoint;
            rply.from = EnvironmentSet.address + ":" + (EnvironmentSet.port).ToString() + "/IMessagePassingComm";
            bldse = new BuildServer();
        }
        //<-----------------Gets data required for building a DLL------------------------------->
        public String libraryCreation(String directoryPath, TestRequestData datasent, string path)
        {
            rply.to = ClientEnvironment.endPoint;
            rply.from = EnvironmentSet.address + ":" + (EnvironmentSet.port).ToString() + "/IMessagePassingComm";
            bldse = new BuildServer();
            List<String> tdrivertCase = new List<String>(10);
            try
            {
                String[] files = Directory.GetFiles(directoryPath, datasent.testDriver);
                foreach (String file in files)
                {
                    tdrivertCase.Add(file);
                    dllName = Path.GetFileNameWithoutExtension(file);
                }
                foreach (String file in datasent.testCode)
                {
                    tdrivertCase.Add(file);
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            String status = libraryCreationHelper(tdrivertCase, directoryPath, datasent, dllName, path);
            datasent.status = status;
            return status;
        }
        // <------------------------------------Creates a DLL using Process-------------------->
        public String libraryCreationHelper(List<String> fileNames, String directory, TestRequestData datasent, String dllName, String path)
        {
            try
            {
                Process Process = new Process();
                String commandNames = "", command;
                foreach (String name in fileNames)
                {
                    commandNames = Path.GetFileName(name) + "  " + commandNames;
                }
                if (datasent.toolChain.Equals("CPP"))
                {
                    Console.WriteLine("\n========================\nBuildig DLL for CPP Tested Codes\n=======================\n");
                    command = @"/C cl /EHsc /nologo  /D_USRDLL /D_WINDLL " + commandNames + "/link /DLL /OUT:" + dllName + ".dll  ";
                }
                else
                {
                    command = @"/C csc /nologo /target:library /out:" + dllName + ".dll /r:../../TestInterfaces/bin/debug/TestInterfaces.dll " + commandNames;
                }
                Process.StartInfo.FileName = "cmd.exe";
                Process.StartInfo.Arguments = command;
                Process.StartInfo.WorkingDirectory = Path.GetFullPath(directory);
                Process.StartInfo.UseShellExecute = false;
                Process.StartInfo.RedirectStandardOutput = true;
                Process.StartInfo.RedirectStandardError = true;
                Process.StartInfo.CreateNoWindow = true;
                try
                {
                    Process.Start();
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
                logInfo = Process.StandardError.ReadToEnd();
                if (datasent.toolChain.Equals("C#"))
                    logInfo = logInfo + Process.StandardOutput.ReadToEnd();
                datasent.logInfo = logInfo;
                if (logInfo.Length <= 0)
                {
                    buildStatus = "Success";
                    rply.body = datasent.testName + "  " + buildStatus + "  " + "Errors : 0 Warnings : 0";
                    datasent.logInfo = "Errors:0 Warnings: 0";
                }
                else
                {
                    buildStatus = "Failed";
                    rply.body = datasent.testName + "   " + buildStatus;
                }
                Process.WaitForExit();
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            return buildStatus;
        }
        //------------------------------------< Test Stub >------------------------------------------------------------
#if (Test_BuildLibrary)
 
    static void Main(String[] args)
    {
        XmlParser xmlparse = new XmlParser();
        BuildLibrary build = new BuildLibrary();
     /*   try{
        String request = "../../../RepoStorage/BuildRequest.xml";
        List<TestRequestData> sampledata = xmlparse.parseXmlRequest(request);
        String buildStatus;
        foreach (TestRequestData reqData in sampledata){
                  if (reqData.Count > 0)
                {
                    authorName = reqData[0].authorName;
                    directorypath = "../../"+authorName + DateTime.Now.ToFileTime();
                    Directory.createDirectory(directorypath);
                    Console.Write("\n\n Created Temporary Directory  :: " + directorypath);
                    buildStatus =  build.libraryCreation(directorypath,reqData);
                }
            else{
                Console.Write("No Data Present");
                }
        Console.Write("\n Build Status"+buildStatus);
              
        }
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }*/
}
#endif
    }
}
