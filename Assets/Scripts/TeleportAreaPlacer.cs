using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class TeleportAreaPlacer : MonoBehaviour
{
    public GameObject teleportationArea;
    public float teleportationAreaAccuracy = 0.25f;
    public float teleportationAreaSlopeLimit = 0.01f;
    public float teleportationAreaMinSize = 0.02f;
    public bool indicesDuplicated = false;

    private Queue<Vector3[]> teleportAreaQueue;

    public void Start(){
        
    }

    public void Update(){
        for(int i=0;i<10;i++){
            if(teleportAreaQueue == null || teleportAreaQueue.Count == 0) break;

            Vector3[] buffer = teleportAreaQueue.Dequeue();

            GameObject square = Instantiate(
                teleportationArea,
                buffer[0],
                Quaternion.Euler(buffer[1])
            );
            square.transform.localScale = buffer[2];
            square.transform.parent = transform;
        }
    }
    
    public async void CalculateAreas(){
        teleportAreaQueue = new Queue<Vector3[]>();

        int childCount = transform.childCount;

        // add collision & teleportation
        for(int c=0;c<childCount;c++){
            // extract information inside main thread
            Transform child = transform.GetChild(c);
            Mesh mesh = child.GetComponent<MeshFilter>().mesh;
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // do calculations outside main thread
            await Task.Run(() => {
                int trianglesLength = triangles.Length;
                if(indicesDuplicated){
                    trianglesLength /= 2;
                }

                // convert verticies to world space
                for(int i=0;i<vertices.Length;i++){
                    vertices[i] = localToWorld.MultiplyPoint3x4(vertices[i]);
                }

                // compute normals of triangles
                Vector3[] normals = new Vector3[trianglesLength];
                for(int i=0;i<trianglesLength;i+=3){
                    normals[i/3] = Vector3.Normalize(Vector3.Cross(vertices[triangles[i+1]] - vertices[triangles[i]], vertices[triangles[i+2]] - vertices[triangles[i]]));
                }

                // calculate bounding box
                Bounds bounds = new Bounds(vertices[0], Vector3.zero);
                for(int i=1;i<vertices.Length;i++){
                    bounds.Encapsulate(vertices[i]);
                }

                // calculate x-z-grid
                float[] xArray = new float[(int)Math.Floor(bounds.size.x / teleportationAreaAccuracy)];
                float[] zArray = new float[(int)Math.Floor(bounds.size.z / teleportationAreaAccuracy)];
                for(int i=0;i<xArray.Length;i++){
                    xArray[i] = bounds.min.x + teleportationAreaAccuracy * i;
                }
                for(int i=0;i<zArray.Length;i++){
                    zArray[i] = bounds.min.z + teleportationAreaAccuracy * i;
                }

                float[,] topView = new float[xArray.Length, zArray.Length];
                
                
                // calculate topView/heightmap with rastering
                for(int k=0;k<trianglesLength;k+=3){
                    if(Math.Abs(normals[k/3].y) < 0.01) continue;

                    Vector3 p0 = vertices[triangles[k]];
                    Vector3 p1 = vertices[triangles[k+1]];
                    Vector3 p2 = vertices[triangles[k+2]];

                    // sort by x
                    if(p0.x > p1.x) (p0, p1) = (p1, p0);
                    if(p1.x > p2.x) (p1, p2) = (p2, p1);
                    if(p0.x > p1.x) (p0, p1) = (p1, p0);

                    // compute directions
                    float dz01;
                    float dz02;
                    float dz12;
                    if(Math.Abs(p1.x - p0.x) > 0.01) {
                        dz01 = (p1.z - p0.z) / (p1.x - p0.x);
                    }else{
                        dz01 = 0;
                    }
                    if(Math.Abs(p2.x - p0.x) > 0.01) {
                        dz02 = (p2.z - p0.z) / (p2.x - p0.x);
                    }else{
                        dz02 = 0;
                    }
                    if(Math.Abs(p2.x - p1.x) > 0.01) {
                        dz12 = (p2.z - p1.z) / (p2.x - p1.x);
                    }else{
                        dz12 = 0;
                    }
                    
                    int startXIndex = (int)Math.Round(Math.Max(0, Math.Min(xArray.Length, (p0.x - bounds.min.x) / teleportationAreaAccuracy)));
                    int midXIndex = (int)Math.Round(Math.Max(0, Math.Min(xArray.Length, (p1.x - bounds.min.x) / teleportationAreaAccuracy)));
                    int endXIndex = (int)Math.Round(Math.Max(0, Math.Min(xArray.Length, (p2.x - bounds.min.x) / teleportationAreaAccuracy)));
                    int startZIndex;
                    int endZIndex;
                    
                    if(p0.z + dz02 * (p1.x - p0.x) < p1.z){
                        // center point above
                        for(int i=startXIndex;i<midXIndex;i++){
                            startZIndex = (int)Math.Round((p0.z + dz02 * (xArray[i] - p0.x) - bounds.min.z) / teleportationAreaAccuracy);
                            endZIndex = (int)Math.Round((p0.z + dz01 * (xArray[i] - p0.x) - bounds.min.z) / teleportationAreaAccuracy);

                            for(int j=startZIndex;j<endZIndex;j++){
                                try{
                                    topView[i, j] = Math.Max(topView[i, j], -(xArray[i] * normals[k/3].x + zArray[j] * normals[k/3].z - Vector3.Dot(normals[k/3], p0)) / normals[k/3].y);
                                }catch{

                                }
                            }
                        }

                        for(int i=midXIndex;i<endXIndex;i++){
                            startZIndex = (int)Math.Round((p0.z + dz02 * (xArray[i] - p0.x) - bounds.min.z) / teleportationAreaAccuracy);
                            endZIndex = (int)Math.Round((p1.z + dz12 * (xArray[i] - p1.x) - bounds.min.z) / teleportationAreaAccuracy);

                            for(int j=startZIndex;j<endZIndex;j++){
                                try{
                                    topView[i, j] = Math.Max(topView[i, j], -(xArray[i] * normals[k/3].x + zArray[j] * normals[k/3].z - Vector3.Dot(normals[k/3], p0)) / normals[k/3].y);
                                }catch{
                                    
                                }
                            }
                        }
                    }else{
                        // center point below
                        for(int i=startXIndex;i<midXIndex;i++){
                            startZIndex = (int)Math.Round((p0.z + dz01 * (xArray[i] - p0.x) - bounds.min.z) / teleportationAreaAccuracy);
                            endZIndex = (int)Math.Round((p0.z + dz02 * (xArray[i] - p0.x) - bounds.min.z) / teleportationAreaAccuracy);

                            for(int j=startZIndex;j<endZIndex;j++){
                                try{
                                    topView[i, j] = Math.Max(topView[i, j], -(xArray[i] * normals[k/3].x + zArray[j] * normals[k/3].z - Vector3.Dot(normals[k/3], p0)) / normals[k/3].y);
                                }catch{
                                    
                                }
                            }
                        }

                        for(int i=midXIndex;i<endXIndex;i++){
                            startZIndex = (int)Math.Round((p1.z + dz12 * (xArray[i] - p1.x) - bounds.min.z) / teleportationAreaAccuracy);
                            endZIndex = (int)Math.Round((p0.z + dz02 * (xArray[i] - p0.x) - bounds.min.z) / teleportationAreaAccuracy);

                            for(int j=startZIndex;j<endZIndex;j++){
                                try{
                                    topView[i, j] = Math.Max(topView[i, j], -(xArray[i] * normals[k/3].x + zArray[j] * normals[k/3].z - Vector3.Dot(normals[k/3], p0)) / normals[k/3].y);
                                }catch{
                                    
                                }
                            }
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

                // convert heightmap/topview into layers
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
                                // grow square tile as much as possible
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

                                // find edge points for angle (& scale)
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

                                // calculate transform information
                                float translation = (((float)size) * teleportationAreaAccuracy) / 2.0f;
                                float x = xArray[i] + translation;
                                float z = zArray[j] + translation;
                                float xScale = 0.1f * size * teleportationAreaAccuracy;
                                float yScale = 0.1f * size * teleportationAreaAccuracy;
                                float zScale = 0.1f * size * teleportationAreaAccuracy;
                                float xAngle = 0.0f;
                                float zAngle = 0.0f;
                                float y = height;
                                if(Math.Abs(positiveX.y - negativeX.y) >= teleportationAreaSlopeLimit){
                                    float dy = positiveX.y - negativeX.y;
                                    float dx = positiveX.x - negativeX.x;
                                    double xAngleRad = Math.Atan2(dy, dx);

                                    y = Math.Max(y, (dy / dx) * (x - negativeX.x) + negativeX.y);

                                    xAngle = (float)(xAngleRad * 180.0 / Math.PI);

                                    xScale *= (float)Math.Abs(Math.Tan(xAngleRad / 2.0f)) + 1.0f;
                                }

                                if(Math.Abs(positiveZ.y - negativeZ.y) >= teleportationAreaSlopeLimit){
                                    float dy = positiveZ.y - negativeZ.y;
                                    float dz = positiveZ.z - negativeZ.z;
                                    double zAngleRad = Math.Atan2(dy, dz);
                                    
                                    y = Math.Max(y, (dy / dz) * (z - negativeZ.z) + negativeZ.y);

                                    zAngle = -(float)(zAngleRad * 180.0 / Math.PI);

                                    zScale *= (float)Math.Abs(Math.Tan(zAngleRad / 2.0f)) + 1.0f;
                                }
                                
                                if(yScale < teleportationAreaMinSize){
                                    continue;
                                }
                                
                                teleportAreaQueue.Enqueue(
                                    new Vector3[]{
                                        new Vector3(x, y + 0.01f, z),
                                        new Vector3(zAngle, 0.0f, xAngle),
                                        new Vector3(xScale, yScale, zScale)
                                    }
                                );
                            }
                        }
                    }
                }
            });
        }
    }
}
