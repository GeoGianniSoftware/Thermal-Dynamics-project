using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class ObjData
{
    public Vector3 pos;
    public Vector3 scale;
    public Quaternion rot;

    public Matrix4x4 matrix {
        get {
            return Matrix4x4.TRS(pos, rot, scale);
        }
    }

    public ObjData(Vector3 p, Vector3 s, Quaternion r) {
        this.pos = p;
        this.rot = r;
        this.scale = s;
    }
}

[System.Serializable]

public class Gas
{
    public GasType gasType;
    public float percentage;
    public float disapationRate = .00015f;

    public Gas() {
        gasType = GasType.AIR;
        percentage = 01f;
    }

    public Gas(GasType t, float p) {
        gasType = t;
        percentage = p;
    }
}


public enum GasType
{
    AIR,
    GAS,
    WATER
}

public class tileNode
{
    public List<Gas> gasContent;
    private List<tileNode> neighbors;
    public Vector3 pos;
    public Vector2Int index;
    public float temperature;
    public bool isWall = false;
    public bool isRoofed = false;

    public GameObject prefabObj;
    public GameObject obj;
    public tileNode(Vector2Int p, Color c) {
        pos = new Vector3(p.x, 0, p.y);
        temperature = 70;
        gasContent = new List<Gas>();
        gasContent.Add(new Gas());
        index = new Vector2Int((int)pos.x, (int)pos.z);
    }

    public void setNeighbors(List<tileNode> ns) {
        neighbors = ns;
    }

    public List<tileNode> getNeighbors() {
        return neighbors;
    }

    public void setWall(bool state) {
        if (isRoofed && !isWall) {
            setWall(true);
            setRoof(false);
            return;
        }

        isWall = state;
        prefabObj = Resources.Load("Wall") as GameObject;

        if(obj == null && state) {

            GameObject temp = GameObject.Instantiate(prefabObj, pos, Quaternion.identity);
            temp.transform.position += Vector3.up;
            if (temp.GetComponent<Structure>()) {
                temp.GetComponent<Structure>().parentNode = this;
            }
            obj = temp;
        }
        else if (obj != null && !state) {
            GameObject.Destroy(obj);
        }


    }

    public void setRoof(bool state) {
        if(!isRoofed  && isWall) {
            setWall(false);
            setRoof(true);
            return;
        }

        isRoofed = state;
        prefabObj = Resources.Load("Roof") as GameObject;

        if (obj == null && state) {

            GameObject temp = GameObject.Instantiate(prefabObj, pos, Quaternion.identity);
            temp.transform.position += Vector3.up*1.45f;
            if (temp.GetComponent<Structure>()) {
                temp.GetComponent<Structure>().parentNode = this;
            }
            obj = temp;
        }
        else if (obj != null && !state) {
            GameObject.Destroy(obj);
        }


    }

    public void tileTick() {
        if (!isRoofed) {
            foreach (Gas g in gasContent) {
                if (g.percentage > 0)
                    g.percentage -= g.disapationRate;
            }
        }

        float totalPercentage = 0f;
        foreach(Gas g in gasContent) {
            if (g.gasType == GasType.AIR)
                continue;

            totalPercentage += g.percentage;
        }


        setGasPercentage(GasType.AIR, 100f - totalPercentage);
    }

    public void setGasPercentage(GasType t, float p) {
        foreach (Gas g in gasContent) {

            if (g.gasType == t) {

                g.percentage = p;
                return;
            }
        }
        gasContent.Add(new Gas(t, p));
    }

    
    public float returnGasPercentage(GasType type) {
        foreach(Gas g in gasContent) {
            if (g.gasType == type)
                return g.percentage;
        }
        return 0f;
    }

    public bool containsGas(GasType t) {
        bool contains = false;
        foreach(Gas g in gasContent) {
            if (g.gasType == t)
                contains = true;
        }

        return contains;
    }

    public void AddGas(GasType t, float p) {
        foreach(Gas g in gasContent) {
            if (g.gasType == GasType.AIR)
                continue;

            if(g.gasType == t) {

                if(g.percentage + p > 100f) {
                    g.percentage = 100f;
                    return;
                }

                if (g.percentage + p < 0f) {
                    g.percentage = 0f;
                    return;
                }

                g.percentage += p;
                return;
            }
        }
        gasContent.Add(new Gas(t, p));
    }

}

public class GPUGen : MonoBehaviour
{
    

