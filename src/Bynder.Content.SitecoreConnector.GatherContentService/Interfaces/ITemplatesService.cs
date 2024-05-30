namespace Bynder.Content.SitecoreConnector.GatherContentService.Interfaces
{
    using Core.Entities;

    /// <summary>
    /// 
    /// </summary>
    public interface ITemplatesService : IService
    {
        TemplatesEntity GetTemplates(string projectId);

        TemplateEntity GetSingleTemplate(string templateId);
    }
}
