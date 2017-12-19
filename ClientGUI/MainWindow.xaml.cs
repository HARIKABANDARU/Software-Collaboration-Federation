////////////////////////////////////////////////////////////////////////////////
//  MainWindow.xaml.cs   -  We can state number of child process to run       //
//                          can be navigate to Build Request Window to create //
//                          build request                                     //
//  ver 1.0                                                                   //
//  Language:         Visual C#  2017                                         //
//  Platform:         Lenovo Z580 Windows 10                                  //
//  Application :     FederationComm , FL17                                   //
//  Author      :     Harika Bandaru, Syracuse University                     //
//                    hbandaru@syr.edu (936)-242-5972)                        //
////////////////////////////////////////////////////////////////////////////////
/* 
 * Modular Operations
 =====================
 *MainWindow of Client GUI that provides capability to select number of process and to create Build Request by 
 * navigating to the Build Request window.
 * 
 * public:
 * ===========
 * window_loaded       :  The Window that gets loaded when Client starts Execution 
 * Window_closing      :  Closes all popup windows when its window is closed.
 * Window_Closed       :  Closes Comm When the Client Window is closed
 * initializeMessageDispatcher  : It uses Comm to retrieve files from the Repository based on command
 * testDriversClick    :  Broses path gets the TestDrivers present in the RepoStorage and displays them to the client.
 * testDrivers_MouseDoubleClick : All the MouseDoubleClick functions helps to view code in the file by popping a new window
 * create              :  Creates a Build Request and sends the file to Repository to store it for future use
 * createAndSend       :  Creates a Build Request and stores in the Repsoitory and commands the Repository to send build Request to Mother Build Server
 * sendClick           :  Sends the selected BuildRequests to the Mother Builder by using Comm Service PostMessage
 * repoDirectories_MouseDoubleClick : uses initializeMessageDispatcher to preapare a message with command to get repositories from the RepoStorage
 * getFilesFromRepository  : Helper functions that shows execution process
 * main                : called to demonstrate requirements when the project is executed by the run.bat file
 * rcvThreadProc       : Reciever thread of ClientGUI comm that blocks on getMessage call to pull out the message from reciever's queue
 * startButton_Click   : Starts the process pool of the MotherBuildServer.
 *  
 *                           
 *  Build Process:
 *  ================
 *  Required files:
 *  -------------------
 *  MainWindow.xaml.cs; App.xaml; CodePopUp.xaml; MainWindow.xaml;CodePopUp.xaml.cs
 *  
 *    
 *  Maintenanace History:
 *  ======================
 *  ver 1.0
 *  */
using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using FederationCommService;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;


#pragma warning disable CS2002

namespace BuildServerFederation
{
    public partial class MainWindow : Window
    {
        IAsyncResult cbResult;
        String[] args { get; set; }
        List<Window> popups { get; set; } = null;
        List<TestRequestData> tdList { get; set; } = null;
        XmlParser crtReqst { get; set; } = null;
        static Comm comm { get; set; } = null;
        public string path { get; set; }
        Thread rcvThread = null;
        CommMessage crtmsg { get; set; } = new CommMessage(CommMessage.MessageType.BuildRequest);
        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        //-----------------------------< Initialie the window Componets takes command line arguments >-------------
        public MainWindow()
        {
            InitializeComponent();
            popups = new List<Window>();
            tdList = new List<TestRequestData>();
            crtReqst = new XmlParser();
            comm = new Comm(ClientEnvironment.address, ClientEnvironment.port);
            initializeMessageDispatcher();
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);
            rcvThread = new Thread(rcvThreadProc);

            rcvThread.IsBackground = true;
            rcvThread.Start();
            args = Environment.GetCommandLineArgs();

        }

