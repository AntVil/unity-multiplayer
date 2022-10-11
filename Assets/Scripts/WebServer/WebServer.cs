using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Unity.Netcode.Transports.UTP;

public class WebServer : Unity.Netcode.NetworkBehaviour
{
    public NetworkMap networkMap;
    public int port;

    private string address;
    private string pageData;
    private HttpListener listener;
    private bool runningWebServer;

    private Thread serverThread;

    public override void OnNetworkSpawn()
    {
        // get address used by server
        address = NetworkManager.GetComponent<UnityTransport>().ConnectionData.Address;

        if (IsServer)
        {
            // read delivered html into memory
            pageData = File.ReadAllText(Path.GetFullPath("./Assets/Scripts/WebServer/index.html"));

            serverThread = new Thread(this.RunBackendLoop);
            serverThread.Start();
        }
    }

    public override void OnNetworkDespawn()
    {
        runningWebServer = false;
        listener.Close();
        serverThread.Abort();
    }
    
    private async Task HandleIncomingConnections()
    {
        runningWebServer = true;

        while (runningWebServer)
        {
            HttpListenerContext ctx = await listener.GetContextAsync();

            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse res = ctx.Response;

            // TODO: handle upload / download of models (+ login/authentication)

            //runningWebServer = false;

            byte[] data = Encoding.UTF8.GetBytes(pageData);
            res.ContentType = "text/html";
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = data.LongLength;

            await res.OutputStream.WriteAsync(data, 0, data.Length);
            res.Close();
        }
    }

    private void RunBackendLoop()
    {
        listener = new HttpListener();
        listener.Prefixes.Add(getRequestURL());
        listener.Start();

        Task listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();

        listener.Close();
    }

    public string getRequestURL()
    {
        return $"http://{address}:{port}/";
    }
}
