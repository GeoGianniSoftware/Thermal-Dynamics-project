using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;

[ExecuteInEditMode]
public class UnitDatabase : MonoBehaviour {
    public List<Nation> database = new List<Nation>();
    private JsonData nationData;
    public bool loadDatabase;

    
    private void Start() {
        nationData = JsonMapper.ToObject(File.ReadAllText(Application.dataPath + "/StreamingAssets/Nations.json"));
        
    }

    private void Update() {
        nationData = JsonMapper.ToObject(File.ReadAllText(Application.dataPath + "/StreamingAssets/Nations.json"));

        if (loadDatabase && database.Count == 0) {
            print("loading database");
            ConstructMinionDatabase();
        }
        else if(!loadDatabase){
            database = new List<Nation>();
        }
    }

    public Nation FindNationByID(int id) {
        for (int i = 0; i < database.Count; i++)
            if (database[i].ID == id)
                return database[i];
        return null;
    }

    void ConstructMinionDatabase() {
        for (int i = 0; i < nationData.Count; i++) {


            int unitCount = (int)nationData[i]["units"].Count;
            List<Unit> unitList = new List<Unit>();

            //Get units from Database;
            for (int u = 0; u < unitCount; u++) {

                //Get perklist from Database
                int perkCount = (int)nationData[i]["units"][u]["perks"].Count;
                int[] perkList = new int[perkCount];
                for (int p = 0; p < perkCount; p++) {
                    perkList[p] = (int)nationData[i]["units"][u]["perks"][p];
                }

                //Get unit from Database
                Unit unit = new Unit(
                    (int)nationData[i]["units"][u]["id"],
                    nationData[i]["units"][u]["slug"].ToString(),
                    nationData[i]["units"][u]["name"].ToString(),
                    perkList,
                    (bool)nationData[i]["units"][u]["rangedAttack"],
                    (bool)nationData[i]["units"][u]["fireDiagonally"],
                    (int)nationData[i]["units"][u]["hitpoints"],
                    (int)nationData[i]["units"][u]["cohesion"],
                    (int)nationData[i]["units"][u]["damage_charge"],
                    (int)nationData[i]["units"][u]["damage_ranged"],
                    (int)nationData[i]["units"][u]["resist_charge"],
                    (bool)nationData[i]["units"][u]["canChangeFormation"],
                    (int)nationData[i]["units"][u]["speed"]
                );
                unitList.Add(unit);
            }

            //Get nation from Database;
            Nation nation = new Nation(
                 (int)nationData[i]["id"],
                    nationData[i]["slug"].ToString(),
                    nationData[i]["name"].ToString(),
                    unitList
                );

            database.Add(nation);
        }
    }
}
[System.Serializable]
public class Nation
{

    public string name;
    public int ID;
    public string Slug;
    public List<Unit> Units;

    public Nation(int id, string slug, string _name, List<Unit> units) {
        this.ID = id;
        this.name = _name;
        this.Slug = slug;
        this.Units = units;
    }

}

[System.Serializable]
public class Unit {

    public string name;
    public int ID;
    public Sprite cardArt; //TODO connect to cardArt;
    public Sprite[] artwork; //TODO connect to artwork;

    [Header("Stats")]
    public int[] perkTypes; //TODO connect to perks;
    public bool hasRangedAttack;
    public bool canFireDiagonally;

    [Header("Combat")]
    public int maxHitpoints;
    public int maxCohesion;

    public int chargeDamage;
    public int rangedAttackDamage;
    public int chargeResistance;

    [Header("Movement")]
    public bool canChangeFormation;
    public int movementSpeed;
   


    public string Slug;

    public Unit(int id, string slug, string _name, int[] perks, bool rangedAttack, bool fireDiag, int hit, int coh, int cDamage, int rDamage, int cResist, bool formation, int speed) {
        this.ID = id;
        this.Slug = slug;
        this.name = _name;
        this.perkTypes = perks;
        this.hasRangedAttack = rangedAttack;
        this.canFireDiagonally = fireDiag;
        this.maxHitpoints = hit;
        this.maxCohesion = coh;
        this.chargeDamage = cDamage;
        this.rangedAttackDamage = rDamage;
        this.chargeResistance = cResist;
        this.canChangeFormation = formation;
        this.movementSpeed = speed;
    }


}
