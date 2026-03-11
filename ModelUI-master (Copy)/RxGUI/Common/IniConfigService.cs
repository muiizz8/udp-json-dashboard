using Salaros.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace RxGUI.Common;

/// <summary>
/// Reads and writes application configuration from Config.inf.
/// </summary>
public sealed class IniConfigService //Sealed Classes are those classes Which cannot be inherited
{
    private readonly ConfigParser config;
    public const string ConfigPath = "Config.inf";
    private const string InstancesSection = "Instances";

    public IniConfigService()
    {
        if (!File.Exists(ConfigPath))
            File.AppendAllText(ConfigPath, "[Network]\n[App]\n");

        config = new ConfigParser(ConfigPath);

        EnsureValue("Network", "LocalIp", "127.0.0.1");
        EnsureValue("Network", "LocalPort", "9000");
        EnsureValue("Network", "RemoteIp", "127.0.0.1");
        EnsureValue("Network", "RemotePort", "9001");
        EnsureValue("App", "InstanceId", "InstanceA");
        EnsureValue(InstancesSection, "Names", "InstanceA");
        EnsureInstanceSection("InstanceA");
    }

    private void EnsureValue(string section, string key, string defaultValue)
    {
        var value = config.GetValue(section, key, defaultValue);
        if (string.IsNullOrWhiteSpace(value))
            value = defaultValue;

        config.SetValue(section, key, value);
        config.Save();
    }
    public string LocalIp
    {
        get => config.GetValue("Network", "LocalIp", "127.0.0.1");
        set {config.SetValue("Network", "LocalIp", value);config.Save();}
    }
    public string LocalPort
    {
        get => config.GetValue("Network", "LocalPort", "9000");
        set {config.SetValue("Network", "LocalPort", value);config.Save();}
    }
    public string RemoteIp
    {
        get => config.GetValue("Network", "RemoteIp", "127.0.0.1");
        set {config.SetValue("Network", "RemoteIp", value);config.Save();}
    }
    public string RemotePort
    {
        get => config.GetValue("Network", "RemotePort", "9001");
        set {config.SetValue("Network", "RemotePort", value);config.Save();}
    }
    public string InstanceId
    {
        get => config.GetValue("App", "InstanceId", "InstanceA");
        set {config.SetValue("App", "InstanceId", value);config.Save();}
    
    }

    /// <summary>
    /// Loads all instance configurations from Config.inf.
    /// </summary>
    public List<InstanceConfig> LoadInstances()
    {
        var namesValue = config.GetValue(InstancesSection, "Names", "InstanceA");
        var names = namesValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 0)
            names = new[] { "InstanceA" };

        var list = new List<InstanceConfig>();
        foreach (var name in names)
        {
            EnsureInstanceSection(name);
            list.Add(new InstanceConfig
            {
                Name = name,
                LocalIp = config.GetValue(name, "LocalIp", "127.0.0.1"),
                LocalPort = config.GetValue(name, "LocalPort", "9000"),
                RemoteIp = config.GetValue(name, "RemoteIp", "127.0.0.1"),
                RemotePort = config.GetValue(name, "RemotePort", "9001")
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
            config.SetValue(name, "LocalIp", instance.LocalIp);
            config.SetValue(name, "LocalPort", instance.LocalPort);
            config.SetValue(name, "RemoteIp", instance.RemoteIp);
            config.SetValue(name, "RemotePort", instance.RemotePort);
        }

        config.SetValue(InstancesSection, "Names", string.Join(", ", names));
        config.Save();
    }

    private void EnsureInstanceSection(string name)
    {
        config.SetValue(name, "LocalIp", config.GetValue(name, "LocalIp", "127.0.0.1"));
        config.SetValue(name, "LocalPort", config.GetValue(name, "LocalPort", "9000"));
        config.SetValue(name, "RemoteIp", config.GetValue(name, "RemoteIp", "127.0.0.1"));
        config.SetValue(name, "RemotePort", config.GetValue(name, "RemotePort", "9001"));
        config.Save();
    }
}
