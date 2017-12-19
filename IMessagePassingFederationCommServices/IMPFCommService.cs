/////////////////////////////////////////////////////////////////////////////
//  IMPFCommServices.cs - Provides the contract for the servers of the     //
//                        system                                           // 
//  ver 4.0                                                                //
//  Language:     C#, VS 2003                                              //
//  Platform:     Dell Dimension 8100, Windows 2000 Pro, SP2               //
//  Application:  Demonstration for CSE681 - Software Modeling & Analysis  //
//  Author     :  Harika Bandaru ;hbandaru@syr.edu ; (936-242-5972)        //
//  Source     :  Jim Fawcett, CST 2-187, Syracuse University              //
//                (315) 443-3948, jfawcett@twcny.rr.com                    //
/////////////////////////////////////////////////////////////////////////////
/*
 * Modular Operations
 *=====================
 *This module defines the ServiceContract and OperationContracts needed that a service provider should
 *implement and provide the services to client.
 * 
 * public:
 * ===========
 * postMessage      :   The implemented by the serviceprovider to provide this service to client(sender)
 * getMessage       :   Unexposed method, only the reciever knows it.
 * openFileForWrite :   The sender of file opens a channel and writes to the path known to the reciever 
 * writeFileBlock   :   Supports file transfer in Block Structure.
 * closeFile        :   The file is closed
 *  
 *  Compiler Command:
 *  ==================
 *  Maintenanace History:
 *  ======================
 *  ver 1.0
 
 * 
 * */

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace FederationCommService
{
    using Command = String;
    using Argument = String;
    using ErrorMessage = String;

    [ServiceContract(Namespace = "FederationCommService")]
    public interface IMessagePassingComm
    {
        /*----< support for message passing >--------------------------*/
        [OperationContract(IsOneWay = true)]
        void postMessage(CommMessage msg);

        // private to receiver so not an OperationContract

        CommMessage getMessage();

        /*----< support for sending file in blocks >-------------------*/
        [OperationContract]
        bool openFileForWrite(string name, int chk);

        [OperationContract]
        bool writeFileBlock(byte[] block);

        [OperationContract]
        void closeFile();
    }
    //---------------------------< Provides the datacontract for the service >--------------------------
    [DataContract]
    [Serializable]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            connect, // initial message sent on successfully connecting
            [EnumMember]
            SpawnProcess, //on command starts process pool of build server
            [EnumMember]
            request,       //Get directories structure from the RepoStorage on command by the clientGUI
            [EnumMember]
            FileRequest,
            [EnumMember]
            FileList,
            [EnumMember]
            File,
            [EnumMember]
            BuildRequest,   //To send message of type Build Request
            [EnumMember]
            Ready,          // Child Builder Sends Ready message to say its started 
            [EnumMember]
            BuildLog,       //Builder sends to the Repository to store log information
            [EnumMember]
            TestLog,       // Test Harness sends to the Repository
            [EnumMember]
            TestRequest,    // Child Builder sends it to the TestHarness when testlibraries are built successfully
            [EnumMember]
            TestResult,     //To notify client both Builder and TestHarness uses this message type
            [EnumMember]
            TestComplete,   //To notify client for file tranfer done to remove the tmeporary directory created during build process
            [EnumMember]
            closeSender,       // close down client
            [EnumMember]
            closeReceiver      // close down server for graceful termination
        }

        public CommMessage(MessageType mt)
        {
            type = mt;
        }
        /*----< data members - all serializable public properties >----*/

        [DataMember]
        public MessageType type { get; set; } = MessageType.connect;

        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }

        [DataMember]
        public string author { get; set; }

        [DataMember]
        public Command command { get; set; } = null;

        [DataMember]
        public string body { get; set; } = null;

        [DataMember]
        public int numProcess { get; set; }

        [DataMember]
        public List<Argument> arguments { get; set; } = new List<Argument>();

        [DataMember]
        public ErrorMessage errorMsg { get; set; } = "no error";

        [DataMember]
        public bool send { get; set; } = false;
        // ---------------------< show the content of the message >-----------------------------------------
        public void show()
        {
            int i = 0;
            Console.Write("\n  CommMessage:");
            Console.Write("\n    MessageType : {0}", type.ToString());
            Console.Write("\n    to          : {0}", to);
            Console.Write("\n    from        : {0}", from);
            Console.Write("\n    author      : {0}", author);
            Console.Write("\n    command     : {0}", command);
            Console.Write("\n    arguments   :");

            if (arguments.Count > 5)
                i = 5;
            else
                i = arguments.Count;
            for (int k = 0; k < i; k++)
                Console.Write("     {0}\n ", arguments[k]);

            Console.Write("\n    errorMsg    : {0}\n", errorMsg);
        }
        //-------------------< clones a message >---------------------------------------------------
        public CommMessage clone()
        {
            CommMessage msg = new CommMessage(MessageType.connect);
            msg.type = type;
            msg.to = to;
            msg.from = from;
            msg.author = author;
            msg.command = command;
            msg.body = body;
            foreach (string arg in arguments)
                msg.arguments.Add(arg);
            return msg;
        }
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello world");
        }
    }
}
