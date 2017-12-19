////////////////////////////////////////////////////////////////////////////////////////
//  MotherBuildServer.cs - This package provides the functionality of mother builder  // 
//                         like creating a process pool and starting child build      //  
//                         server                                                     //
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
 *The main purpose of this modle is spawn a process pool and start a child builder on the spawned 
 * process. Maintain a reay and request queue and forward the build request to for processing to child build
 * server.
 * 
 * Interface
 * ===========
 * 
 * private 
 * --------
 * initializeEnvironment : Environment set up for the comm required for the MotherBuildServer 
 * initializeMessageDispatcher : For processing the messages recieved by the build server depending on the 
 *                               type of recieved message.
 * initializechildBuilder : On reieving the build request send the message to the child builder 
 *                          depending on the child process present in the ready queue
 * spawnProcessPool       : Created the process pool on command by the Client.
 * rcvThreadProc          : Reciever thread of the Mother BuildServer deques the messages from the 
 *                          recieve queue of the MotherBuildServer
 *                          
 * BuildProcess:
 * ==============
 * Required Files:
 * ---------------
 * MotherBuildServer.cs; BuildServer.cs; BuildLibrary.cs; Environment.cs ; IMPFCommService.cs; MPCommService.cs
 * 
 * BuildCommand:
 * --------------
 * 
 * 
**/
using System;
using System.Collections.Generic;
using FederationCommService;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace BuildServerFederation
{
    class MotherBuilder
    {
        public static SWTools.BlockingQueue<CommMessage> readyQueue { get; set; } = null;
        public static SWTools.BlockingQueue<CommMessage> requestQueue { get; set; } = null;
        Comm comm { get; set; } = null;
        Thread rcvThread = null;
        Dictionary<CommMessage.MessageType, Action<CommMessage>> messageDispatcher = new Dictionary<CommMessage.MessageType, Action<CommMessage>>();
        /*----------- creating a Comm that attaches reciever and sender to the MotherBuildServer 
         * -----------Initialise the ready and request queue and start reciever thread ----------*/
        MotherBuilder()
        {
            initializeEnvironment();
            readyQueue = new SWTools.BlockingQueue<CommMessage>();
            requestQueue = new SWTools.BlockingQueue<CommMessage>();
            comm = new Comm(MotherBuildServer.address, MotherBuildServer.port);
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }
        //----------< Set up Environment set up for the MotherBuildServer to access the Comm Services >------------
        void initializeEnvironment()
        {
            EnvironmentSet.address = MotherBuildServer.address;
            EnvironmentSet.port = MotherBuildServer.port;
            EnvironmentSet.endPoint = MotherBuildServer.endPoint;
            EnvironmentSet.maxChildBuilders = MotherBuildServer.maxChildBuilders;
        }
        //---------< That processes the message recieved depending on the type of the message >---------
        void initializeMessageDispatcher(CommMessage msg)
        {
            if (msg.type == CommMessage.MessageType.BuildRequest)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine("=======================================================\n       Requirement #4 Recieved Build Request from Repo sent by Client \n========================================================");
                Console.ResetColor();
                requestQueue.enQ(msg);
                Console.Write("\n Change in Build Request Queue size::{0}", requestQueue.size());
            }

            // Prepare ready queue
            if (msg.type == CommMessage.MessageType.Ready)
            {
                Console.WriteLine("===============================================\n       Requirement #3 & #6 Recieved Messgae from Child Builder \n===============================================");
                readyQueue.enQ(msg);
                Console.Write("\nChane in Ready Queue size::{0}", readyQueue.size());
            }
        }
        //----------< start sending the BuildRequest messages to the Child Build Server >----------
        void initializechildBuilder(CommMessage msg)
        {
            if (requestQueue.size() > 0 && readyQueue.size() > 0)
            {
                CommMessage buildRequest = requestQueue.deQ();
                CommMessage readyMessage = readyQueue.deQ();
                buildRequest.to = readyMessage.from;
                buildRequest.from = MotherBuildServer.endPoint;
                Console.WriteLine("\n===============================================\n       Requirement #3 & #6 Post Build Request to Child Builder \n===============================================");
                Console.Write("\n Build Request to Child Builder::");
                buildRequest.show();
                this.comm.postMessage(buildRequest);
            }
        }
        //-------------< Create a process pool from where it spawns the child build server >-------------
        void spawnProcessPool(int numProcess)
        {
            for (int i = 0; i < numProcess; i++)
            {
                Process p2 = new Process();
                string fileName = "../../../BuildServerProcessPool/bin/debug/BuildServerProcessPool.exe";
                try
                {
                    Process.Start(Path.GetFullPath(fileName), "908" + ((i + 1).ToString()));
                    Console.WriteLine("Child Process Started on port::908{0}", (i + 1).ToString());
                }
                catch
                {
                    Console.WriteLine("Exception Caught while Process Spawn");
                }
            }
        }
        //----------------< A thread that deques the messages from the reciever queue  >--------------
        void rcvThreadProc()
        {
            Console.WriteLine("\n Starting Recive Thread of Mother Build Server");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                msg.show();
                Console.WriteLine("\nReady size {0}", readyQueue.size());
                Console.WriteLine("\nRequest size {0}", requestQueue.size());
                if (msg.type == CommMessage.MessageType.connect)
                    continue;
                initializeMessageDispatcher(msg);
                initializechildBuilder(msg);
                if (msg.command == null) continue;
                if ((msg.command).Equals("SpawnProcess"))
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write("\n==========================================\nRequirement # 5 Process Pool\n==========================================\n");
                    Console.ResetColor();
                    spawnProcessPool(msg.numProcess);
                }
            }
        }
        //--------------------< Sends connect message to check Comm Connection >------------------------------
        public static void Main(string[] args)
        {
            MotherBuilder mb = new MotherBuilder();
            CommMessage cntMsg = new CommMessage(CommMessage.MessageType.connect);
            cntMsg.author = "Harika";
            string localEndPoint = EnvironmentSet.endPoint;
            cntMsg.to = localEndPoint;
            cntMsg.from = localEndPoint;
            mb.comm.postMessage(cntMsg);
        }
    }

}
