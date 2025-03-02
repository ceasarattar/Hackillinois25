import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { PkmngoBackendTesting } from "../target/types/pkmngo_backend_testing";
import { Keypair } from "@solana/web3.js";

describe("pkmngo_backend_testing", () => {
  const provider = anchor.AnchorProvider.env();
  anchor.setProvider(provider);
  const program = anchor.workspace.PkmngoBackendTesting as Program<PkmngoBackendTesting>;
  const payer = provider.wallet as anchor.Wallet;
  const gameDataSeed = "gameData";


  // og account key bytes: 
  // [19,181,89,188,154,7,42,176,93,212,212,90,255,181,79,221,218,119,67,159,231,103,245,210,181,236,201,41,227,225,54,146,238,45,24,196,140,9,0,16,48,156,202,24,26,233,62,177,65,13,145,63,36,196,154,22,207,154,129,75,78,57,161,164]

  // new account key bytes: usrtFG8kbXNxQebcqm4fEW7hmDt2YTAdTF1VtZsfBXn
  const keypairBytes = Uint8Array.from([206,157,165,241,160,13,140,97,62,31,78,
    246,157,9,56,103,7,210,9,84,28,123,190,30,86,181,185,116,178,6,100,116,13,
    139,181,176,161,195,173,40,149,144,132,201,119,217,238,204,57,198,155,69,37,
    233,86,193,102,156,67,99,244,146,118,49]);

  const player2 = Keypair.fromSecretKey(keypairBytes);

  it("Init player and chop tree!", async () => {
    console.log("Local address", payer.publicKey.toBase58());

    const balance = await anchor
      .getProvider()
      .connection.getBalance(payer.publicKey);

    if (balance < 1e8) {
      const res = await anchor
        .getProvider()
        .connection.requestAirdrop(payer.publicKey, 1e9);
      await anchor
        .getProvider()
        .connection.confirmTransaction(res, "confirmed");
    }

    const [playerPDA] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("player"), payer.publicKey.toBuffer()],
      program.programId
    );

    console.log("Player PDA", playerPDA.toBase58());

    const [player2PDA] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("player"), player2.publicKey.toBuffer()],
      program.programId
    );

    console.log("Player 2 PDA", player2PDA.toBase58());

    const [gameDataPDA] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from(gameDataSeed)],
      program.programId
    );

    try {
      console.log("test log asddfasdf");
      let tx = await program.methods
        .initPlayer(gameDataSeed)
        .accountsStrict({
          player: playerPDA,
          signer: payer.publicKey,
          gameData: gameDataPDA,
          systemProgram: anchor.web3.SystemProgram.programId,
        })
        .rpc({ skipPreflight: true });

        /*
          .transaction()
        */

      console.log("Init transaction", tx);

      await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");
      console.log("Confirmed", tx);
    } catch (e) {
      console.log("Player already exists: ", e);
    }

    // resetting game
    let tx = await program.methods
        .resetGame(gameDataSeed)
        .accountsStrict({
          player: playerPDA,
          signer: payer.publicKey,
          gameData: gameDataPDA,
          systemProgram: anchor.web3.SystemProgram.programId,
          sessionToken: null,
        })
        .rpc({ skipPreflight: false});
      console.log("Game reset", tx);

    try {
      let tx = await program.methods
        .resetPlayer(gameDataSeed)
        .accountsStrict({
          player: playerPDA,
          signer: payer.publicKey,
          gameData: gameDataPDA,
          systemProgram: anchor.web3.SystemProgram.programId,
          sessionToken: null,
        })
        .rpc({ skipPreflight: false});
      console.log("Player reset", tx);

      await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");
      console.log("Confirmed", tx);
    } catch (e) {
      console.log("idk smth ", e);
    }

    // for (let i = 0; i < 11; i++) {
    //   console.log(`Chop instruction ${i}`);

    //   let tx = await program.methods
    //     .chopTree(gameDataSeed, 0)
    //     .accountsStrict({
    //       player: playerPDA,
    //       sessionToken: null,
    //       signer: payer.publicKey,
    //       gameData: gameDataPDA,
    //       systemProgram: anchor.web3.SystemProgram.programId,
    //     })
    //     .rpc();
    //   console.log("Chop instruction", tx);
    //   await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");
    // }

    // Fetch game data to get the current gym boss
  let gameData = await program.account.gameData.fetch(gameDataPDA);
  let gymBossPublicKey = gameData.pokeGym.gymBoss;

  for (let i = 0; i < 11; i++) {
    console.log(`Catch Pokemon instruction ${i}`);

    let tx = await program.methods
      .catchPokemon(gameDataSeed, 0)
      .accountsStrict({
        player: playerPDA,
        sessionToken: null,
        signer: payer.publicKey,
        gameData: gameDataPDA,
        gymBossAccount: gymBossPublicKey,  // Provide the gym boss account
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .rpc();
    console.log("Catch pokemon instruction", tx);
    await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");
  }

    // challenging gym
    tx = await program.methods
      .challengeGym(gameDataSeed, 0)
      .accountsStrict({
        player: playerPDA,
        sessionToken: null,
        signer: payer.publicKey,
        gameData: gameDataPDA,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .rpc();
    console.log("Challenge gym instruction", tx);
    await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");

    // SECOND PLAYER PHASE:

    try {
      console.log("test log asddfasdf");
      let transaction = await program.methods
        .initPlayer(gameDataSeed)
        .accountsStrict({
          player: player2PDA,
          signer: player2.publicKey,
          gameData: gameDataPDA,
          systemProgram: anchor.web3.SystemProgram.programId,
        })
        .transaction();

        transaction.recentBlockhash = (await provider.connection.getLatestBlockhash()).blockhash;
  
        transaction.feePayer = player2.publicKey;
      
        transaction.sign(player2);
      
        const txHash = await provider.connection.sendRawTransaction(transaction.serialize());

      console.log("Init transaction", transaction);

      await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");
      console.log("Confirmed", tx);
    } catch (e) {
      console.log("Player already exists: ", e);
    }

    try {
      let transaction = await program.methods
        .resetPlayer(gameDataSeed)
        .accountsStrict({
          player: player2PDA,
          signer: player2.publicKey,
          gameData: gameDataPDA,
          systemProgram: anchor.web3.SystemProgram.programId,
          sessionToken: null,
        })
        .transaction();

        transaction.recentBlockhash = (await provider.connection.getLatestBlockhash()).blockhash;
  
        transaction.feePayer = player2.publicKey;
      
        transaction.sign(player2);
      
        const txHash = await provider.connection.sendRawTransaction(transaction.serialize());

      console.log("Player reset", txHash);

      await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");
      console.log("Confirmed", transaction);
    } catch (e) {
      console.log("idk smth ", e);
    }

    // Fetch game data to get the current gym boss
    gameData = await program.account.gameData.fetch(gameDataPDA);
    gymBossPublicKey = gameData.pokeGym.gymBoss;

    for (let i = 0; i < 11; i++) {
      console.log(`Catch Pokemon instruction ${i}`);

      let transaction = await program.methods
        .catchPokemon(gameDataSeed, 0)
        .accountsStrict({
          player: player2PDA,
          sessionToken: null,
          signer: player2.publicKey,
          gameData: gameDataPDA,
          gymBossAccount: gymBossPublicKey,  // Provide the gym boss account
          systemProgram: anchor.web3.SystemProgram.programId,
        })
        .transaction();

        transaction.recentBlockhash = (await provider.connection.getLatestBlockhash()).blockhash;
  
        transaction.feePayer = player2.publicKey;
      
        transaction.sign(player2);
      
        const txHash = await provider.connection.sendRawTransaction(transaction.serialize());
      console.log("Catch pokemon instruction", txHash);
      await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");
    }

    let transaction = await program.methods
      .challengeGym(gameDataSeed, 0)
      .accountsStrict({
        player: player2PDA,
        sessionToken: null,
        signer: player2.publicKey,
        gameData: gameDataPDA,
        systemProgram: anchor.web3.SystemProgram.programId,
      })
      .transaction();

        transaction.recentBlockhash = (await provider.connection.getLatestBlockhash()).blockhash;
  
        transaction.feePayer = player2.publicKey;
      
        transaction.sign(player2);
      
        const txHash = await provider.connection.sendRawTransaction(transaction.serialize());
    console.log("Challenge gym instruction", txHash);
    await anchor.getProvider().connection.confirmTransaction(tx, "confirmed");

    const accountInfo = await anchor
      .getProvider()
      .connection.getAccountInfo(playerPDA, "confirmed");

    const decoded = program.coder.accounts.decode(
      "playerData",
      accountInfo.data
    );
    console.log("Player account info", JSON.stringify(decoded));
  });

  
});
