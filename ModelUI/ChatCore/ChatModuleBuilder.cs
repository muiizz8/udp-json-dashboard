using System;
using ChatCore.Engine;
using ChatCore.Interfaces;

namespace ChatCore;

/// <summary>
/// Fluent builder — the single integration point for embedding ChatCore in any app.
/// </summary>
public sealed class ChatModuleBuilder
{
    private IChatStorage? _storage;
    private IConfigProvider? _configProvider;
    private IMessagingTransport? _transport;

    public ChatModuleBuilder WithStorage(IChatStorage storage)           { _storage = storage; return this; }
    public ChatModuleBuilder WithConfig(IConfigProvider configProvider)  { _configProvider = configProvider; return this; }
    public ChatModuleBuilder WithTransport(IMessagingTransport transport) { _transport = transport; return this; }

    public ChatEngine Build()
    {
        if (_storage == null)        throw new InvalidOperationException("Storage is required. Call WithStorage().");
        if (_configProvider == null) throw new InvalidOperationException("Config provider is required. Call WithConfig().");

        var engine = new ChatEngine(_storage, _configProvider);
        if (_transport != null)
            engine.SetTransport(_transport);
        return engine;
    }
}
