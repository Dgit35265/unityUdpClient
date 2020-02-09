using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject cube;
    public GameObject myCube;
    List<GameObject> cubes;
    List<string> newID;
    List<string> dropID;
    int spawnNum;

    string myID;

    // Start is called before the first frame update
    void Start()
    {
        cubes = new List<GameObject>();
        newID = new List<string>();
        dropID = new List<string>();
        spawnNum = 0;

        udp = new UdpClient();
        
        udp.Connect("18.209.27.229", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");    
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        //InvokeRepeating("HeartBeat", 1, 1);
        InvokeRepeating("SendPosition", 1, 0.033f);
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        UPDATE_LIST,
        DROP,
        RECEIVE_ID
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }
    
    [Serializable]
    public class Player{
        public string id;
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color; 
        public Vector3 position;
        public Vector3 rotation;
    }

    [Serializable]
    public class NewPlayer{
        public Player[] player;
    }

    [Serializable]
    public class PlayerInfo{
        public Vector3 sendPos;
        public Vector3 sendRot;
    }


    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        //Debug.Log("Got this: " + returnData);     
        latestMessage = JsonUtility.FromJson<Message>(returnData);

        try{
            switch(latestMessage.cmd){ //int here 
                case commands.NEW_CLIENT:
                    NewPlayer np = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach(var it in np.player)
                    {
                        newID.Add(it.id);
                    }
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    Debug.Log("Update Data");
                    break;
                case commands.UPDATE_LIST:
                    NewPlayer p = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach(var it in p.player)
                    {
                        newID.Add(it.id);
                    }
                    Debug.Log("UPDATE LIST");
                    break;
                case commands.DROP:
                    NewPlayer dp = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach(var it in dp.player)
                    {
                        dropID.Add(it.id);
                    }
                    Debug.Log("DROP CLIENT");
                    break;
                case commands.RECEIVE_ID:
                    NewPlayer idPlayer = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach(var it in idPlayer.player)
                    {
                        myID = it.id;
                    }  
                    Debug.Log("Receive ID");          
                    //Debug.Log(myID.ToString());
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(string id){
        foreach(var it in cubes)
        {
            if(it.GetComponent<ID>().id == id)
                return;
        }
        GameObject temp = Instantiate(cube, new Vector3(spawnNum, 0, 0), cube.transform.rotation);
        temp.GetComponent<ID>().id = id;
        if(id == myID)
        {         
            temp.AddComponent<PlayerMovement>();
            myCube = temp;
            Debug.Log("Find My Cube");
        }

        cubes.Add(temp);
        spawnNum++;
    }

    void UpdatePlayers(){
        foreach(var it in cubes)
        {
            foreach(var p in lastestGameState.players)
            {
                Color c = new Color(p.color.R, p.color.G, p.color.B);
                it.GetComponent<Renderer>().material.SetColor("_Color", c);
                if(it.GetComponent<ID>().id != myID)
                {
                    if(it.GetComponent<ID>().id == p.id)
                    {
                        Debug.Log(p.id);
                        Debug.Log(p.position);
                        it.transform.position = p.position;
                        it.transform.rotation = Quaternion.Euler(p.rotation);
                    }
                }
            }
        }
    }

    void DestroyPlayers(string id){
        for(int i = 0; i < cubes.Count; i++)
        {
            GameObject it = cubes[i];
            if(it.GetComponent<ID>().id == id)
            {
                GameObject temp = it;
                cubes.Remove(it);
                Destroy(temp);
            }
        }
    }
    
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }
    void SendPosition()
    {
        if(myCube != null)
        {
            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.sendPos = myCube.transform.position;  
            playerInfo.sendRot = myCube.transform.rotation.eulerAngles;     
            Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(playerInfo));
            udp.Send(sendBytes, sendBytes.Length);
        }
    }

    void Update(){
        if(newID.Count > 0)
        {
            for(int i = 0; i < newID.Count; i++)
            {
                string it = newID[i];
                SpawnPlayers(it);
            }
        }
        UpdatePlayers();

        if(dropID.Count > 0)
        {
            for(int i = 0; i < dropID.Count; i++)
            {
                string it = dropID[i];
                DestroyPlayers(it);
            }
        }
    }
}