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
using System.Security.Cryptography;

public class WebServer : Unity.Netcode.NetworkBehaviour{
    public NetworkMap networkMap;
    public int port;

    public int availableModelsCount = 3;
    private bool[] updateAvailableModelNames;
    private bool[] updateAvailableModelImages;
    private bool[] updateAvailableModels;

    private string address;
    private HttpListener listener;
    private bool runningWebServer;

    public string contentPath = "./Assets/Scripts/WebServer/content/";
    public string modelPath = "./Assets/Scripts/WebServer/models/";
    public string ifcConverter = "./Assets/Scripts/WebServer/ifcConvert.exe";

    private Thread serverThread;

    public override void OnNetworkSpawn(){
        // get address used by server
        address = NetworkManager.GetComponent<UnityTransport>().ConnectionData.Address;

        if (IsServer){
            serverThread = new Thread(this.RunBackendLoop);
            serverThread.Start();

            updateAvailableModelNames = new bool[availableModelsCount];
            updateAvailableModelImages = new bool[availableModelsCount];
            updateAvailableModels = new bool[availableModelsCount];
        }
    }

    public override void OnNetworkDespawn(){
        runningWebServer = false;
        listener.Close();
        serverThread.Abort();
    }

    private void RunBackendLoop(){
        listener = new HttpListener();
        listener.Prefixes.Add(GetRequestURL());
        listener.Start();

        Task listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();

        listener.Close();
    }
    
    private async Task HandleIncomingConnections()
    {
        runningWebServer = true;

        while (runningWebServer){
            HttpListenerContext ctx = await listener.GetContextAsync();

            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse res = ctx.Response;

            List<string> urlPath = req.Url.AbsolutePath.Split('/').ToList();
            for(int i=urlPath.Count-1;i>=0;i--){
                if(urlPath[i].Length == 0){
                    urlPath.RemoveAt(i);
                }
            }

            if(req.HttpMethod == "GET"){
                if(urlPath.Count == 0 || urlPath[0] == "index.html"){
                    SendWebpage(res);
                }else if(urlPath[0] == "getModelName"){
                    SendModelName(urlPath, res);
                }else if(urlPath[0] == "getModelImage"){
                    SendModelImage(urlPath, res);
                }else if(urlPath[0] == "getModelObj"){
                    SendModelObj(urlPath, res);
                }else if(urlPath[0] == "getModelMtl"){
                    SendModelMtl(urlPath, res);
                }else{
                    SendNotFound(res);
                }
            }else if(req.HttpMethod == "POST"){
                if(urlPath[0] == "login"){
                    ValidateLogin(urlPath, res);
                }else if(urlPath[0] == "changePassword"){
                    ValidateChangePassword(urlPath, res);
                }else if(urlPath[0] == "setModelName"){
                    ValidateSetModelName(urlPath, req, res);
                }else if(urlPath[0] == "setModelImage"){
                    ValidateSetModelImage(urlPath, req, res);
                }else if(urlPath[0] == "setModelIfc"){
                    ValidateSetModelIfc(urlPath, req, res);
                }else{
                    SendNotFound(res);
                }
            }else{
                SendNotFound(res);
            }
        }
    }

    private void SendResponse(HttpListenerResponse res, string contentType, string rawdata){
        byte[] data = Encoding.UTF8.GetBytes(rawdata);
        res.ContentType = contentType;
        res.ContentEncoding = Encoding.UTF8;
        res.ContentLength64 = data.LongLength;
        res.OutputStream.Write(data, 0, data.Length);
        res.Close();
    }

    private void SendNotFound(HttpListenerResponse res){
        SendResponse(res, "text/html", "{\"response\": \"not found\"}");
    }

    private void SendWebpage(HttpListenerResponse res){
        SendResponse(res, "text/html", File.ReadAllText(Path.GetFullPath(contentPath + "index.html")));
    }

    private void SendModelName(List<string> urlPath, HttpListenerResponse res){
        if(urlPath.Count > 1){
            string index = Int32.Parse(urlPath[1]).ToString();
            SendResponse(res, "text/plain", File.ReadAllText(Path.GetFullPath(modelPath + $"model_{index}.txt")));
        }else{
            SendNotFound(res);
        }
    }

    private void SendModelImage(List<string> urlPath, HttpListenerResponse res){
        if(urlPath.Count > 1){
            string index = Int32.Parse(urlPath[1]).ToString();
            SendResponse(res, "image/png", File.ReadAllText(Path.GetFullPath(modelPath + $"model_{index}.png")));
        }else{
            SendNotFound(res);
        }
    }
    
    private void SendModelObj(List<string> urlPath, HttpListenerResponse res){
        if(urlPath.Count > 1){
            string index = Int32.Parse(urlPath[1]).ToString();
            SendResponse(res, "model/obj", File.ReadAllText(Path.GetFullPath(modelPath + $"model_{index}.obj")));
        }else{
            SendNotFound(res);
        }
    }

