using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ButtonManager : MonoBehaviour
{
	//===============================================================
	public static ButtonManager Instance;

	public static bool _buttonPressed = false;

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
	//		BUTTON QUIT: quits the game
	public void Button_Quit()
	{
		// play click sfx
		AudioManager.Instance.PlaySFX(0);
		AudioManager.Instance.StopAllMusic();

		CanvasManager.Instance.Enable11_QuitCanvas();

		Application.Quit(); 
	}

	//===============================================================
	//		MAIN MENU: PLAY BUTTON
	//===============================================================
	public void PlayButton()
	{
		// play click sfx
		AudioManager.Instance.PlaySFX(0);

		// call the wallet transaction Coroutine handler here
		StartCoroutine(HandlePlay_ExternalServices_MainMenu());
	}

	//===============================================================
	//		GAME OVER: TRY AGAIN
	public void TryAgainButton()
	{
		// play click sfx
		AudioManager.Instance.PlaySFX(0);

		// push new last and high score to mongo
		StartCoroutine(Scenes_Controller.Instance.LoadGameplay_Co());
		return;
	}

	//===============================================================
	//		GAME OVER: CONTINUE
	public void GameoverContinueButton()
	{
		// play click sfx
		AudioManager.Instance.PlaySFX(0);

		StartCoroutine(GameOverContinue_co());
		return;
	}

	private IEnumerator GameOverContinue_co()
	{
		yield return null;
		LocalPlayerData.Instance.GetAndCheckTop10();

		if (LocalPlayerData.ListUpdated)
		{
			LocalPlayerData.ListUpdated = false;
		}

		StartCoroutine(Scenes_Controller.Instance.LoadScoreboard_Co());
		yield break;
	}

	//===============================================================
	//		SCOREBOARD: CONTINUE
	public void ScoreboardContinueButton()
	{
		// play click sfx
		AudioManager.Instance.PlaySFX(0);

		StartCoroutine(Scenes_Controller.Instance.LoadMainMenu_Co());
		return;
	}




	//===============================================================
	//		COROUTINES FOR DEALING WITH CRYPTO + MONGO
	//===============================================================
	// fake handling the wallet transaction
	private IEnumerator HandlePlay_ExternalServices_MainMenu()
	{
		// first check if user interacted with contract before
		string fakeUserAddressOnDatabase = "aSolWalletAddress";
		yield return StartCoroutine(CryptoTx_Manager.Instance.CheckContractInteraction(fakeUserAddressOnDatabase));

		// if the contract is signed continue to spend transaction
		if (CryptoTx_Manager.ContractSigned)
		{
			//=====================================================================
			// prompt to sign the cryptoTXID
			yield return StartCoroutine(CryptoTx_Manager.Instance.FakeSpendTx());

			// if contract signed and txSigned, continue
			if (CryptoTx_Manager.ContractSigned && CryptoTx_Manager.TxSigned)
			{
				//=====================================================================
				// prompt to check if txid is valid
				yield return StartCoroutine(CryptoTx_Manager.Instance.CheckTxidValid());

				// if contract, signature and txid is valid continue
				if (CryptoTx_Manager.ContractSigned && CryptoTx_Manager.TxSigned && CryptoTx_Manager.TxidValid)
				{
					//=====================================================================
					// ------>>>> EVERYTHING IS VALID HERE <<<<------
					// continue to push / get from mongo... then to load the next scene


					//=====================================================================
					// ------>>>> MONGO STUFF SHOULD HAPPEN HERE BEFORE LOADING THE GAMEPLAY
					//=====================================================================



					StartCoroutine(LoadGameplay_MainMenu_co());

					// reset bools and break
					CryptoTx_Manager.ResetAllCryptoBools();
					yield break;
					//=====================================================================
				}
				// error transaction was NOT Valid
				else
				{
					// reset bools then break... let the user start again
					CryptoTx_Manager.ResetAllCryptoBools();
					yield break;
				}
				//=====================================================================
			}
			// error transaction was NOT Signed
			else
			{
				// reset bools then break... let the user start again
				CryptoTx_Manager.ResetAllCryptoBools();
				yield break;
			}
			//=====================================================================
		}
		else if (!CryptoTx_Manager.ContractSigned)
		{
			//prompt to sign the contract first
			yield return StartCoroutine(CryptoTx_Manager.Instance.FakeContractInteractionTx());
			// reset bools then break... let the user start again once contract is signed
			CryptoTx_Manager.ResetAllCryptoBools();
			yield break;
		}
	}


	//===============================================================
	// once the crypto handling is done, continue to push / get data from mongo and load cutscene
	private IEnumerator LoadGameplay_MainMenu_co()
	{
		// once the transaction is valid register the last txid, date and +1 times played
		LocalPlayerData.RegisterTimesPlayed("theValidTxidFromPlayTransaction");

		// register the last played
		yield return StartCoroutine(MDB_PlayerData.Instance.MDB_PUSH_registerLastPlayed());

		// gameplay stuff
		// -> give the player 4 lives (1 is subtracted when gameplay starts) and reset the best score (this is best per session)
		MainGameStatus.GiveLives();
		MainGameStatus.ResetBestScore();

		// load the cutscene
		StartCoroutine(Scenes_Controller.Instance.LoadGameplay_Co());
		yield break;
	}


	//==================================================================================================
	public void SpaceButton(InputAction.CallbackContext context)
	{
		if (context.performed && !_buttonPressed && !Application.isMobilePlatform)
		{
			// MAIN MENU
			if (CanvasManager.Instance._canvasArray[3].activeSelf)
			{
				_buttonPressed = true;
				PlayButton();
			}

			// GAME OVER
			else if (CanvasManager.Instance._canvasArray[8].activeSelf)
			{
				if (MainGameStatus.GameLives >= 1)
				{
					_buttonPressed = true;
					TryAgainButton();
				}
				else if (MainGameStatus.GameLives <= 0)
				{
					_buttonPressed = true;
					GameoverContinueButton();
				}
			}

			// SCOREBOARD
			else if (CanvasManager.Instance._canvasArray[9].activeSelf)
			{
				_buttonPressed = true;
				ScoreboardContinueButton();
			}
		}
	}

	public static void ResetButtonPressed()
	{
		_buttonPressed = false;
	}

	public static void ToggleMuteUnmute()
	{
		// toggle the actual sound
		AudioManager.Instance.ToggleMute();

		// toggle the public static bool so the text changes by itself
		MuteButtonController.GameMuted = !MuteButtonController.GameMuted;
	}
}
