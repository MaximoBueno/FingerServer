using FingerServer.Helpers;
using libzkfpcsharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FingerServer
{
   
        class WebServer
        {

            private TcpListener listener;
            private int port;
            private static string NOTFOUND404 = "HTTP/1.1 404 Not Found";
            private static string OK200 = "HTTP/1.1 200 OK\r\n\r\n\r\n";
            private static int MAX_SIZE = 1000;

            //setup var
            private zkfp fpInstance = null; //instance device
            private int initializeCallBackCode;
            private byte[] FPBuffer; //image buffer or length
            private int mfpWidth = 0;
            private int mfpHeight = 0;
            private Thread captureThread = null;
            private bool bIsTimeToDie = false; //flag conection
            private int cbCapTmp = 2048;
            private byte[] CapTmp = new byte[2048];
            private IntPtr FormHandle = IntPtr.Zero; //pointer of memory by for kernel
            const int MESSAGE_CAPTURED_OK = 0x0400 + 6; // message of memory by for kernel
            const string RESPONSE_HTTP = "HTTP/1.1 200 OK\r\n\r\n\r\n";
            private string string_md5;

            private bool enabled_finger = false;
            private int equipo = 0;

            //add export event of kernel
            [DllImport("user32.dll", EntryPoint = "SendMessageA")]
            public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);


            public WebServer(int port, bool enabled_finger, int equipo)
            {
                this.port = port;
                this.enabled_finger = enabled_finger;
                this.equipo = equipo;

                listener = new TcpListener(IPAddress.Any, port);

                if (enabled_finger){
                    Console.WriteLine("Init Setup Zteco");
                    setupConfiguracionDevice(equipo); //start setup
                }  
            }

            private void setupConfiguracionDevice(int equipo) {
                try
                {
                    this.fpInstance = new zkfp(); //only once instance device 
                    this.initializeCallBackCode = fpInstance.Initialize();

                    if (zkfp.ZKFP_ERR_OK == initializeCallBackCode)
                    {
                        int nCount = fpInstance.GetDeviceCount();
                        if (nCount < 0)
                        {
                            int finalizeCount = fpInstance.Finalize();
                        }

                        //init conection

                        int openDeviceCallBackCode = fpInstance.OpenDevice(equipo); //device position 0, order install driver

                        if (zkfp.ZKFP_ERR_OK != openDeviceCallBackCode)
                        {
                            Console.WriteLine("Uable to connect with the device! (Return Code: {openDeviceCallBackCode} )");
                            return;
                        }


                        byte[] paramValue = new byte[4];
                        int size = 4;

                        fpInstance.GetParameters(1, paramValue, ref size);
                        zkfp2.ByteArray2Int(paramValue, ref mfpWidth);

                        size = 4;
                        fpInstance.GetParameters(2, paramValue, ref size);
                        zkfp2.ByteArray2Int(paramValue, ref mfpHeight);

                        FPBuffer = new byte[mfpWidth * mfpHeight];


                        //addd thread by for caoture data of divice
                        captureThread = new Thread(new ThreadStart(DoCapture));
                        captureThread.IsBackground = true;
                        captureThread.Start();


                    }
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }


            private void DoCapture()
            {
                try
                {
                    while (!bIsTimeToDie)
                    {
                        cbCapTmp = 2048;
                        int ret = fpInstance.AcquireFingerprint(FPBuffer, CapTmp, ref cbCapTmp);
                        if (ret == zkfp.ZKFP_ERR_OK)
                        {
                            SendMessage(FormHandle, MESSAGE_CAPTURED_OK, IntPtr.Zero, IntPtr.Zero);
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex) {

                    Console.WriteLine(ex.Message);
                }
            }

            public void Start()
            {
                listener.Start();
                Console.WriteLine(string.Format("Server: Localhost:{0}", port));

                Console.CancelKeyPress += delegate
                {
                    Console.WriteLine("Stopping server");
                    StopServer();
                };
            }

            public void Listen()
            {
                try
                {
                    while (true)
                    {
                        Byte[] result = new Byte[MAX_SIZE];
                        string requestData;

                        TcpClient client = listener.AcceptTcpClient();
                        NetworkStream stream = client.GetStream();
                        int size = stream.Read(result, 0, result.Length);
                        requestData = System.Text.Encoding.ASCII.GetString(result, 0, size);
                        Console.WriteLine("Received: {0}", requestData);

                        Request request = GetRequest(requestData);
                        ProcessRequest(request, stream);
                        client.Close();
                    }
                }catch(Exception ex){
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    StopServer();
                }
            }

            private void ProcessRequest(Request request, NetworkStream stream)
            {
                GenerateResponse("test", stream, OK200);
            }

            private void GenerateResponse(string content,
                NetworkStream stream,
                string responseHeader)
            {
                Console.WriteLine("GenerateResponse");
                if(enabled_finger){
                    Console.WriteLine("enabled_finger");
                    MemoryStream ms;
                    using ( ms = new MemoryStream())
                    {
                        Console.WriteLine("ms");
                        BitmapFormat.GetBitmap(FPBuffer, mfpWidth, mfpHeight, ref ms);
                        string_md5 = Convert.ToBase64String(ms.ToArray());
                    }
                }else{
                    string_md5 = "";
                }

                if (string_md5 != "")
                {
                    try
                    {
                        Console.WriteLine("Atach Finger");
                        byte[] msg = System.Text.Encoding.UTF8.GetBytes(RESPONSE_HTTP + string_md5);
                        stream.Write(msg, 0, msg.Length);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                } else {
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(RESPONSE_HTTP);
                    stream.Write(msg, 0, msg.Length);
                }

                stream.Flush();

                return;
            }

            private void StopServer()
            {
                listener.Stop();

                if (enabled_finger){
                    bIsTimeToDie = true; //stop getfinger
                    captureThread.Abort(); //stop thread

                    int result = fpInstance.CloseDevice(); //stop device

                    if (result == zkfp.ZKFP_ERR_OK)
                    {
                        Thread.Sleep(100);
                        fpInstance.Finalize();   // CLEAR RESOURCES
                    }
                }
            }

            private Request GetRequest(string data)
            {
                Request request = new Request();
                var list = data.Split(' ');
                if (list.Length < 3)
                    return null;

                request.Command = list[0];
                request.Path = list[1];
                request.Protocol = list[2].Split('\n')[0];

                Console.WriteLine("Instruction: {0}\nPath: {1}\nProtocol: {2}",
                    request.Command,
                    request.Path,
                    request.Protocol);
                return request;
            }

        
        }

        public class Request
        {
            public string Command;
            public string Path;
            public string Protocol;
        }
}