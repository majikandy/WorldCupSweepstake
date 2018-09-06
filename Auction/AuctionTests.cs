using System;
using FluentAssertions;
using FluentAssertions.Execution;
using NSubstitute;
using Stratis.SmartContracts;
using WorldCupSweepstake.Tests.TestTools;
using Xunit;

namespace AuctionSample
{
    public class AuctionTests
    {
        private static readonly Address ContractOwnerAddress = (Address) "contract_owner_address";
        private static readonly Address BidderOne = (Address) "bidder_one_address";
        private static readonly Address BidderTwo = (Address) "bidder_two_address";

        private readonly TestSmartContractState smartContractState;
        private readonly IInternalTransactionExecutor transactionExecutor;
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
                GasLimit = (Gas)GasLimit,
                Sender = ContractOwnerAddress,
                Value = Value
            };

            this.transactionExecutor = Substitute.For<IInternalTransactionExecutor>();

            this.smartContractState = new TestSmartContractState(
                block,
                message,
                new InMemoryState(),
                null,
                transactionExecutor,
                () => Balance,
                new TestInternalHashHelper()
            );
        }

        [Fact]
        public void Auction_for_20_blocks_opens_for_owner_and_stores_end_block()
        {
            const ulong duration = 20;
            var contract = new Auction(smartContractState, duration);

            AssertionExtensions.Should((object) contract.Owner).Be(ContractOwnerAddress);
            AssertionExtensions.Should((bool) contract.HasEnded).BeFalse();
            AssertionExtensions.Should((ulong) contract.EndBlock).Be(duration + smartContractState.Block.Number);
        }

        [Fact]
        public void First_bid_becomes_highest_bid()
        {
            var contract = new Auction(smartContractState, 20);

            AssertionExtensions.Should((object) contract.HighestBidder).Be(default(Address));
            AssertionExtensions.Should((ulong) contract.HighestBid).Be(0uL);

            var message = ((TestMessage) smartContractState.Message);

            message.Value = 100;
            message.Sender = BidderOne;
            contract.Bid();

            AssertionExtensions.Should((object) contract.HighestBidder).Be(BidderOne);
            AssertionExtensions.Should((ulong) contract.HighestBid).Be(100ul);
        }

        [Fact]
        public void Second_bidder_higher_than_first_becomes_highest_bid()
        {
            var contract = new Auction(smartContractState, 20);

            var message = ((TestMessage) smartContractState.Message);

            message.Value = 100;
            message.Sender = BidderOne;
            contract.Bid();

            message.Value = 200;
            message.Sender = BidderTwo;
            contract.Bid();

            Assert.Equal<Address>(BidderTwo, contract.HighestBidder);
            Assert.Equal(200uL, smartContractState.PersistentState.GetUInt64("HighestBid"));
        }

        [Fact]
        public void EndAuction_attempt_before_end_block_fails()
        {
            var contract = new Auction(smartContractState, 20);

            var exception = Assert.Throws<SmartContractAssertException>(() => contract.AuctionEnd());
            Assert.Equal("Assert failed.", exception.Message);
        }

        [Fact]
        public void Bid_after_end_block_fails()
        {
            var contract = new Auction(smartContractState, 0);

            var message = ((TestMessage) smartContractState.Message);

            message.Value = 200;
            message.Sender = BidderTwo;

            var exception = Assert.Throws<SmartContractAssertException>(() => contract.Bid());
            Assert.Equal("Assert failed.", exception.Message);
        }

        [Fact]
        public void EndAuction_attempt_after_end_block_succeeds()
        {
            var contract = new Auction(smartContractState, 1);

            var message = ((TestMessage) smartContractState.Message);

            message.Value = 200;
            message.Sender = BidderOne;
            contract.Bid();

            smartContractState.Block.GetType().GetProperty("Number")
                .SetValue(smartContractState.Block, contract.EndBlock);

            contract.AuctionEnd();
            Assert.True((bool) contract.HasEnded);
            this.transactionExecutor.Received().TransferFunds(smartContractState, ContractOwnerAddress, 200ul, null);
        }

        [Fact]
        public void Bidder_can_withdraw_their_bid_but_currently_at_least_2_bids_before_they_can()
        {
            var contract = new Auction(smartContractState, 1);

            var message = ((TestMessage) smartContractState.Message);

            message.Value = 200;
            message.Sender = BidderOne;
            contract.Bid();
            message.Value = 300;
            contract.Bid();

            contract.Withdraw();
        }

        [Fact]
        public void Bidder_cant_withdraw_bid_if_only_one_bid()
        {
            var contract = new Auction(smartContractState, 1);
            var message = ((TestMessage)smartContractState.Message);
            message.Value = 200;
            message.Sender = BidderOne;
            contract.Bid();

            Action withdraw = () => contract.Withdraw();

            withdraw.Should().Throw<SmartContractAssertException>()
                .WithMessage("Assert failed.");
        }

        [Fact]
        public void Bidder_can_outbid_themselves()
        {
            var contract = new Auction(smartContractState, 1);

            var message = ((TestMessage)smartContractState.Message);

            message.Value = 200;
            message.Sender = BidderOne;
            contract.Bid();
            message.Value = 250;
            contract.Bid();
            message.Value = 300;
            contract.Bid();

            AssertionExtensions.Should((object) contract.HighestBidder).Be(BidderOne);
            AssertionExtensions.Should((ulong) contract.HighestBid).Be(300ul);
        }

        [Fact]
        public void Bidder_cant_bid_same_amount()
        {
            var contract = new Auction(smartContractState, 1);

            var message = ((TestMessage)smartContractState.Message);

            message.Value = 200;
            message.Sender = BidderOne;
            contract.Bid();
            message.Value = 200;
            Action bid = () => contract.Bid();

            bid.Should().Throw<SmartContractAssertException>()
                .WithMessage("Assert failed.");
        }

        [Fact]
        public void Bidder_cant_bid_lower_amount()
        {
            var contract = new Auction(smartContractState, 1);
            
            var message = ((TestMessage)smartContractState.Message);

            message.Value = 200;
            message.Sender = BidderOne;
            contract.Bid();
            message.Value = 100;
            Action bid = () => contract.Bid();

            bid.Should().Throw<SmartContractAssertException>()
                .WithMessage("Assert failed.");
        }
    }
}
