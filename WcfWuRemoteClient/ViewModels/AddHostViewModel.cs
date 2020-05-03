/*
    Windows Update Remote Service
    Copyright(C) 2016-2020  Elia Seikritt

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.If not, see<https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using WcfWuRemoteClient.Models;

namespace WcfWuRemoteClient.ViewModels
{
    internal class AddHostViewModel
    {
        public static string DefaultScheme = Uri.UriSchemeNetTcp;
        public static int DefaultPort = 8523;
        public static string DefaultPath = "WuRemoteService";

        public Exception Exception { get; private set; }
        public string Url { get; private set; }
        public bool Success { get; private set; }

        public IWuEndpoint Endpoint { get; private set; }

        public AddHostViewModel(string url, bool success, Exception error, IWuEndpoint endpoint)
        {
            if (String.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));

            Exception = error;
            Success = success;
            Endpoint = endpoint;
            Url = url;
        }

        async private static Task<AddHostViewModel> ConnectToHost(WuEndpointFactory endpointFactory, string url)
        {
            if (String.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
            if (endpointFactory == null) throw new ArgumentNullException(nameof(endpointFactory));

            Task<AddHostViewModel> addEndpoint = Task.Run(() =>
            {
                Uri uri = null;
                try
                {
                    uri = GetHostUriFromString(url);
                    if (uri.Scheme != Uri.UriSchemeNetTcp) throw new NotSupportedException($"Scheme {uri.Scheme.ToString()} is not supported.");

                    var remoteAddr = new EndpointAddress(uri.ToString());
                    var binding = new NetTcpBinding();
                    if (endpointFactory.TryCreateWuEndpoint(binding, remoteAddr, 
                        out IWuEndpoint endpoint, out Exception exception))
                    {
                        Debug.Assert(exception == null || exception is EndpointNeedsUpgradeException);
                        return new AddHostViewModel(uri.ToString(), true, exception, endpoint);
                    }
                    else
                    {
                        return new AddHostViewModel(uri.ToString(), false, exception, null);
                    }
                }
                catch (Exception ex)
                {
                    return new AddHostViewModel((uri!=null)?uri.ToString():url, false, ex, null);
                }
            });
            return await addEndpoint;
        }

        private static Uri GetHostUriFromString(string s)
        {
            Uri uriResult = null;
            if (!Uri.TryCreate(s, UriKind.Absolute, out uriResult))
            {
                Uri implicitUri;
                if (Uri.TryCreate($"{DefaultScheme}://{s}:{DefaultPort}/{DefaultPath}", UriKind.Absolute, out implicitUri))
                {
                    uriResult = implicitUri;
                }
                else
                {
                    throw new ArgumentException($"{s} is not an valid uri.", nameof(s));
                }
            }
            return uriResult;
        }

        async public static Task<IEnumerable<AddHostViewModel>> ConnectToHosts(
            WuEndpointFactory endpointFactory, WuEndpointCollection endpointCollection, string urls)
        {
            if (endpointFactory == null) throw new ArgumentNullException(nameof(endpointFactory));
            if (endpointCollection == null) throw new ArgumentNullException(nameof(endpointCollection));
            if (String.IsNullOrWhiteSpace(urls)) throw new ArgumentNullException(nameof(urls));

            List<Task<AddHostViewModel>> runningActions = new List<Task<AddHostViewModel>>();
            using (StringReader reader = new StringReader(urls))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    var connectTask = ConnectToHost(endpointFactory, line);
                    runningActions.Add(connectTask);
                }
            }
            await Task.WhenAll(runningActions.ToArray());

            List<AddHostViewModel> resultSet = new List<AddHostViewModel>();

            foreach (var viewModel in runningActions.Select(a => a.Result))
            {
                var endpoint = viewModel.Endpoint;
                if (endpoint != null && (endpoint.ConnectionState == CommunicationState.Created || endpoint.ConnectionState == CommunicationState.Opened)) // Already connected to the same host?
                {
                    if (endpointCollection.Any(e => e.FQDN!=null && e.FQDN.Equals(endpoint.FQDN)) || resultSet.Any(a => a.Endpoint != null && a.Endpoint.FQDN.Equals(endpoint.FQDN)))
                    {
                        endpoint.Disconnect(); // Disconnect and not use this endpoint, dublicate
                        endpoint.Dispose();
                    }
                    else
                    {
                        resultSet.Add(viewModel);
                    }
                }
                else
                {
                    endpoint?.Disconnect();
                    endpoint?.Dispose();
                    resultSet.Add(viewModel);
                }
            }
            if(resultSet.Any(r => r.Success))
            {
                endpointCollection.AddRange(resultSet.Where(r => r.Success).Select(a => a.Endpoint));
            }        
            return resultSet;
        }
    }
}
