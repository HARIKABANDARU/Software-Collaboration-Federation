//////////////////////////////////////////////////////////////////////////////////////////
// FederationCommService.cs - Implements the interface of IMPFService                   //
//  Language:         Visual C#  2017                                                   //
//  Platform:         Lenovo Z580 Windows 10                                            //
//  Application :     BuildServer , FL17                                                //
//  Author      :     Harika Bandaru, Syracuse University                               //
//                    hbandaru@syr.edu (936)-242-5972)                                  //
// Source       :     Jim Fawcett, Syracuse University, CST 2-187                       //
//////////////////////////////////////////////////////////////////////////////////////////
/* 
 * Modular Operations
 =====================
 *This module implements the all the services defined in the IService interface so that the client can
 * request any of the service 
 *
 * 
 * public:
 * ===========
 * PostMessage      :   The implements to provide this service to client(sender)
 * GetMessage       :   Unexposed method, only the reciever knows it.
 * OpenFileForWrite :   The sender of file opens a channel and writes to the path known to the reciever 
 * WriteFileBlock   :   Supports file transfer in Block Structure.
 * closeFile        :   The file is closed
*/
using System;
using System.ServiceModel;
using System.IO;
using BuildServerFederation;
using System.Threading;
using System.Net.Sockets;

namespace FederationCommService
{
    ///////////////////////////////////////////////////////////////////
    // Receiver class - receives CommMessages and Files from Senders
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class Receiver : IMessagePassingComm
    {
        public static SWTools.BlockingQueue<CommMessage> rcvQ { get; set; } = null;
        public bool restartFailed { get; set; } = false;
        ServiceHost commHost = null;
        static FileStream fs = null;
        string lastError = "";

        /*----< constructor >------------------------------------------*/

        public Receiver()
        {
            if (rcvQ == null)
                rcvQ = new SWTools.BlockingQueue<CommMessage>();
        }
        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * baseAddress is of the form: http://IPaddress or http://networkName
         */
        public bool start(string baseAddress, int port)
        {

            try
            {
                string address = baseAddress + ":" + port.ToString() + "/IMessagePassingComm";
                createCommHost(address);
                restartFailed = false;
                return true;
            }
            catch (Exception ex)
            {
                restartFailed = true;
                Console.Write("\n{0}\n", ex.Message);
                Console.Write("\n  You can't restart a listener on a previously used port");
                Console.Write(" - Windows won't release it until the process shuts down");
                return false;
            }
        }
        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * address is of the form: http://IPaddress:8080/IMessagePassingComm
         */
        public void createCommHost(string address)
        {
            try
            {
                WSHttpBinding binding = new WSHttpBinding();
                Uri baseAddress = new Uri(address);
                commHost = new ServiceHost(typeof(Receiver), baseAddress);
                commHost.AddServiceEndpoint(typeof(IMessagePassingComm), binding, baseAddress);
                commHost.Open();
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        /*----< enqueue a message for transmission to a Receiver >-----*/
        [OperationBehavior(TransactionAutoComplete = true)]
        public void postMessage(CommMessage msg)
        {
            rcvQ.enQ(msg);
        }
        /*----< retrieve a message sent by a Sender instance >---------*/

        public CommMessage getMessage()
        {
            CommMessage msg = rcvQ.deQ();
            if (msg.type == CommMessage.MessageType.closeReceiver)
            {
                close();
            }
            if (msg.type == CommMessage.MessageType.connect)
            {
                msg = rcvQ.deQ();  // discarding the connect message
            }
            return msg;
        }
        /*----< how many messages in receive queue? >-----------------*/

        public int size()
        {
            return rcvQ.size();
        }
        /*----< close ServiceHost >----------------------------------*/

        public void close()
        {
            Console.Write("\n  closing receiver - please wait");
            try
            {
                commHost.Close();
                (commHost as IDisposable).Dispose();
                Console.Write("\n  commHost.Close() returned");
            }
            catch (Exception e)
            {
                Console.WriteLine("exception rised{0}", e.Message);
            }
            finally
            {
                if (commHost != null)
                    (commHost as IDisposable).Dispose();
            }
        }
        /*---< called by Sender's proxy to open file on Receiver >-----*/
        [OperationBehavior(TransactionAutoComplete = true)]
        public bool openFileForWrite(string name, int chk)
        {
            try
            {
                string writePath = null;
                if (chk == 0)
                    writePath = Path.Combine(ChildBuildServer.root, name);
                else if (chk == 1)
                    writePath = Path.Combine(TestHarness.root, name);
                fs = File.OpenWrite(writePath);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< write a block received from Sender instance >----------*/
        [OperationBehavior(TransactionAutoComplete = true)]
        public bool writeFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< close Receiver's uploaded file >-----------------------*/
        [OperationBehavior(TransactionAutoComplete = true)]
        public void closeFile()
        {
            fs.Close();
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Sender class - sends messages and files to Receiver

    public class Sender
    {
        private IMessagePassingComm channel;
        private ChannelFactory<IMessagePassingComm> factory = null;
        private SWTools.BlockingQueue<CommMessage> sndQ = null;
        private int port = 0;
        private string fromAddress = "";
        private string toAddress = "";
        Thread sndThread = null;
        int tryCount = 0, maxCount = 10;
        string lastError = "";
        string lastUrl = "";
        FileStream fs { get; set; } = null;
        long bytesRemaining { get; set; }
        String path { get; set; } = null;

        /*----< constructor >------------------------------------------*/

        public Sender(string baseAddress, int listenPort)
        {
            port = listenPort;
            fromAddress = baseAddress + listenPort.ToString() + "/IMessagePassingComm";
            sndQ = new SWTools.BlockingQueue<CommMessage>();
            sndThread = new Thread(threadProc);
            sndThread.Start();
        }
        /*----< creates proxy with interface of remote instance >------*/

        public void createSendChannel(string address)
        {
            try
            {
                EndpointAddress baseAddress = new EndpointAddress(address);
                WSHttpBinding binding = new WSHttpBinding();
                factory = new ChannelFactory<IMessagePassingComm>(binding, address);
                channel = factory.CreateChannel();
            }
            catch (SocketException e)
            {
                Console.Write("\n  already closed");
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.Write("\n  already closed");
                Console.WriteLine(e.Message);
            }
        }
        /*----< attempts to connect to Receiver instance >-------------*/

        public bool connect(string baseAddress, int port)
        {
            toAddress = baseAddress + ":" + port.ToString() + "/IMessagePassingComm";
            return connect(toAddress);
        }
        /*----< attempts to connect to Receiver instance >-------------*/
        /*
         * - attempts a finite number of times to connect to a Receiver
         * - first attempt to send will throw exception of no listener
         *   at the specified endpoint
         * - to test that we attempt to send a connect message
         */
        public bool connect(string toAddress)
        {

            Console.WriteLine("\nattempting to connect to \"" + toAddress + "\"");
            createSendChannel(toAddress);
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
            while (true)
            {
                try
                {
                    channel.postMessage(connectMsg);
                    tryCount = 0;
                    return true;
                }
                catch (Exception ex)
                {
                    if (++tryCount < maxCount)
                    {
                        Console.WriteLine("failed to connect - waiting to try again");
                    }
                    else
                    {
                        Console.WriteLine("failed to connect - quitting");
                        lastError = ex.Message;
                        return false;
                    }
                }
            }
        }
        /*----< closes Sender's proxy >--------------------------------*/

        public void close()
        {
            try
            {
                if (factory != null)
                    factory.Close();
            }
            catch (SocketException se)
            {
                Console.WriteLine("socket exception is {0}", se.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("exception is {0}", e.Message);
            }
        }
        /*----< processing for send thread >--------------------------*/
        /*
         * - send thread dequeues send message and posts to channel proxy
         * - thread inspects message and routes to appropriate specified endpoint
         */
        void threadProc()
        {
            while (true)
            {
                CommMessage msg = sndQ.deQ();
                if (msg.type == CommMessage.MessageType.closeSender)
                {
                    Console.WriteLine("Sender send thread quitting");
                    break;
                }
                if (msg.to == lastUrl)
                {
                    try
                    {
                        channel.postMessage(msg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("exceprion caught{0}", ex.Message);
                    }
                }
                else
                {
                    close();
                    if (!connect(msg.to))
                        continue;
                    lastUrl = msg.to;
                    try
                    {
                        channel.postMessage(msg);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("new exception{0}", e.Message);
                    }
                }
            }
        }
        /*----< main thread enqueues message for sending >-------------*/

        public void postMessage(CommMessage msg)
        {
            sndQ.enQ(msg);
        }
        /*----< uploads file to Receiver instance >--------------------*/

        public bool postFile(CommMessage msg)
        {
            foreach (string fileName in msg.arguments)
            {
                if (msg.to != lastUrl)
                {
                    close();
                    if (!connect(msg.to))
                        continue;
                    lastUrl = msg.to;
                }
                try
                {
                    if ((msg.from).Equals(Repository.endPoint))
                    {
                        path = Path.Combine(Repository.buildRequest, fileName);
                        channel.openFileForWrite(fileName, 0);
                    }
                    else
                    {
                        path = Path.GetFullPath(msg.command);
                        path = Path.Combine(path, fileName);
                        channel.openFileForWrite(fileName, 1);
                    }
                    fs = File.OpenRead(path);
                    bytesRemaining = fs.Length;
                    while (true)
                    {
                        long bytesToRead = Math.Min(ClientEnvironment.blockSize, bytesRemaining);
                        byte[] blk = new byte[bytesToRead];
                        long numBytesRead = fs.Read(blk, 0, (int)bytesToRead);
                        bytesRemaining -= numBytesRead;
                        channel.writeFileBlock(blk);
                        if (bytesRemaining <= 0)
                        {
                            Thread.Sleep(2000);
                            channel.closeFile();
                            break;
                        }
                    }
                    channel.closeFile();
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message); return false;
                }
            }
            return true;
        }
    }

    public class Comm
    {
        private Receiver rcvr = null;
        private Sender sndr = null;
        private string address = null;
        private int portNum = 0;

        /*----< constructor >------------------------------------------*/
        /*
         * - starts listener listening on specified endpoint
         */
        public Comm(string baseAddress, int port)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n=================================================\nRequirement #2 : Starting Communication Service \n=================================================");
            Console.ResetColor();
            address = baseAddress;
            portNum = port;
            rcvr = new Receiver();
            rcvr.start(baseAddress, port);
            sndr = new Sender(baseAddress, port);
        }
        /*----< shutdown comm >----------------------------------------*/

        public void close()
        {
            Console.Write("\n  Comm closing");
            try
            {

                rcvr.close();
                sndr.close();
            }
            catch
            {
                Console.WriteLine("Exception caught");
            }


        }
        /*----< restart comm >-----------------------------------------*/

        public bool restart(int newport)
        {
            rcvr = new Receiver();
            rcvr.start(address, newport);
            if (rcvr.restartFailed)
            {
                return false;
            }
            sndr = new Sender(address, portNum);
            return true;
        }
        /*----< closes connection but keeps comm alive >---------------*/

        public void closeConnection()
        {
            sndr.close();
        }
        /*----< post message to remote Comm >--------------------------*/

        public void postMessage(CommMessage msg)
        {
            try
            {
                sndr.postMessage(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught{0}", ex.Message);
            }
        }
        /*----< retrieve message from remote Comm >--------------------*/

        public CommMessage getMessage()
        {
            return rcvr.getMessage();
        }
        /*----< called by remote Comm to upload file >-----------------*/

        public bool postFile(CommMessage msg)
        {
            return sndr.postFile(msg);
        }
        /*----< how many messages in receive queue? >-----------------*/

        public int size()
        {
            return rcvr.size();
        }
        //----------------------------< Test stub >-------------------------
#if(Test_Comm)
        public static void Main(string[] args)
        {

            Comm comm = new Comm("http://localhost", 8081);
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.connect);
            csndMsg.command = "show";
            csndMsg.author = "Harika";
            csndMsg.to = "http://localhost:8081/IMessagePassingComm";
            csndMsg.from = "http://localhost:8081/IMessagePassingComm";
            comm.postMessage(csndMsg);
            CommMessage crcvMsg = comm.getMessage();
            crcvMsg.show();
            crcvMsg = comm.getMessage();
        }
#endif
    }

}