        //----< make Environment equivalent to ClientEnvironment >-------
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                comm.close();
                Console.WriteLine("Console window closing, death imminent");
            }
            return false;
        }
        //------------------------------------ < Add BuildRequest files using Dispatcher.Invoke to UI thread >------------
        void buildRequestDispatchHelp(CommMessage msg)
        {
            buildRequestFiles.Items.Clear();
            foreach (string file in msg.arguments)
            {
                Dispatcher.Invoke(
                  new Action<string>(addBuildRequest),
                               System.Windows.Threading.DispatcherPriority.Background,
                   new string[] { file }
                );
            }
            if (args.Length == 2)
            {
                buildRequestFiles.SelectedItems.Add(buildRequestFiles.Items.GetItemAt(1));
                buildRequestFiles.SelectedItems.Add(buildRequestFiles.Items.GetItemAt(2));
                sendSelectedBuildRequest();
            }
            return;
        }

        void initializeMessageDispatcher()
        {
            // load remoteFiles listbox with files from root
            messageDispatcher["getBuildRequestFiles"] = (CommMessage msg) =>
            {
                buildRequestDispatchHelp(msg);
            };
            // load remoteDirs listbox with dirs from root
            messageDispatcher["getTopDirs"] = (CommMessage msg) =>
            {
                repoDirectories.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    repoDirectories.Items.Add(dir);
                }
            };
            // load remoteFiles listbox with files from folder
            messageDispatcher["getTestLogFiles"] = (CommMessage msg) =>
            {
                testLogFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    Dispatcher.Invoke(
                     new Action<string>(addTestLogs),
                                  System.Windows.Threading.DispatcherPriority.Background,
                      new string[] { file }
                   );
                }
            };
            // load remoteDirs listbox with dirs from folder
            messageDispatcher["getBuildLogFiles"] = (CommMessage msg) =>
            {
                buildLogFiles.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    Dispatcher.Invoke(
                      new Action<string>(addBuildLogs),
                                   System.Windows.Threading.DispatcherPriority.Background,
                       new string[] { dir }
                    );
                }
            };
        }
        void addTestLogs(string file)
        {
            testLogFiles.Items.Insert(0, file);
        }
        void addBuildRequest(string file)
        {
            buildRequestFiles.Items.Insert(0, file);
        }

        //-------------------< Double click to view data content in file >------------------------------
        private void testDrivers_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            string fileName = testDrivers.SelectedValue as string;
            showFileContent(Path.Combine(path, fileName));
        }
        //-----------------------< Browse to ge testDrivers Information > -----------------------
        private void testDriversClick(object sender, RoutedEventArgs e)
        {
            testDrivers.Items.Clear();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (args.Length == 2)
                path = Repository.buildRequest;
            else
                path = AppDomain.CurrentDomain.BaseDirectory;
            dlg.SelectedPath = path;
            DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                path = dlg.SelectedPath;
                string pattern = "TestDriver*.cs";
                Action<string, string, string> proc = this.search;
                cbResult = proc.BeginInvoke(path, pattern, testDrivers.Name, null, null);
            }
        }
        //-----------< Creates a build request Xml and commands the repository to store the build request >------------
        private void create(object sender, RoutedEventArgs e)
        {
            crtmsg.send = false;
            createBuildRequest();
        }
        //--------< creates a build request and commands repository to store the request and send to Build server >------
        private void createAndSend(object sender, RoutedEventArgs e)
        {
            crtmsg.send = true;
            createBuildRequest();
        }
        //---------------------< Add tests to the build request >--------------------------------
        private void addtestClick(object sender, RoutedEventArgs e)
        {
            testHelper();
        }
        void addBuildLogs(String file)
        {
            buildLogFiles.Items.Insert(0, file);
        }
        //----------------------< Shows the added tests to the Build Request in the list box >-------------
        void addAddedTests(StringBuilder s)
        {
            addtest.Items.Add(s);
        }
        //----------------------< Popups a window to show the content of the file >--------------------
        private void testCase_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            foreach (string filename in testcase.SelectedItems)
            {
                showFileContent(Path.Combine(path, filename));
            }
        }
        //-----------------------< Browses the selected path and gets list of test case files >---------
        private void addtestCase(object sender, RoutedEventArgs e)
        {
            testcase.Items.Clear();
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            string path;
            if (args.Length == 2)
                path = Repository.buildRequest;
            else
                path = AppDomain.CurrentDomain.BaseDirectory;
            dlg.SelectedPath = path;
            DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                path = dlg.SelectedPath;
                string pattern = "CodeToTest*.cs";
                Action<string, string, string> proc = this.search;

                cbResult = proc.BeginInvoke(path, pattern, testcase.Name, null, null);
            }
        }

        //--------------------------< Starts a window shows view build requests or create a new build requests >------------------------
        private void repoDirectoriesClick(Object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = Repository.endPoint;
            msg1.author = "Harika";
            msg1.command = "getTopDirs";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
        }

        //---------------------< Sends the selected Build Request to the MotherBuild Server via Repository >-------
        private void sendClick(Object sender, RoutedEventArgs e)
        {
            sendSelectedBuildRequest();
        }
        void sendSelectedBuildRequest()
        {
            if (buildRequestFiles.SelectedItems.Count <= 0)
                System.Windows.MessageBox.Show("Please select Build Request to send to Build Server through Repository");
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.BuildRequest);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = Repository.endPoint;
            msg1.author = "Harika";
            msg1.command = null;
            msg1.arguments.Add("");
            foreach (string filename in buildRequestFiles.SelectedItems)
            {
                CommMessage msg2 = msg1.clone();
                try
                {
                    msg2.body = File.ReadAllText(Path.GetFullPath(filename));
                }
                catch (Exception ex)
                {
                    msg2.errorMsg = ex.Message;
                }
                if (msg2.errorMsg != null)
                    comm.postMessage(msg2);
            }
            return;
        }
        //--------------------------< Closes all windows >-------------------------------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            comm.close();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        //-------------------------< close the comm on closing the Window >----------------------------
        private void Window_Closed(object sender, EventArgs e)
        {

        }
        //--------------------------< Show the list of files in the Directory on mouse double click on directory path >-------------
        private void repoDirectories_MouseDoubleClick(Object sender, System.Windows.Input.MouseEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = Repository.endPoint;
            msg1.author = "Harika";
            msg1.arguments.Add("");
            string chkToget = repoDirectories.SelectedItem.ToString();
            if (chkToget.Contains("BuildRequest"))
            {
                msg1.command = "getBuildRequestFiles";
            }
            else if (chkToget.Contains("TestLog"))
            {
                msg1.command = "getTestLogFiles";
            }
            else if (chkToget.Contains("BuildLog"))
            {
                msg1.command = "getBuildLogFiles";
            }
            comm.postMessage(msg1);
        }
        //-----------------< Show list of files of Build Request on Command >---------------
        private void requestFile_MouseDoubleClick(Object sender, RoutedEventArgs e)
        {
            foreach (string filename in buildRequestFiles.SelectedItems)
            {
                showFileContent(filename);

            }
        }

        //---------------------< Show the content of buildLogFiles on doubleclick >-------------------
        private void buildLogFiles_MouseDoubleClick(Object sender, RoutedEventArgs e)
        {
            string fileName = buildLogFiles.SelectedValue as string;
            showFileContent(fileName);
        }

        //---------------------< Show the content of testLogs >---------------------------------------
        private void testLogFile_MouseDoubleClick(Object sender, RoutedEventArgs e)
        {
            string fileName = testLogFiles.SelectedValue as string;
            showFileContent(fileName);
        }
        //-------------------< called when executed from run.bat to load code files from repository >-------------
        public void getFilesFromRepository()
        {
            string[] tdriver = Directory.GetFiles(Path.GetFullPath("../../../RepoStorage"), "TestDriver*");
            foreach (string tdriv in tdriver)
                Console.WriteLine(Path.GetFileName(tdriv));
            string[] tCases = Directory.GetFiles(Path.GetFullPath("../../../RepoStorage"), "CodeToTest*");
            foreach (string tc in tCases)
                Console.WriteLine(Path.GetFileName(tc));
        }
        void search(string path, string pattern, String lb)
        {
            /* called on asynch delegate's thread */
            string[] files = System.IO.Directory.GetFiles(path, pattern);
            foreach (string file in files)
            {
                if ((lb).Equals("testDrivers"))
                {
                    if (Dispatcher.CheckAccess())
                        addFiletDriver(file);
                    else
                        Dispatcher.Invoke(
                          new Action<string>(addFiletDriver),
                          System.Windows.Threading.DispatcherPriority.Background,
                           new string[] { file }
                        );
                }
                else
                {
                    if (Dispatcher.CheckAccess())
                        addFiletCases(file);
                    else
                        Dispatcher.Invoke(
                          new Action<string>(addFiletCases),
                                       System.Windows.Threading.DispatcherPriority.Background,
                           new string[] { file }
                        );
                }
            }
            string[] dirs = System.IO.Directory.GetDirectories(path);
            foreach (string dir in dirs)
                search(dir, pattern, lb);
        }

        //-----------------------< Add test Drivers to respective list box >------------------------------------
        void addFiletDriver(string file)
        {
            testDrivers.Items.Insert(0, Path.GetFileName(file));
        }
        //-----------------------< Add test cases to respective list box >------------------------------------
        void addFiletCases(string file)
        {
            testcase.Items.Insert(0, Path.GetFileName(file));
        }
        //----< define processing for GUI's receive thread >-------------
        void showPath(string path)
        {
            textBlock1.Text = path;
        }
        //---------------------< Recieve thread of the reciever >-----------------------

        void rcvThreadProc()
        {
            Console.Write("\n  Starting client's receive thread");
            try
            {
                while (true)
                {
                    CommMessage msg = comm.getMessage();
                    msg.show();
                    if (msg.type == CommMessage.MessageType.TestResult)
                    {
                        Console.WriteLine("\n===========================================\n       Requirement #7 & #9 : Showing Build or Test Status \n===========================================");
                        Dispatcher.Invoke(
                        new Action<String>(showPath),
                                     System.Windows.Threading.DispatcherPriority.Background,
                         new String[] { msg.body }
                      );

                    }

                    if (msg.command == null)
                        continue;
                    // pass the Dispatcher's action value to the main thread for execution
                    Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });

                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Caught a exception{0}", e.Message);
            }
        }

        //--------------------------< shows the content of file >--------------------------------------
        public void showFileContent(string fileName)
        {
            try
            {
                string path = Path.GetFullPath(fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                Console.WriteLine("Error{0}", msg);
            }
            return;
        }
        //-----------------------------< the config files that shows configuration file details >------------
        private void loadConfigFile(object sender, RoutedEventArgs e)
        {
            string path = Path.GetFullPath("BuildConfigXmlFile.xml");
            string content = File.ReadAllText(path);
            listBox1.Items.Clear();
            listBox1.Items.Insert(0, "Configuration File");
            listBox1.Items.Add(content);
            numberofProcess.Items.Clear();
            for (int i = 1; i <= MotherBuildServer.maxChildBuilders; i++)
            {
                numberofProcess.Items.Add(i);
            }
            if (args.Length == 2)
                numberofProcess.SelectedValue = int.Parse(args[1]);
            else
                numberofProcess.SelectedValue = 1;
        }
        //---------------------< starts the process pool on command >-----------------------------------
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            CommMessage strtProcessPool = new CommMessage(CommMessage.MessageType.SpawnProcess);
            strtProcessPool.author = "Harika";
            strtProcessPool.to = MotherBuildServer.endPoint;
            strtProcessPool.from = ClientEnvironment.endPoint;
            strtProcessPool.command = "SpawnProcess";
            strtProcessPool.numProcess = int.Parse(numberofProcess.Text);
            comm.postMessage(strtProcessPool);
            return;
        }
        //-----------------------------< Started when GUI is called >---------------------------------------------------
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("window loaded");
            if (args.Length == 2)
            {

                loadConfigFile(sender, e);
                startButton_Click(sender, e);
                repoDirectoriesClick(sender, e);
                main();
            }

        }
        //-------------------< To create a build request on files selected b the user >--------------
        void testHelper()
        {
            TestRequestData td = new TestRequestData();
            int count = testcase.SelectedItems.Count;
            StringBuilder f1 = new StringBuilder(count);
            f1.Append("TestName : Test");
            int i = 0;
            string m1 = "Please select a Test Driver";
            if (testDrivers.SelectedItems.Count <= 0)
                System.Windows.MessageBox.Show(m1);
            foreach (String tdriver in testDrivers.SelectedItems)
            {
                td.testDriver = tdriver;
                f1.Append("\n" + Path.GetFileName(tdriver));
            }
            td.testCode = new List<String>(testcase.SelectedItems.Count);
            foreach (String l1 in testcase.SelectedItems)
            {
                td.testCode.Add(l1);
                i += 1;
                f1.Append("\n" + Path.GetFileName(l1));
            }
            td.testName = "test";
            td.authorName = "Harika";
            td.toolChain = "C#";
            Console.WriteLine("\n-----Test Added to Build Request\n");
            td.showTestRequest();
            tdList.Add(td);
            Dispatcher.Invoke(
                      new Action<StringBuilder>(addAddedTests),
                                   System.Windows.Threading.DispatcherPriority.Background,
                       new StringBuilder[] { f1 }
                    );
        }
        //---------------< Send the selcted requests to repo and command it to send to Mother Build Server for processing >-----------
        void createBuildRequest()
        {
            buildRequest.Items.Clear();
            if (tdList.Count > 0)
            {
                string buildRqst = crtReqst.XmlCreation("BuildRequest", tdList);
                buildRequest.Items.Add(buildRqst);
                crtmsg.to = Repository.endPoint;
                crtmsg.from = ClientEnvironment.endPoint;
                crtmsg.body = buildRqst;
                crtmsg.command = "create";
                Console.WriteLine("\nBuild Request Created ::{0}\n", crtmsg.body);
                Console.Write("\n====================================\n     Requirement #13 Commands repo to send to Build Server for build Processing \n====================================\n");
                Console.WriteLine("\n Sending to repository to store::msg.toAddress{0}", crtmsg.to);
                comm.postMessage(crtmsg);
            }
            else
            {
                addtest.Items.Add("please select test structure");
            }
            tdList.Clear();
            addtest.Items.Clear();
        }
        //-------------------< Acts as test stub and calls series of things to show how requirements are met >------------
        public void main()
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n====================================\n     Requirement #5 \n====================================\n");
            Console.ResetColor();
            Console.Write("\nStarting Process Pool");
            Console.Write("\n====================================\n     Requirement #4 Client commands repo for files \n====================================\n");

            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = Repository.endPoint;
            msg1.author = "Harika";
            msg1.arguments.Add("");
            msg1.command = "getBuildRequestFiles";
            Console.Write("\n To address of message : {0}", msg1.to);
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getBuildLogFiles";
            comm.postMessage(msg2);
            CommMessage msg3 = msg1.clone();
            msg3.command = "getTestLogFiles";
            comm.postMessage(msg3);
            path = Repository.buildRequest;
            Console.Write("\n====================================\n     Requirement #11 Client browses for Testcode files \n====================================");
            string pattern = "CodeToTest*.cs";
            Action<string, string, string> proc = this.search;
            proc.Invoke(path, pattern, testcase.Name);
            Console.Write("cbresult is {0}", cbResult);
            Console.Write("\n====================================\n     Requirement 11#Client browses for TestDriver files \n====================================");
            String pattern2 = "TestDriver*.cs";
            proc.Invoke(path, pattern2, testDrivers.Name);
            Console.Write(cbResult);
            if (testDrivers.Items.Count > 0)
            {
                testDrivers.SelectedItem = (testDrivers.Items.GetItemAt(1));
                testcase.SelectedItems.Add(testcase.Items.GetItemAt(1));
                testcase.SelectedItems.Add(testcase.Items.GetItemAt(2));
                testHelper();
                testDrivers.SelectedItem = testDrivers.Items.GetItemAt(0);
                testcase.SelectedItem = testcase.Items.GetItemAt(0);
                testcase.SelectedItems.Add(testcase.Items.GetItemAt(2));
                testHelper();
                Console.Write("\n====================================\n     Requirement #12 Client creates Build Request Structure \n====================================");
                crtmsg.send = true;
                Console.Write("\n====================================\n     Requirement #12 Sends to Repos for storage and commands repo to send to Mother Builder \n====================================");
                createBuildRequest();
            }
        }
    }
}
