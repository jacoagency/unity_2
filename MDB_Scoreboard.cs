using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// fake mongoDB scoreboard
// this script handles the Top10 Scoreboard data 

public class MDB_Scoreboard : MonoBehaviour
{
	//===============================================================
	public static MDB_Scoreboard Instance;

	private static System.Random random = new System.Random();
	//===============================================================

	// este es la lista de top10... son pares de String con Highscore (int)
	[HideInInspector] public List<KeyValuePair<string, int>> MDB_Top10Scoreboard;

	//===============================================================
	private float _fakeResponseTime = 0.5f;

	//===============================================================
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

	//===============================================================================================================
	// simplemente regresa la lista que esta guardada en este mismo scrip (top10 fake)
	public List<KeyValuePair<string, int>> MDB_GET_Scoreboard()
	{
		return MDB_Top10Scoreboard;
	}

	//===============================================================================================================
	// PUSH NEW top10 LIST TO MONGODB
	// sobreescribe la lista de este mismo script... deberia sobreescribir la lista de top10 de mongo
	public IEnumerator MDB_PUSH_newTop10scoreboard_list_co()
	{
		yield return new WaitForSeconds(_fakeResponseTime);

		MDB_Top10Scoreboard = LocalPlayerData.Instance.Local_Top10Scoreboard;
	}

	//===============================================================================================================
	// setup a fake top10 default list
	// es una funcion para hacer un top10 falso
	private void SetupRandomTop10_list()
	{
		MDB_Top10Scoreboard = PopulateList();
	}

	//===============================================================================================================
	// generates a list of 10 items with a random string and value
	// it's random GamerTag string and random HighScore value for each one
	public static List<KeyValuePair<string, int>> PopulateList()
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();

		for (int i = 0; i < 10; i++)
		{
			string key = GenerateRandomKey();
			int value = random.Next(0, 21); // Upper bound is exclusive

			// Add a new KeyValuePair to the list
			list.Add(new KeyValuePair<string, int>(key, value));
		}

		return list;
	}

	//===============================================================================================================
	// generates a string of 4 random letters
	private static string GenerateRandomKey()
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		int number = 12;
		char[] key = new char[number];

		for (int i = 0; i < number; i++)
		{
			key[i] = chars[random.Next(chars.Length)];
		}

		return new string(key);
	}
	//===============================================================================================================
}
