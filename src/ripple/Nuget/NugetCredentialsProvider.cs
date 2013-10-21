using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using NuGet;

namespace ripple.Nuget
{
    public class NugetCredentialsProvider : ICredentialProvider
    {
        private readonly Dictionary<string, ICredentials> _credentials = new Dictionary<string, ICredentials>();
        private static readonly Lazy<NugetCredentialsProvider> lazy = new Lazy<NugetCredentialsProvider>(() => new NugetCredentialsProvider()); 

        private NugetCredentialsProvider() {}

        public static NugetCredentialsProvider Instance { get { return lazy.Value; } }

        public void AddCredentials(string key, ICredentials credentials)
        {
            _credentials.Add(key, credentials);
        }

        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            return _credentials[uri.OriginalString];
        }

        public bool TryGetCredentials(string uri, out ICredentials credentials)
        {
            if (_credentials.ContainsKey(uri))
            {
                credentials = _credentials[uri];
                return true;
            }
            credentials = null;
            return false;
        }
        
        public bool TryGetRootCredentials(string uri, out ICredentials credentials)
        {
            var matchingRoot =
                    _credentials.Keys
                        .SingleOrDefault(credUrl => uri.StartsWith(credUrl, StringComparison.InvariantCultureIgnoreCase));
            if (!String.IsNullOrEmpty(matchingRoot))
            {
                credentials = _credentials[matchingRoot];
                return true;
            }
            credentials = null;
            return false;
        }
    }
}
