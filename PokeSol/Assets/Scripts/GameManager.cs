using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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
    private WalletBase wallet;

    [SerializeField, Tooltip("The UI button to catch Pokémon")]
    private UnityEngine.UI.Button catchButton;

    [SerializeField, Tooltip("The UI button to connect the wallet")]
    private UnityEngine.UI.Button connectWalletButton;

    private bool isWalletConnected = false;

    void Start()
    {
        IRpcClient rpcClient = ClientFactory.GetClient(Cluster.DevNet);
        IStreamingRpcClient streamingRpcClient = ClientFactory.GetStreamingClient(Cluster.DevNet);

        client = new PkmngoBackendTestingClient(rpcClient, streamingRpcClient, programId);

        // Setup button listeners
        if (catchButton != null)
            catchButton.onClick.AddListener(OnCatchPokemonButton);
        if (connectWalletButton != null)
            connectWalletButton.onClick.AddListener(() => StartCoroutine(ConnectWalletRoutine()));
    }

    private IEnumerator ConnectWalletRoutine()
    {
        Task connectTask = ConnectWallet();
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (isWalletConnected)
        {
            Task fetchGameDataTask = FetchGameData();
            yield return new WaitUntil(() => fetchGameDataTask.IsCompleted);

            Task fetchPlayerDataTask = FetchPlayerData();
            yield return new WaitUntil(() => fetchPlayerDataTask.IsCompleted);

            Task subscribeTask = SubscribeToPlayerData();
            yield return new WaitUntil(() => subscribeTask.IsCompleted);
        }
    }

    private async Task ConnectWallet()
    {
        try
        {
            // This is a simplified wallet connection - in production, use a proper wallet adapter
            wallet = new Wallet(new byte[32]); // Replace with actual wallet implementation
            playerPublicKey = wallet.Account.PublicKey;
            isWalletConnected = true;
            Debug.Log($"Connected wallet: {playerPublicKey}");

            // Disable connect button after successful connection
            if (connectWalletButton != null)
                connectWalletButton.interactable = false;
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
            if (result.WasSuccessful)
            {
                GameData gameData = result.Result;
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
            if (result.WasSuccessful)
            {
                PlayerData playerData = result.Result;
                Debug.Log($"Player: {playerData.Name}, Level: {playerData.Level}, Wood: {playerData.Wood}");
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
            var gymBossAccount = new PublicKey("11111111111111111111111111111111"); // Replace with actual logic

            var accounts = new CatchPokemonAccounts
            {
                SessionToken = null,
                Player = playerDataAddress,
                GameData = gameDataAddress,
                Signer = playerPublicKey,
                GymBossAccount = gymBossAccount
            };

            var instruction = PkmngoBackendTestingProgram.CatchPokemon(accounts, levelSeed, counter);
            var blockhash = await client.RpcClient.GetLatestBlockhashAsync();

            var tx = new TransactionBuilder()
                .SetRecentBlockHash(blockhash.Result.Value.Blockhash)
                .AddInstruction(instruction)
                .Build(wallet.Account); // Pass the signing account

            var signedTx = wallet.SignTransaction(tx);
            var txId = await client.RpcClient.SendTransactionAsync(signedTx);
            var confirmation = await client.RpcClient.ConfirmTransactionAsync(txId.Result);

            if (!confirmation.WasSuccessful)
            {
                var error = client.GetErrorForInstruction(txId.Result);
                Debug.LogError($"Transaction failed: {error?.Message}");
            }

            return confirmation.WasSuccessful;
        }
        catch (Exception ex)
        {
            Debug.LogError($"CatchPokemon error: {ex.Message}");
            return false;
        }
    }

    public void OnCatchPokemonButton()
    {
        if (!isWalletConnected)
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

        bool result = false;
        Task<bool> catchTask = CatchPokemon(levelSeed, counter);
        yield return new WaitUntil(() => catchTask.IsCompleted);
        result = catchTask.Result;

        Debug.Log(result ? "Pokémon caught!" : "Failed to catch Pokémon");

        if (catchButton != null)
            catchButton.interactable = true;

        yield return null;
    }

    void OnDestroy()
    {
        if (catchButton != null)
            catchButton.onClick.RemoveListener(OnCatchPokemonButton);
        if (connectWalletButton != null)
            connectWalletButton.onClick.RemoveAllListeners();
    }
}