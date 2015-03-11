using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace TCPInterfaceTestClient
{
    class Program
    {
        static void Main(string[] args)
        {

            TcpClient client = null;
            try
            {
                client = new TcpClient(args[0], Convert.ToInt16(args[1]));
                Stream s = client.GetStream();
                StreamReader sr = new StreamReader(s);
                StreamWriter sw = new StreamWriter(s);
                sw.AutoFlush = true;
                // Console.WriteLine(sr.ReadLine());
                while (true)
                {
                    Console.Write("Send message: ");
                    string msg = Console.ReadLine();
                    if (msg == "") break;
                    sw.WriteLine(msg);
                    // Console.WriteLine(sr.ReadLine());
                }
                s.Close();
            }
            catch (IndexOutOfRangeException iore)
            {
                Console.WriteLine("Usage: TCPInterfaceTestClient.exe HOST PORT");
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Herp Derp: {0}", e.Message));
            }
            finally
            {
                if (client != null ) client.Close();
            }
        }
    }
}

