using System;
using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class TestTransferResult : ITransferResult
    {
        public TestTransferResult(bool succeed)
        {
            this.Success = succeed;
        }

        public object ReturnValue { get; }
        public Exception ThrownException { get; }
        public bool Success { get; }
    }
}