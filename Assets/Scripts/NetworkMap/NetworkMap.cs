using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
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

    public string modelPath = "./Assets/Scripts/NetworkMap/models/";

    public TextMeshProUGUI[] texts;
    public GameObject[] images;
    public GameObject[] podiums;

    private bool[] updateAvailableModelNames;
    private bool[] updateAvailableModelImages;
    private bool[] updateAvailableModels;

    public GameObject teleportationArea;
    public float teleportationAreaAccuracy = 0.25f;
    public float teleportationAreaSlopeLimit = 0.01f;

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

    private void UpdateModel(int modelId){
        updateAvailableModels[modelId] = false;

        GameObject loadedObject = new OBJLoader().Load(modelPath + $"model_{modelId}.obj");
        loadedObject.transform.Rotate(-90, 0, 0);

        // add backfaces to meshes (adds collision & better rendering at performance cost)
        foreach(MeshFilter meshFilter in loadedObject.GetComponentsInChildren<MeshFilter>()){
            meshFilter.mesh.SetIndices(meshFilter.mesh.GetIndices(0).Concat(meshFilter.mesh.GetIndices(0).Reverse()).ToArray(), MeshTopology.Triangles, 0);
        }

        // add collision & teleportation
        foreach (Transform child in loadedObject.transform){
            MeshCollider collider = child.gameObject.AddComponent<MeshCollider>();

            Bounds bounds = child.GetComponent<Renderer>().bounds;

            float[] xArray = new float[(int)Math.Floor(bounds.size.x / teleportationAreaAccuracy)];
            float[] zArray = new float[(int)Math.Floor(bounds.size.z / teleportationAreaAccuracy)];
            for(int i=0;i<xArray.Length;i++){
                xArray[i] = bounds.min.x + teleportationAreaAccuracy * i;
            }
            for(int i=0;i<zArray.Length;i++){
                zArray[i] = bounds.min.z + teleportationAreaAccuracy * i;
            }

            float[,] topView = new float[xArray.Length, zArray.Length];
            for(int i=0;i<xArray.Length;i++){
                for(int j=0;j<zArray.Length;j++){
                    RaycastHit hit;
                    if(collider.Raycast(new Ray(new Vector3(xArray[i], bounds.max.y + 1, zArray[j]), Vector3.down), out hit, 100)){
                        topView[i, j] = hit.point.y;
                    }else{
                        topView[i, j] = 0.0f;
                    }
                }
            }

            // remove lonely points
            for(int i=0;i<xArray.Length;i++){
                for(int j=0;j<zArray.Length;j++){
                    if(topView[i, j] != 0.0f){
                        int neighbours = 0;
                        
                        if(i != 0 && Math.Abs(topView[i-1, j] - topView[i, j]) < teleportationAreaSlopeLimit) neighbours++;
                        if(j != 0 && Math.Abs(topView[i, j-1] - topView[i, j]) < teleportationAreaSlopeLimit) neighbours++;
                        if(i != xArray.Length-1 && Math.Abs(topView[i+1, j] - topView[i, j]) < teleportationAreaSlopeLimit) neighbours++;
                        if(j != zArray.Length-1 && Math.Abs(topView[i, j+1] - topView[i, j]) < teleportationAreaSlopeLimit) neighbours++;

                        if(neighbours == 0) topView[i, j] = 0.0f;
                    }
                }
            }

            List<float> uniqueHeight = new List<float>();
            for(int i=0;i<xArray.Length;i++){
                for(int j=0;j<zArray.Length;j++){
                    if(topView[i, j] == 0.0f){ continue; }

                    bool isNew = true;
                    for(int k=0;k<uniqueHeight.Count;k++){
                        if(Math.Abs(uniqueHeight[k] - topView[i, j]) < teleportationAreaSlopeLimit){
                            isNew = false;
                            break;
                        }
                    }
                    if(isNew){
                        uniqueHeight.Add(topView[i, j]);
                    }
                }
            }

            foreach(float height in uniqueHeight){
                bool[,] layer = new bool[xArray.Length, zArray.Length];

                for(int i=0;i<xArray.Length;i++){
                    for(int j=0;j<zArray.Length;j++){
                        layer[i, j] = Math.Abs(topView[i, j] - height) < teleportationAreaSlopeLimit;
                    }
                }

                for(int i=0;i<xArray.Length;i++){
                    for(int j=0;j<zArray.Length;j++){
                        if(layer[i, j]){
                            int size = 1;
                            bool extendable = true;
                            while(extendable){
                                if(i + size > xArray.Length-1 || j + size > zArray.Length-1){
                                    break;
                                }
                                
                                for(int k=0;k<size;k++){
                                    if(i + k > xArray.Length-1 || j + k > zArray.Length-1 || !layer[i + k, j + size] || !layer[i + size, j + k]){
                                        extendable = false;
                                        break;
                                    }
                                }

                                if(extendable){
                                    size++;
                                }
                            }

                            for(int p=0;p<size;p++){
                                for(int q=0;q<size;q++){
                                    layer[i+p, j+q] = false;
                                }
                            }

                            Vector3 positiveX = new Vector3(xArray[i+size-1], height, zArray[j+size/2]);
                            Vector3 positiveZ = new Vector3(xArray[i+size/2], height, zArray[j+size-1]);
                            Vector3 negativeX = new Vector3(xArray[i], height, zArray[j+size/2]);
                            Vector3 negativeZ = new Vector3(xArray[i+size/2], height, zArray[j]);
                            
                            for(int k=0;i + k + size < xArray.Length;k++){
                                int a = i + k + size;
                                int b = j + (size / 2);
                                if(Math.Abs(topView[a, b] - height) >= teleportationAreaSlopeLimit){
                                    if(topView[a, b] > height){
                                        positiveX = new Vector3(xArray[a], topView[a, b], zArray[b]);
                                    }
                                    break;
                                }else{
                                    positiveX = new Vector3(xArray[a], height, zArray[b]);
                                }
                            }
                            for(int k=0;j + k + size < zArray.Length;k++){
                                int a = i + (size / 2);
                                int b = j + k + size;
                                if(Math.Abs(topView[a, b] - height) >= teleportationAreaSlopeLimit){
                                    if(topView[a, b] > height){
                                        positiveZ = new Vector3(xArray[a], topView[a, b], zArray[b]);
                                    }
                                    break;
                                }else{
                                    positiveZ = new Vector3(xArray[a], height, zArray[b]);
                                }
                            }
                            for(int k=0;i + k >= 0;k--){
                                int a = i + k;
                                int b = j + (size / 2);
                                if(Math.Abs(topView[a, b] - height) >= teleportationAreaSlopeLimit){
                                    if(topView[a, b] > height){
                                        negativeX = new Vector3(xArray[a], topView[a, b], zArray[b]);
                                    }
                                    break;
                                }else{
                                    negativeX = new Vector3(xArray[a], height, zArray[b]);
                                }
                            }
                            for(int k=0;j + k >= 0;k--){
                                int a = i + (size / 2);
                                int b = j + k;
                                if(Math.Abs(topView[a, b] - height) >= teleportationAreaSlopeLimit){
                                    if(topView[a, b] > height){
                                        negativeZ = new Vector3(xArray[a], topView[a, b], zArray[b]);
                                    }
                                    break;
                                }else{
                                    negativeZ = new Vector3(xArray[a], height, zArray[b]);
                                }
                            }

                            float translation = (((float)size) * teleportationAreaAccuracy) / 2.0f;
                            float x = xArray[i] + translation;
                            float z = zArray[j] + translation;
                            float xScale = 0.1f * size * teleportationAreaAccuracy;
                            float yScale = 0.1f * size * teleportationAreaAccuracy;
                            float zScale = 0.1f * size * teleportationAreaAccuracy;
                            float xAngle = 0.0f;
                            float zAngle = 0.0f;
                            float y = height;
                            if(positiveX.y != negativeX.y){
                                float dy = positiveX.y - negativeX.y;
                                float dx = positiveX.x - negativeX.x;
                                double xAngleRad = Math.Atan2(dy, dx);

                                y = Math.Max(y, (dy / dx) * (x - negativeX.x) + negativeX.y);

                                xAngle = (float)(xAngleRad * 180.0 / Math.PI);

                                xScale *= (float)Math.Abs(Math.Tan(xAngleRad / 2.0f)) + 1.0f;
                            }

                            if(positiveZ.y != negativeZ.y){
                                float dy = positiveZ.y - negativeZ.y;
                                float dz = positiveZ.z - negativeZ.z;
                                double zAngleRad = Math.Atan2(dy, dz);
                                
                                y = Math.Max(y, (dy / dz) * (z - negativeZ.z) + negativeZ.y);

                                zAngle = -(float)(zAngleRad * 180.0 / Math.PI);

                                zScale *= (float)Math.Abs(Math.Tan(zAngleRad / 2.0f)) + 1.0f;
                            }
                            
                            GameObject square = Instantiate(
                                teleportationArea,
                                new Vector3(x, y + 0.01f, z),
                                Quaternion.Euler(zAngle, 0.0f, xAngle)
                            );
                            square.transform.localScale = new Vector3(xScale, yScale, zScale);
                            square.transform.parent = child;
                        }
                    }
                }
            }
        }
    }
    
    public void UpdatePodiumModel(int modelId){
        GameObject loadedObject = new OBJLoader().Load(modelPath + $"model_{modelId}.obj");
        loadedObject.transform.Rotate(-90, 0, 0);

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
    }

    public void UpdateAllModelFiles(){
        for(int i=0;i<webserver.availableModelsCount;i++){
            UpdateModelNameFile(i);
            UpdateModelImageFile(i);
            UpdateModelFile(i);
        }
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
