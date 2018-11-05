namespace SD226856.PowerBIReportsExplorerExtension
{
    public class CustomEntityDefinition
    {
        public string Server;
        public string Vault;
        public EntityDefinition[] EntityDefinitions;
    }

    public class EntityDefinition
    {
        public string dispNameField;
        public string dispNamePluralField;
        public long idField;
        public string nameField;
    }
}