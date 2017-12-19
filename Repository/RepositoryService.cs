////////////////////////////////////////////////////////////////////////////////////////
//  RepositoryService.cs -  This pqackage provides the required functionality from    //
//                          repository point of view to the other Servers in the      //
//                          system                                                    //
//  ver 1.0                                                                           //
//  Language:         Visual C#  2017                                                 //
//  Platform:         Lenovo Z580 Windows 10                                          //
//  Application :     FederationComm , FL17                                           //
//  Author      :     Harika Bandaru, Syracuse University                             //
//                    hbandaru@syr.edu (936)-242-5972)                                //
////////////////////////////////////////////////////////////////////////////////////////
/*
 * Modular Operations:
 * ====================
 *The main purpose of this modle is to provide some of the Repsotory functionalities to the Build Server
 * to perform it's intended tasks. Repository acts as a store house for build code and for logs generated
 * by test harness and build server.
 * 
 * Interface
 * =========
 * public
 * -------
 * messageProcessing     : Function required for processing different message types recieved from the different 
 *                         servers in the system.
 * 
 * private
 * -------
 * initializeEnvironment : Environment set up for the comm required for the Repository server
 * rcvThreadProc         : The reciver thread fo the Repository used to deque the messages in the
 *                         reciever queue of the Repository
 * initializeDispatcher  : The message dispatcher that uses Func<CommMessage,CommMessage> delegate 
 *                         process the messages recieved from the Client and prepares a reply message
 * 
 * BuildPRocess
 * =============
 * RequiredFiles
 * --------------
 * RepositoryService.cs ; XmlParser.cs ; Comm.cs ; IMPFCommServices.cs
 * 
 * 
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using FederationCommService;
using System.Threading;
using System.IO;

namespace BuildServerFederation
{
    class RepositoryService
    {
        Comm comm { get; set; } = null;
        Thread rcvThread = null;
        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = new Dictionary<string, Func<CommMessage, CommMessage>>();
        //---------------------< Sets-up the comm that has both sender and reciever to the Repository starts reciver thread >----------
        public RepositoryService()
        {
            initializeEnvironment();
            comm = new Comm(EnvironmentSet.address, EnvironmentSet.port);
            initializeDispatcher();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }
        //------------------------< Initilaises the environment for the Comm with port number >--------------
        void initializeEnvironment()
        {
            EnvironmentSet.address = Repository.address;
            EnvironmentSet.port = Repository.port;
            EnvironmentSet.endPoint = Repository.endPoint;
        }
        //-----------------------< Starts processing the messages from pulled out from the reciever queue >--------
        public void messageProcessing(CommMessage msg)
        {
            if (msg.type == CommMessage.MessageType.FileRequest)
            {
                CommMessage msg2 = msg.clone();
                msg2.type = CommMessage.MessageType.FileList;
                msg2.to = msg.from;
                msg2.from = msg.to;
                bool status = this.comm.postFile(msg2);
                if (status)
                    this.comm.postMessage(msg2);
                return;
            }
            if (msg.type == CommMessage.MessageType.BuildLog)
            {
                Console.WriteLine("\n================================================\n       Requirement #8 : Logging Build errors and Warnings \n================================================");
                Console.WriteLine("\nBuild Log{0}\n", msg.body);
                try
                {
                    File.WriteAllText(Repository.buildLogPath + "/BuildLog" + DateTime.Now.ToFileTime(), msg.body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (msg.type == CommMessage.MessageType.TestLog)
            {
                Console.WriteLine("\n==================================================\n       Requirement #9 :Recieved Test Log from Test Harness \n=======================================================");
                Console.WriteLine("\nTest Log{0}\n", msg.body);
                try
                {
                    File.WriteAllText(Repository.testLogPath + "/TestLog" + DateTime.Now.ToFileTime(), msg.body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (msg.type == CommMessage.MessageType.request)
            {
                CommMessage reply = this.messageDispatcher[msg.command](msg);
                Console.Write("\n======================================================================================\n      Requirement #4 Repository sending files to Client based on command \n======================================================================================");
                reply.show();
                this.comm.postMessage(reply);
            }
            if (msg.type == CommMessage.MessageType.BuildRequest)
            {
                buildRequestHelper(msg);
            }
        }
        void buildRequestHelper(CommMessage msg)
        {
            Console.Write("========================================================\n Requirement #12 : Store Build Request and Transmit to Build Server \n=========================================================");
            Console.WriteLine("\n msg.from  Client{0}::\n", msg.from);
            if (msg.command != null && (msg.command).Equals("create"))
            {
                try
                {
                    File.WriteAllText(Repository.buildRequest + "/BuildRequest" + DateTime.Now.ToFileTime() + ".xml", msg.body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (!msg.send)
                    return;
            }
            Console.Write("\n Sending message to Mother Build Server");
            CommMessage bldRqst = msg.clone();
            bldRqst.to = MotherBuildServer.endPoint;
            bldRqst.from = msg.to;
            Console.WriteLine("\n Message to Build Server msg.to::{0}", bldRqst.to);
            comm.postMessage(bldRqst);
            return;
        }

        //--------------------------< Reciever thread of Repository reciever >--------------------
        void rcvThreadProc()
        {

            Console.WriteLine("\n Starting Repository's Recieve Thread ");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                msg.show();
                if (msg.type == CommMessage.MessageType.closeReceiver)
                {
                    comm.close();
                    comm = null;
                    break;
                }
                if (msg.type == CommMessage.MessageType.connect)
                    continue;
                messageProcessing(msg);
            }
        }
        //---------------------------------< Initialise the message dispatcher used for ClientGUI >------------
        void initializeDispatcher()
        {
            //Sends Build request files to the clientGUI
            Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.File);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getBuildRequestFiles";
                string[] files = Directory.GetFiles("../../../RepoStorage/BuildRequest", "*.xml");
                reply.arguments = files.ToList<string>();
                return reply;
            };
            messageDispatcher["getBuildRequestFiles"] = getTopFiles;
            Func<CommMessage, CommMessage> getTopDirs = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.File);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopDirs";
                string[] files = Directory.GetDirectories("../../../RepoStorage");
                reply.arguments = files.ToList<string>();
                return reply;
            };
            messageDispatcher["getTopDirs"] = getTopDirs;
            Func<CommMessage, CommMessage> moveIntoFolderFiles = (CommMessage msg) =>
             {
                 CommMessage reply = new CommMessage(CommMessage.MessageType.File);
                 reply.to = msg.from;
                 reply.from = msg.to;
                 reply.command = "getTestLogFiles";
                 string[] files = Directory.GetFiles("../../../RepoStorage/TestLog");
                 reply.arguments = files.ToList<string>();
                 return reply;
             };
            messageDispatcher["getTestLogFiles"] = moveIntoFolderFiles;
            Func<CommMessage, CommMessage> moveIntoFolderDirs = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.File);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getBuildLogFiles";
                string[] files = Directory.GetFiles("../../../RepoStorage/BuildLog");
                reply.arguments = files.ToList<string>();
                return reply;
            };
            messageDispatcher["getBuildLogFiles"] = moveIntoFolderDirs;
        }
        static void Main(string[] args)
        {
            RepositoryService mb = new RepositoryService();
            CommMessage cntMsg = new CommMessage(CommMessage.MessageType.connect);
            cntMsg.author = "Harika";
            string localEndPoint = EnvironmentSet.endPoint;
            cntMsg.to = localEndPoint;
            cntMsg.from = localEndPoint;
            mb.comm.postMessage(cntMsg);
        }
    }
}