    public Vector2Int size;
    public int instances;
    public Mesh prefab;
    public GameObject floorPrefab;
    public Material objMaterial;
    public Gradient tempColor = new Gradient();

    private List<List<ObjData>> batches = new List<List<ObjData>>();

    public tileNode[,] tileNodes;

    float t = 0f;
    public bool runSimulation = true;
    public float transferSpeed = GlobalProperties.conductionRate;
    public float tickTimer = GlobalProperties.tickLength;

    // Start is called before the first frame update
    void Start()
    {
        InstantiateWorld();




    }


    void InstantiateWorld() {
        instances = size.x * size.y;
        tileNodes = new tileNode[size.x, size.y];

        GameObject floor = Instantiate(floorPrefab, new Vector3(size.x/2 -.5f, 0, size.y/2 - .5f), Quaternion.identity);
        floor.transform.localScale = new Vector3(size.x, 1, size.y);

        int batchIndexNum = 0;
        List<ObjData> currBatch = new List<ObjData>();
        int i = 0;
        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                AddObj(new Vector2Int(x, y), currBatch, i);
                tileNodes[x, y] = new tileNode(new Vector2Int(x, y), Random.ColorHSV());
                batchIndexNum++;
                if (batchIndexNum >= 1000) {
                    batches.Add(currBatch);
                    currBatch = BuildNewBatch();
                    batchIndexNum = 0;
                }
                i++;
            }
        }
        batches.Add(currBatch);
        currBatch = BuildNewBatch();
        batchIndexNum = 0;
    }

    public List<tileNode> getNeighbors(tileNode tile) {
        if(tile.getNeighbors() == null) {
            List<tileNode> temp = new List<tileNode>();
            if (tile.index.x > 0) {
                temp.Add(tileNodes[tile.index.x - 1, tile.index.y]);
            }
            if (tile.index.x < size.x - 1) {
                temp.Add(tileNodes[tile.index.x + 1, tile.index.y]);
            }

            if (tile.index.y > 0) {
                temp.Add(tileNodes[tile.index.x, tile.index.y - 1]);
            }
            if (tile.index.y < size.y - 1) {
                temp.Add(tileNodes[tile.index.x, tile.index.y + 1]);
            }

            if (tile.index.y < size.y - 1 && tile.index.x < size.x - 1) {
                temp.Add(tileNodes[tile.index.x + 1, tile.index.y + 1]);
            }
            if (tile.index.x > 0 && tile.index.y > 0) {
                temp.Add(tileNodes[tile.index.x - 1, tile.index.y - 1]);
            }
            if (tile.index.y < size.y - 1 && tile.index.x > 0) {
                temp.Add(tileNodes[tile.index.x - 1, tile.index.y + 1]);
            }
            if (tile.index.x < size.x - 1 && tile.index.y > 0) {
                temp.Add(tileNodes[tile.index.x + 1, tile.index.y - 1]);
            }
            tile.setNeighbors(temp);
            return temp;
        }
        else {
            return tile.getNeighbors();
        }
        
    }

    private List<ObjData> BuildNewBatch() {
        return new List<ObjData>();
    }

    List<Gas> calculateAverageGasContent(List<Gas> gases) {
        List<Gas> first = new List<Gas>();
        List<GasType> gasesContained = new List<GasType>();
        List<int> gasTypeAmt = new List<int>();

        foreach(Gas g in gases) {
            if (gasesContained.Contains(g.gasType)) {
                for (int i = 0; i < gasesContained.Count; i++) {
                    if (gasesContained[i] == g.gasType) {

                        first[i].percentage += g.percentage;
                        gasTypeAmt[i]++;
                    }
                }
            }
            else {
                first.Add(g);
                gasesContained.Add(g.gasType);
                gasTypeAmt.Add(1);
            }
        }
        List<Gas> final = new List<Gas>();
        for (int i = 0; i < first.Count; i++) {
            print(first[i].gasType.ToString() + ": " + (first[i].percentage / gasTypeAmt[i]) + " " + first[i].percentage + " / " + gasTypeAmt[i] );
            final.Add(new Gas(first[i].gasType, first[i].percentage / gasTypeAmt[i]));
        }
        return final;

    }

    List<Gas> calculateTotalGasContent(List<Gas> gases) {
        List<Gas> first = new List<Gas>();
        List<GasType> gasesContained = new List<GasType>();

        foreach (Gas g in gases) {
            if (gasesContained.Contains(g.gasType)) {
                for (int i = 0; i < gasesContained.Count; i++) {
                    if (gasesContained[i] == g.gasType) {

                        first[i].percentage += g.percentage;
                    }
                }
            }
            else {
                first.Add(new Gas(g.gasType, g.percentage));
                gasesContained.Add(g.gasType);
            }
        }
        return first;

    }
    float returnGasPercentage(List<Gas> gases, GasType type) {
        foreach (Gas g in gases) {
            if (g.gasType == type)
                return g.percentage;
        }
        return 0f;
    }

    private void OnDrawGizmos() {
        if(tileNodes != null) {
            

            foreach(tileNode t in tileNodes) {
                if (t.isWall)
                    Gizmos.color = Color.black;
                else {
                    Color temp = Color.white;
                    temp.g = (t.returnGasPercentage(GasType.GAS)/100) * 50;
                    temp.b = (t.returnGasPercentage(GasType.WATER) / 100) * 50;
                    Gizmos.color = temp;
                }


                Gizmos.DrawCube(t.pos+Vector3.up, Vector3.one);
            }
        }
    }

    private void AddObj(Vector2Int pos, List<ObjData> currBatch, int i) {
        currBatch.Add(new ObjData(new Vector3(pos.x, 0, pos.y), new Vector3(1, 1, 1), Quaternion.identity));
    }

    // Update is called once per frame
    void Update()
    {
        RenderBatches();
        if (runSimulation) {
            if (t <= 0) {

                TickAllTiles();
                t = tickTimer;
            }
        }
        
        t -= Time.deltaTime;

        /*int degreesPerSecond = 500;

        tileNodes[0, 0].temperature += degreesPerSecond * Time.deltaTime;


        tileNodes[size.x - 1, size.y - 1].temperature += degreesPerSecond * Time.deltaTime;

        tileNodes[0, size.y - 1].temperature += degreesPerSecond * Time.deltaTime;


        tileNodes[size.x - 1, 0].temperature += degreesPerSecond * Time.deltaTime;*/
    }

    void CalculateTileTemperatureAndGasses(tileNode t) {
        int nCount = 0;
        float averageTemp = 0;
        List<Gas> averageGasContent = new List<Gas>();
        foreach (tileNode n in getNeighbors(t)) {

            if (!n.isWall) {
                averageTemp += n.temperature;
                nCount++;

                foreach (Gas g in n.gasContent) {
                    averageGasContent.Add(g);
                }
            }

        }

        List<Gas> newAverageGas = calculateTotalGasContent(averageGasContent);

        if (nCount > 0) {
            averageTemp = averageTemp / nCount;


            //Temperature Exchange
            if (t.temperature != averageTemp) {
                float a = t.temperature / GlobalProperties.maxGlobalTemperature;
                float b = averageTemp / GlobalProperties.maxGlobalTemperature;

                t.temperature += (b - a) * transferSpeed*5;
            }
            //Global Temp
            if (t.temperature != GlobalProperties.globalTemperature) {
                float a = t.temperature / GlobalProperties.maxGlobalTemperature;
                float b = GlobalProperties.globalTemperature / GlobalProperties.maxGlobalTemperature;

                float mod = 1f;

                if (t.isRoofed) {
                    mod = .15f;
                }

                t.temperature += (b - a) * mod * GlobalProperties.globalTemperatureIncreaseMod * transferSpeed;
            }
            //Gas Exchange
            foreach (Gas avg in newAverageGas) {
                if (avg.gasType == GasType.AIR)
                    continue;

                float gA = returnGasPercentage(t.gasContent, avg.gasType) / 100f;
                float gB = avg.percentage / (nCount) / 100f;
                if (!gA.ToString("F6").Equals(gB.ToString("F6"))) {
                    float change = (gB - gA) * (transferSpeed);


                    if (!t.isWall || (t.isWall && change < 0)) {

                        t.AddGas(avg.gasType, change);
                    }
                }

            }
        }
        

    }

    void TickAllTiles() {
        foreach(tileNode t in tileNodes) {
            t.tileTick();
            CalculateTileTemperatureAndGasses(t);

        }
    }

    public void SetWall(Vector2Int gridPos, bool state) {
        tileNodes[gridPos.x, gridPos.y].setWall(state);
    }

    private void RenderBatches() {
        foreach (var batch in batches) {
            Graphics.DrawMeshInstanced(prefab, 0, objMaterial, batch.Select((a) => a.matrix).ToList());
        }
    }
}
