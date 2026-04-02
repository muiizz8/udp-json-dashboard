using System.Collections.Generic;
using ChatCore.Models;

namespace ChatCore.Interfaces;

public interface IConfigProvider
{
    IEnumerable<InstanceConfig> GetInstances();
    void SaveInstances(IEnumerable<InstanceConfig> instances);
}
