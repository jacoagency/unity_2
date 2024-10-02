using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FreshMainMenu_Buttons : MonoBehaviour
{
	//===============================================================
	// the input field
	[SerializeField] private InputField _usernameInputField;
	// the inputed text from user
	public static string _userInputTextUsername;

	private bool _emptyFieldUsername = false;
	private bool _hasEmptyFields = false;

	//===============================================================
	// if string is not null or empty, saves the inputs to the variables
	public void SaveUserInput()
	{
		//=== username input
		if (!string.IsNullOrEmpty(_usernameInputField.text) || _usernameInputField.text.Length >= 1)
		{
			_userInputTextUsername = _usernameInputField.text;
		}
		else
		{
			_emptyFieldUsername = true;
			_hasEmptyFields = true;
		}
	}

	//===============================================================
	public void ResetEmptyBools()
	{
		_emptyFieldUsername = false;
		_hasEmptyFields = false;
	}

	//===============================================================
	//		FRESH MAIN MENU: PLAY BUTTON
	public void FreshPlayButton()
	{
		// play click sfx
		AudioManager.Instance.PlaySFX(0);

		SaveUserInput();
		if (_hasEmptyFields || _emptyFieldUsername)
		{
			ErrorPopup_Manager.Instance.CallError("inputEmpty");
			// reset the bools so the user can try again then break this function
			ResetEmptyBools();
			return;
		}
        else if (!_hasEmptyFields && !_emptyFieldUsername)
        {
			// this will handle the crypto spend transaction... then load the cutscene
			StartCoroutine(HandlePlay_ExternalServices_FreshMain());
		}
	}


	//===============================================================
	// fake handling the wallet transaction
	private IEnumerator HandlePlay_ExternalServices_FreshMain()
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


					//=====================================================================
					// ------>>>> MONGO STUFF SHOULD HAPPEN HERE BEFORE LOADING THE GAMEPLAY
					//=====================================================================



					StartCoroutine(LoadCutscene_FreshMain_co());

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
	private IEnumerator LoadCutscene_FreshMain_co()
	{
		// once the transaction is valid -> save username, wallet to local
		LocalPlayerData.SaveWalletAddress("theSignerWalletAddress");
		LocalPlayerData.SaveGamertag(_userInputTextUsername);
		// register the last txid, date and +1 times played
		LocalPlayerData.RegisterTimesPlayed("theValidTxidFromPlayTransaction");


		// push new username to mongo
		yield return StartCoroutine(MDB_PlayerData.Instance.MDB_PUSH_username());
		// get the full player data from mongo
		LocalPlayerData.Instance.OverwriteLocalDataWithMongoDB();
		// register the last played
		yield return StartCoroutine(MDB_PlayerData.Instance.MDB_PUSH_registerLastPlayed());


		// gameplay stuff
		// -> give the player 4 lives (1 is subtracted when gameplay starts) and reset the best score (this is best per session)
		MainGameStatus.GiveLives();
		MainGameStatus.ResetBestScore();

		// load the cutscene
		StartCoroutine(Scenes_Controller.Instance.LoadCutscene_Co());
		yield break;
	}

	//==================================================================================================
	public void SpaceButton(InputAction.CallbackContext context)
	{
		if (context.performed && !ButtonManager._buttonPressed && !Application.isMobilePlatform)
		{
			// MAIN MENU
			if (CanvasManager.Instance._canvasArray[1].activeSelf)
			{
				ButtonManager._buttonPressed = true;
				FreshPlayButton();
			}
		}
	}
}
