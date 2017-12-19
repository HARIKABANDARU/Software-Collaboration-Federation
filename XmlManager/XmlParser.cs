////////////////////////////////////////////////////////////////////////////////
//  XmlParser.cs - This  is Xml Manager package  for BuildServer              //
//  ver 1.0                                                                   //
//  Language:      Visual C#  2017                                            //
//  Platform:      Lenovo Z580 Windows 10                                     //
//  Application:   BuildServer , FL17                                         //
//  Author:        Harika Bandaru, Syracuse University                        //
//                 (936) 242-5972, hbandaru@syr.edu                           //
//  Source          Jim Fawcett, Syracuse University, CST 2-187               //    
//                  jfawcett @twcny.rr.com, (315) 443-3948                    //
////////////////////////////////////////////////////////////////////////////////

/*
Module Operations:
==================
This package is used to perform operations related to Xml Parsing and Xml Creation.

public:
------
TestRequestData      :  Holds Parse data as list. 
parseXMLRequest      :  Method used to Parse the given XML file and send data as List<TestRequestData>
XmlCreation          :  Creates an XML from the list<TestRequestData> 
showTestRequest      :  Dispalys the Parsed Xml file content.

Compiler Command:
-------------------
csc /target:exe /define:Test_XmlParser XmlParser.cs

BuildProcess
===============
Reuired files:
-----------------
XmlParser.cs

Maintenance History:
====================
ver 1.0

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
namespace BuildServerFederation
{
    public class TestRequestData
    {
        public string testName { get; set; }
        public string authorName { get; set; }
        public DateTime timeStamp { get; set; }
        public string testDriver { get; set; }
        public List<string> testCode { get; set; }
        public String status { get; set; }
        public String logInfo { get; set; }
        public String toolChain { get; set; }
        public void showTestRequest()
        {
            Console.Write("\n  {0,-12} : {1}", "test name", testName);
            Console.Write("\n  {0,12} : {1}", "author", authorName);
            Console.Write("\n  {0,12} : {1}", "time stamp", timeStamp);
            Console.Write("\n  {0,12} : {1}", "test driver", testDriver);
            foreach (string library in testCode)
            {
                Console.Write("\n  {0,12} : {1}", "library", library);
            }
            if (status != null)
                Console.Write("\n  {0,12} : {1}", "Status", status);
            if (logInfo != null)
                Console.Write("\n   {0,12} : {1}", "Log Info", logInfo);
        }
    }
    public class XmlParser
    {

        public XmlParser()
        {


        }
        //   <------------------parses request and returns in list form------------------------->
        public List<TestRequestData> parseXMLRequest(String XmlRequest)
        {
            List<TestRequestData> testList_ = new List<TestRequestData>();
            TextReader tr = new StringReader(XmlRequest);
            try
            {
                XDocument doc;
                testList_ = new List<TestRequestData>();
                doc = XDocument.Load(tr);
                if (doc == null)
                    return null;
                string author = doc.Descendants("author").First().Value;
                TestRequestData test = null;
                XElement[] xtests = doc.Descendants("test").ToArray();

                int numTests = xtests.Count();
                for (int i = 0; i < numTests; ++i)
                {
                    test = new TestRequestData();
                    test.testCode = new List<string>();
                    test.authorName = author;
                    test.timeStamp = DateTime.Now;
                    test.testName = xtests[i].Attribute("name").Value;
                    test.toolChain = xtests[i].Element("toolChain").Value;
                    test.testDriver = xtests[i].Element("testDriver").Value;
                    IEnumerable<XElement> xtestCode = xtests[i].Elements("tested");
                    foreach (var xlibrary in xtestCode)
                    {
                        test.testCode.Add(xlibrary.Value);
                    }
                    test.showTestRequest();
                    testList_.Add(test);
                }
                return testList_;
            }
            catch (Exception e)
            {
                Console.Write("\n Excetion Caught while parsing::" + e.Message);
            }
            finally
            {
                tr.Close();
            }
            return testList_;
        }
        // ----------------------< Creation of Xml for creating BuildLog, TestLog and TestRequest >-----------
        public String XmlCreation(String destination, List<TestRequestData> data)
        {
            XDocument doc;
            if (data != null)
            {
                TestRequestData tdata1 = new TestRequestData();
                tdata1 = data[0];
                String authorName = data[0].authorName;
                doc = new XDocument(new XElement(destination,
                new XElement("author", authorName),
                from TestRequestData tdata in data
                select new XElement("test",
                new XAttribute("name", tdata.testName),
                new XElement("toolChain", tdata.toolChain),
                new XElement("testDriver", tdata.testDriver)
      )));
                if (destination.Equals("BuildRequest"))
                {
                    XElement[] xtests = doc.Descendants("test").ToArray();
                    int i = 0;
                    foreach (XElement xtest in xtests)
                    {
                        foreach (String testcase in data[i].testCode)
                        {
                            xtest.Add(
                            new XElement("tested", testcase));

                        }
                        ++i;
                    }
                }
                if (destination.Equals("BuildLog") || destination.Equals("TestLog"))
                {
                    XElement[] xtests = doc.Descendants("test").ToArray();
                    int i = 0;
                    foreach (XElement xtest in xtests)
                    {

                        xtest.Add(
                        new XElement("status", data[i].status),
                        new XElement("Message", data[i].logInfo));
                        ++i;
                    }
                }
                return doc.ToString();
            }
            return null;
        }



        //---------------------------------------------< Test Stub >-----------------------------------------------------------
#if (Test_XMLParser)
        public static void Main(String[] args)
        {
            XmlParser demo = new XmlParser();
            List<TestRequestData> testList_ = new List<TestRequestData>();
            try
            {
                string path = "../../../RepoStorage/BuildRequest/BuildRequest.xml";
                string xmlcontent = File.ReadAllText(path);
                Console.WriteLine(xmlcontent);
                demo.parseXMLRequest(xmlcontent);
            }
            catch (Exception ex)
            {
                Console.Write("\n\n  {0}", ex.Message);
            }
        }
#endif

    }

}


