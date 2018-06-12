using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Stratis.SmartContracts;
using WorldCupSweepstake.Tests.TestTools;
using Xunit;

namespace WorldCupSweepstake.Tests
{
    public class SweepStakeTests
    {
        private static readonly Address ContractOwnerAddress = (Address) "contract_owner_address";

        private readonly TestSmartContractState smartContractState;
        private IInternalTransactionExecutor transactionExecutor;
        private const ulong Balance = 0;
        private const ulong GasLimit = 10000;
        private const ulong Value = 0;

        public SweepStakeTests()
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
        public void Create_sweepstake_providing_start_block_list_of_teams_prize_allocation_and_end_date()
        {
            var contract = SetupValidSweepstake();

            contract.Owner.Should().Be(ContractOwnerAddress);
            contract.Teams.Should().Be("Germany,Brazil,England,Argentina");
            contract.FirstPrize.Should().Be(100);
            contract.SecondPrize.Should().Be(40);
            contract.ThirdPrize.Should().Be(20);
        }

        private Sweepstake SetupValidSweepstake()
        {
            uint entryFee = 5;
            uint firstPrize = 100;
            uint secondPrize = 40;
            uint thirdPrize = 20;

            var contract = new Sweepstake(smartContractState, "Germany,Brazil,England,Argentina", entryFee, firstPrize,
                secondPrize, thirdPrize);
            return contract;
        }

        [Theory]
        [InlineData("S1", "Germany")]
        [InlineData("S2", "Brazil")]
        [InlineData("S3", "England")]
        [InlineData("S4", "Argentina")]
        public void PickTeam(string senderAddress, string expectedTeam)
        {
            var contract = SetupValidSweepstake();

            var assignedTeam = contract.PickTeam(senderAddress);
            assignedTeam.Should().Be(expectedTeam);
        }

        [Fact]
        public void When_team_already_chosen_it_just_gets_next_unchosen_one_wrapping_around_if_needed()
        {
            var contract = SetupValidSweepstake();

            var germany = contract.PickTeam("S1");

            var assignedTeam = contract.PickTeam("S1");
            assignedTeam.Should().Be("Brazil");

            var argentina = contract.PickTeam("S4");
            assignedTeam = contract.PickTeam("S4");
            assignedTeam.Should().Be("England");
        }

        [Fact]
        public void Error_if_entry_fee_too_low()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void Error_when_no_teams_left()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void Only_contract_owner_can_announce_results()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void Prizes_awarded_to_first_second_and_third()
        {
            throw new NotImplementedException();
        }
    }
}