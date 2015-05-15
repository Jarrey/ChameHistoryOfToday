using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace ChameHOT.Resources
{
    public class ResourcesLoader
    {
        private static ResourcesLoader _loader;
        public static ResourcesLoader Loader
        {
            get
            {
                if (_loader == null)
                    _loader = new ResourcesLoader();
                return _loader;
            }
        }

        private ResourcesLoader()
        {
            resourceLoader = new ResourceLoader(@"Resources");
        }

        private ResourceLoader resourceLoader;
        public string this[string name]
        {
            get
            {
                string result = resourceLoader.GetString(name);
                if (!string.IsNullOrEmpty(result))
                    return result;
                else
                    return name;
            }
        }
    }
}
