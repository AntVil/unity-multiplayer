using UnityEngine;
using System.IO;
using System.Net;

public class NetworkMap : Unity.Netcode.NetworkBehaviour
{
    public WebServer webserver;
    public WebClient client;

    public override void OnNetworkSpawn(){
        if (!IsServer){
            client = new WebClient();

            for(int i=0;i<webserver.availableModelsCount;i++){
                UpdateModelName(i);
                UpdateModelImage(i);
                UpdateModel(i);
            }
        }
    }

    public void Update(){
        if(IsServer){
            bool[] names = webserver.GetUpdateAvailableModelNames();
            for(int i=0;i<names.Length;i++){
                if(names[i]){
                    UpdateModelNameClientRpc(i);
                }
            }
            
            bool[] images = webserver.GetUpdateAvailableModelImages();
            for(int i=0;i<images.Length;i++){
                if(images[i]){
                    UpdateModelImageClientRpc(i);
                }
            }
            
            bool[] models = webserver.GetUpdateAvailableModels();
            for(int i=0;i<models.Length;i++){
                if(models[i]){
                    UpdateModelClientRpc(i);
                }
            }
        }
    }

    private void UpdateModelName(int modelId){
        Stream data = client.OpenRead($"{webserver.GetRequestURL()}getModelName/{modelId}");
        StreamReader reader = new StreamReader(data);
        string s = reader.ReadToEnd();
        print(s);
        data.Close();
        reader.Close();
    }

    private void UpdateModelImage(int modelId){
        Stream data = client.OpenRead($"{webserver.GetRequestURL()}getModelImage/{modelId}");
        StreamReader reader = new StreamReader(data);
        string s = reader.ReadToEnd();
        print(s);
        data.Close();
        reader.Close();
    }

    private void UpdateModel(int modelId){
        Stream dataObj = client.OpenRead($"{webserver.GetRequestURL()}getModelObj/{modelId}");
        StreamReader readerObj = new StreamReader(dataObj);
        string obj = readerObj.ReadToEnd();
        print(obj);
        dataObj.Close();
        readerObj.Close();

        Stream dataMtl = client.OpenRead($"{webserver.GetRequestURL()}getModelMtl/{modelId}");
        StreamReader readerMtl = new StreamReader(dataMtl);
        string mtl = readerMtl.ReadToEnd();
        print(mtl);
        dataMtl.Close();
        readerMtl.Close();
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelNameClientRpc(int modelId){
        if(IsOwner){ UpdateModelName(modelId); }
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelImageClientRpc(int modelId){
        if(IsOwner){ UpdateModelImage(modelId); }
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelClientRpc(int modelId){
        if(IsOwner){ UpdateModel(modelId); }
    }
}
