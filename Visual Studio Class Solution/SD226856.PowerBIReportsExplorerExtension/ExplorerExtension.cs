using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.Connectivity.Explorer.Extensibility;
using Newtonsoft.Json;

[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("3b6470dd-40e3-41e5-b2d1-e98171930491")]
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("12.0")]

namespace SD226856.PowerBIReportsExplorerExtension
{
    public class ExplorerExtension : IExplorerExtension
    {
        private readonly string _customObjectName;
        private readonly string _udpName;
        private readonly string _tabCaption;

        public ExplorerExtension()
        {
            var configFullName = Assembly.GetExecutingAssembly().Location + ".config";
            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = configFullName };
            var configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            var section = configuration.GetSection("ExtensionSettings") as AppSettingsSection;
            if (section != null)
            {
                _customObjectName = section.Settings["CustomObjectName"].Value;
                _udpName = section.Settings["UdpName"].Value;
                _tabCaption = section.Settings["TabCaption"].Value;
            }

            if (_customObjectName == null || _udpName == null || _tabCaption == null)
                throw new ConfigurationErrorsException();
        }

        #region IExplorerExtension Members
        public IEnumerable<CommandSite> CommandSites()
        {
            return null;
        }

        public IEnumerable<DetailPaneTab> DetailTabs()
        {
            var extensionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (extensionPath == null)
                return null;

            var jsonFile = Path.Combine(extensionPath, "CustomEntityDefinitions.json");
            if (File.Exists(jsonFile))
            {
                var text = File.ReadAllText(jsonFile);
                var definitions = JsonConvert.DeserializeObject<CustomEntityDefinition[]>(text);
                foreach (var definition in definitions)
                {
                    var entityDefinition = definition.EntityDefinitions.FirstOrDefault(
                        e => e.dispNameField == _customObjectName);
                    if (entityDefinition != null)
                    {
                        var selectionTypeId = new SelectionTypeId(entityDefinition.nameField);
                        var detailPaneTab = new DetailPaneTab(
                            "SD226856.PowerBIReports.ReportTab",
                            _tabCaption,
                            selectionTypeId,
                            typeof(WebBrowserUserControl));
                        detailPaneTab.SelectionChanged += SelectionChanged;

                        return new List<DetailPaneTab> { detailPaneTab };
                    }
                }
            }

            return null;
        }

        public IEnumerable<CustomEntityHandler> CustomEntityHandlers()
        {
            return null;
        }

        public IEnumerable<string> HiddenCommands()
        {
            return null;
        }

        public void OnStartup(IApplication application)
        {
        }

        public void OnShutdown(IApplication application)
        {
        }

        public void OnLogOn(IApplication application)
        {
            var extensionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (extensionPath == null)
                return;

            var settingsFile = Path.Combine(extensionPath, "CustomEntityDefinitions.json");

            if (File.Exists(settingsFile))
                return;

            var definitions = new List<EntityDefinition>();
            var wsm = application.Connection.WebServiceManager;
            foreach (var custEntDef in wsm.CustomEntityService.GetAllCustomEntityDefinitions())
            {
                definitions.Add(new EntityDefinition
                {
                    dispNameField = custEntDef.DispName,
                    dispNamePluralField = custEntDef.DispNamePlural,
                    idField = custEntDef.Id,
                    nameField = custEntDef.Name
                });
            }

            var definition = new CustomEntityDefinition
            {
                EntityDefinitions = definitions.ToArray(),
                Server = application.Connection.Server,
                Vault = application.Connection.Vault
            };

            var json = JsonConvert.SerializeObject(new[] { definition }, Formatting.None);
            File.WriteAllText(settingsFile, json);

            MessageBox.Show(
                $@"A new tab for custom objects of type '{_customObjectName}' has been detected." +
                    Environment.NewLine +
                    @"Please restart Vault to activate this tab!", 
                @"Restart Vault", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Stop);
        }

        public void OnLogOff(IApplication application)
        {
        }
        #endregion

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Context.SelectedObject == null)
                return;

            var wsm = e.Context.Application.Connection.WebServiceManager;
            var custEnt = wsm.CustomEntityService.GetCustomEntitiesByIds(
                new[] { e.Context.SelectedObject.Id })[0];

            var propDefs = wsm.PropertyService.GetPropertyDefinitionsByEntityClassId("CUSTENT");
            var propDef = propDefs.SingleOrDefault(p => p.DispName == _udpName);
            if (propDef == null)
                throw new ConfigurationErrorsException($"The UDP '{_udpName}' must be configured in Vault!");

            var propInsts = wsm.PropertyService.GetProperties(
                "CUSTENT",
                new[] { custEnt.Id },
                new[] { propDef.Id, propDef.Id });

            var userControl = (WebBrowserUserControl)e.Context.UserControl;
            var url = propInsts[0]?.Val?.ToString();
            if (!string.IsNullOrEmpty(url))
                userControl.Navigate(url);
            else
                userControl.Navigate("about:blank");
        }
    }
}
