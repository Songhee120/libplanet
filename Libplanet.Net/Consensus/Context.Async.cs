using System;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Blocks;
using Libplanet.Net.Messages;

namespace Libplanet.Net.Consensus
{
    public partial class Context<T>
    {
        /// <summary>
        /// Starts the round #0 of consensus for <see cref="Height"/>.
        /// </summary>
        /// <param name="lastCommit">A <see cref="Block{T}.LastCommit"/> from previous block.
        /// </param>
        public void StartAsync(BlockCommit? lastCommit = null)
        {
            _lastCommit = lastCommit;
            StartRound(0);

            _ = MessageConsumerTask(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Consumes the every <see cref="ConsensusMessage"/> in the message queue.
        /// </summary>
        /// <param name="ctx">A cancellation token for reading message from message queue.</param>
        private async Task MessageConsumerTask(CancellationToken ctx)
        {
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET
            await foreach (ConsensusMessage message in _messageRequests.Reader.ReadAllAsync(ctx))
            {
#else
            while (!ctx.IsCancellationRequested)
            {
                ConsensusMessage message = await _messageRequests.Reader.ReadAsync(ctx);
#endif
                try
                {
                    HandleMessage(message);
                }
                catch (Exception e)
                {
                    _logger.Error(
                        e,
                        "Unexpected exception occurred during {FName}. {E}",
                        nameof(HandleMessage),
                        e);
                }
            }
        }

        /// <summary>
        /// A timeout task for a round if no <see cref="ConsensusPropose"/> is received in
        /// <see cref="TimeoutPropose"/> and <see cref="Libplanet.Net.Consensus.Step.Propose"/>
        /// step.
        /// </summary>
        /// <param name="height">A height that the timeout task is scheduled for.</param>
        /// <param name="round">A round that the timeout task is scheduled for.</param>
        private async Task OnTimeoutPropose(long height, int round)
        {
            TimeSpan timeout = TimeoutPropose(round);
            await Task.Delay(timeout, _cancellationTokenSource.Token);
            _logger.Debug(
                "TimeoutPropose has occurred in {Timeout}. {Info}",
                timeout,
                ToString());
            TimeoutOccurred?.Invoke(this, (Step.Propose, TimeoutPropose(round)));
            ProcessTimeoutPropose(height, round);
        }

        /// <summary>
        /// A timeout task for a round if <see cref="ConsensusVote"/> is received +2/3 any but has
        /// no majority neither Block nor NIL in
        /// <see cref="TimeoutPreVote"/> and <see cref="Libplanet.Net.Consensus.Step.PreVote"/>
        /// step.
        /// </summary>
        /// <param name="height">A height that the timeout task is scheduled for.</param>
        /// <param name="round">A round that the timeout task is scheduled for.</param>
        private async Task OnTimeoutPreVote(long height, int round)
        {
            TimeSpan timeout = TimeoutPreVote(round);
            await Task.Delay(timeout, _cancellationTokenSource.Token);
            _logger.Debug(
                "TimeoutPreVote has occurred in {Timeout}. {Info}",
                timeout,
                ToString());
            TimeoutOccurred?.Invoke(this, (Step.PreVote, TimeoutPreVote(round)));
            ProcessTimeoutPreVote(height, round);
        }

        /// <summary>
        /// A timeout task for a round if <see cref="ConsensusCommit"/> is received +2/3 any but has
        /// no majority neither Block or NIL in
        /// <see cref="TimeoutPreCommit"/> and <see cref="Libplanet.Net.Consensus.Step.PreCommit"/>
        /// step.
        /// </summary>
        /// <param name="height">A height that the timeout task is scheduled for.</param>
        /// <param name="round">A round that the timeout task is scheduled for.</param>
        private async Task OnTimeoutPreCommit(long height, int round)
        {
            TimeSpan timeout = TimeoutPreCommit(round);
            await Task.Delay(timeout, _cancellationTokenSource.Token);
            _logger.Debug(
                "TimeoutPreCommit has occurred in {Timeout}. {Info}",
                timeout,
                ToString());
            TimeoutOccurred?.Invoke(this, (Step.PreCommit, TimeoutPreCommit(round)));
            ProcessTimeoutPreCommit(height, round);
        }
    }
}
