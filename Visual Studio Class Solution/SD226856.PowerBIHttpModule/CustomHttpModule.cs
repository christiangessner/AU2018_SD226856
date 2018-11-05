using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;

namespace SD226856.PowerBIHttpModule
{
    /*
    To enable the custom HttpModule in IIS, add the <modules> tag to the following file:
    C:\Program Files\Autodesk\ADMS Professional 2019\Server\Web\Services\web.config

      <system.webServer>
        ...
        <modules>
          <add name="PowerBIHttpModule" type="SD226856.PowerBIHttpModule.CustomHttpModule, SD226856.PowerBIHttpModule" />
        </modules>
      </system.webServer>
    */

    public class CustomHttpModule : IHttpModule
    {
        #region Settings
        private const int PowerBiTimeout = 2000;
        private const string PowerBiResourceUri = "https://analysis.windows.net/powerbi/api";
        private const string PowerBiAuthorityUri = "https://login.windows.net/common/oauth2/authorize";
        private const string PowerBiDatasetName = "VaultActivities";
        private const string PowerBiTableName = "ServiceMethodCalls";

        private static string _sqLiteFileLocation;
        private static string _logFileLocation;
        private static string _powerBiClientId;
        private static string _powerBiUser;
        private static string _powerBiPassword;
        #endregion

        #region Constructor
        public CustomHttpModule()
        {
            var dllFullFileName = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;
            var configuration = ConfigurationManager.OpenExeConfiguration(dllFullFileName);
            var httpModuleSection = configuration.GetSection("HttpModule") as AppSettingsSection;
            if (httpModuleSection != null)
            {
                _sqLiteFileLocation = httpModuleSection.Settings["SqLiteFileLocation"].Value;
                _logFileLocation = httpModuleSection.Settings["LogFileLocation"].Value;
            }
            var powerBiSection = configuration.GetSection("PowerBI") as AppSettingsSection;
            if (powerBiSection != null)
            {
                _powerBiClientId = powerBiSection.Settings["ClientId"].Value;
                _powerBiUser = powerBiSection.Settings["User"].Value;
                _powerBiPassword = powerBiSection.Settings["Password"].Value;
            }

            LogDebug("HttpModule started...");
        }
        #endregion

        #region PowerBI
        private static string _token;
        private static string _datasetId;

        public static async System.Threading.Tasks.Task GetTokenAsync()
        {
            try
            {
                var credential = new UserPasswordCredential(_powerBiUser, _powerBiPassword);
                var authenticationContext = new AuthenticationContext(PowerBiAuthorityUri);
                var authenticationResult = await authenticationContext.AcquireTokenAsync(PowerBiResourceUri, _powerBiClientId, credential);

                if (authenticationResult != null)
                    _token = authenticationResult.AccessToken;
            }
            catch (Exception ex)
            {
                LogErrors(ex);
                _token = null;
            }
        }

        public static string GetToken()
        {
            if (_token == null)
                GetTokenAsync().Wait();

            return _token;
        }

        private static string GetDatasetId(bool allowRetry = true)
        {
            try
            {
                var client = new RestClient("https://api.powerbi.com/") { Timeout = PowerBiTimeout };
                var request = new RestRequest("v1.0/myorg/datasets");
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", $"Bearer {GetToken()}");

                var response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && allowRetry)
                {
                    _token = null;
                    return GetDatasetId(false);
                }

                var datasets = JsonConvert.DeserializeObject<Datasets>(response.Content);
                var dataset = datasets.Value.FirstOrDefault(d => d.Name.Equals(PowerBiDatasetName));
                if (dataset != null)
                    return dataset.Id;
            }
            catch (Exception ex)
            {
                LogErrors(ex);
            }

            return null;
        }

