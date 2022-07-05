using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PractiTest_Nuget
{
    public class PractiTest
    {
        public static string baseURL;
        public static string endPoint;
        HttpResponseMessage practiResponse = null;

        public PractiTest(string baseURL)
        {
            PractiTest.baseURL = baseURL;
        }

        public HttpResponseMessage makeRequest(string endPoint, string requestType, string jsonBody = "", JObject objToken = null)
        {
            string responseString = "";
            HttpResponseMessage response = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseURL);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (objToken != null)
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(objToken["type"].ToString(), objToken["token"].ToString());
                    }

                    switch (requestType.ToUpper())
                    {
                        case "GET":
                            response = client.GetAsync(endPoint).Result;
                            // responseString = response.Content.ReadAsStringAsync().Result;
                            break;
                        case "POST":
                            //var postRequest = JsonConvert.DeserializeObject(jsonBody);
                            var content = new StringContent(jsonBody);
                           // response = client.PostAsJsonAsync(endPoint, postRequest).Result;
                            response = client.PostAsync(endPoint, content).Result;
                            // responseString = response.Content.ReadAsStringAsync().Result;
                            break;
                        case "PUT":
                            break;
                        case "DELETE":
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                responseString = e.Message;
                Console.WriteLine(e.Message);
            }
            endPoint = "";
            return response;
        }


        public string GetProjectID(string endPoint, JObject tokenInfo, string projectName)
        {
            try
            {
                practiResponse = makeRequest(endPoint, "GET", "", tokenInfo);
                string response = practiResponse.Content.ReadAsStringAsync().Result;
                JObject objResponse = JObject.Parse(response);
                var projectID = objResponse.SelectToken("$.data[?(@.attributes.name=='" + projectName + "')].id").ToString();
                Console.WriteLine("Found Project : " + projectName);
                return projectID;
            }
            catch (Exception ex)
            {
                Console.WriteLine(projectName + " Project not found");
                return "";
            }

        }


        public JObject GetInstances(string endPoint, JObject tokenInfo, string instanceSetName)
        {
            List<Datum> allTest = new List<Datum>();
            JObject objResponse = new JObject();
            Console.WriteLine("Loading Test Instances from " + instanceSetName);
            try
            {
                practiResponse = makeRequest(endPoint, "GET", "", tokenInfo);
                string response = practiResponse.Content.ReadAsStringAsync().Result;
                objResponse = JObject.Parse(response);
                int totalPages = int.Parse(objResponse.SelectToken("$.meta.total-pages").ToString());
                for (int j = 2; j <= totalPages; j++)
                {
                    practiResponse = makeRequest(endPoint + "&&page[number]=" + j, "GET", "", tokenInfo);
                    response = practiResponse.Content.ReadAsStringAsync().Result;
                    JObject objResponsetemp = JObject.Parse(response);
                    objResponse.Merge(objResponsetemp, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Union
                    });
                }
                Console.WriteLine("All Instances Loaded from TestSet  " + instanceSetName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Failed to load all Instances from  " + instanceSetName + " Excepion - " + ex.ToString());
            }
            return objResponse;
        }

        public string GetInstanceId(JObject tokenInfo, string setID, string testName, string testID)
        {
            try
            {
                practiResponse = makeRequest(endPoint, "GET", "", tokenInfo);
                string response = practiResponse.Content.ReadAsStringAsync().Result;
                JObject objResponse = JObject.Parse(response);
                var instnceID = objResponse.SelectToken("$.data[?(@.attributes.name == '" + testName + "' && @.attributes['set-id'] ==" + setID + " && @.attributes['test-id'] ==" + testID + ")].id").ToString();
                //var instnceID = objResponse.SelectToken("$.data[?(@.attribute.name == '" + testName + "')].id").ToString();
                Console.WriteLine("Instance found from TestSet");
                return instnceID;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Instance not found from Testset");
                return "";
            }

        }
        public HttpResponseMessage UpdateInstance(JObject tokenInfo, string instanceID, string status, string duration)
        {
            try
            {
                int statuscode = status.ToUpper() == "PASSED" ? 0 : -1;
                var data = new JObject{
                                                                {"type", "instances"},
                                                                {"attributes",  new JObject { { "instance-id" , instanceID  },{ "exit-code",  + statuscode  }, {"automated-execution-output", "Result updated from test result mapping application" }, { "status",  status.ToUpper() } ,{ "run-duration", duration } } }
                                    };
                var json_data = (new JObject { { "data", data } }).ToString();
                practiResponse = makeRequest(endPoint, "POST", json_data, tokenInfo);
                //if (practiResponse.IsSuccessStatusCode)
                //    Console.WriteLine("Instance Updated ");
                //else
                //    Console.WriteLine("Instance update failed ");
                ////string response = practiResponse.Content.ReadAsStringAsync().Result;
                ////JObject objResponse = JObject.Parse(response);
                ////Console.WriteLine("response :" + objResponse.ToString(Newtonsoft.Json.Formatting.None));
                ////Console.WriteLine("Instance Updated ");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Instance update failed :  " + ex.ToString());

            }
            return practiResponse;

        }

        public string GetSetId(string endPoint, JObject tokenInfo, string setName)
        {
            try
            {
                practiResponse = makeRequest(endPoint, "GET", "", tokenInfo);
                string response = practiResponse.Content.ReadAsStringAsync().Result;
                JObject objResponse = JObject.Parse(response);
                //Console.WriteLine("response :" + objResponse.ToString(Newtonsoft.Json.Formatting.None));
                var setID = objResponse.SelectToken("$.data[?(@.attributes.name=='" + setName + "')].id").ToString();
                Console.WriteLine("TestSet found : " + setName);
                return setID;
            }
            catch (Exception ex)
            {
                Console.WriteLine(setName + " testset not found");
                return "";
            }
        }

        public string GetTestId(string endPoint, JObject tokenInfo, string hostName)
        {
            try
            {
                practiResponse = makeRequest(endPoint, "GET", "", tokenInfo);
                string response = practiResponse.Content.ReadAsStringAsync().Result;
                JObject objResponse = JObject.Parse(response);
                var testID = objResponse.SelectToken("$.data[?(@.attributes['custom-fields']['---f-112482']=='" + hostName + "')].id").ToString();
                Console.WriteLine("Test found from Test Library ");
                return testID;
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Test not found from Test Library");
                return "";
            }
        }

        public JObject GetTests(string endPoint, JObject tokenInfo)
        {
            List<Datum> allTest = new List<Datum>();
            JObject objResponse = new JObject();
            Console.WriteLine("Loading Test Library");
            try
            {
                practiResponse = makeRequest(endPoint, "GET", "", tokenInfo);
                string response = practiResponse.Content.ReadAsStringAsync().Result;
                string fullResponse = response;
                //var testList = practiResponse.Content.ReadAsAsync<TestCaseObject>().Result;
                var testList = JsonConvert.DeserializeObject<TestCaseObject>(response);
                objResponse = JObject.Parse(response);

                foreach (Datum data in testList.data)
                    allTest.Add(data);
                int totalPages = int.Parse(objResponse.SelectToken("$.meta.total-pages").ToString());
                for (int j = 2; j <= totalPages; j++)
                {
                    practiResponse = makeRequest(endPoint + "?page[number]=" + j, "GET", "", tokenInfo);
                    response = practiResponse.Content.ReadAsStringAsync().Result;
                    fullResponse = fullResponse + response;
                    //testList = practiResponse.Content.ReadAsAsync<TestCaseObject>().Result;
                    testList = JsonConvert.DeserializeObject<TestCaseObject>(response);
                    JObject objResponsetemp = JObject.Parse(response);
                    objResponse.Merge(objResponsetemp, new JsonMergeSettings
                    {
                        // union array values together to avoid duplicates
                        MergeArrayHandling = MergeArrayHandling.Union
                    });
                    foreach (Datum data in testList.data)
                        allTest.Add(data);
                }
                Console.WriteLine("All Tests Loaded from Test Library ");

            }
            catch (Exception ex)
            {
                Console.WriteLine(" Failed to load all tests from Test Library - " + ex.ToString());

            }
            return objResponse;
        }

        public string GetTestId(string testName, string hostName, JObject testLibrary)
        {
            try
            {
                var testID = testLibrary.SelectToken("$.data[?(@.attributes['custom-fields']['---f-112482']=='" + hostName + "'  && @.attributes['name']=='" + testName + "')].id").ToString();
                Console.WriteLine("Test found from Test Library ");
                return testID.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Test not found from Test Library");
                return "";
            }
        }

        public string GetInstances(JObject tokenInfo, string setID)
        {
            try
            {
                practiResponse = makeRequest(endPoint, "GET", "", tokenInfo);
                string response = practiResponse.Content.ReadAsStringAsync().Result;
                JObject objResponse = JObject.Parse(response);
                var instnceID = objResponse.SelectToken("$.data[?(@.attributes['set-id'] ==" + setID + ")]").ToString();
                //var instnceID = objResponse.SelectToken("$.data[?(@.attribute.name == '" + testName + "')].id").ToString();
                Console.WriteLine("Instance found from TestSet");
                return instnceID;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Instance not found from Testset");
                return "";
            }

        }

        public string GetInstanceId(string testName, string testID, JObject testLibrary)
        {
            try
            {
                var instnceID = testLibrary.SelectToken("$.data[?(@.attributes.name == '" + testName + "' && @.attributes['test-id'] ==" + testID + ")].id").ToString();
                Console.WriteLine("Instance found from Testset ");
                return instnceID.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Instance not found from Testset");
                return "";
            }
        }


    }

    public class TestCase
    {
        public long TestCaseID { get; set; }

        public string TestCaseName { get; set; }

        public string TestCasePath { get; set; }

        public string Host { get; set; }

        public string Result { get; set; }

        public string KnownIssue { get; set; }

        public string Duration { get; set; }
    }


    public class TestCaseObject
    {
        public Datum[] data { get; set; }
        public Links links { get; set; }
        public Meta meta { get; set; }
    }

    public class Links
    {
        public string self { get; set; }
        public string first { get; set; }
        public object prev { get; set; }
        public string next { get; set; }
        public string last { get; set; }
    }

    public class Meta
    {
        public int currentpage { get; set; }
        public int nextpage { get; set; }
        public object prevpage { get; set; }
        public int totalpages { get; set; }
        public int totalcount { get; set; }
    }

    public class Datum
    {
        public string id { get; set; }
        public string type { get; set; }
        public Attributes attributes { get; set; }
    }

    public class Attributes
    {
        public int projectid { get; set; }
        public int displayid { get; set; }
        public string name { get; set; }
        public object description { get; set; }
        public object preconditions { get; set; }
        public int stepscount { get; set; }
        public string status { get; set; }
        public string runstatus { get; set; }
        public DateTime? lastrun { get; set; }
        public int authorid { get; set; }
        public object assignedtoid { get; set; }
        public object assignedtotype { get; set; }
        public object clonedfromid { get; set; }
        public object plannedexecution { get; set; }
        public object version { get; set; }
        public object priority { get; set; }
        public string durationestimate { get; set; }
        public CustomFields customfields { get; set; }
        public AutomatedTestInfo automatedtestinfo { get; set; }
        public string[] tags { get; set; }
        public object folderid { get; set; }
        public DateTime createdat { get; set; }
        public DateTime updatedat { get; set; }
    }

    public class CustomFields
    {
        public string f112479 { get; set; }
        public string f112480 { get; set; }
        public string f112481 { get; set; }
        public string f112482 { get; set; }
        public string f112483 { get; set; }
        public string f112484 { get; set; }
        public string f112485 { get; set; }
        public string f112495 { get; set; }
        public string f112496 { get; set; }
        public string f112500 { get; set; }
        public string f112504 { get; set; }
        public string f112508 { get; set; }
        public string f112965 { get; set; }
    }

    public class AutomatedTestInfo
    {
        public string automatedtestdesign { get; set; }
        public string scriptrepository { get; set; }
        public int? numofresults { get; set; }
        public object suitepath { get; set; }
        public object clienttype { get; set; }
        public string scriptpath { get; set; }
        public object resultspath { get; set; }
        public object scriptname { get; set; }
    }
}
