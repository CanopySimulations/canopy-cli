using System;
using System.Collections.Generic;
using System.Text;

namespace Canopy.Api.Client
{
    public class CanopyApiConfiguration
    {
        public CanopyApiConfiguration(string baseUrl)
        {
            this.BaseUrl = baseUrl;
        }

        public string BaseUrl { get; }
    }
}
