using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalPlayerData : MonoBehaviour
{
	//===============================================================
	public static LocalPlayerData Instance;
	//===============================================================
	// WalletAddress = la billetera del user este nos lo da la integracion de crypto y es el identificador del usuario
	// con este deberiamos poder buscar "existe el usuario con esta billetera?"
	// si existe = get player data (last score, high score, etc...)
	[HideInInspector] public static string WalletAddress;

	// GamerTag = el nickname que utiliza el "Top10 Scoreboard"
	// este se sobreescribe cada vez que se juega por primera vez (la primera vez que abres el link del juego, mientras no cierres el juego se sigue usando el mismo gamertag)
	// al cerrar el juego y volver a abrir te va a pedir escribir un gamertag, se puede escribir el mismo que antes o uno distinto es solo para scoreboard
	[HideInInspector] public static string GamerTag;

	// LastScore = ultimo score del juego (el ultimo run... cada que pagas 1 vez puedes jugar 3 runs "3 vidas")
	[HideInInspector] public static int LastScore;

	// HighScore = mayor puntaje obtenido en todos los runs del juego
	[HideInInspector] public static int HighScore;

	// TimesPlayed = suma +1 cada vez que el jugador paga para jugar 
	[HideInInspector] public static int TimesPlayed;

	// LastPlayedDateString = el texto de la fecha y hora en que se paga para jugar 
	[HideInInspector] public static string LastPlayedDateString;

	// LastPlayedTxidString = el texto del codigo identificador de la ultima transaccion que se paga para jugar (es como el id del ticket de compra)
	// este me lo da la integracion con crypto y solamente es Push a la mongoDB
	[HideInInspector] public static string LastPlayedTxidString;

	// es una LISTA que guarda pares de "string con int" "gamerTag con highscore"
	[HideInInspector] public List<KeyValuePair<string, int>> Local_Top10Scoreboard;

	// es una copia de la lista original que se obtiene de mongo... al terminar las 3 vidas el juego evalua si es necesario o no hacer push a una nueva lista de top10
	// (se obtiene la lista y el juego evalua "el mejor puntaje de las 3 vidas del user es mayor al puntaje del 10mo lugar? 
	// si si, hay que hacer nueva lista con el nuevo gamertag y high score, luego hacer push a la nueva lista a mongoDB)
	[HideInInspector] public List<KeyValuePair<string, int>> Local_OrderedTop10Scoreboard;

	private float _fakeResponseTime = 0.5f;
	public static bool ListUpdated = false;

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
	}

	//===============================================================
	//		overwrite LocalPlayerData
	//===============================================================

	// la funcion simplemente toma un string y lo guarda como WalletAddress (se usa con la integracion crypto)
	public static void SaveWalletAddress(string address)
	{
		WalletAddress = address;
	}

	// el juego toma el texto que escribe el usuario y lo guarda localmente
	public static void SaveGamertag(string gamertag)
	{
		GamerTag = gamertag;
	}

	// cada que 1 pago es exitoso se registra +1 a TimesPlayed, se registra fecha y el crypto id del pago
	public static void RegisterTimesPlayed(string txid)
	{
		TimesPlayed++;
		LastPlayedDateString = DateTime.Now.ToString();
		LastPlayedTxidString = txid;
	}

	// la funcion sobreescribe (de forma local el last_score de la ultima vida que se juega)
	// tambien revisa si es necesario sobreescribir el highscore localmente
	// cualquiera de los casos: empuja el nuevo data a mongoDB ya sea solo LastScore o ambos LastScore y HighScore
	public void SaveLastCheckHighScore(int score)
	{
		LastScore = score;
		if (LastScore > HighScore)
		{
			HighScore = LastScore;
			StartCoroutine(MDB_PlayerData.Instance.MDB_PUSH_LastScore());
			StartCoroutine(MDB_PlayerData.Instance.MDB_PUSH_HighScore());
		}
		else
		{
			StartCoroutine(MDB_PlayerData.Instance.MDB_PUSH_LastScore());
		}
	}

	
	//===============================================================
	// es la funcion que de forma FAKE sobreescribe el data de mongo (el script "MDB_PlayerData") con el data de "LocalPlayerData (es este mismo script)"
	public void OverwriteLocalDataWithMongoDB()
	{
		LastScore = MDB_PlayerData.MDB_GET_LastScore();
		HighScore = MDB_PlayerData.MDB_GET_HighScore();
		TimesPlayed = MDB_PlayerData.MDB_GET_TimesPlayed();
		LastPlayedDateString = MDB_PlayerData.MDB_GET_LastPlayedDate();
		LastPlayedTxidString = MDB_PlayerData.MDB_GET_LastPlayedTxid();
	}

	//===============================================================
	//	SCOREBOARD HANDLING
	//===============================================================

	// cuando al user se le terminan las 3 vidas, el juego llama a mongoDB para obtener la lista de top10 y revisa si es necesario subir una lista nueva
	// (en caso de que el score del user sea mayor al score del 10mo lugar)
	// es una coroutine (esta espera hasta tener respuesta de mongo)
	public void GetAndCheckTop10()
	{
		StartCoroutine(GetTop10_CheckIfNewTop10());
	}

	//===============================================================
	// this will be called when the button continue is clicked

	public IEnumerator GetTop10_CheckIfNewTop10()
	{
		// get the top10 list
		Local_Top10Scoreboard = MDB_Scoreboard.Instance.MDB_Top10Scoreboard;

		// wait for mongoDB response
		yield return new WaitForSeconds(_fakeResponseTime);

		// this is an ordered list of the mongoDB top10 list
		Local_OrderedTop10Scoreboard = CreateOrderedScore_ListFromList(Local_Top10Scoreboard);

		// this is a copied list... it checks if there's a new highscore or not
		Local_Top10Scoreboard = UpdateOrderedScore_List(Local_OrderedTop10Scoreboard, GamerTag, MainGameStatus.BestScore);

		// si la lista copia tiene un nuevo valor (el user alcanza el top10)
		if (ListUpdated)
		{
			// push the new LIST to mongoDB -> _listUpdated bool will be changed by Scenes_Controller OnEnable
			StartCoroutine(MDB_Scoreboard.Instance.MDB_PUSH_newTop10scoreboard_list_co());
		}
	}

	//===============================================================================================================
	//	UTILITY for the scoreboard logic
	//===============================================================================================================
	// esta es la funcion que ordena la lista por highscore mayor (1er lugar) al 10mo lugar 
	private List<KeyValuePair<string, int>> CreateOrderedScore_ListFromList(List<KeyValuePair<string, int>> list)
	{
		return list
			.OrderByDescending(x => x.Value)
			.ToList();
	}

	//===============================================================================================================
	// B. CREATE A NEW LIST WITH A NEW VALUE ATTACHED (IN CASE MY CURRENT SCORE FITS INTO THE TOP 10), takes the previous ordered list
	//	_toBeSHOWN_OrderedTop10_List
	private List<KeyValuePair<string, int>> UpdateOrderedScore_List(List<KeyValuePair<string, int>> orderedList, string key, int newScore)
	{
		// Check if the new score qualifies for the top 10 (equal to or greater than the 10th place)
		if (orderedList.Count < 10 || newScore >= orderedList[orderedList.Count - 1].Value)
		{
			// Flag indicating the list will be updated
			ListUpdated = true;

			// If the list already has 10 entries, remove the 10th place (the lowest score)
			if (orderedList.Count == 10)
			{
				orderedList.RemoveAt(9);
			}

			// Add the new score to the list with the player's key
			orderedList.Add(new KeyValuePair<string, int>(key, newScore));

			// Reorder the list in descending order based on the scores
			orderedList = orderedList.OrderByDescending(x => x.Value).ToList();
		}

		// Return the updated or unchanged ordered list
		return orderedList;
	}
}
