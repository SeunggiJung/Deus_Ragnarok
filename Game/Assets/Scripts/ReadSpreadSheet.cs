using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using static Enemy;

public class ReadSpreadSheet : MonoBehaviour
{
    public readonly string ADDRESS = "https://docs.google.com/spreadsheets/d/161B3mae2j6yc1g4C3it8mmm7UOnaLp9UEVZhnCKdIU8";
    public readonly string RANGE = "A3:I";
    public readonly long SHEET_ID = 0;
    public UnityWebRequest www;
    //public List<Enemy_status> enemies;
    public bool loaded = false;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadData());
    }
    private IEnumerator LoadData()
    {
        www = UnityWebRequest.Get(GetTSVAddress(ADDRESS, RANGE, SHEET_ID));
        yield return www.SendWebRequest();
        //List<Enemy_status> enemies = GetDatas<Enemy_status>(www.downloadHandler.text);
        Debug.Log("db 로드");
        loaded = true;
    }
    public static string GetTSVAddress(string address, string range, long sheetID)
    {
        return $"{address}/export?format=tsv&range={range}&gid={sheetID}";
    }
    // Update is called once per frame
    public T GetData<T>(string[] datas)
    {
        object data = Activator.CreateInstance(typeof(T));

        // 클래스에 있는 변수들을 순서대로 저장한 배열
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        for (int i = 0; i < 9; i++)
        {
            try
            {
                // string > parse
                Type type = fields[i].FieldType;

                if (string.IsNullOrEmpty(datas[i])) continue;

                // 변수에 맞는 자료형으로 파싱해서 넣는다
                if (type == typeof(int))
                    fields[i].SetValue(data, int.Parse(datas[i]));

                else if (type == typeof(float))
                    fields[i].SetValue(data, float.Parse(datas[i]));

                else if (type == typeof(bool))
                    fields[i].SetValue(data, bool.Parse(datas[i]));

                else if (type == typeof(string))
                    fields[i].SetValue(data, datas[i]);

                // enum
                else
                    fields[i].SetValue(data, Enum.Parse(type, datas[i]));
            }

            catch (Exception e)
            {
                Debug.LogError($"SpreadSheet Error : {e.Message}");
            }
        }

        return (T)data;
    }
    public List<T> GetDatas<T>(string data)
    {
        List<T> returnList = new List<T>();
        string[] splitedData = data.Split('\n');
        
        foreach(string element in splitedData)
        {
            string[] datas = element.Split('\t');
            returnList.Add(GetData<T>(datas));
        }
        return returnList;
    }
    public class Animal
    {
        public int ID;
        public float height = 1.7f;
        public string enemyName;
        public int maxHp;
        public int atkDmg;
        public float atkSpeed;
        public float moveSpeed;
        public float atkRange;
        public float fieldOfVision;
    }
    void Update()
    {
        
    }
}
