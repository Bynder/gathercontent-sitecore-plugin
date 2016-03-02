using GatherContent.Connector.Managers.Models.TemplateModel;

namespace GatherContent.Connector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITemplatesManager : IManager
    {
	    TemplateMappingModel GetTemplateMappingModel();

	    void PostTemplate(TemplateMappingModel model);
    }
}
