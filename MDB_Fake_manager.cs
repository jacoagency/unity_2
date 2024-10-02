using System.Collections;
using MongoDB.Bson;
using MongoDB.Driver;
using UnityEngine;

public class MDB_PlayerData : MonoBehaviour
{
    public static MDB_PlayerData Instance;
    private static string connectionString = "mongodb+srv://root:root@cluster0.qbkfnji.mongodb.net/unity?retryWrites=true&w=majority&appName=Cluster0";
    private float _fakeResponseTime = 0.5f;

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
    }

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
}
