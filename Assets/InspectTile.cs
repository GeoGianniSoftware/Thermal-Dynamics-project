using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InspectTile : MonoBehaviour
{
    GPUGen GEN;
    public Vector2 GUIOffset;
    public Vector2Int mousePosition;



    Vector3 panelStartPos;
    public Text text;
    public bool followCursor = false;

    // Start is called before the first frame update
    void Start()
    {
        GEN = FindObjectOfType<GPUGen>();
        text = GetComponentInChildren<Text>();
        panelStartPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(followCursor)
        transform.position = Vector3.Lerp(transform.position, Input.mousePosition + new Vector3(GUIOffset.x, GUIOffset.y, 0), .25f);
        else {
            transform.position = Vector3.Lerp(transform.position, panelStartPos, .18f);
        }


        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, 100)) {

            if (hit.transform.GetComponent<Structure>()) {
                hit.point = hit.transform.position;
            }

           mousePosition = new Vector2Int((int)(hit.point.x+.5f), (int)(hit.point.z+.5f));
        }
        tileNode tile = null;
        if (mousePosition.x >= 0 && mousePosition.y >= 0 && mousePosition.x <= GEN.size.x-1 && mousePosition.y <= GEN.size.y - 1) {
            tile = GEN.tileNodes[mousePosition.x, mousePosition.y];

            text.text = "(" + mousePosition.x + " , " + mousePosition.y + ")" + "\n " + tile.temperature.ToString("F2") + "\n";
            text.text += "W: " + tile.isWall + " R:" + tile.isRoofed + "\n";

            foreach (Gas g in tile.gasContent) {
                text.text += g.gasType.ToString() + ": " + g.percentage.ToString("F2") + "\n";
            }
        }

        if (Input.GetMouseButtonDown(1)) {
            GEN.SetWall(mousePosition, false);
        }
        if (Input.GetMouseButtonDown(0)) {
            if (Input.GetKey(KeyCode.LeftAlt)) {
                tile.setRoof(!tile.isRoofed);
                return;
            }
            GEN.SetWall(mousePosition, true);
        }
        if (Input.GetMouseButtonDown(2)) {
            if (tile != null) {
                if (Input.GetKey(KeyCode.LeftAlt)) {
                    tile.temperature += 100f;
                    return;
                }
                if (Input.GetKey(KeyCode.LeftControl)) {
                    tile.setGasPercentage(GasType.WATER, 50f);
                    return;
                }
                tile.setGasPercentage(GasType.GAS, 50f);
                print("spawned gas");
            }
        }

        if (Input.GetKeyDown(KeyCode.Numlock)) {
            followCursor = !followCursor;
        }
    }
}
