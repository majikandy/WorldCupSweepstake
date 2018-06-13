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
        private static readonly Address ContractOwnerAddress = new Address("contract_owner_address");
        private Address ContractAddress = new Address("ContractAddress");
        private Address Punter1Address = new Address("Punter1 address");
        private Address Punter2Address = new Address("Punter2 address");
        private Address Punter3Address = new Address("Punter3 address");

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
                Number = 2
            };

            var message = new TestMessage
            {
                ContractAddress = ContractAddress,
                GasLimit = (Gas) GasLimit,
                Sender = Punter1Address,
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
            contract.TeamsCsv.Should().Be("Germany,Brazil,England,Argentina");
            contract.FirstPrize.Should().Be(100);
            contract.SecondPrize.Should().Be(40);
            contract.ThirdPrize.Should().Be(20);
        }

        private Sweepstake SetupValidSweepstake()
        {
            var message = ((TestMessage)smartContractState.Message);
            message.Sender = ContractOwnerAddress;

            uint entryFee = 5;
            uint firstPrize = 100;
            uint secondPrize = 40;
            uint thirdPrize = 20;

            var contract = new Sweepstake(smartContractState, "Germany,Brazil,England,Argentina", entryFee, firstPrize,
                secondPrize, thirdPrize);
            return contract;
        }

        [Fact]
        public void Assign_teams_psuedo_randomly_after_final_player_joins_the_game()
        {
            var contract = SetupValidSweepstake();

            contract.JoinGame();
            contract.JoinGame();
            contract.JoinGame();
            contract.JoinGame(); 
            contract.AssignedTeams.GetValue(0).Should().Be("England");
            contract.AssignedTeams.GetValue(1).Should().Be("Argentina");
            contract.AssignedTeams.GetValue(2).Should().Be("Germany");
            contract.AssignedTeams.GetValue(3).Should().Be("Brazil");
        }

        //[Fact]
        public void Error_if_entry_fee_too_low()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void Error_when_no_teams_left()
        {
            var contract = SetupValidSweepstake();

            Action joinGame = () => contract.JoinGame();

            joinGame();
            joinGame();
            joinGame();
            joinGame();

            joinGame.Should().Throw<Exception>()
                .WithMessage("Condition inside 'Assert' call was false.");
        }

        [Fact]
        public void Only_contract_owner_can_announce_results()
        {
            var contract = SetupValidSweepstake();

            Action declareResult = () => contract.DeclareResult("Brazil", "England", "Germany");

            var message = ((TestMessage)smartContractState.Message);
            message.Sender = new Address("not_owner");

            declareResult.Should().Throw<Exception>();
        }

        [Fact]
        public void AnnounceResult_and_payout()
        {
            var message = ((TestMessage)smartContractState.Message);

            var contract = SetupValidSweepstake();
            message.Sender = ContractOwnerAddress;
            contract.JoinGame(); 
            message.Sender = Punter1Address;
            contract.JoinGame(); 
            message.Sender = Punter2Address;
            contract.JoinGame();
            message.Sender = Punter3Address;
            contract.JoinGame();

            contract.AssignedTeams.GetValue(0).Should().Be("Argentina");
            contract.AssignedTeams.GetValue(1).Should().Be("Germany");
            contract.AssignedTeams.GetValue(2).Should().Be("Brazil");
            contract.AssignedTeams.GetValue(3).Should().Be("England");

            Action declareResult = () => contract.DeclareResult("Brazil", "England", "Germany");

            message.Sender = ContractOwnerAddress;

            declareResult();
            contract.FirstPlacePlayer.Should().Be("Punter2 address : Brazil : 100");
            contract.SecondPlacePlayer.Should().Be("Punter3 address : England : 40");
            contract.ThirdPlacePlayer.Should().Be("Punter1 address : Germany : 20");

            this.transactionExecutor.Received().TransferFunds(smartContractState, Punter2Address, 100ul, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, Punter3Address, 40ul, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, Punter1Address, 20ul, null);
        }

        public void When_declaring_result_check_all_teams_are_different_and_exist()
        {
            throw new NotImplementedException();
        }

        // [Fact]
        public void Prizes_awarded_to_first_second_and_third()
        {
            
        }
    }
}