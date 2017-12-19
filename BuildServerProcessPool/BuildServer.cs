//////////////////////////////////////////////////////////////////////////////////////
//  BuilderServer.cs   -    Acts as a process in process pool to spawn Builder      //
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
 =====================
 *This module is a process that the process pool contains, and is spawned by the Mother Builder.
 * Each of this process have thier own Comm to communicate with rest of endpoint in the federation
 * It has all the functionalities of Build Server implemented in Project #2
 * 
 * public:
 * ===========
 * parseBuildRequest     :   Enqueues a message into its own sendQueue.
 * close                 :   closes a channel
 * threadProc            :   A function sits wiating for the incomming messages once a 
 *                           message has been arrived deQueues it and starts processing the message.
 * startBuildingDlls     :   It calls a method that builds Dll from the parsed build request and files
 *                           send by Repository on request of Builder.
 *  swap                 :   Used to chage the form and to adress of message to communicate between the 
 *                           sender of one process to the reciever of another process.
 *  Build Process:
 *  ================
 *  Required files:
 *  BuildServerController.cs
 *  BuilderComm.cs
 *  Environment.cs
 *    
 *  Maintenanace History:
 *  ======================
 *  ver 1.0
 *  */


using System;
using System.Threading;
using FederationCommService;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace BuildServerFederation
{
    class BuildServer
    {
        public Comm comm { get; set; } = null;
        public List<TestRequestData> _data { get; set; }
        List<TestRequestData> Data { get; set; } = null;
        BuildLibrary blib { get; set; } = null;
        CommMessage readymsg { get; set; } = null;
        XmlParser xmlParse { get; set; } = null;
        public string doc { get; set; } = null;
        List<TestRequestData> testLog { get; set; }
        public string testRequest { get; set; } = null;
        CommMessage tRmsg { get; set; } = null;
        public string toolChain { get; set; } = null;
        Thread rcvThread = null;
        public BuildServer()
        {

        }
        //-------------< ChildBuildServer Port number sent by the mother builder to start a process from process pool >--------
        public BuildServer(int port)
        {
            initializeEnvironment(port);
            comm = new Comm(ChildBuildServer.address, ChildBuildServer.port);
            Data = new List<TestRequestData>();
            xmlParse = new XmlParser();
            blib = new BuildLibrary();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }
        //---------------------< BuildRequest pushed by the repository is parsed to know the files for building Dll's >-----------------
        public void parseBuildRequest(CommMessage msg)
        {
            List<string> fileNames = new List<string>();

            this.Data = xmlParse.parseXMLRequest(msg.body);
            Console.WriteLine("data count{0}", Data.Count);
            foreach (TestRequestData data in Data)
            {
                fileNames.Add(data.testDriver);
                foreach (string td in data.testCode)
                    fileNames.Add(td);
            }
            try
            {
                ChildBuildServer.root = EnvironmentSet.root + "/" + Data[0].authorName + DateTime.Now.ToFileTime();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (!System.IO.Directory.Exists(ChildBuildServer.root))
            {
                System.IO.Directory.CreateDirectory(ChildBuildServer.root);
            }
            CommMessage filerqst = msg.clone();
            filerqst.type = CommMessage.MessageType.FileRequest;
            filerqst.to = Repository.endPoint;
            filerqst.from = msg.to;
            filerqst.arguments = fileNames;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("\n===============================================================\n    Requirement #3 : Request Files \n===============================================================\n");
            Console.ResetColor();
            filerqst.show();
            this.comm.postMessage(filerqst);
        }
        //----------------------< Initialise the enviorment for the ChildBuilder >-----------------------
        void initializeEnvironment(int port)
        {
            EnvironmentSet.address = ChildBuildServer.address;
            ChildBuildServer.port = port;
            EnvironmentSet.port = ChildBuildServer.port;
            EnvironmentSet.endPoint = ChildBuildServer.endPoint;
            EnvironmentSet.root = ChildBuildServer.root;
        }
        //----------------------------< To process a message based on message type tie to different functions >------------
        void initializeMessageProcess(CommMessage msg)
        {
            if (msg.type == CommMessage.MessageType.BuildRequest)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("=================================================================\n       Requirement #3 & #6 Recieved Build Request From Mother Builder \n==================================================================");
                Console.ResetColor();
                Console.WriteLine(msg.body);
                parseBuildRequest(msg);
            }
            if (msg.type == CommMessage.MessageType.FileList)
            {
                messageFileListHelper(msg);
            }
            if (msg.type == CommMessage.MessageType.FileRequest)
            {
                msg.show();
                CommMessage msg2 = msg.clone();
                msg2.type = CommMessage.MessageType.FileList;
                msg2.to = msg.from;
                msg2.from = msg.to;
                bool status = this.comm.postFile(msg2);
                if (status)
                {
                    this.comm.postMessage(msg2);
                }
            }
            if (msg.type == CommMessage.MessageType.TestComplete)
            {
                try
                {
                    Directory.Delete(Path.GetFullPath(msg.command), true);
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        //---------------------------------< Get filelist from repo make build test libraries send logs to repo >--------
        void messageFileListHelper(CommMessage msg)
        {
            msg.show();
            requestRepoForFilesBuildDll(Data, ChildBuildServer.root, ChildBuildServer.root);
            this.comm.postMessage(this.readymsg);
            CommMessage log = msg.clone();
            log.type = CommMessage.MessageType.BuildLog;
            log.to = Repository.endPoint;
            log.from = EnvironmentSet.endPoint;
            log.body = doc;
            Console.Write("\n========================================================\n          Requirement #7 : Build Logs Sending to RepoStorage\n=======================================================");
            Console.Write("\nBuild Log:\n{0}", log.body);
            this.comm.postMessage(log);
            this.comm.postMessage(blib.rply);
            if (this.toolChain.Equals("C#"))
            {
                if (tRmsg.body != null)
                {
                    Console.WriteLine("\n===========================================\n Requirement #8 :Sending Test Request To Test Harness \n===========================================");
                    Console.WriteLine("\nTest Request::{0}\n", tRmsg.body);
                    this.comm.postMessage(this.tRmsg);
                }
            }
            return;
        }
        //---------------------------< Reciever thread that deques message from reciever queue>------------------------------------------------
        void rcvThreadProc()
        {
            Console.WriteLine("\n Starting Child Thread Reciever");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                if (msg.type == CommMessage.MessageType.connect)
                {
                    msg.show();
                    continue;
                }
                initializeMessageProcess(msg);
            }
        }
        //------------------------------< start processing the building of test libraries >----------------
        public string requestRepoForFilesBuildDll(List<TestRequestData> Data, String Destination, String path)
        {
            String buildStatus;
            ArrayList testNames = new ArrayList();
            ArrayList testDlls = new ArrayList();
            foreach (TestRequestData dataRequested in Data)
            {
                Console.Write("\n=================================================\nRequirement #7 ## Attempt to Build\n=================================================");
                Console.Write("\n\nInvoke Build Library to build DLL  for : {0}", dataRequested.testName);
                buildStatus = blib.libraryCreation(Destination, dataRequested, path);
                if (buildStatus.Equals("Success"))
                {
                    String driver = Path.GetFileNameWithoutExtension(dataRequested.testDriver);
                    testNames.Add(dataRequested.testName);
                    testDlls.Add(driver + ".dll");
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                }
                Console.Write("\n-----------------------------------------------\n    Test Name '{0}'      :    Build Status '{1}' ", dataRequested.testName, buildStatus);
                Console.ResetColor();
            }
            doc = xmlParse.XmlCreation("BuildLog", Data);
            testLog = new List<TestRequestData>(testNames.Count);
            if (testDlls.Count > 0)
            {
                for (int i = 0; i < testNames.Count; i++)
                {
                    TestRequestData tem = new TestRequestData();
                    tem = Data[i];
                    tem.testName = (String)testNames[i];
                    tem.testDriver = (String)testDlls[i];
                    testLog.Add(tem);
                }
                testRequest = xmlParse.XmlCreation("TestRequest", testLog);
                createTestRequestMessage(testRequest);
            }
            toolChain = Data[0].toolChain;
            Data.Clear();
            return doc;
        }
        //------------------< TestRequest message for Test Harness >-----------------------------------
        void createTestRequestMessage(string tr)
        {
            this.tRmsg.body = tr;
            this.tRmsg.author = "Harika";
            this.tRmsg.command = ChildBuildServer.root;
            return;
        }
        //-----------------< Main the starting point og Child Builder >---------------------------
        static void Main(string[] args)
        {
            Console.WriteLine("Spawned Process");
            BuildServer mb = new BuildServer(Int32.Parse(args[0]));
            CommMessage cntMsg = new CommMessage(CommMessage.MessageType.connect);
            cntMsg.author = "Harika";
            string localEndPoint = EnvironmentSet.address + ":" + (EnvironmentSet.port).ToString() + "/IMessagePassingComm";
            cntMsg.to = localEndPoint;
            cntMsg.from = localEndPoint;
            mb.comm.postMessage(cntMsg);


            mb.readymsg = new CommMessage(CommMessage.MessageType.Ready);
            mb.readymsg.author = "Harika";
            mb.readymsg.to = MotherBuildServer.endPoint;
            mb.readymsg.from = localEndPoint;
            mb.comm.postMessage(mb.readymsg);

            mb.tRmsg = new CommMessage(CommMessage.MessageType.TestRequest);
            mb.tRmsg.to = TestHarness.endPoint;
            mb.tRmsg.from = localEndPoint;
        }
    }
}
