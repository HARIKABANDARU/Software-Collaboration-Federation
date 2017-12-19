////////////////////////////////////////////////////////////////////////////////
// Environment.cs - defines environment properties for all the Servers in     //
//                  the Project#$4                                            //
//  Language:         Visual C#  2017                                         //
//  Platform:         Lenovo Z580 Windows 10                                  //
//  Application :     BuildServer , FL17                                      //
//  Author      :     Harika Bandaru, Syracuse University                     //
//                    hbandaru@syr.edu (936)-242-5972)                        //
// Source       :     Jim Fawcett, Syracuse University, CST 2-187             //  
////////////////////////////////////////////////////////////////////////////////
/* 
 * Modular Operations
 =====================
 *This module provides intial environment configuration for all the servers in the Federation
 * Every server will have a reciver and a sender running on thier respective port numbers
 * 
 * public:
 * ===========
 * ClientEnvironment : A structure that defines the Client Environment for the ClientGUI
 * MotherBuildServer : A structure that defines the Mother Builder Environment
 * ChildBuildServer  : A structure that defines the ChildBuildServer Environment every instance of Child
 *                     builder creates it's own instance.
 * Repository        : A structure that defines Environment for the Repository.
 * TestHarness       : A structure that defines Environment for the TestHarness.
 * 
 * Build Process:
 *  ================
 *  Required files:
 *  ----------------
 *  Environment.cs
 *  Maintenanace History:
 *  ======================
 *  ver 1.0
 *  */
/* 
 * Build Server start on port 9080 and all its child builder on ports starting 
 * 9081 to 9089
 * Test Harness start on port 9090
 * Repository start on port 9070
 * ClientGUI will start on port 9060
 * 
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildServerFederation
{
    //-----------------< This environment will be intialised in ever class which uses Comm >------------------
    public struct EnvironmentSet
    {
        public static string root { get; set; }
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; }
        public static string address { get; set; }
        public static int port { get; set; }
        public static bool verbose { get; set; }
        public static int maxChildBuilders { get; set; }
    }
    //---------------------------< ClientEnvirinment for Client GUI >-----------------------------------
    public struct ClientEnvironment
    {
        public static string root { get; set; } = "../../../ClientFiles/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:9060/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 9060;
        public static bool verbose { get; set; } = false;
    }

    public struct ServerEnvironment
    {
        public static string root { get; set; } = "../../../ServerFiles/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8080/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8080;
        public static bool verbose { get; set; } = false;
    }
    //-------------------------------< Environment for MotherBuildServer on which MotherBuildServe Comm Runs >---------------
    public struct MotherBuildServer
    {

        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 9080;
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:9080/IMessagePassingComm";
        public static int maxChildBuilders { get; set; } = 10;
    }
    //------------------------------< ChildBuildServer Environmnet each Process from Process Pool uses the instance of this Environmnet >---------------------
    public struct ChildBuildServer
    {
        public static string root { get; set; } = "../../../ChildBuilderStorage/";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 9081; //First child Builder's port
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:9081/IMessagePassingComm";
    }

    //--------------------------< Repository server environment >---------------------------------------
    public struct Repository
    {

        public static string buildLogPath { get; set; } = "../../../RepoStorage/BuildLog";
        public static string testLogPath { get; set; } = "../../../RepoStorage/TestLog";
        public static string buildRequest { get; set; } = "../../../RepoStorage/BuildRequest";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 9070;
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:9070/IMessagePassingComm";
    }

    //-----------------------------------< Environment for TestHarness Server >-------------------------
    public struct TestHarness
    {
        public static string root { get; set; } = "../../../TestHarnessStorage/";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 9090;
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:9090/IMessagePassingComm";

    }

}
