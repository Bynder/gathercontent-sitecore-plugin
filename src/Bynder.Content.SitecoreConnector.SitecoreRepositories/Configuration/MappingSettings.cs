using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Configuration
{
    public class MappingSettings
    {
        public MappingSettings()
        {
            AllowedMappings = new Dictionary<string, string>();
        }

        public Dictionary<string, string> AllowedMappings { get; private set; }

        public void AddAllowedMapping(System.Xml.XmlNode node)
        {
            if (node.Attributes == null)
            {
                return;
            }

            if (
                node.Attributes["from"] != null &&
                node.Attributes["to"] != null &&
                !string.IsNullOrWhiteSpace(node.Attributes["from"].Value) &&
                !AllowedMappings.ContainsKey(node.Attributes["from"].Value))
            {
                AllowedMappings.Add(node.Attributes["from"].Value, node.Attributes["to"].Value);
            }
        }
    }
}
