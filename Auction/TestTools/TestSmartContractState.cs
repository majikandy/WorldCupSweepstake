using System;
using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class TestSmartContractState : ISmartContractState
    {
        public IBlock Block { get; }
        public IMessage Message { get; }
        public IPersistentState PersistentState { get; }
        public IGasMeter GasMeter { get; }
        public IInternalTransactionExecutor InternalTransactionExecutor { get; }
        public Func<ulong> GetBalance { get; }
        public IInternalHashHelper InternalHashHelper { get; }

        public TestSmartContractState(
            IBlock block,
            IMessage message,
            IPersistentState persistentState,
            IGasMeter gasMeter,
            IInternalTransactionExecutor transactionExecutor,
            Func<ulong> getBalance,
            IInternalHashHelper hashHelper)
        {
            this.Block = block;
            this.Message = message;
            this.PersistentState = persistentState;
            this.GasMeter = gasMeter;
            this.InternalTransactionExecutor = transactionExecutor;
            this.GetBalance = getBalance;
            this.InternalHashHelper = hashHelper;
        }
    }
}