        private static void CreateDataset(bool allowRetry = true)
        {
            _datasetId = GetDatasetId();
            if (_datasetId != null)
                return;

            string json = "{" +
            "	\"name\": \"" + PowerBiDatasetName + "\", \"tables\": [" +
            "		{" +
            "			\"name\": \"" + PowerBiTableName + "\", \"columns\": [" +
            "				{ \"name\": \"Ticket\", \"dataType\": \"string\" }," +
            "				{ \"name\": \"User\", \"dataType\": \"string\" }," +
            "				{ \"name\": \"Vault\", \"dataType\": \"string\" }," +
            "				{ \"name\": \"Service\", \"dataType\": \"string\" }," +
            "				{ \"name\": \"Method\", \"dataType\": \"string\" }," +
            "				{ \"name\": \"Time\", \"dataType\": \"DateTime\" }" +
            "			]" +
            "		}" +
            "	]" +
            "}";

            try
            {
                var client = new RestClient("https://api.powerbi.com/") { Timeout = PowerBiTimeout };
                var request = new RestRequest("v1.0/myorg/datasets", Method.POST);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", $"Bearer {GetToken()}");
                request.AddJsonBody(json);

                var response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && allowRetry)
                {
                    _token = null;
                    CreateDataset(false);
                    return;
                }

                var value = JsonConvert.DeserializeObject<Value>(response.Content);
                _datasetId = value.Id;

                Debug.WriteLine(response.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                LogErrors(ex);
            }
        }

