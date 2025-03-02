using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using PkmngoBackendTesting;

public class GameManager : MonoBehaviour
{
    private PkmngoBackendTestingClient client;
    private PublicKey programId = new PublicKey("pkm3zzV6AqQoZDaev9gciaiE4R3CDxE8LsrrzBFnfGB");
    private PublicKey playerPublicKey;
    private Account walletAccount;

    [SerializeField] private Button catchButton;
    [SerializeField] private Button connectWalletButton;

    private bool isWalletConnected = true;

    // HARDCODED PRIVATE KEY (USE WITH CAUTION)
    private Wallet wallet;
    private byte[] privateKeyBytes = new byte[]
    {
        206,157,165,241,160,13,140,97,62,31,78,
        246,157,9,56,103,7,210,9,84,28,123,190,30,86,181,185,116,178,6,100,116,13,
        139,181,176,161,195,173,40,149,144,132,201,119,217,238,204,57,198,155,69,37,
        233,86,193,102,156,67,99,244,146,118,49
    };

    // HARDCODED PUBLIC KEY
    private const string PUBLIC_KEY = "usrtFG8kbXNxQebcqm4fEW7hmDt2YTAdTF1VtZsfBXn";

    void Start()
    {
        IRpcClient rpcClient = ClientFactory.GetClient(Cluster.DevNet);
        IStreamingRpcClient streamingRpcClient = ClientFactory.GetStreamingClient(Cluster.DevNet);
        client = new PkmngoBackendTestingClient(rpcClient, streamingRpcClient, programId);

        if (catchButton != null) catchButton.onClick.AddListener(OnCatchPokemonButton);
        if (connectWalletButton != null) connectWalletButton.onClick.AddListener(() => StartCoroutine(ConnectWalletRoutine()));
    }

    private IEnumerator ConnectWalletRoutine()
    {
        ConnectWallet();
        yield return new WaitForSeconds(1);

        if (isWalletConnected)
        {
            yield return FetchGameData();
            yield return FetchPlayerData();
            yield return SubscribeToPlayerData();
        }
    }

