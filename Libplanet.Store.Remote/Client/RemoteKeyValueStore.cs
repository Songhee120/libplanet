using Google.Protobuf;
using Grpc.Core;
using Libplanet.Store.Remote.Extensions;
using Libplanet.Store.Remote.Server;
using Libplanet.Store.Trie;

namespace Libplanet.Store.Remote.Client
{
    /// <summary>
    /// The <a href="https://grpc.io/">gRPC</a> client for
    /// <see cref="IKeyValueStore"/>. This class is a thin wrapper around the
    /// <see cref="KeyValueStore.KeyValueStoreClient"/> generated by <c>protoc</c>.
    ///
    /// <para> <see cref="RemoteKeyValueStore"/> needs to be used with the
    /// <see cref="RemoteKeyValueService"/>.
    /// The <see cref="RemoteKeyValueService"/> should be hosted on a remote server or
    /// a local process, and the <see cref="RemoteKeyValueStore"/> should be used on a client.
    /// </para>
    /// </summary>
    public sealed class RemoteKeyValueStore : IKeyValueStore
    {
        private readonly KeyValueStore.KeyValueStoreClient _client;

        /// <summary>
        /// Creates a new instance of <see cref="RemoteKeyValueStore"/>.
        /// </summary>
        /// <param name="client">
        /// The <see cref="KeyValueStore.KeyValueStoreClient"/> to use for communication.
        /// </param>
        public RemoteKeyValueStore(KeyValueStore.KeyValueStoreClient client)
        {
            _client = client;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do nothing.
        }

        /// <inheritdoc/>
        public byte[] Get(in KeyBytes key)
        {
            KeyValueStoreValue value;
            try
            {
                value = _client.GetValue(new GetValueRequest
                {
                    Key = key.ToKeyValueStoreKey(),
                });
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
            {
                throw new KeyNotFoundException();
            }

            return value.Data.ToByteArray();
        }

        /// <inheritdoc/>
        public void Set(in KeyBytes key, byte[] value) =>
            _client.SetValue(new SetValueRequest
            {
                Item = new KeyValueStorePair
                {
                    Key = key.ToKeyValueStoreKey(),
                    Value = ByteString.CopyFrom(value).ToKeyValueStoreValue(),
                },
            });

        /// <inheritdoc/>
        public void Set(IDictionary<KeyBytes, byte[]> values) =>
            _client.SetValues(new SetValuesRequest
            {
                Items =
                {
                    values.Select(kv =>
                        new KeyValueStorePair
                        {
                            Key = kv.Key.ToKeyValueStoreKey(),
                            Value = ByteString.CopyFrom(kv.Value).ToKeyValueStoreValue(),
                        }),
                },
            });

        /// <inheritdoc/>
        public void Delete(in KeyBytes key) =>
            _client.DeleteValue(new DeleteValueRequest
            {
                Key = key.ToKeyValueStoreKey(),
            });

        /// <inheritdoc/>
        public void Delete(IEnumerable<KeyBytes> keys) =>
            _client.DeleteValues(new DeleteValuesRequest
            {
                Keys = { keys.Select(KeyBytesExtensions.ToKeyValueStoreKey) },
            });

        /// <inheritdoc/>
        public bool Exists(in KeyBytes key) =>
            _client.ExistsKey(new ExistsKeyRequest
            {
                Key = key.ToKeyValueStoreKey(),
            }).Exists;

        /// <inheritdoc/>
        public IEnumerable<KeyBytes> ListKeys() =>
            _client
                .ListKeys(new ListKeysRequest())
                .Keys
                .Select(kv => kv.ToKeyBytes());
    }
}
