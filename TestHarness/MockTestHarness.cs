//////////////////////////////////////////////////////////////////////////////////////
//  MockTestHarness.cs -  MockTestHarness parses TestRequest, start TestHarness     //
//                        into the loader of the childAppDomain                     //
//                                                                                  //      
//  ver 1.0                                                                         //
//  Language:         Visual C#  2017                                               //
//  Platform:         Lenovo Z580 Windows 10                                        //
//  Application :     FederationComm , FL17                                         //
//  Author      :     Harika Bandaru, Syracuse University                           //
//                    hbandaru@syr.edu (936)-242-5972)                              //
//////////////////////////////////////////////////////////////////////////////////////
/*
 * Modular Operations
 * ===================
 * This module have a refernce to Comm to communicate with other servers in the Project ; uses XmlManager
 * to parse the TestRequest sent by the ChildBuilder; start processing of test dll present in the
 * testrequest using AppDomain
 * 
 * public:
 * --------
 * parseTestRequest :   Parses the Test Request sent by the Build Server and creates a temporary directory 
 *                      Posts a File Request Message to the Child Build Server
 * startTestProcessing :Starts to process test dll by loading it into the Appdomain with the help of loader
 * 
 * private :
 * ----------
 * initializeEnvironment : Environment set-up for the TestHarness Comm
 * initialiseMessageProcess : It acts like message querry depending on the type of CommMessage recieved 
 *                            it process the message and calls the required functions
 * rcvThreadProc            : Reciever blocks on getMessage and waits till the message is recieved into t
 *                            the recievr's queue it runs on seperate Thread.
 * BuildProcess:
 * ==============
 * Required Files:
 * ----------------
 * MockTestHarness.cs ; AppDomainModule.cs; XmlParser.cs; Loader.cs ; ITest.cs Program.cs ; Environment.cs
 * 
 * Maintenanace History
 * =====================
 *  ver 1.0
 * 
 * */
using System;
using System.Collections.Generic;
using FederationCommService;
using System.Threading;
using System.Collections;
using System.IO;

namespace BuildServerFederation
{
    class MockTestHarness
    {
        Comm comm { get; set; } = null;
        Thread rcvThread { get; set; } = null;
        XmlParser xmlParse { get; set; } = null;
        List<TestRequestData> Data { get; set; } = null;
        public string tlogmesssage { get; set; } = null;
        CommMessage tlogmessage { get; set; } = null;
        CommMessage tclientMessage { get; set; } = null;
        private AppDomainModule appDomainModule;
        private LoaderProxy prox;
        // --------------------< Constructor that initialisies the comm, Environment, and other required classes >-----------
        MockTestHarness()
        {
            initializeEnvironment();
            comm = new Comm(EnvironmentSet.address, EnvironmentSet.port);
            Data = new List<TestRequestData>();
            appDomainModule = new AppDomainModule();
            prox = new LoaderProxy();
            xmlParse = new XmlParser();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }
        //------------------- < Inititalise the environment for the TestHarness >------------
        void initializeEnvironment()
        {
            EnvironmentSet.root = TestHarness.root;
            EnvironmentSet.address = TestHarness.address;
            EnvironmentSet.port = TestHarness.port;
            EnvironmentSet.endPoint = TestHarness.endPoint;
        }

        // ---------------------< Uses XmlManager to parse TestRequest sent by the BuildServerProcessPool >------------
        public void parseTestRequest(CommMessage msg)
        {
            List<string> fileNames = new List<string>();
            this.Data = xmlParse.parseXMLRequest(msg.body);
            foreach (TestRequestData data in Data)
            {
                fileNames.Add(data.testDriver);
                foreach (string td in data.testCode)
                    fileNames.Add(td);
            }
            TestHarness.root = EnvironmentSet.root + "/" + Data[0].authorName + DateTime.Now.ToFileTime();
            if (!System.IO.Directory.Exists(TestHarness.root))
            {
                System.IO.Directory.CreateDirectory(TestHarness.root);
            }
            CommMessage filerqst = msg.clone();
            filerqst.type = CommMessage.MessageType.FileRequest;
            filerqst.to = msg.from;
            filerqst.from = msg.to;
            filerqst.arguments = fileNames;
            this.comm.postMessage(filerqst);
        }

