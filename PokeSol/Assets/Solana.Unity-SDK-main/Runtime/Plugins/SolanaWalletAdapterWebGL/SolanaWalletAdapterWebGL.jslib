mergeInto(LibraryManager.library, {
  InitWalletAdapter: async function (callback, rpcClusterPtr) {
    const isXnft = Boolean(
      "xnft" in window &&
        window.xnft != undefined &&
        window.xnft.solana != undefined &&
        window.xnft.solana.publicKey != undefined
    );
    window.rpcCluster = UTF8ToString(rpcClusterPtr);
    // Add UnityWalletAdapter from CDN
    if (window.walletAdapterLib == undefined) {
      var script = document.createElement("script");
      script.src =
        "https://cdn.jsdelivr.net/npm/@magicblock-labs/unity-wallet-adapter@1.2.1";
      document.head.appendChild(script);
      script.onload = function () {
        {{{ makeDynCall('vi', 'callback') }}}(isXnft);
      };
    } else {
      window.walletAdapterLib.refreshWalletAdapters();
      {{{ makeDynCall('vi', 'callback') }}}(isXnft);
    }
  },
  ExternGetWallets: async function (callback) {
    try {
      const wallets = await window.walletAdapterLib.getWallets();
      var bufferSize = lengthBytesUTF8(wallets) + 1;
      var walletsPtr = _malloc(bufferSize);
      stringToUTF8(wallets, walletsPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(walletsPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternConnectWallet: async function (walletNamePtr, callback) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var pubKey;
      if (walletName === "XNFT") {
        pubKey = window.xnft.solana.publicKey.toString();
      } else {
        pubKey = await window.walletAdapterLib.connectWallet(walletName);
      }
      if (pubKey == undefined) {
        throw new Error("Unable to connect to: " + walletName);
      }
      var bufferSize = lengthBytesUTF8(pubKey) + 1;
      var pubKeyPtr = _malloc(bufferSize);
      stringToUTF8(pubKey, pubKeyPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(pubKeyPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternSignTransactionWallet: async function (
    walletNamePtr,
    transactionPtr,
    callback
  ) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var base64transaction = UTF8ToString(transactionPtr);
      let signedTransaction;
      if (walletName === "XNFT") {
        const transaction =
          window.walletAdapterLib.getTransactionFromStr(base64transaction);
        signedTransaction = await window.xnft.solana.signTransaction(
          transaction
        );
      } else {
        signedTransaction = await window.walletAdapterLib.signTransaction(
          walletName,
          base64transaction
        );
      }
      let txStr = Buffer.from(signedTransaction.serialize()).toString("base64");
      var bufferSize = lengthBytesUTF8(txStr) + 1;
      var txPtr = _malloc(bufferSize);
      stringToUTF8(txStr, txPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(txPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternSignMessageWallet: async function (
    walletNamePtr,
    messagePtr,
    callback
  ) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var base64Message = UTF8ToString(messagePtr);
      let signatureStr;
      if (walletName === "XNFT") {
        const messageBytes = Uint8Array.from(atob(base64Message), (c) =>
          c.charCodeAt(0)
        );
        var signedMessage = await window.xnft.solana.signMessage(
          messageBytes
        );
        if (typeof signedMessage === 'object' && signedMessage !== null && 'signature' in signedMessage) {
            signedMessage = signedMessage.signature;
        }
        signatureStr = btoa(String.fromCharCode(...signedMessage));
      } else {
        var signature = await window.walletAdapterLib.signMessage(
          walletName,
          atob(base64Message)
        );
        if(signature instanceof Uint8Array) {
          signatureStr = btoa(String.fromCharCode(...signature));
        } else {
          signatureStr = signature.toString("base64");
        }
      }
      var bufferSize = lengthBytesUTF8(signatureStr) + 1;
      var signaturePtr = _malloc(bufferSize);
      stringToUTF8(signatureStr, signaturePtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(signaturePtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
  ExternSignAllTransactionsWallet: async function (
    walletNamePtr,
    transactionsPtr,
    callback
  ) {
    try {
      const walletName = UTF8ToString(walletNamePtr);
      var base64transactionsStr = UTF8ToString(transactionsPtr);
      var base64transactions = base64transactionsStr.split(",");
      let signedTransactions;
      if (walletName === "XNFT") {
        let transactions = [];
        for (var i = 0; i < base64transactions.length; i++) {
          const transaction = window.walletAdapterLib.getTransactionFromStr(
            base64transactions[i]
          );
          transactions.push(transaction);
        }
        signedTransactions = await window.xnft.solana.signAllTransactions(
          transactions
        );
      } else {
        signedTransactions = await window.walletAdapterLib.signAllTransactions(
          walletName,
          base64transactions
        );
      }
      var serializedSignedTransactions = [];
      for (var i = 0; i < signedTransactions.length; i++) {
        var signedTransaction = signedTransactions[i];
        var txStr = Buffer.from(signedTransaction.serialize()).toString("base64");
        serializedSignedTransactions.push(txStr);
      }
      var txsStr = serializedSignedTransactions.join(",");
      var bufferSize = lengthBytesUTF8(txsStr) + 1;
      var txsPtr = _malloc(bufferSize);
      stringToUTF8(txsStr, txsPtr, bufferSize);
      {{{ makeDynCall('vi', 'callback') }}}(txsPtr);
    } catch (err) {
      console.error(err.message);
      {{{ makeDynCall('vi', 'callback') }}}(null);
    }
  },
});
