using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using UnityEngine;

public class MDB_Manager : MonoBehaviour
{
    public static MDB_Manager Instance;

    // MongoDB connection string
    private static string connectionString = "mongodb+srv://root:root@cluster0.qbkfnji.mongodb.net/unity?retryWrites=true&w=majority&appName=Cluster0";
    private float _fakeResponseTime = 0.5f;

    // Player data fields (previously in MDB_PlayerData)
    [HideInInspector] public static string MDB_WalletAddress;
    [HideInInspector] public static string MDB_GamerTag;
    [HideInInspector] public static int MDB_LastScore;
    [HideInInspector] public static int MDB_HighScore;
    [HideInInspector] public static int MDB_TimesPlayed;
    [HideInInspector] public static string MDB_LastPlayedDateString;
    [HideInInspector] public static string MDB_LastPlayedTxidString;

    // Top 10 Scoreboard data fields (previously in MDB_Scoreboard)
    [HideInInspector] public List<KeyValuePair<string, int>> MDB_Top10Scoreboard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        SetupRandomTop10_list();
    }

    // MongoDB Player Data Operations ========================================

    public IEnumerator MDB_PUSH_username()
    {
        yield return new WaitForSeconds(_fakeResponseTime);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("PlayerData");

        var update = Builders<BsonDocument>.Update.Set("GamerTag", LocalPlayerData.GamerTag);
        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("WalletAddress", LocalPlayerData.WalletAddress),
            update
        );
    }

    public IEnumerator MDB_PUSH_registerLastPlayed()
    {
        yield return new WaitForSeconds(_fakeResponseTime);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("PlayerData");

        var update = Builders<BsonDocument>.Update
            .Set("TimesPlayed", LocalPlayerData.TimesPlayed)
            .Set("LastPlayedDateString", LocalPlayerData.LastPlayedDateString)
            .Set("LastPlayedTxidString", LocalPlayerData.LastPlayedTxidString);

        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("WalletAddress", LocalPlayerData.WalletAddress),
            update
        );
    }

    public IEnumerator MDB_PUSH_LastScore()
    {
        yield return new WaitForSeconds(_fakeResponseTime);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("PlayerData");

        var update = Builders<BsonDocument>.Update.Set("LastScore", LocalPlayerData.LastScore);
        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("WalletAddress", LocalPlayerData.WalletAddress),
            update
        );
    }

    public IEnumerator MDB_PUSH_HighScore()
    {
        yield return new WaitForSeconds(_fakeResponseTime);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("PlayerData");

        var update = Builders<BsonDocument>.Update.Set("HighScore", LocalPlayerData.HighScore);
        await collection.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("WalletAddress", LocalPlayerData.WalletAddress),
            update
        );
    }

    public static int MDB_GET_LastScore()
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("PlayerData");

        var document = collection.Find(Builders<BsonDocument>.Filter.Eq("WalletAddress", LocalPlayerData.WalletAddress)).FirstOrDefault();
        return document != null ? document["LastScore"].AsInt32 : 0;
    }

    public static int MDB_GET_HighScore()
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("PlayerData");

        var document = collection.Find(Builders<BsonDocument>.Filter.Eq("WalletAddress", LocalPlayerData.WalletAddress)).FirstOrDefault();
        return document != null ? document["HighScore"].AsInt32 : 0;
    }

    // MongoDB Scoreboard Operations ========================================

    public List<KeyValuePair<string, int>> MDB_GET_Scoreboard()
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("Scoreboard");

        var scoreboard = collection.Find(new BsonDocument()).ToList();
        List<KeyValuePair<string, int>> top10List = new List<KeyValuePair<string, int>>();

        foreach (var item in scoreboard)
        {
            string gamerTag = item["GamerTag"].AsString;
            int highScore = item["HighScore"].AsInt32;
            top10List.Add(new KeyValuePair<string, int>(gamerTag, highScore));
        }

        return top10List.OrderByDescending(x => x.Value).ToList();
    }

    public IEnumerator MDB_PUSH_newTop10scoreboard_list_co()
    {
        yield return new WaitForSeconds(_fakeResponseTime);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("GameDatabase");
        var collection = database.GetCollection<BsonDocument>("Scoreboard");

        // Remove all existing top10 records (optional based on your design)
        await collection.DeleteManyAsync(new BsonDocument());

        foreach (var kvp in LocalPlayerData.Instance.Local_Top10Scoreboard)
        {
            var document = new BsonDocument
            {
                { "GamerTag", kvp.Key },
                { "HighScore", kvp.Value }
            };

            await collection.InsertOneAsync(document);
        }
    }

    private void SetupRandomTop10_list()
    {
        MDB_Top10Scoreboard = PopulateList();
    }

    public static List<KeyValuePair<string, int>> PopulateList()
    {
        List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
        for (int i = 0; i < 10; i++)
        {
            string key = GenerateRandomKey();
            int value = new System.Random().Next(0, 21); // Upper bound is exclusive
            list.Add(new KeyValuePair<string, int>(key, value));
        }
        return list;
    }

    private static string GenerateRandomKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, 12)
          .Select(s => s[new System.Random().Next(s.Length)]).ToArray());
    }
}