        //-----------------------< To process the message recieved depending on message type >------------
        bool initialiseMessageProcess(CommMessage msg)
        {
            if (msg.type == CommMessage.MessageType.TestRequest)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.Write("\n=======================================================================\n   Requirement #8 Test Request Sent by Child Build Server\n=======================================================================");
                Console.ResetColor();
                Console.Write("\nTest Request{0}\n\n", msg.body);
                if (msg.body != null)
                    parseTestRequest(msg);
            }
            if (msg.type == CommMessage.MessageType.FileList)
            {
                CommMessage msg2 = msg.clone();
                msg2.type = CommMessage.MessageType.TestComplete;
                msg2.to = msg.from;
                msg2.from = msg.to;
                this.comm.postMessage(msg2);
                startTestProcessing();
                this.tlogmessage.body = tlogmesssage;
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("\n=====================================================================\nRequirement #9 :Sending Test Log To Repository and Notification to ClientGUI \n=====================================================================");
                Console.ResetColor();
                Console.WriteLine("\n TestLog:\n{0}", this.tlogmessage.body);
                comm.postMessage(tlogmessage);
                comm.postMessage(tclientMessage);
            }
            return true;
        }
        // --------------------< Recieve Thread that keeps wainting for incomming message to the Recieve Queue >------------
        void rcvThreadProc()
        {

            Console.WriteLine("\n Starting Recieve Thread of TestHarness");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                msg.show();
                if (msg.type == CommMessage.MessageType.connect)
                    continue;
                bool makeSynch = initialiseMessageProcess(msg);
                if (makeSynch) continue;
            }
        }


        //-------------------< start TestHarness Processing and loads the loader to ChildAppDomain >-----------
        public string startTestProcessing()
        {
            ArrayList testLog = new ArrayList(); 
            if (Data.Count > 0)
            {
                int i = 0;

                AppDomain childAppDomain = appDomainModule.createAppDomain(Data[0].authorName, TestHarness.root);
                Console.WriteLine("\n=====================Using AppDomains to load test libraries=====================");
                LoaderProxy loaderProxy = appDomainModule.getLoaderProxyInstance();
                try
                {
                    string[] files = Directory.GetFiles(TestHarness.root, "*.dll");
                    loaderProxy.loadAllAssemblies(files);
                }
                catch (Exception e) { Console.Write("Exception directory path incorrect{0}", e.Message); }
                bool tdCount = loaderProxy.createInstaceforTesting();
                if (tdCount)
                {
                    testLog = loaderProxy.run();
                    try
                    {
                        tclientMessage = loaderProxy.loaderProxyMessage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                appDomainModule.unloadAppDomain(childAppDomain);
                Console.Write("\n Deleting directory that is created for test request directory Name" + TestHarness.root);
                try
                {
                    Directory.Delete(Path.GetFullPath(TestHarness.root), true);
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                }
                foreach (TestRequestData tlog in Data)
                {
                    tlog.logInfo = (String)testLog[i];
                    i = i + 1;
                }
                //  <--------------------------Creates Test Log --------------------------------->
                tlogmesssage = xmlParse.XmlCreation("TestLog", Data);
            }
            Data.Clear();
            return tlogmesssage;
        }
        //---------------< Start of the program that checks for proper comm establishment >--------
        static void Main(string[] args)
        {
            MockTestHarness th = new MockTestHarness();
            CommMessage cntMsg = new CommMessage(CommMessage.MessageType.connect);
            cntMsg.author = "Harika";
            string localEndPoint = EnvironmentSet.endPoint;
            cntMsg.to = localEndPoint;
            cntMsg.from = localEndPoint;
            th.comm.postMessage(cntMsg);
            th.tlogmessage = new CommMessage(CommMessage.MessageType.TestLog);
            th.tlogmessage.to = Repository.endPoint;
            th.tlogmessage.from = EnvironmentSet.endPoint;
            th.tclientMessage = new CommMessage(CommMessage.MessageType.TestRequest);
            th.tclientMessage.to = Repository.endPoint;
            th.tclientMessage.from = EnvironmentSet.endPoint;

        }
    }
}
