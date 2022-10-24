using System.Collections;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Net;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using System.Threading.Tasks;

public class NetworkMap : Unity.Netcode.NetworkBehaviour
{
    public WebServer webserver;
    public WebClient client;

    public string modelPath = "./Assets/Scripts/NetworkMap/models/";

    public TextMeshProUGUI[] texts;
    public GameObject[] images;

    private bool[] updateAvailableModelNames;
    private bool[] updateAvailableModelImages;
    private bool[] updateAvailableModels;

    public void Start(){
        client = new WebClient();

        int availableModelsCount = webserver.availableModelsCount;
        updateAvailableModelNames = new bool[availableModelsCount];
        updateAvailableModelImages = new bool[availableModelsCount];
        updateAvailableModels = new bool[availableModelsCount];
    }

    public override void OnNetworkSpawn(){
        Thread updateAllThread = new Thread(this.UpdateAllModelFiles);
        updateAllThread.Start();
        print("onnetworkspwan done");
    }

    public void Update(){
        if(IsServer){
            UpdateServer();
        }
        UpdateClient();
    }

    public void UpdateClient(){
        for(int i=0;i<updateAvailableModelNames.Length;i++){
            if(updateAvailableModelNames[i]){ UpdateModelName(i); }
        }

        for(int i=0;i<updateAvailableModelImages.Length;i++){
            if(updateAvailableModelImages[i]){ UpdateModelImage(i); }
        }

        for(int i=0;i<updateAvailableModels.Length;i++){
            if(updateAvailableModels[i]){ UpdateModel(i); }
        }
    }

    public void UpdateServer(){
        bool[] names = webserver.GetUpdateAvailableModelNames();
        for(int i=0;i<names.Length;i++){
            if(names[i]){ UpdateModelNameClientRpc(i); }
        }
        
        bool[] images = webserver.GetUpdateAvailableModelImages();
        for(int i=0;i<images.Length;i++){
            if(images[i]){ UpdateModelImageClientRpc(i); }
        }
        
        bool[] models = webserver.GetUpdateAvailableModels();
        for(int i=0;i<models.Length;i++){
            if(models[i]){ UpdateModelClientRpc(i); }
        }
    }

    private byte[] GetAllStreamBytes(Stream stream){
        MemoryStream ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] result = ms.ToArray();
        ms.Close();

        return result;
    }

    private void UpdateModelNameFile(int modelId){
        Stream data = client.OpenRead($"{webserver.GetRequestURL()}getModelName/{modelId}");
        StreamReader reader = new StreamReader(data);
        File.WriteAllText(
            Path.GetFullPath(modelPath + $"model_{modelId}.txt"),
            reader.ReadToEnd()
        );
        data.Close();
        reader.Close();

        updateAvailableModelNames[modelId] = true;
    }

    private void UpdateModelImageFile(int modelId){
        Stream data = client.OpenRead($"{webserver.GetRequestURL()}getModelImage/{modelId}");
        File.WriteAllBytes(
            Path.GetFullPath(modelPath + $"model_{modelId}.png"),
            GetAllStreamBytes(data)
        );
        data.Close();

        updateAvailableModelImages[modelId] = true;
    }

    private void UpdateModelFile(int modelId){
        Stream dataObj = client.OpenRead($"{webserver.GetRequestURL()}getModelObj/{modelId}");
        StreamReader readerObj = new StreamReader(dataObj);
        File.WriteAllText(
            Path.GetFullPath(modelPath + $"model_{modelId}.obj"),
            readerObj.ReadToEnd()
        );
        dataObj.Close();
        readerObj.Close();

        Stream dataMtl = client.OpenRead($"{webserver.GetRequestURL()}getModelMtl/{modelId}");
        StreamReader readerMtl = new StreamReader(dataMtl);
        File.WriteAllText(
            Path.GetFullPath(modelPath + $"model_{modelId}.mtl"),
            readerMtl.ReadToEnd()
        );
        dataMtl.Close();
        readerMtl.Close();

        updateAvailableModels[modelId] = true;
    }
    
    private void UpdateModelName(int modelId){
        texts[modelId].text = File.ReadAllText(
            Path.GetFullPath(modelPath + $"model_{modelId}.txt")
        );
        updateAvailableModelNames[modelId] = false;
    }

    private void UpdateModelImage(int modelId){
        // load image
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(
            File.ReadAllBytes(modelPath + $"model_{modelId}.png")
        );

        // change scale to scale of parent canvas
        RectTransform rectTransform = images[modelId].GetComponent<RectTransform>();
        Vector2 givenSize = new Vector2(tex.width, tex.height);
        Vector2 boundingSize = rectTransform.parent.gameObject.GetComponent<RectTransform>().sizeDelta;
        float scaling = Math.Min(boundingSize.x / givenSize.x, boundingSize.y / givenSize.y);
        rectTransform.sizeDelta = scaling * givenSize;

        // display image
        Image img = images[modelId].GetComponent<Image>();
        img.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);

        updateAvailableModelImages[modelId] = false;
    }

    private void UpdateModel(int modelId){
        print("not implemented model import from file");

        updateAvailableModels[modelId] = false;
    }

    public void UpdateAllModelFiles(){
        for(int i=0;i<webserver.availableModelsCount;i++){
            UpdateModelNameFile(i);
            UpdateModelImageFile(i);
            UpdateModelFile(i);
        }
        print("updateall done");
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelNameClientRpc(int modelId){
        UpdateModelNameFile(modelId);
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelImageClientRpc(int modelId){
        UpdateModelImageFile(modelId);
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelClientRpc(int modelId){
        UpdateModelFile(modelId);
    }
}