        private static void AddRow(string ticket, string user, string vault, string service, string method, bool allowRetry = true)
        {
            if (_token == null)
                GetTokenAsync().Wait();

            var json = "{" +
            "   \"rows\": [" +
            "       {" +
            "           \"Ticket\": \"" + ticket + "\"," +
            "           \"User\": \"" + user + "\"," +
            "           \"Vault\": \"" + vault + "\"," +
            "           \"Service\": \"" + service + "\"," +
            "           \"Method\": \"" + method + "\"," +
            "           \"Time\": \" " + DateTime.Now.ToString("o") + "\"" +
            "		}" +
            "	]" +
            "}";

            try
            {
                var client = new RestClient("https://api.powerbi.com/") { Timeout = PowerBiTimeout };
                var request = new RestRequest($"v1.0/myorg/datasets/{_datasetId}/tables/{PowerBiTableName}/rows", Method.POST);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", $"Bearer {GetToken()}");
                request.AddJsonBody(json);

                var response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && allowRetry)
                {
                    _token = null;

                    System.Threading.Tasks.Task.Run(() => AddRow(ticket, user, vault, service, method, false));
                }
            }
            catch (Exception ex)
            {
                LogErrors(ex);
            }
        }
        #endregion

        #region SQLite
        private static object _sqlLiteLock = new object();

        public static void CreateDatabase()
        {
            lock (_sqlLiteLock)
            {
                SQLiteConnection connection = null;
                try
                {

                    if (!File.Exists(_sqLiteFileLocation))
                        SQLiteConnection.CreateFile(_sqLiteFileLocation);

                    connection = new SQLiteConnection($"Data Source={_sqLiteFileLocation};Version=3;");
                    connection.Open();

                    var exists = 0;
                    using (var selectCommand = new SQLiteCommand(
                        "SELECT COUNT(*) AS Count FROM sqlite_master WHERE type = 'table' and name = 'VaultTickets'", connection))
                    {
                        var reader = selectCommand.ExecuteReader();
                        while (reader.Read())
                        exists = Convert.ToInt32(reader["Count"]);
                    }

                    if (exists <= 0)
                    {
                        using (var createCommand = new SQLiteCommand(
                            "CREATE TABLE VaultTickets (Ticket TEXT, UserId TEXT, User TEXT, Vault TEXT, Server TEXT)", connection))
                        {
                            createCommand.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogErrors(ex);
                }
                finally
                {
                    if (connection != null && connection.State != System.Data.ConnectionState.Closed)
                        connection.Close();

                    connection?.Dispose();
                }
            }
        }

        public static VaultTicket GetTicket(string ticket)
        {
            VaultTicket vaultTicket = null;

            lock (_sqlLiteLock)
            {
                SQLiteConnection connection = null;
                try
                {
                    connection = new SQLiteConnection($"Data Source={_sqLiteFileLocation};Version=3;");
                    connection.Open();

                    using (var selectCommand = new SQLiteCommand(
                        $"SELECT Ticket, UserId, User, Vault, Server FROM VaultTickets WHERE Ticket = '{ticket}'", connection))
                    {
                        using (var reader = selectCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                vaultTicket = new VaultTicket
                                {
                                    Ticket = reader["Ticket"]?.ToString(),
                                    UserId = reader["UserId"]?.ToString(),
                                    User = reader["User"]?.ToString(),
                                    Vault = reader["Vault"]?.ToString(),
                                    Server = reader["Server"]?.ToString()
                                };
                            }
                            reader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogErrors(ex);
                }
                finally
                {
                    if (connection != null && connection.State != System.Data.ConnectionState.Closed)
                        connection.Close();

                    connection?.Dispose();
                }
            }
            return vaultTicket;
        }

        public static void AddTicket(VaultTicket vaultTicket)
        {
            lock (_sqlLiteLock)
            {
                SQLiteConnection connection = null;
                try
                {
                    connection = new SQLiteConnection($"Data Source={_sqLiteFileLocation};Version=3;");
                    connection.Open();

                    using (var insertCommand = new SQLiteCommand(
                        "INSERT INTO VaultTickets (Ticket, UserId, User, Vault, Server) VALUES (@ticket, @userId, @user, @vault, @server)", connection))
                    {
                        insertCommand.Parameters.AddWithValue("@ticket", vaultTicket.Ticket);
                        insertCommand.Parameters.AddWithValue("@userId", vaultTicket.UserId);
                        insertCommand.Parameters.AddWithValue("@user", vaultTicket.User);
                        insertCommand.Parameters.AddWithValue("@vault", vaultTicket.Vault);
                        insertCommand.Parameters.AddWithValue("@server", vaultTicket.Server);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    LogErrors(ex);
                }
                finally
                {
                    if (connection != null && connection.State != System.Data.ConnectionState.Closed)
                        connection.Close();

                    connection?.Dispose();
                }
            }
        }

        public static void DeleteTicket(string ticket)
        {
            lock (_sqlLiteLock)
            {
                SQLiteConnection connection = null;
                try
                {
                    connection = new SQLiteConnection($"Data Source={_sqLiteFileLocation};Version=3;");
                    connection.Open();

                    using (var deleteCommand = new SQLiteCommand("DELETE FROM VaultTickets WHERE Ticket = @ticket", connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@ticket", ticket);
                        deleteCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    LogErrors(ex);
                }
                finally
                {
                    if (connection != null && connection.State != System.Data.ConnectionState.Closed)
                        connection.Close();

                    connection?.Dispose();
                }
            }
        }
        #endregion

        #region HttpModule
        private StreamWatcher _responseWatcher;
        private string _requestBody;

        public void Dispose()
        {
            LogDebug("Disposing HttpModule...");
        }

        public void Init(HttpApplication context)
        {
            LogDebug("Initializing HttpModule...");

            CreateDatabase();
            CreateDataset();

            context.BeginRequest += (sender, e) =>
            {
                if (sender != null && sender is HttpApplication)
                {
                    var application = (HttpApplication) sender;
                    var request = application.Request;
                    var response = application.Response;
                    if (request.AppRelativeCurrentExecutionFilePath != null && 
                        request.AppRelativeCurrentExecutionFilePath.Contains("_impl")) return;

                    _responseWatcher = new StreamWatcher(context.Response.Filter);
                    response.Filter = _responseWatcher;

                    try
                    {
                        var bytes = new byte[request.InputStream.Length];
                        request.InputStream.Read(bytes, 0, bytes.Length);
                        request.InputStream.Position = 0;
                        _requestBody = Encoding.UTF8.GetString(bytes);
                    }
                    catch (Exception ex)
                    {
                        LogErrors(ex);
                    }
                }
            };

            context.EndRequest += (sender, e) =>
            {
                if (sender != null && sender is HttpApplication)
                {
                    var application = (HttpApplication) sender;
                    var request = application.Request;
                    if (request.AppRelativeCurrentExecutionFilePath != null && 
                        request.AppRelativeCurrentExecutionFilePath.Contains("_impl")) return;

                    try
                    {
                        var requestEnvelope = GetEnvelope(_requestBody);
                        if (requestEnvelope.Body?.XmlElement != null)
                        {
                            var serviceName = request.FilePath.Substring(request.FilePath.LastIndexOf('/') + 1);

                            if (requestEnvelope.Body.XmlElement.Name == "SignIn")
                            {
                                var nodes = requestEnvelope.Body.XmlElement.ChildNodes;
                                var dataServer = nodes[0].InnerText;
                                var userName = nodes[1].InnerText;
                                var knowledgeVault = nodes[3].InnerText;

                                var responseEnvelope = GetEnvelope(_responseWatcher.ToString());
                                var ticket = responseEnvelope.Header.SecurityHeader.Ticket;
                                var userId = responseEnvelope.Header.SecurityHeader.UserId;

                                var vaultTicket = new VaultTicket
                                {
                                    Ticket = ticket,
                                    UserId = userId,
                                    User = userName,
                                    Vault = knowledgeVault,
                                    Server = dataServer
                                };
                                AddTicket(vaultTicket);

                                System.Threading.Tasks.Task.Run(() => 
                                    AddRow(vaultTicket.Ticket, vaultTicket.User, vaultTicket.Vault, serviceName, "SignIn"));
                            }
                            else if (requestEnvelope.Header?.SecurityHeader != null)
                            {
                                var ticket = requestEnvelope.Header.SecurityHeader.Ticket;

                                if (requestEnvelope.Body.XmlElement.Name == "SignOut")
                                {
                                    var vaultTicket = GetTicket(ticket);
                                    if (vaultTicket != null)
                                    {
                                        System.Threading.Tasks.Task.Run(() => 
                                            AddRow(vaultTicket.Ticket, vaultTicket.User, vaultTicket.Vault, serviceName, "SignOut"));
                                        DeleteTicket(ticket);
                                    }
                                }
                                else
                                {
                                    var vaultTicket = GetTicket(ticket);
                                    if (vaultTicket != null)
                                        System.Threading.Tasks.Task.Run(() => 
                                            AddRow(vaultTicket.Ticket, vaultTicket.User, vaultTicket.Vault, serviceName, requestEnvelope.Body.XmlElement.Name));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogErrors(ex);
                    }
                }
            };
        }

        private static Envelope GetEnvelope(string body)
        {
            var envelopString = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">{0}</s:Envelope>";
            var regex = new Regex(string.Format(envelopString, "(.*)"));

            var match = regex.Match(body);
            var envelopInnerXml = match.Groups[1].ToString();
            var envelopOuterXml = string.Format(envelopString, envelopInnerXml);

            return Deserialize<Envelope>(envelopOuterXml);
        }

        public static T Deserialize<T>(string s)
        {
            try
            {
                using (var sr = new StringReader(s))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    serializer.UnknownElement += SerializerUnknownElement;

                    return (T)serializer.Deserialize(sr);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return default(T);
            }
        }

        public static void SerializerUnknownElement(object sender, XmlElementEventArgs e)
        {
            var body = e.ObjectBeingDeserialized as Body;
            if (body == null)
                return;

            body.XmlElement = e.Element;
        }
        #endregion

        #region Logging
        private static object _fileWriteLock = new object();

        private static void LogErrors(Exception ex)
        {
#if DEBUG
            lock (_fileWriteLock)
            {
                var user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
                var s =
                    $"{DateTime.Now:G} ({user}) ERROR: {ex} {Environment.NewLine}";
                File.AppendAllText(_logFileLocation, s);
            }
#endif
        }

        private static void LogDebug(string message)
        {
#if DEBUG
            lock (_fileWriteLock)
            {
                var user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
                var s =
                    $"{DateTime.Now:G} ({user}) DEBUG: {message} {Environment.NewLine}";
                File.AppendAllText(_logFileLocation, s);
            }
#endif
        }
        #endregion
    }
}