    private void SendModelMtl(List<string> urlPath, HttpListenerResponse res){
        if(urlPath.Count > 1){
            string index = Int32.Parse(urlPath[1]).ToString();
            SendResponse(res, "model/mtl", File.ReadAllText(Path.GetFullPath(modelPath + $"model_{index}.mtl")));
        }else{
            SendNotFound(res);
        }
    }

    private string SecureHash(string s){
        using (var alg = SHA256.Create()){
            return string.Join(null, alg.ComputeHash(Encoding.UTF8.GetBytes(s)).Select(x => x.ToString("x2")));
        }
    }

    private bool IsValidLogin(List<string> urlPath){
        return (urlPath.Count > 1) && (SecureHash(urlPath[1]) == File.ReadAllText(Path.GetFullPath(contentPath + "hash.txt")));
    }

    private void ValidateLogin(List<string> urlPath, HttpListenerResponse res){
        if(IsValidLogin(urlPath)){
            SendResponse(res, "application/json", "{\"response\": \"access allowed\"}");
        }else{
            SendResponse(res, "application/json", "{\"response\": \"access denied\"}");
        }
    }

    private void ValidateChangePassword(List<string> urlPath, HttpListenerResponse res){
        if(IsValidLogin(urlPath) && urlPath.Count > 2){
            File.WriteAllText(Path.GetFullPath(contentPath + "hash.txt"), SecureHash(urlPath[2]));
            SendResponse(res, "application/json", "{\"response\": \"access allowed\"}");
        }else{
            SendResponse(res, "application/json", "{\"response\": \"access denied\"}");
        }
    }

    private byte[] GetFullRequestContentBytes(HttpListenerRequest req){
        // read byte buffer fully
        int len = (int)req.ContentLength64;
        byte[] buffer = new byte[len];
        int totalRead = 0;
        while(totalRead < len){
            totalRead += req.InputStream.Read(buffer, totalRead, len - totalRead);
        }
        
        return buffer;
    }

    private void ValidateSetModelName(List<string> urlPath, HttpListenerRequest req, HttpListenerResponse res){
        if(IsValidLogin(urlPath) && urlPath.Count > 2){
            int index = Int32.Parse(urlPath[2]);
            File.WriteAllBytes(
                Path.GetFullPath(modelPath + $"model_{index}.txt"),
                GetFullRequestContentBytes(req)
            );
            SendResponse(res, "application/json", "{\"response\": \"access allowed\"}");
            
            updateAvailableModelNames[index] = true;
        }else{
            SendResponse(res, "application/json", "{\"response\": \"access denied\"}");
        }
    }
    
    private void ValidateSetModelImage(List<string> urlPath, HttpListenerRequest req, HttpListenerResponse res){
        if(IsValidLogin(urlPath) && urlPath.Count > 2){
            int index = Int32.Parse(urlPath[2]);
            File.WriteAllBytes(
                Path.GetFullPath(modelPath + $"model_{index}.png"),
                GetFullRequestContentBytes(req)
            );
            SendResponse(res, "application/json", "{\"response\": \"access allowed\"}");

            updateAvailableModelImages[index] = true;
        }else{
            SendResponse(res, "application/json", "{\"response\": \"access denied\"}");
        }
    }
    
    private void ValidateSetModelIfc(List<string> urlPath, HttpListenerRequest req, HttpListenerResponse res){
        if(IsValidLogin(urlPath) && urlPath.Count > 2){
            int index = Int32.Parse(urlPath[2]);
            File.WriteAllBytes(
                Path.GetFullPath(modelPath + $"model_{index}.ifc"),
                GetFullRequestContentBytes(req)
            );
            SendResponse(res, "application/json", "{\"response\": \"access allowed\"}");

            Thread conversion = new Thread(
                () => ConvertIfcFile(
                    Path.GetFullPath(modelPath + $"model_{index}.ifc"),
                    Path.GetFullPath(modelPath + $"model_{index}.obj"),
                    index
                )
            );
            conversion.Start();
        }else{
            SendResponse(res, "application/json", "{\"response\": \"access denied\"}");
        }
    }

    private void ConvertIfcFile(string ifcPath, string objPath, int modelId){
        int threads = 1;

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = $"/C \"\"{ifcConverter}\" -y -j {threads} --use-element-guids {ifcPath} \"{objPath}\"";

        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();

        byte[] mtlPathBytes = Encoding.UTF8.GetBytes($"mtllib ./model_{modelId}.mtl\n#");
        using (FileStream fs = File.Open(objPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            fs.Write(mtlPathBytes, 0, mtlPathBytes.Length);
        }

        updateAvailableModels[modelId] = true;
    }

    public bool[] GetUpdateAvailableModelNames(){
        bool[] result = updateAvailableModelNames;
        updateAvailableModelNames = new bool[result.Length];
        return result;
    }

    public bool[] GetUpdateAvailableModelImages(){
        bool[] result = updateAvailableModelImages;
        updateAvailableModelImages = new bool[result.Length];
        return result;
    }

    public bool[] GetUpdateAvailableModels(){
        bool[] result = updateAvailableModels;
        updateAvailableModels = new bool[result.Length];
        return result;
    }

    public string GetRequestURL(){
        return $"http://{address}:{port}/";
    }
}
