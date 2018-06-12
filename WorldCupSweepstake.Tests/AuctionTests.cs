using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
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
        private IInternalTransactionExecutor transactionExecutor;
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

            IGasMeter gasMeter = null;
            this.transactionExecutor = Substitute.For<IInternalTransactionExecutor>();

            this.smartContractState = new TestSmartContractState(
                block,
                message,
                persistentState,
                gasMeter,
                transactionExecutor,
                getBalance,
                internalHashHelper
            );
        }

        [Fact]
        public void Auction_for_20_blocks_opens_for_owner_and_stores_end_block()
        {
            const ulong duration = 20;
            var contract = new Auction(smartContractState, duration);

            contract.Owner.Should().Be(ContractOwnerAddress);
            contract.HasEnded.Should().BeFalse();
            contract.EndBlock.Should().Be(duration + smartContractState.Block.Number);
        }

        [Fact]
        public void First_bid_becomes_highest_bid()
        {
            var contract = new Auction(smartContractState, 20);

            contract.HighestBidder.Should().Be(default(Address));
            contract.HighestBid.Should().Be(0uL);

            var message = ((TestMessage) smartContractState.Message);

            message.Value = 100;
            message.Sender = BidderOne;
            contract.Bid();

            contract.HighestBidder.Should().Be(BidderOne);
            contract.HighestBid.Should().Be(100ul);
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

            Assert.Equal(BidderTwo, contract.HighestBidder);
            Assert.Equal(200uL, smartContractState.PersistentState.GetUInt64("HighestBid"));
        }

        [Fact]
        public void EndAuction_attempt_before_end_block_fails()
        {
            var contract = new Auction(smartContractState, 20);

            var exception = Assert.Throws<Exception>(() => contract.AuctionEnd());
            Assert.Equal("Condition inside 'Assert' call was false.", exception.Message);
        }

        [Fact]
        public void Bid_after_end_block_fails()
        {
            var contract = new Auction(smartContractState, 0);

            var message = ((TestMessage) smartContractState.Message);

            message.Value = 200;
            message.Sender = BidderTwo;

            var exception = Assert.Throws<Exception>(() => contract.Bid());
            Assert.Equal("Condition inside 'Assert' call was false.", exception.Message);
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
            Assert.True(contract.HasEnded);
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

            withdraw.Should().Throw<Exception>()
                .WithMessage("Condition inside 'Assert' call was false.");
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

            contract.HighestBidder.Should().Be(BidderOne);
            contract.HighestBid.Should().Be(300ul);
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

            bid.Should().Throw<Exception>()
                .WithMessage("Condition inside 'Assert' call was false.");
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

            bid.Should().Throw<Exception>()
                .WithMessage("Condition inside 'Assert' call was false.");
        }
    }
}
