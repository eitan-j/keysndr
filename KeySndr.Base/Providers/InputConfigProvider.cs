﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeySndr.Common;

namespace KeySndr.Base.Providers
{
    public class InputConfigProvider : IInputConfigProvider
    {
        private readonly List<InputConfiguration> configs;
        public IEnumerable<InputConfiguration> Configs => configs;
        private readonly IStorageProvider storageProvider;

        public InputConfigProvider()
        {
            configs = new List<InputConfiguration>();
            storageProvider = ObjectFactory.GetProvider<IStorageProvider>();
        }

        public InputConfigProvider(IStorageProvider p)
        {
            configs = new List<InputConfiguration>();
            storageProvider = p;
        }

        public void Add(InputConfiguration config)
        {
            lock (configs)
            {
                if (!configs.Contains(config))
                    configs.Add(config);
            }
        }

        public void AddOrUpdate(InputConfiguration config)
        {
            lock (configs)
            {
                var index = configs.IndexOf(config);
                if (index > -1)
                {
                    configs[index] = config;
                    return;
                }
                configs.Add(config);
            }
        }
        public void Remove(InputConfiguration config)
        {
            lock (configs)
            {
                if (configs.Contains(config))
                    configs.Remove(config);
            } 
        }

        public InputConfiguration FindConfigForName(string name)
        {
            lock (configs)
            {
                return configs.FirstOrDefault(c => c.Name.Equals(name));
            }
        }

        public void Clear()
        {
            lock (configs)
            {
                configs.Clear();
            }
        }

        public async Task Prepare()
        {
            Clear();

            await Task.Run(() =>
            {
                foreach (var loadInputConfiguration in storageProvider.LoadInputConfigurations())
                {
                    Add(loadInputConfiguration);
                }
            });
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
