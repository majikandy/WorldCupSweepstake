using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class TestBlock : IBlock
    {
        public Address Coinbase { get; set; }
        public ulong Number { get; set; }
    }
}