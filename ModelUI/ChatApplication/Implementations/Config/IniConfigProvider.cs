using Salaros.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using ChatCore.Interfaces;
using ChatCore.Models;

namespace ChatApplication.Implementations.Config;

/// <summary>
/// Reads and writes application configuration from Config.inf.
/// Implements IConfigProvider for ChatCore integration.
/// </summary>
public sealed class IniConfigProvider : IConfigProvider
{
    private readonly ConfigParser _config;
    public const string ConfigPath = "Config.inf";
    private const string InstancesSection = "Instances";

    public IniConfigProvider()
    {
        if (!File.Exists(ConfigPath))
            File.AppendAllText(ConfigPath, "[Network]\n[App]\n");

        _config = new ConfigParser(ConfigPath);

        EnsureValue("Network", "LocalIp", "127.0.0.1");
        EnsureValue("Network", "LocalPort", "9000");
        EnsureValue("Network", "RemoteIp", "127.0.0.1");
        EnsureValue("Network", "RemotePort", "9001");
        EnsureValue("App", "InstanceId", "InstanceA");
        EnsureValue(InstancesSection, "Names", "InstanceA");
        EnsureInstanceSection("InstanceA");
    }

    // ── IConfigProvider ───────────────────────────────────────────────────

    /// <summary>
    /// Loads all instance configurations from Config.inf.
    /// </summary>
    public IEnumerable<InstanceConfig> GetInstances()
    {
        var namesValue = _config.GetValue(InstancesSection, "Names", "InstanceA");
        var names = namesValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 0)
            names = new[] { "InstanceA" };

        var list = new List<InstanceConfig>();
        foreach (var name in names)
        {
            EnsureInstanceSection(name);
            list.Add(new InstanceConfig
            {
                Name       = name,
                LocalIp    = _config.GetValue(name, "LocalIp",   "127.0.0.1"),
                LocalPort  = _config.GetValue(name, "LocalPort",  "9000"),
                RemoteIp   = _config.GetValue(name, "RemoteIp",  "127.0.0.1"),
                RemotePort = _config.GetValue(name, "RemotePort", "9001")
            });
        }

        return list;
    }

    /// <summary>
    /// Saves the provided instance configurations to Config.inf.
    /// </summary>
    public void SaveInstances(IEnumerable<InstanceConfig> instances)
    {
        var names = new List<string>();
        foreach (var instance in instances)
        {
            var name = string.IsNullOrWhiteSpace(instance.Name) ? "Instance" : instance.Name.Trim();
            names.Add(name);
            EnsureInstanceSection(name);
            _config.SetValue(name, "LocalIp",   instance.LocalIp);
            _config.SetValue(name, "LocalPort",  instance.LocalPort);
            _config.SetValue(name, "RemoteIp",  instance.RemoteIp);
            _config.SetValue(name, "RemotePort", instance.RemotePort);
        }

        _config.SetValue(InstancesSection, "Names", string.Join(", ", names));
        _config.Save();
    }

    // ── Additional non-interface config properties ────────────────────────

    public string LocalIp
    {
        get => _config.GetValue("Network", "LocalIp", "127.0.0.1");
        set { _config.SetValue("Network", "LocalIp", value); _config.Save(); }
    }

    public string LocalPort
    {
        get => _config.GetValue("Network", "LocalPort", "9000");
        set { _config.SetValue("Network", "LocalPort", value); _config.Save(); }
    }

    public string RemoteIp
    {
        get => _config.GetValue("Network", "RemoteIp", "127.0.0.1");
        set { _config.SetValue("Network", "RemoteIp", value); _config.Save(); }
    }

    public string RemotePort
    {
        get => _config.GetValue("Network", "RemotePort", "9001");
        set { _config.SetValue("Network", "RemotePort", value); _config.Save(); }
    }

    public string InstanceId
    {
        get => _config.GetValue("App", "InstanceId", "InstanceA");
        set { _config.SetValue("App", "InstanceId", value); _config.Save(); }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void EnsureValue(string section, string key, string defaultValue)
    {
        var value = _config.GetValue(section, key, defaultValue);
        if (string.IsNullOrWhiteSpace(value))
            value = defaultValue;

        _config.SetValue(section, key, value);
        _config.Save();
    }

    private void EnsureInstanceSection(string name)
    {
        _config.SetValue(name, "LocalIp",   _config.GetValue(name, "LocalIp",   "127.0.0.1"));
        _config.SetValue(name, "LocalPort",  _config.GetValue(name, "LocalPort",  "9000"));
        _config.SetValue(name, "RemoteIp",  _config.GetValue(name, "RemoteIp",  "127.0.0.1"));
        _config.SetValue(name, "RemotePort", _config.GetValue(name, "RemotePort", "9001"));
        _config.Save();
    }
}
