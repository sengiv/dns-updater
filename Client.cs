﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;

namespace dns_updater
{
    public class Client
    {
        /// <summary>
        /// The one client mode method, to start & run client mode.
        /// Uses infinite loop to keep client running, method does not close.
        /// </summary>
        public static void Run()
        {

            //get receiver address from user
            string receiverAddress = GetReceiverAddress();
            //update receiver every X (seconds), set default 10
            int interval = 10;

            //check if IP has changed
            CheckIp:
            if (IsIpChanged())
            {
                //IP has changed, so send new IP to receiver
                SendNewIpToReceiver(receiverAddress);

                //save a copy of the sent IP as Old IP
                SaveOldIp(receiverAddress);

                //wait interval
                WaitInterval(interval);

                //check again if IP has changed
                goto CheckIp;
            }
            //if IP is same, not changed
            else
            {
                //wait interval
                WaitInterval(interval);

                //check again if IP has changed
                goto CheckIp;
            }
        }

        private static string GetReceiverAddress()
        {
            //prompt user to type IP address
            Console.WriteLine("Type the IP address of DNS server");

            //store user input
            string ipAddress = Console.ReadLine();

            //return IP address to caller
            return ipAddress;
        }

        /// <summary>
        /// Pauses the application for interval time (seconds)
        /// </summary>
        /// <param name="interval">Time to wait in seconds</param>
        private static void WaitInterval(int interval)
        {
            //convert interval seconds to microseconds
            int intervalMs = interval * 1000;

            //execute sleeper
            Thread.Sleep(intervalMs);
        }

        private static void SendNewIpToReceiver(string receiverAddress)
        {
            //get old IP
            string oldIp = GetOldIp();
            //get new IP
            string newIp = GetNewIp();

            //combine old & new IP into a XML
            XElement ipList = new XElement("IP",
                new XElement("OLD", oldIp),
                new XElement("NEW", newIp)
            );

            //let user know old & new IPs
            Console.WriteLine($"Old IP:{oldIp}\nNew IP:{newIp}");

            //send old & new IP to receiver, get response also
            string response = SendDataToReceiver(receiverAddress, ipList);

            //display response temp
            //Console.WriteLine(response);
        }

        /// <summary>
        /// Sends data in the XML format to specified IP address
        /// return response from receiver as string
        /// </summary>
        /// <param name="receiverAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string SendDataToReceiver(string receiverAddress, XElement data)
        {
            //construct message to be sent
            string dataString = data.ToString();

            //set TCP port number
            int port = 80;

            //to store the response from receiver
            String responseData = String.Empty;

            //try to parse receivers IP address
            IPAddress ipAddress = IPAddress.Parse(receiverAddress);

            //if IP address did not parse, return error
            if (ipAddress == null) { throw new Exception("Receiver IP address not valid!"); }

            //prepare tcp connector
            TcpClient client = new TcpClient(receiverAddress, port);

            //convert string to byte
            Byte[] dataByte = System.Text.Encoding.ASCII.GetBytes(dataString);

            try
            {
                // Get a client stream for reading and writing.
                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(dataByte, 0, dataByte.Length);

                //let user know data has been sent
                Console.WriteLine("Data sent server");

                // Buffer to store the response bytes.
                //byteData = new Byte[256];

                // Read the first batch of the TcpServer response bytes.
                //Int32 bytes = stream.Read(byteData, 0, byteData.Length);

                //save the response data
                //responseData = System.Text.Encoding.ASCII.GetString(byteData, 0, bytes);

                //close everything.
                stream.Close();
                client.Close();
            }
            //if fail
            catch (Exception e)
            {
                //show error message to user
                Console.WriteLine("Error when sending data:\n {0}", e);
            }

            return responseData;
        }

        private static bool IsIpChanged()
        {
            //get old IP (represents the IP in receivers current records)
            string oldIp = GetOldIp();
            //get new IP
            string newIp = GetNewIp();

            //if both IPs match
            if (oldIp == newIp)
            {
                //IP is the same, no change
                return false;
            }
            else
            {
                //IP has changed
                return true;
            }
        }

        /// <summary>
        /// Gets computers public IP address from remote site
        /// </summary>
        private static string GetNewIp()
        {
            String address = "";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = stream.ReadToEnd();
            }

            int first = address.IndexOf("Address: ") + 9;
            int last = address.LastIndexOf("</body>");
            address = address.Substring(first, last - first);

            return address;
        }

        public static string GetOldIp()
        {
            XDocument ipAddressList;
        GetIp:
            try
            {
                //load file which has IP address
                ipAddressList = XDocument.Load(Const.OldIpListFile);
            }
            //if file not found
            catch (FileNotFoundException)
            {
                //make a new file, with blank IP address
                SaveOldIp("0.0.0.0");

                //try to load IP file again
                goto GetIp;
            }

            //extract IP address from loaded file
            string ipAddress = ((XElement)ipAddressList.FirstNode).Value;

            //return ip address to caller
            return ipAddress;
        }

        public static void SaveOldIp(string ipAddress)
        {
            //prepare to save IP address
            XElement ipList = new XElement("OldIP", ipAddress);

            //save ip address to file on disk  (TEMP FILE LOCATION)
            ipList.Save(Const.OldIpListFile);

        }

    }
}