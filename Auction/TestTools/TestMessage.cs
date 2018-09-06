using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class TestMessage : IMessage
    {
        public Address ContractAddress { get; set; }
        public Address Sender { get; set; }
        public Gas GasLimit { get; set; }
        public ulong Value { get; set; }
    }
}