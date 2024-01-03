using System;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;

namespace Libplanet.Blockchain
{
    /// <summary>
    /// A default implementation of <see cref="IBlockChainStates" /> interface.
    /// </summary>
    public class BlockChainStates : IBlockChainStates
    {
        private readonly IStore _store;
        private readonly IStateStore _stateStore;

        public BlockChainStates(IStore store, IStateStore stateStore)
        {
            _store = store;
            _stateStore = stateStore;
        }

        /// <inheritdoc cref="IBlockChainStates.GetWorldState(BlockHash?)"/>
        public IWorldState GetWorldState(BlockHash? offset)
            => new WorldBaseState(GetTrie(offset), _stateStore);

        /// <inheritdoc cref="IBlockChainStates.GetWorldState(HashDigest{SHA256}?)"/>
        public IWorldState GetWorldState(HashDigest<SHA256>? stateRootHash)
            => new WorldBaseState(GetTrie(stateRootHash), _stateStore);

        /// <inheritdoc cref="IBlockChainStates.GetAccountState(BlockHash?, Address)"/>
        public IAccountState GetAccountState(BlockHash? offset, Address address)
            => GetWorldState(offset).GetAccount(address);

        /// <inheritdoc cref="IBlockChainStates.GetAccountState(HashDigest{SHA256}?)"/>
        public IAccountState GetAccountState(HashDigest<SHA256>? stateRootHash)
            => new AccountState(GetTrie(stateRootHash));

        /// <inheritdoc cref="IBlockChainStates.GetState(BlockHash?, Address, Address)"/>
        public IValue? GetState(BlockHash? offset, Address accountAddress, Address address)
            => GetAccountState(offset, accountAddress).GetState(address);

        /// <inheritdoc cref="IBlockChainStates.GetState(HashDigest{SHA256}?, Address)"/>
        public IValue? GetState(HashDigest<SHA256>? stateRootHash, Address address)
            => GetAccountState(stateRootHash).GetState(address);

        /// <inheritdoc cref=
        /// "IBlockChainStates.GetBalance(BlockHash?, Address, Address, Currency)"/>
        public FungibleAssetValue GetBalance(
            BlockHash? offset,
            Address accountAddress,
            Address address,
            Currency currency)
            => GetAccountState(offset, accountAddress).GetBalance(address, currency);

        /// <inheritdoc cref=
        /// "IBlockChainStates.GetBalance(HashDigest{SHA256}?, Address, Currency)"/>
        public FungibleAssetValue GetBalance(
            HashDigest<SHA256>? stateRootHash,
            Address address,
            Currency currency)
            => GetAccountState(stateRootHash).GetBalance(address, currency);

        /// <inheritdoc cref="IBlockChainStates.GetTotalSupply(BlockHash?, Address, Currency)"/>
        public FungibleAssetValue GetTotalSupply(
            BlockHash? offset,
            Address accountAddress,
            Currency currency)
            => GetAccountState(offset, accountAddress).GetTotalSupply(currency);

        /// <inheritdoc cref="IBlockChainStates.GetTotalSupply(HashDigest{SHA256}?, Currency)"/>
        public FungibleAssetValue GetTotalSupply(
            HashDigest<SHA256>? stateRootHash,
            Currency currency)
            => GetAccountState(stateRootHash).GetTotalSupply(currency);

        /// <inheritdoc cref="IBlockChainStates.GetValidatorSet(BlockHash?, Address)"/>
        public ValidatorSet GetValidatorSet(BlockHash? offset, Address accountAddress)
            => GetAccountState(offset, accountAddress).GetValidatorSet();

        /// <inheritdoc cref="IBlockChainStates.GetValidatorSet(HashDigest{SHA256}?)"/>
        public ValidatorSet GetValidatorSet(HashDigest<SHA256>? stateRootHash)
            => GetAccountState(stateRootHash).GetValidatorSet();

        /// <summary>
        /// Returns the state root associated with <see cref="BlockHash"/>
        /// <paramref name="offset"/>.
        /// </summary>
        /// <param name="offset">The <see cref="BlockHash"/> to look up in
        /// the internally held <see cref="IStore"/>.</param>
        /// <returns>An <see cref="ITrie"/> representing the state root associated with
        /// <paramref name="offset"/>.</returns>
        /// <exception cref="ArgumentException">Thrown for one of the following reasons.
        /// <list type="bullet">
        ///     <item><description>
        ///         If <paramref name="offset"/> is not <see langword="null"/> and
        ///         <paramref name="offset"/> cannot be found in <see cref="IStore"/>.
        ///     </description></item>
        ///     <item><description>
        ///         If <paramref name="offset"/> is not <see langword="null"/> and
        ///         the state root hash associated with <paramref name="offset"/>
        ///         cannot be found in <see cref="IStateStore"/>.
        ///     </description></item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// An <see cref="ITrie"/> returned by this method is read-only.
        /// </remarks>
        private ITrie GetTrie(BlockHash? offset)
        {
            if (!(offset is { } hash))
            {
                return _stateStore.GetStateRoot(null);
            }
            else if (_store.GetStateRootHash(hash) is { } stateRootHash)
            {
                return GetTrie(stateRootHash);
            }
            else
            {
                throw new ArgumentException(
                    $"Could not find block hash {hash} in {nameof(IStore)}.",
                    nameof(offset));
            }
        }

        private ITrie GetTrie(HashDigest<SHA256>? hash)
        {
            ITrie trie = _stateStore.GetStateRoot(hash);
            return trie.Recorded
                ? trie
                : throw new ArgumentException(
                    $"Could not find state root {hash} in {nameof(IStateStore)}.",
                    nameof(hash));
        }
    }
}
