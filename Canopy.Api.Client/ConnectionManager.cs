using System;
using System.IO;
using Newtonsoft.Json;

namespace Canopy.Api.Client
{
	public class ConnectionManager
	{
		public const string DefaultApiEndpoint = "https://api.canopysimulations.com";

		public static readonly ConnectionManager Instance = new ConnectionManager();

		private readonly string saveFolder;
		private readonly string saveConnectionFile;

		private ConnectionInformation connection;

		public ConnectionManager()
		{
			this.saveFolder = PlatformUtilities.AppDataFolder();
			this.saveConnectionFile = Path.Combine(this.saveFolder, "connection.json");
		}

		public ConnectionInformation Connection
		{
			get
			{
				if (this.connection == null)
				{
					throw new RecoverableException("Not connected.");
				}

				return this.connection;
			}
		}

		public void SetConnectionInformation(ConnectionInformation connection)
		{
			if (!connection.Endpoint.EndsWith("/", StringComparison.Ordinal))
			{
				connection = new ConnectionInformation(
					connection.Endpoint + "/",
					connection.ClientId,
					connection.ClientSecret);
			}

			this.connection = connection;
			this.SaveConnectionInformation();
		}

        public void ClearConnectionInformation()
        {
            this.connection = null;
            this.DeleteConnectionInformation();
        }

		public bool LoadConnectionInformation()
		{
			if (File.Exists(this.saveConnectionFile))
			{
				var json = File.ReadAllText(this.saveConnectionFile);
				this.connection = JsonConvert.DeserializeObject<ConnectionInformation>(json);
				return !string.IsNullOrWhiteSpace(this.connection.Endpoint);
			}

			return false;
		}

		private void SaveConnectionInformation()
		{
			var json = JsonConvert.SerializeObject(this.connection);
			File.WriteAllText(this.saveConnectionFile, json);
		}

		private void DeleteConnectionInformation()
		{
			if (File.Exists(this.saveConnectionFile))
			{
				File.Delete(this.saveConnectionFile);
			}
		}
	}
}