    private void ConnectWallet()
    {
        try
        {
            // Use the hardcoded public key
            playerPublicKey = new PublicKey(PUBLIC_KEY);

            // Initialize the wallet with the hardcoded private key
            PrivateKey privateKey;
            try
            {
                privateKey = new PrivateKey(privateKeyBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize private key: {ex.Message}");
                return;
            }

            // Create a Wallet using the private key bytes directly
            wallet = new Wallet(privateKeyBytes);
            walletAccount = wallet.Account; // Get the Account from the Wallet
            Debug.Log($"Connected wallet: {playerPublicKey}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Wallet connection failed: {ex.Message}");
            isWalletConnected = false;
        }
    }

    public PublicKey GetPlayerDataAddress(PublicKey playerKey)
    {
        var seeds = new List<byte[]> { System.Text.Encoding.UTF8.GetBytes("player_data"), playerKey.KeyBytes };
        PublicKey.TryFindProgramAddress(seeds, programId, out PublicKey pda, out _);
        return pda;
    }

    public PublicKey GetGameDataAddress()
    {
        var seeds = new List<byte[]> { System.Text.Encoding.UTF8.GetBytes("game_data") };
        PublicKey.TryFindProgramAddress(seeds, programId, out PublicKey pda, out _);
        return pda;
    }

    public async Task FetchGameData()
    {
        try
        {
            var gameDataAddress = GetGameDataAddress();
            var result = await client.GetGameDataAsync(gameDataAddress.ToString());
            if (result.WasSuccessful && result.ParsedResult != null)
            {
                GameData gameData = result.ParsedResult;
                Debug.Log($"Total Wood: {gameData.TotalWoodCollected}, Pokémon: {gameData.TotalPokemonInWorld}");
            }
            else
            {
                Debug.LogError("Failed to fetch game data");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"FetchGameData error: {ex.Message}");
        }
    }

    public async Task FetchPlayerData()
    {
        if (!isWalletConnected) return;

        try
        {
            var playerDataAddress = GetPlayerDataAddress(playerPublicKey);
            var result = await client.GetPlayerDataAsync(playerDataAddress.ToString());
            if (result.WasSuccessful && result.ParsedResult != null)
            {
                PlayerData playerData = result.ParsedResult;
                Debug.Log($"Player: {playerData.Name}, Level: {playerData.Level}, Wood: {playerData.Wood}");
            }
            else
            {
                Debug.LogError("Failed to fetch player data");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"FetchPlayerData error: {ex.Message}");
        }
    }

    public async Task SubscribeToPlayerData()
    {
        if (!isWalletConnected) return;

        try
        {
            var playerDataAddress = GetPlayerDataAddress(playerPublicKey);
            await client.SubscribePlayerDataAsync(playerDataAddress.ToString(), (state, response, playerData) =>
            {
                if (playerData != null)
                {
                    Debug.Log($"Player data updated - Energy: {playerData.Energy}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"SubscribeToPlayerData error: {ex.Message}");
        }
    }

    public async Task<bool> CatchPokemon(string levelSeed, ushort counter)
    {
        if (!isWalletConnected)
        {
            Debug.LogError("Wallet not connected");
            return false;
        }

        try
        {
            var playerDataAddress = GetPlayerDataAddress(playerPublicKey);
            var gameDataAddress = GetGameDataAddress();
            var gymBossAccount = new PublicKey("11111111111111111111111111111111111111111111111"); // Placeholder

            var accounts = new CatchPokemonAccounts
            {
                SessionToken = null,
                Player = playerDataAddress,
                GameData = gameDataAddress,
                Signer = playerPublicKey,
                GymBossAccount = gymBossAccount
            };

            var instruction = PkmngoBackendTestingProgram.CatchPokemon(accounts, levelSeed, counter);

            var latestBlockhash = await client.RpcClient.GetLatestBlockHashAsync();
            if (latestBlockhash == null || latestBlockhash.Result == null)
            {
                Debug.LogError("Failed to retrieve latest blockhash from Solana.");
                return false;
            }

            Debug.Log($"Latest Blockhash: {latestBlockhash.Result.Value.Blockhash}");

            var tx = new TransactionBuilder()
                .SetRecentBlockHash(latestBlockhash.Result.Value.Blockhash)
                .AddInstruction(instruction)
                .SetFeePayer(playerPublicKey)
                .Build(walletAccount);

            Debug.Log($"Transaction built successfully. Sending to Solana...");

            var txId = await client.RpcClient.SendTransactionAsync(tx);
            if (!txId.WasSuccessful)
            {
                Debug.LogError($"Transaction failed: {txId.Reason}");
                return false;
            }

            Debug.Log($"Transaction sent successfully. ID: {txId.Result}");

            // Wait for confirmation
            bool confirmed = false;
            for (int i = 0; i < 10; i++)
            {
                var checkStatus = await client.RpcClient.GetSignatureStatusesAsync(new List<string> { txId.Result });
                if (checkStatus.Result.Value[0] != null && checkStatus.Result.Value[0].ConfirmationStatus == "confirmed")
                {
                    confirmed = true;
                    break;
                }
                await Task.Delay(2000);
            }

            if (!confirmed)
            {
                Debug.LogError("Transaction confirmation failed!");
                return false;
            }

            Debug.Log("Pokémon caught successfully! Transaction confirmed!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"CatchPokemon error: {ex.Message}");
            return false;
        }
    }

    public void OnCatchPokemonButton()
    {
        if (!isWalletConnected || playerPublicKey == null)
        {
            Debug.LogError("Please connect wallet first");
            return;
        }

        StartCoroutine(CatchPokemonRoutine("forest_level", 1));
    }

    private IEnumerator CatchPokemonRoutine(string levelSeed, ushort counter)
    {
        if (catchButton != null)
            catchButton.interactable = false;

        Task<bool> catchTask = CatchPokemon(levelSeed, counter);
        yield return new WaitUntil(() => catchTask.IsCompleted);

        Debug.Log(catchTask.Result ? "Pokémon caught!" : "Failed to catch Pokémon");

        if (catchButton != null)
            catchButton.interactable = true;
    }

    void OnDestroy()
    {
        if (catchButton != null) catchButton.onClick.RemoveListener(OnCatchPokemonButton);
        if (connectWalletButton != null) connectWalletButton.onClick.RemoveAllListeners();
    }
}