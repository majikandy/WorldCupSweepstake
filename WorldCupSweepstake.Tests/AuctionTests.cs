using System;
using Stratis.SmartContracts;
using WorldCupSweepstake.Tests.TestTools;
using Xunit;

namespace WorldCupSweepstake.Tests
{
    public class AuctionTests
    {
        private static readonly Address ContractOwnerAddress = (Address) "contract_owner_address";
        private static readonly Address BidderOne = (Address) "bidder_one_address";
        private static readonly Address BidderTwo = (Address) "bidder_two_address";

        private readonly TestSmartContractState smartContractState;
        private const ulong Balance = 0;
        private const ulong GasLimit = 10000;
        private const ulong Value = 0;

        public AuctionTests()
        {
            var block = new TestBlock
            {
                Coinbase = ContractOwnerAddress,
                Number = 1
            };

            var message = new TestMessage
            {
                ContractAddress = ContractOwnerAddress,
                GasLimit = (Gas) GasLimit,
                Sender = ContractOwnerAddress,
                Value = Value
            };

            var getBalance = new Func<ulong>(() => Balance);
            var persistentState = new InMemoryState();
            var internalHashHelper = new TestInternalHashHelper();

            this.smartContractState = new TestSmartContractState(
                block,
                message,
                persistentState,
                null,
                null,
                getBalance,
                internalHashHelper
            );
        }

        [Fact]
        public void Auction_for_20_blocks_opens_for_owner_and_stores_end_block()
        {
            const ulong duration = 20;
            var contract = new Auction(smartContractState, duration);

            Assert.Equal(ContractOwnerAddress, contract.Owner);
            Assert.False(contract.HasEnded);
            Assert.Equal(duration + smartContractState.Block.Number, contract.EndBlock);
        }

        [Fact]
        public void First_bid_becomes_highest_bid()
        {
            var contract = new Auction(smartContractState, 20);

            Assert.Equal(default(Address), contract.HighestBidder);
            Assert.Equal(0uL, contract.HighestBid);

            ((TestMessage) smartContractState.Message).Value = 100;
            ((TestMessage) smartContractState.Message).Sender = BidderOne;
            contract.Bid();

            Assert.Equal(BidderOne, contract.HighestBidder);
            Assert.Equal(100uL, contract.HighestBid);
        }

        [Fact]
        public void Second_bidder_higher_than_first_becomes_highest_bid()
        {
            var contract = new Auction(smartContractState, 20);

            ((TestMessage) smartContractState.Message).Value = 100;
            ((TestMessage) smartContractState.Message).Sender = BidderOne;
            contract.Bid();

            ((TestMessage) smartContractState.Message).Value = 200;
            ((TestMessage) smartContractState.Message).Sender = BidderTwo;
            contract.Bid();

            Assert.Equal(BidderTwo, contract.HighestBidder);
            Assert.Equal(200uL, smartContractState.PersistentState.GetUInt64("HighestBid"));
        }

        [Fact]
        public void EndAuction_attempt_before_end_block_fails()
        {
            var contract = new Auction(smartContractState, 20);

            var exception = Assert.Throws<Exception>(() => contract.AuctionEnd());
            Assert.Equal("Condition inside 'Assert' call was false.", exception.Message);
            //Would be good if the exception could be defined and/or the message inside the exception
        }

        [Fact]
        public void Bid_after_end_block_fails()
        {
            var contract = new FundsTransferCapturingAuction(smartContractState, 0);

            ((TestMessage) smartContractState.Message).Value = 200;
            ((TestMessage) smartContractState.Message).Sender = BidderTwo;

            var exception = Assert.Throws<Exception>(() => contract.Bid());
            Assert.Equal("Condition inside 'Assert' call was false.", exception.Message);
            //Would be good if the exception could be defined and/or the message inside the exception
        }

        [Fact]
        public void EndAuction_attempt_after_end_block_succeeds()
        {
            var contract = new FundsTransferCapturingAuction(smartContractState, 1);

            ((TestMessage) smartContractState.Message).Value = 200;
            ((TestMessage) smartContractState.Message).Sender = BidderOne;
            contract.Bid();

            smartContractState.Block.GetType().GetProperty("Number")
                .SetValue(smartContractState.Block, contract.EndBlock);

            contract.SetupTransferFundsToSucceed();
            contract.AuctionEnd();
            Assert.True(contract.HasEnded);
            Assert.Equal(200ul, contract.WinningBidThatTransferFundsCalledWith);
            Assert.Equal(ContractOwnerAddress, contract.OwnerThatTransferFundsCalledWith);
        }

        [Fact]
        public void Bidder_can_withdraw_their_bid()
        {
            var contract = new FundsTransferCapturingAuction(smartContractState, 1);

            ((TestMessage) smartContractState.Message).Value = 200;
            ((TestMessage) smartContractState.Message).Sender = BidderOne;
            contract.Bid();
            ((TestMessage) smartContractState.Message).Value = 300;
            contract.Bid();

            contract.Withdraw();
        }

        //[Fact]
        public void Bidder_cannot_outbid_themselves()
        {
            //currently they can
        }

        //[Fact]
        public void Outbid_bidders_get_their_money_back()
        {
            //currently they don't?
        }
    }

    public class FundsTransferCapturingAuction : Auction
    {
        private bool succeed;

        public FundsTransferCapturingAuction(ISmartContractState smartContractState, ulong durationBlocks) : base(smartContractState, durationBlocks)
        {
        }

        public void SetupTransferFundsToSucceed()
        {
            this.succeed = true;
        }

        public ulong WinningBidThatTransferFundsCalledWith { get; set; }

        public Address OwnerThatTransferFundsCalledWith { get; set; }

        protected override ITransferResult TransferFundsTo(Address owner, ulong winningBid)
        {
            this.OwnerThatTransferFundsCalledWith = owner;
            this.WinningBidThatTransferFundsCalledWith = winningBid;
            return new TestTransferResult(this.succeed);
        }
    }
}
