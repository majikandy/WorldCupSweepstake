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
        private uint entryFeeStrats = 5;
        private const ulong SatoshiMuliplier = 100000000; //satoshis

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
                GasLimit = (Gas)(ulong)10000,
                Sender = Punter1Address,
                Value = 0
            };

            this.transactionExecutor = Substitute.For<IInternalTransactionExecutor>();

            this.smartContractState = new TestSmartContractState(
                block,
                message,
                new InMemoryState(),
                null,
                transactionExecutor,
                () => (ulong)0,
                new TestInternalHashHelper()
            );
        }

        private Sweepstake SetupValidSweepstake()
        {
            var message = ((TestMessage)smartContractState.Message);
            message.Sender = ContractOwnerAddress;
            message.Value = entryFeeStrats;

            var teams = "Germany,Brazil,England,Argentina";

            var totalPrizeFund = entryFeeStrats * (ulong)teams.Split(",").Length;
            uint firstPrize = (uint)(totalPrizeFund * 0.7);
            uint secondPrize = (uint)(totalPrizeFund * 0.2);
            uint thirdPrize = (uint)(totalPrizeFund * 0.1);

            var contract = new Sweepstake(smartContractState, teams, entryFeeStrats, firstPrize, secondPrize, thirdPrize);

            return contract;
        }

        [Fact]
        public void Create_sweepstake_providing_start_block_list_of_teams_prize_allocation_and_end_date()
        {
            var contract = SetupValidSweepstake();

            contract.Owner.Should().Be(ContractOwnerAddress);
            contract.TeamsCsv.Should().Be("Germany,Brazil,England,Argentina");
            contract.FirstPrizeSatoshis.Should().Be(14ul * SatoshiMuliplier);
            contract.SecondPrizeSatoshis.Should().Be(4ul * SatoshiMuliplier);
            contract.ThirdPrizeSatoshis.Should().Be(2ul * SatoshiMuliplier);
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

        [Fact]
        public void Error_if_entry_fee_too_low()
        {
            var contract = SetupValidSweepstake();
            var message = ((TestMessage)smartContractState.Message);
            message.Value = entryFeeStrats - 1;

            Action joinGame = () => contract.JoinGame();

            joinGame.Should().Throw<Exception>()
                .WithMessage("Condition inside 'Assert' call was false.");
        }

        [Fact]
        public void Error_if_entry_fee_too_high()
        {
            var contract = SetupValidSweepstake();
            var message = ((TestMessage)smartContractState.Message);
            message.Value = entryFeeStrats + 1;

            Action joinGame = () => contract.JoinGame();

            joinGame.Should().Throw<Exception>()
                .WithMessage("Condition inside 'Assert' call was false.");
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
        public void Error_when_prizes_dont_add_up_to_number_of_players()
        {
            var message = ((TestMessage)smartContractState.Message);
            message.Sender = ContractOwnerAddress;
            message.Value = entryFeeStrats;

            uint firstPrize = 100;
            uint secondPrize = 40;
            uint thirdPrize = 20;

            Action construct = () => new Sweepstake(smartContractState, "Germany,Brazil,England,Argentina", entryFeeStrats, firstPrize,
                secondPrize, thirdPrize);

            construct.Should().Throw<Exception>();
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
            contract.Result.Should().Be(
@"Punter2 address : Brazil : 14 SC-TSTRAT
Punter3 address : England : 4 SC-TSTRAT
Punter1 address : Germany : 2 SC-TSTRAT");

            this.transactionExecutor.Received().TransferFunds(smartContractState, Punter2Address, 14ul * SatoshiMuliplier, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, Punter3Address, 4ul * SatoshiMuliplier, null);
            this.transactionExecutor.Received().TransferFunds(smartContractState, Punter1Address, 2ul * SatoshiMuliplier, null);
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