using System;
using System.Threading.Tasks;
using Nethereum.Geth;
using Nethereum.Web3.Accounts;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualBasic.CompilerServices;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using Nethereum.Web3.Transactions;

namespace AccountTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            new TransferDemoService().TransferDemo().Wait();
        }
    }

    public class TransferDemoService
    {
        public async Task TransferDemo()
        {
            var address = "0x12890d2cce102216644c59daE5baed380d84830c";
            var defaultPassword = "password";
            var web3 = new Web3Geth();
            var accountBalance = await web3.Eth.GetBalance.SendRequestAsync(address);
            var accounts = new List<string>();
            await web3.Personal.UnlockAccount.SendRequestAsync(address, defaultPassword, 60000);
           
            for (var i = 1; i < 10; i++)
            {
                var newAccount = await web3.Personal.NewAccount.SendRequestAsync(defaultPassword);
                accounts.Add(newAccount);
            }

            await web3.Miner.Start.SendRequestAsync(6);
            var initialBalance = await web3.Eth.GetBalance.SendRequestAsync(address);
            //2 Ether
            var transactionReceiptsInitial = await TransferEqualAmounts(web3, address, web3.Convert.ToWei(2), accounts.ToArray());

            var mainAccountBalance = await web3.Eth.GetBalance.SendRequestAsync(address);
           
            var account1Balance = await web3.Eth.GetBalance.SendRequestAsync(accounts[0]);

            await web3.Personal.UnlockAccount.SendRequestAsync(accounts[0], defaultPassword, 60000);
            var transactionReceipts2 = await TransferEqualAmounts(web3, accounts[0], web3.Convert.ToWei(0.01), accounts.Where( x => x != accounts[0]).ToArray());
            var account2Balance = await web3.Eth.GetBalance.SendRequestAsync(accounts[1]);

            await web3.Personal.UnlockAccount.SendRequestAsync(accounts[1], defaultPassword, 60000);
            var transactionReceipts3 = await TransferEqualAmounts(web3, accounts[1], web3.Convert.ToWei(0.001), accounts.Where(x => x != accounts[1]).ToArray());
            var account3Balance = await web3.Eth.GetBalance.SendRequestAsync(accounts[1]);

            await web3.Miner.Stop.SendRequestAsync();
        }

        public async Task<List<TransactionReceipt>> TransferEqualAmounts(Web3 web3, string from, BigInteger amount,
            params string[] toAdresses)
        {
            var transfers = new List<Func<Task<string>>>();

          
            foreach (var to in toAdresses)
            {
                transfers.Add(() =>
                    web3.Eth.TransactionManager.SendTransactionAsync(new TransactionInput()
                    {
                        From = from,
                        To = to,
                        Value = new HexBigInteger(amount),
                    }));
            }
            var pollingService = new TransactionReceiptPollingService(web3);
            return await pollingService.SendRequestAsync(transfers);
        }

    }
}