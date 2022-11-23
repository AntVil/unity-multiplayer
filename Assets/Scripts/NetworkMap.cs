using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Net;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using System.Threading.Tasks;
using Dummiesman;

public class NetworkMap : Unity.Netcode.NetworkBehaviour
{
    public WebServer webserver;
    public WebClient client;

    private string modelPath = $"{Application.streamingAssetsPath}/NetworkMap/models/";

    public TextMeshProUGUI[] texts;
    public GameObject[] images;
    public GameObject[] podiums;
    public GameObject[] modelAreas;

    private bool[] updateAvailableModelNames;
    private bool[] updateAvailableModelImages;
    private bool[] updateAvailableModels;

    public GameObject teleportationArea;
    public float teleportationAreaAccuracy = 0.25f;
    public float teleportationAreaSlopeLimit = 0.01f;
    public float teleportationAreaMinSize = 0.02f;

    public void Start(){
        client = new WebClient();

        int availableModelsCount = webserver.availableModelsCount;
        updateAvailableModelNames = new bool[availableModelsCount];
        updateAvailableModelImages = new bool[availableModelsCount];
        updateAvailableModels = new bool[availableModelsCount];
    }

    public override void OnNetworkSpawn(){
        UpdateAllModelFiles();
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
            if(updateAvailableModels[i]){
                UpdatePodiumModel(i);
                UpdateModel(i);
            }
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

    private async void UpdateModel(int modelId){
        updateAvailableModels[modelId] = false;

        // clear loaded model
        foreach (Transform child in modelAreas[modelId].transform) {
            GameObject.Destroy(child.gameObject);
        }

        GameObject loadedObject = new OBJLoader().Load(modelPath + $"model_{modelId}.obj");
        loadedObject.transform.Rotate(-90, 0, 0);
        loadedObject.transform.Rotate(0, 0, modelAreas[modelId].transform.rotation.eulerAngles.y);

        loadedObject.transform.localScale = new Vector3(
            (float)Math.Abs(loadedObject.transform.localScale.x),
            (float)Math.Abs(loadedObject.transform.localScale.y),
            (float)Math.Abs(loadedObject.transform.localScale.z)
        );
        
        Bounds modelBounds = new Bounds(loadedObject.transform.position, Vector3.zero);
        foreach (Renderer renderer in loadedObject.GetComponentsInChildren<Renderer>()){
            modelBounds.Encapsulate(renderer.bounds);
        }
        
        Vector3 position = modelAreas[modelId].transform.position;
        float angle = modelAreas[modelId].transform.rotation.eulerAngles.y;
        loadedObject.transform.position = await Task.Run(() => {
            float xTranslation = 0.0f;
            float zTranslation = 0.0f;

            if(Math.Abs(angle) < 0.1){
                xTranslation = -modelBounds.max.x;
                zTranslation = -modelBounds.center.z;
            }else if(Math.Abs(angle - 90) < 0.1){
                xTranslation = -modelBounds.center.x;
                zTranslation = -modelBounds.min.z;
            }else if(Math.Abs(angle - 180) < 0.1){
                xTranslation = -modelBounds.min.x;
                zTranslation = -modelBounds.center.z;
            }else{
                xTranslation = -modelBounds.center.x;
                zTranslation = -modelBounds.max.z;
            }

            return new Vector3(
                position.x + xTranslation, 0.0f, position.z + zTranslation
            );
        });
        
        loadedObject.transform.parent = modelAreas[modelId].transform;

        // add backfaces to meshes (adds collision & better rendering at performance cost)
        foreach(MeshFilter meshFilter in loadedObject.GetComponentsInChildren<MeshFilter>()){
            int[] indices = meshFilter.mesh.GetIndices(0);
            indices = await Task.Run(() => {
                return indices.Concat(indices.Reverse()).ToArray();
            });
            meshFilter.mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }

        // add collision & teleportation
        foreach (Transform child in loadedObject.transform){
            MeshCollider collider = child.gameObject.AddComponent<MeshCollider>();
        }

        TeleportAreaPlacer placer = loadedObject.AddComponent<TeleportAreaPlacer>();
        placer.teleportationArea = teleportationArea;
        placer.teleportationAreaAccuracy = teleportationAreaAccuracy;
        placer.teleportationAreaSlopeLimit = teleportationAreaSlopeLimit;
        placer.teleportationAreaMinSize = teleportationAreaMinSize;
        placer.indicesDuplicated = true;
        placer.CalculateAreas();
    }
    
    public async void UpdatePodiumModel(int modelId){
        // clear loaded model
        foreach (Transform child in podiums[modelId].transform) {
            GameObject.Destroy(child.gameObject);
        }

        GameObject loadedObject = new OBJLoader().Load(modelPath + $"model_{modelId}.obj");
        loadedObject.transform.Rotate(-90, 0, 0);
        loadedObject.transform.Rotate(0, 0, podiums[modelId].transform.rotation.eulerAngles.y);

        // scale element to fit onto podium (counter without distortion)
        Bounds podiumBounds;
        podiumBounds = new Bounds(podiums[modelId].transform.position, Vector3.zero);
        foreach (Renderer renderer in podiums[modelId].GetComponentsInChildren<Renderer>()){
            podiumBounds.Encapsulate(renderer.bounds);
        }

        Bounds bounds;
        bounds = new Bounds(loadedObject.transform.position, Vector3.zero);
        foreach (Renderer renderer in loadedObject.GetComponentsInChildren<Renderer>()){
            bounds.Encapsulate(renderer.bounds);
        }
        
        float scaling = Math.Min(podiumBounds.extents.x / bounds.extents.x, podiumBounds.extents.z / bounds.extents.z);
        loadedObject.transform.localScale = new Vector3(
            scaling / transform.localScale.x,
            scaling / transform.localScale.z,
            scaling / transform.localScale.y
        );    
        
        // recalculate bounds to center loaded element
        bounds = new Bounds(loadedObject.transform.position, Vector3.zero);
        foreach (Renderer renderer in loadedObject.GetComponentsInChildren<Renderer>()){
            bounds.Encapsulate(renderer.bounds);
        }
        loadedObject.transform.position = new Vector3(podiums[modelId].transform.position.x - bounds.center.x, transform.localScale.y + 0.01f, podiums[modelId].transform.position.z - bounds.center.z);
        loadedObject.transform.parent = podiums[modelId].transform;

        // add backfaces to meshes (adds collision & better rendering at performance cost)
        foreach(MeshFilter meshFilter in loadedObject.GetComponentsInChildren<MeshFilter>()){
            int[] indices = meshFilter.mesh.GetIndices(0);
            indices = await Task.Run(() => {
                return indices.Concat(indices.Reverse()).ToArray();
            });
            meshFilter.mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }

        // add collision
        foreach (Transform child in loadedObject.transform){
            MeshCollider collider = child.gameObject.AddComponent<MeshCollider>();
        }
    }

    private async void UpdateAllModelFiles(){
        await Task.Delay(100);

        for(int i=0;i<webserver.availableModelsCount;i++){
            await Task.Run(() => UpdateModelNameFile(i));
            await Task.Run(() => UpdateModelImageFile(i));
            await Task.Run(() => UpdateModelFile(i));
        }
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelNameClientRpc(int modelId){
        Task.Run(() => UpdateModelNameFile(modelId));
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelImageClientRpc(int modelId){
        Task.Run(() => UpdateModelImageFile(modelId));
    }

    [Unity.Netcode.ClientRpc]
    public void UpdateModelClientRpc(int modelId){
        Task.Run(() => UpdateModelFile(modelId));
    }
}
